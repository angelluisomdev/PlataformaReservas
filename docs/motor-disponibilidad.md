# Motor de disponibilidad

**Fase 10.** Cómo se calculan los huecos libres de un servicio en un día: el contrato, el algoritmo tal y como está implementado, la conversión de zona horaria y las pruebas que lo sostienen.

Es el componente técnico central del TFM (SPEC §12). Vive en `Aplicacion/Disponibilidad/` y no depende de la interfaz ni de EF Core.

> Documento escrito en la Fase 20 a partir del código y de `docs/fases/fase-10.md`. La especificación de referencia es SPEC §12. Si este documento y el código discrepan, manda el código, y la discrepancia es un error de este documento.

---

## 1. Piezas

| Pieza | Dónde | Papel |
|---|---|---|
| `IMotorDisponibilidad` | `Aplicacion/Disponibilidad/` | Contrato de SPEC §12.1 |
| `MotorDisponibilidad` | `Aplicacion/Disponibilidad/` | **La única implementación** (RN-60 ⚑) |
| `HuecoDisponible` | `Aplicacion/Disponibilidad/` | Resultado: profesional, nombre, inicio y fin en UTC |
| `IConsultasDisponibilidad` | `Aplicacion/Abstracciones/` | Puerto de lectura del motor (fase 10, duda 1) |
| `ConsultasDisponibilidad` | `Infraestructura/Consultas/` | Implementación con EF Core, sin seguimiento |
| `PoliticasReserva` | `Dominio/ValueObjects/` | Intervalo, antelación mínima y ventana máxima, con sus rangos |
| `FranjaHoraria` | `Dominio/ValueObjects/` | Intervalo UTC semiabierto y la regla de solapamiento |
| `IRelojSistema` | `Aplicacion/Abstracciones/` | El "ahora", inyectado para que las pruebas lo fijen |

### Contrato

```csharp
public interface IMotorDisponibilidad
{
    Task<IReadOnlyList<HuecoDisponible>> ObtenerHuecosAsync(
        Guid empresaId,
        Guid servicioId,
        Guid? profesionalId,      // null = todos los que prestan el servicio
        DateOnly fecha,
        CancellationToken ct);
}

public sealed record HuecoDisponible(Guid ProfesionalId, string NombreProfesional, DateTime InicioUtc, DateTime FinUtc);
```

### De dónde lee los datos

El contrato recibe `empresaId` porque lo usan flujos **sin** claim de empresa: el público y el de cliente. Por eso el motor no lee de los `IRepositorio*`, que filtran por `IContextoEmpresa` (`AGENTS.md` §8). Lee de un puerto propio, `IConsultasDisponibilidad`, que:

- no recibe contexto;
- filtra **por el `empresaId` recibido** en cada consulta;
- es de solo lectura y devuelve DTOs.

*Fase 10, duda 1.*

| Método de `IConsultasDisponibilidad` | Qué devuelve |
|---|---|
| `ObtenerEmpresaAsync(empresaId)` | Si está activa y sus políticas |
| `ObtenerServicioAsync(empresaId, servicioId)` | Duración, si está activo y sus políticas sobrescritas. Un servicio de otra empresa no se encuentra |
| `ObtenerProfesionalesQuePrestanAsync(empresaId, servicioId, profesionalId)` | Profesionales activos, de la empresa y asociados al servicio, con su horario semanal y sus fechas no disponibles |
| `ObtenerReservasActivasAsync(empresaId, profesionales, desdeUtc, hastaUtc)` | Reservas no canceladas de esos profesionales que tocan el rango |

### Un solo motor para todos (RN-60 ⚑)

Lo usan dos casos de uso, y ninguno recalcula nada por su cuenta:

| Consumidor | Para qué |
|---|---|
| `ConsultarDisponibilidad` | Mostrar los huecos: flujo público de reserva (`/book/{slug}`) |
| `CrearReserva` | **Recomprobar** el hueco dentro de la transacción, justo antes de insertar (RN-76 ⚑, `docs/concurrencia.md`) |

RN-60 se verifica con `grep ": IMotorDisponibilidad" src/`, que da un único resultado (*Fase 10, duda 4*).

