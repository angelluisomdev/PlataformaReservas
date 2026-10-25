# Concurrencia en la creación de reservas

**Fase 11.** Por qué es imposible que existan dos reservas solapadas del mismo profesional, aunque dos clientes pulsen "Confirmar" en el mismo milisegundo, y qué prueba lo demuestra contra PostgreSQL real.

Es la regla que justifica técnicamente el proyecto: RN-77 ⚑, "nunca pueden existir dos reservas no canceladas del mismo profesional cuyos intervalos se solapen".

> Documento escrito en la Fase 20 a partir del código y de `docs/fases/fase-11.md`. La especificación de referencia es SPEC §13. Si este documento y el código discrepan, manda el código, y la discrepancia es un error de este documento.

---

## 1. Tres capas, una sola garantía

| Capa | Mecanismo | Dónde | Qué cubre | Qué **no** cubre |
|---|---|---|---|---|
| 1. Interfaz | Botón deshabilitado mientras se confirma (`enviando`) y guarda de reentrada | `FlujoReserva.razor`, `ConfirmarAsync` | El doble clic del mismo usuario, el caso más frecuente | Dos usuarios distintos |
| 2. Aplicación | Recomprobación con el **mismo** motor, dentro de la transacción y justo antes de insertar (RN-76 ⚑) | `CrearReserva.EjecutarAsync` | Un mensaje limpio cuando el hueco se ocupó mientras el cliente decidía | Dos transacciones simultáneas: con `READ COMMITTED` ninguna ve la inserción de la otra y las dos superan la comprobación |
| 3. Base de datos | Restricción `EXCLUDE USING gist` | Migración `20260930185037_Inicial` | **Todo**: es declarativa y no se puede eludir, ni siquiera con SQL manual | — |

Las dos primeras son ergonomía. Solo la tercera es una garantía (SPEC §13).

---

## 2. La restricción

Escrita a mano en `Up()` de la migración inicial, porque EF Core no sabe modelarla (`AGENTS.md` §10):

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE reservas
ADD CONSTRAINT ex_reservas_sin_solape
EXCLUDE USING gist (
    profesional_id WITH =,
    tstzrange(inicio_utc, fin_utc, '[)') WITH &&
)
WHERE (estado <> 1);   -- 1 = Cancelada
```

`Down()` la elimina antes de borrar las tablas: `ALTER TABLE reservas DROP CONSTRAINT ex_reservas_sin_solape;`.

| Parte | Por qué |
|---|---|
| `profesional_id WITH =` | Solo chocan reservas del **mismo** profesional. `btree_gist` hace falta para usar `=` dentro de un índice GiST |
| `tstzrange(…, '[)') WITH &&` | El intervalo semiabierto de RN-50 ⚑. `&&` es exactamente `InicioA < FinB && FinA > InicioB`, la regla de `FranjaHoraria.SeSolapaCon` (RN-51 ⚑): aplicación y base de datos **no pueden discrepar** |
| `WHERE (estado <> 1)` | Una reserva cancelada libera el hueco sola, sin borrar nada |
| Sin índice B-tree adicional | El índice GiST de la restricción también sirve a las consultas por profesional y rango |

La convención `snake_case` (D-13) es lo que permite escribir este SQL sin entrecomillar cada identificador.

### Alternativas descartadas (SPEC §13)

| Alternativa | Por qué no |
|---|---|
| Concurrencia optimista (`RowVersion`) | Son dos **inserciones** de filas distintas: no hay fila en conflicto que comparar |
| `SERIALIZABLE` | Funciona, pero obliga a reintentar en toda la aplicación y penaliza transacciones que nada tienen que ver |
| Bloqueo pesimista (`FOR UPDATE` o bloqueo consultivo) | Serializa todas las reservas de un profesional aunque no se solapen, y depende de que nadie olvide tomar el bloqueo |

---

## 3. Del error de PostgreSQL a "Esa hora ya no está disponible"

```text
PostgreSQL  23P01 exclusion_violation (o 40P01, ver abajo)
   │
Infraestructura  UnidadTrabajo.GuardarCambiosAsync
   │               TraductorErroresPostgres.EsHuecoOcupado(error, insertaReservas)
   │               → desengancha las Reserva rechazadas del rastreador
   │               → registra un aviso sin datos del cliente
   │               → lanza HuecoOcupadoException  (Aplicacion/Compartido)
   │
Aplicacion  CrearReserva  →  Resultado.Fallo(CodigoError.HuecoYaNoDisponible, …)
   │