---

## 2. Algoritmo

Es `MotorDisponibilidad.ObtenerHuecosAsync`. Sigue SPEC §12.2, con dos añadidos decididos en la Fase 10: la empresa inactiva (duda 3) y el cambio de hora (duda 2).

| Paso | Qué hace | Dónde | Regla |
|---|---|---|---|
| 0 | La empresa no existe o no está activa → vacío | `ObtenerEmpresaAsync` y `empresa.Activa` | RN-13 (fase 10, duda 3) |
| 1 | El servicio no existe, no está activo o es de otra empresa → vacío | `ObtenerServicioAsync` y `servicio.Activo` | RN-71 |
| 2 | Resuelve las políticas efectivas: lo sobrescrito en el servicio o, si no, lo de la empresa | `PoliticasReserva.Resolver` | SPEC §4.1 |
| 3 | La fecha es posterior a hoy + ventana → vacío | `SuperaLaVentana` y `FechasReservables` | RN-55 |
| 4 | Profesionales candidatos: activos, de la empresa, asociados al servicio y, si se indicó, solo el elegido | `ObtenerProfesionalesQuePrestanAsync` | RN-56 |
| 5.1 | El profesional tiene una excepción ese día → no aporta huecos | `FechasNoDisponibles.Contains(fecha)` | RN-57 |
| 5.2 | Genera los candidatos de cada intervalo del horario de ese día de la semana: desde la hora de inicio, mientras inicio + duración ≤ fin, avanzando el intervalo | `Candidatos` | RN-52, RN-58 |
| 5.3 | Convierte cada candidato de hora local a UTC y **descarta** el que empieza o acaba en una hora local que no existe | `ALocalUtc` | RN-102, RN-104 |
| 5.4 | Descarta los que empiezan antes de ahora + antelación mínima | `limiteAntelacion` | RN-54 |
| 5.5 | Carga **una vez** las reservas no canceladas de todos los candidatos, acotadas en SQL al rango que ocupan | `ObtenerReservasActivasAsync` | `AGENTS.md` §3.7 (excepción de la fase 10, duda 5) |
| 5.6 | Descarta los que se solapan con una reserva del mismo profesional | `FranjaHoraria.SeSolapaCon` | RN-50 ⚑, RN-51 ⚑, RN-53 ⚑ |
| 6 | Ordena por inicio y, si empatan, por nombre del profesional con la cultura actual (`es-ES`) | `OrderBy` y `ThenBy` | RN-59 |

**Por qué las reservas se cargan después de los candidatos.** El motor necesita las reservas solo para los profesionales y el rango que de verdad tienen candidatos. Si no queda ninguno tras la antelación, el motor termina sin consultar reservas.

**El filtro SQL de rango no decide nada.** `ObtenerReservasActivasAsync` acota en SQL con la condición semiabierta (`Inicio < hasta && Fin > desde`) solo para traer menos filas. La decisión de descartar un hueco la toma siempre `FranjaHoraria.SeSolapaCon` (`AGENTS.md` §3.7).

### Semántica del intervalo (SPEC §12.3, RN-58)

`IntervaloHuecosMinutos` es la distancia entre el **inicio** de un candidato y el del siguiente. No es la duración. Con un horario de 09:00 a 14:00 y un servicio de 45 minutos:

| Intervalo | Candidatos |
|---|---|
| 45 | 09:00, 09:45, 10:30, 11:15, 12:00, 12:45 |
| 15 | 09:00, 09:15, 09:30, 09:45, 10:00, … (huecos solapados entre sí, a propósito) |

### Rangos de las políticas

Son invariantes de `PoliticasReserva`; un valor fuera de rango no se puede construir.

| Política | Rango | Por qué importa |
|---|---|---|
| `IntervaloHuecosMinutos` | 1-480 | **Un intervalo de cero colgaría el motor**: el bucle de `Candidatos` no avanzaría nunca |
| `AntelacionMinimaMinutos` | 0-43200 | Hasta 30 días |
| `AntelacionMaximaDias` | 1-365 | La ventana empieza en "hoy" en hora de Madrid |

---

## 3. Zona horaria: un único punto de conversión (RN-102)

| Dato | Tipo | Zona |
|---|---|---|
| Horario semanal y fechas de excepción | `TimeOnly`, `DateOnly` | Hora local, sin conversión en base de datos (RN-101) |
| Reservas, ahora y huecos devueltos | `DateTime` con `Kind = Utc` | UTC (RN-100) |
| Zona de la empresa | Fija, `Europe/Madrid` | RN-103, D-08 |

Toda conversión pasa por los métodos de `MotorDisponibilidad`:

| Método | Sentido | Uso |
|---|---|---|
| `ALocalUtc(fecha, hora)` (privado) | local → UTC | Inicio y fin de cada candidato. Devuelve `null` si la hora local no existe |
| `ALocal(instanteUtc)` | UTC → local | Mostrar horas en la interfaz y calcular "hoy". **Lanza** si el instante no es UTC |
| `FechasReservables(ahoraUtc, ventana)` | — | Primera y última fecha reservable: de hoy en Madrid a hoy + ventana. La usa el selector de fecha de la reserva |
| `DiaLocalEnUtc(fecha)` | local → UTC | Rango semiabierto `[00:00, 00:00 del día siguiente)` de un día de Madrid, para las consultas por día del área de empresa |

Fuera del motor no hay ningún `TimeZoneInfo`. `AGENTS.md` §13 prohíbe `TimeZoneInfo.Local`, `DateTime.Now` y `DateTime.Today`.

### Cambio de hora estacional (RN-104, fase 10, duda 2)

La regla es la conversión estándar del sistema, sin lógica propia, con una precaución:

- **Marzo:** de 02:00 a 03:00 no existe. `TimeZoneInfo.ConvertTimeToUtc` lanzaría una excepción. El motor **descarta** el candidato cuyo inicio o fin caen en esa hora (`ZonaHoraria.IsInvalidTime`). Una hora que no existe no puede ofrecerse.
- **Octubre:** de 02:00 a 03:00 se repite. Se acepta la conversión estándar, que elige el horario de invierno.

En los dos días, un horario de 09:00 a 14:00 sigue dando huecos a las 09:00 locales, aunque esas 09:00 sean distinta hora UTC que la víspera. Sin este cuidado, una conversión ingenua desplaza la agenda de todo el día (SPEC §16.1, nota del caso 13).

---

## 4. Pruebas

`tests/PlataformaReservas.PruebasUnitarias/Disponibilidad/PruebasMotorDisponibilidad.cs`, con dobles en memoria de `IConsultasDisponibilidad` y del reloj. Ningún caso toca la base de datos.

### Los 13 casos de SPEC §16.1, más el 14