Web  FlujoReserva  →  recarga los huecos del día y muestra "Esa hora ya no está disponible. Elige otra."
```

- **El detalle de PostgreSQL se queda en `Infraestructura`.** `Aplicacion` solo conoce `HuecoOcupadoException`, un tipo propio, y el caso de uso devuelve un `Resultado`. El hueco ocupado es un desenlace normal, no una catástrofe (`AGENTS.md` §8 bis).
- **El interbloqueo `40P01` (fase 11, desviación 1).** La prueba de concurrencia descubrió que dos inserciones que chocan en la restricción pueden esperarse mutuamente, y PostgreSQL aborta una con `40P01` en lugar de `23P01`. Npgsql lo entrega envuelto en una `InvalidOperationException` de fallo transitorio. `EsHuecoOcupado` busca la `PostgresException` en la cadena interna y trata `40P01` como hueco ocupado **solo si el guardado inserta una `Reserva`**; `UnidadTrabajo` lo mira en el `ChangeTracker` antes de guardar. Sin reintentos (RN-80) y sin bloqueos.
- **Desenganchar lo rechazado.** En `InteractiveServer` el `ContextoDatos` dura lo que el circuito (R-1, `arquitectura.md` §4.3). Si la reserva rechazada se quedara en el rastreador, el siguiente guardado del mismo circuito la reinsertaría. `UnidadTrabajo` la desengancha antes de lanzar (corrección de la revisión de la Fase 11).
- **Sin efectos tras el fallo.** La transacción no se confirma y no se publica ningún evento: no sale ningún correo de una reserva que no existe (`AGENTS.md` §8 ter).

---

## 4. Lo que el caso de uso comprueba antes (SPEC §13, "Validaciones de la creación")

`CrearReserva.EjecutarAsync`, en este orden:

1. **Usuario autenticado** (RN-70). Si no, `NoAutorizado`.
2. **Empresa existente y activa** (RN-13).
3. **Servicio existente, activo y de esa empresa** (RN-71).
4. **El profesional existe, está activo, es de la empresa y presta el servicio** (RN-72, RN-73).
5. **Observaciones válidas** y **teléfono de contacto** en el perfil (RN-81).
6. **Se abre la transacción** y **se recomprueba el hueco con `IMotorDisponibilidad`** (RN-76 ⚑). Eso cubre a la vez la antelación mínima, la ventana máxima y que el hueco esté libre (RN-75).
7. **`FinUtc` se calcula en servidor** a partir de la duración del servicio (RN-74 ⚑). El comando no trae fin.
8. Guardar. Si falla, la cadena del apartado 3.
9. **Confirmar y, después, publicar** `ReservaConfirmada`.

---

## 5. Pruebas

| Prueba | Fichero | Qué demuestra |
|---|---|---|
| `Diez_peticiones_simultaneas_sobre_el_mismo_hueco_crean_una_sola_reserva` | `Reservas/PruebasConcurrenciaReservas.cs` | **SPEC §16.3**: 10 tareas en paralelo, **cada una con su propio ámbito de DI y su propio `ContextoDatos`**, 5 iteraciones. Exactamente un éxito y nueve `HuecoYaNoDisponible` en cada una |
| `Tras_cancelar_diez_peticiones_simultaneas_sobre_el_hueco_liberado_crean_una_sola_reserva` | Ídem | La cláusula `WHERE (estado <> 1)`: el hueco liberado por una cancelación se vuelve a disputar, y otra vez gana uno solo (Fase 17) |
| `La_restriccion_de_no_solapamiento_existe_en_la_base_de_datos` | `Persistencia/PruebasEsquema.cs` | La restricción está en `pg_constraint` (`contype = 'x'`). Es la defensa contra R-3 |
| `Dos_reservas_solapadas_del_mismo_profesional_lanzan_hueco_ocupado` · `Tras_un_hueco_ocupado_el_mismo_ambito_puede_guardar_otra_reserva` · `Un_hueco_ocupado_registra_un_aviso_sin_datos_del_cliente` · `Otro_error_de_la_base_de_datos_sube_sin_traducir` | `Persistencia/PruebasUnidadTrabajo.cs` | La traducción, el desenganche y el registro |
| `Una_violacion_de_exclusion_es_un_hueco_ocupado` · `Un_interbloqueo_al_insertar_reservas_es_un_hueco_ocupado` · `Un_interbloqueo_envuelto_como_fallo_transitorio_al_insertar_reservas_es_un_hueco_ocupado` · `Un_interbloqueo_sin_insertar_reservas_no_se_traduce` | `Errores/PruebasTraductorErroresPostgres.cs` | Qué errores son un hueco ocupado y cuáles no |
| `Un_hueco_ocupado_por_una_reserva_confirmada_da_hueco_ya_no_disponible_y_no_deja_nada` · `Una_reserva_contigua_a_otra_se_acepta` · `Una_validacion_o_un_hueco_ocupado_no_publican_nada` · `Crea_la_reserva_confirmada_con_las_copias_y_el_fin_calculado_en_servidor` | `CasosUso/Reservas/PruebasCrearReserva.cs` | La recomprobación en la transacción, los límites exactos, que no queden efectos y el fin calculado en servidor |

Todas en `tests/PlataformaReservas.PruebasIntegracion`, contra el contenedor de PostgreSQL de `BaseDatosFixture`.

### R-3: el éxito falso

Sin la restricción en la base de datos de pruebas, la prueba de concurrencia **puede pasar sin probar nada**. Si las tareas no llegan a coincidir en el tiempo, la recomprobación de la capa 2 las rechaza una tras otra, y la aserción "un éxito" se cumple sin que la garantía real haya intervenido (R-3, `arquitectura.md` §15).

Por eso:

- **El esquema de pruebas sale de las migraciones**, nunca de `EnsureCreated`, que no ejecuta el SQL manual (`AGENTS.md` §13, `arquitectura.md` §12.2).
- **`PruebasEsquema` comprueba que la restricción existe** antes de dar nada por bueno.
- **La prueba repite cinco iteraciones**, para descartar que pase por azar.