| Caso de SPEC §16.1 | Pruebas | Reglas |
|---|---|---|
| 1. Sin horario ese día | `Caso01_sin_horario_ese_dia_no_hay_huecos` | RN-52 |
| 2. Horario simple, sin reservas | `Caso02_horario_simple_sin_reservas_da_los_huecos_completos_y_correctos` | RN-52 |
| 3. Jornada partida | `Caso03_jornada_partida_da_dos_bloques_sin_huecos_en_el_intermedio` | RN-52 |
| 4. Excepción ese día | `Caso04_una_excepcion_ese_dia_anula_los_huecos` · `Caso04_una_excepcion_en_otra_fecha_no_afecta` | RN-57 |
| 5. Reserva en medio | `Caso05_una_reserva_en_medio_excluye_los_huecos_que_se_solapan` · `Caso05_una_reserva_cancelada_no_bloquea` · `Caso05_la_reserva_de_otro_profesional_no_bloquea` | RN-53 ⚑ |
| 6. Reservas al inicio y al final | `Caso06_reservas_al_inicio_y_al_final_del_horario_excluyen_solo_los_huecos_afectados` | RN-53 ⚑ |
| 7. Hueco insuficiente | `Caso07_un_horario_mas_corto_que_el_servicio_no_emite_huecos` · `Caso07_un_servicio_que_cabe_justo_emite_un_hueco` · `Caso07_el_ultimo_candidato_que_no_cabe_hasta_el_fin_no_se_emite` | RN-52 |
| 8. Límites exactos `[inicio, fin)` | `Caso08_reservas_consecutivas_con_limites_exactos_no_producen_falsos_solapes` | RN-50 ⚑, RN-51 ⚑ |
| 9. Intervalo menor que la duración | `Caso09_un_intervalo_menor_que_la_duracion_genera_huecos_solapados_entre_si` | RN-58 |
| 10. Intervalo heredado frente a sobrescrito | `Caso10_sin_sobrescritura_el_intervalo_se_hereda_de_la_empresa` · `Caso10_un_cambio_de_politicas_de_la_empresa_se_hereda_en_el_servicio` · `Caso10_el_intervalo_sobrescrito_en_el_servicio_prevalece_sobre_el_de_la_empresa` | SPEC §4.1 |
| 11. Antelación y ventana | `Caso11_la_antelacion_minima_heredada_admite_el_hueco_que_empieza_justo_en_el_limite` · `Caso11_la_antelacion_minima_sobrescrita_en_el_servicio_prevalece` · `Caso11_una_fecha_pasada_no_tiene_huecos` · `Caso11_la_ventana_maxima_heredada_admite_hoy_mas_la_ventana_y_ni_un_dia_mas` · `Caso11_la_ventana_maxima_sobrescrita_en_el_servicio_prevalece` · `Caso11_hoy_es_la_fecha_local_de_madrid_y_no_la_fecha_utc` | RN-54, RN-55 |
| 12. Varios profesionales | `Caso12_varios_profesionales_solo_los_activos_y_asociados_agregados_y_ordenados_por_inicio_y_nombre` · `Caso12_en_empate_el_nombre_con_tilde_se_ordena_por_su_letra_y_no_por_su_codigo` · `Caso12_el_filtro_por_profesional_devuelve_solo_sus_huecos` · `Caso12_el_filtro_por_un_profesional_inactivo_no_devuelve_huecos` | RN-56, RN-59 |
| 13. Cambio de hora | `Caso13_en_los_dias_del_cambio_de_hora_los_huecos_siguen_alineados_con_el_horario_local` · `Caso13_en_marzo_se_descartan_los_candidatos_que_empiezan_o_acaban_en_la_hora_que_no_existe` · `Caso13_en_octubre_la_hora_repetida_se_convierte_con_la_conversion_estandar` | RN-102, RN-104 |
| 14. Empresa y servicio no válidos (fase 10, duda 3) | `Caso14_una_empresa_inactiva_no_tiene_huecos` · `Caso14_una_empresa_inexistente_no_tiene_huecos` · `Caso14_un_servicio_inactivo_no_tiene_huecos` · `Caso14_un_servicio_inexistente_no_tiene_huecos` · `Caso14_un_servicio_de_otra_empresa_no_tiene_huecos` | RN-13, RN-71 |

### Métodos auxiliares

| Prueba | Qué fija |
|---|---|
| `FechasReservables_va_de_hoy_en_madrid_a_hoy_mas_la_ventana` | La ventana empieza en el "hoy" de Madrid, no en el de UTC |
| `ALocal_convierte_el_instante_utc_a_la_hora_de_madrid` | La conversión de UTC a local, incluidos los dos horarios |
| `ALocal_rechaza_un_instante_que_no_esta_en_utc` | Un `DateTime` no UTC es un error de programación |
| `DiaLocalEnUtc_da_el_rango_semiabierto_del_dia_en_madrid` | El día local como rango UTC `[desde, hasta)` |

### Lo que se prueba en otro sitio

| Qué | Dónde |
|---|---|
| La regla de solapamiento | `ValueObjects/PruebasFranjaHoraria.cs` (unitarias) |
| Los rangos de las políticas | `ValueObjects/PruebasPoliticasReserva.cs` y los objetos de valor de cada política |
| Que `ConsultasDisponibilidad` filtra por empresa y trae solo lo activo | `Consultas/PruebasConsultasDisponibilidad.cs` (integración) |
| La recomprobación dentro de la transacción y la carrera entre peticiones | `docs/concurrencia.md` |
