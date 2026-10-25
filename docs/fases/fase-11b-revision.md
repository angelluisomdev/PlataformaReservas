# Revisión de la fase 11b

## Revisión 2026-10-12 23:52

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-11b.md (APROBADA)
**Commit base:** f647a1d
**Ficheros revisados:** 39 de código y pruebas, más 4 documentos en `docs/` (están en `.gitignore`, así que no tienen diff)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 327 | 327 | 0 |
| Integración | 231 | 231 | 0 |

`dotnet build`: 0 avisos y 0 errores. Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=0`.

**Radio de cambio:** todos los ficheros están dentro de §4. Cuatro ficheros de pruebas no se nombran expresamente: `PruebasCompletarReserva`, `PruebasConsultasDisponibilidad`, `PruebasRepositorioReservas` y `PruebasMotorDisponibilidad`. En los cuatro, el cambio se limita a ajustar las llamadas a `CancelarPorEmpresa` a la nueva firma con `OrigenCancelacion.Empresa`, y a añadir el `using` necesario. §4 lo autoriza ("las llamadas existentes a `CancelarPorEmpresa`").

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura · alcance (§7) | tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasCrearReserva.cs:270-278 | fase-11b §6 (fila de `CrearReserva`: "…y la reserva queda sin eventos"), §7 criterio 2; AGENTS §11 bis | `Tras_confirmar_publica_un_unico_evento_de_reserva_confirmada` solo comprueba lo publicado. Ninguna prueba de los cinco casos de uso verifica que se llame a `LimpiarEventos()` después de publicar: si se quita `reserva.LimpiarEventos()` de `CrearReserva.cs:97`, la suite sigue en verde. Por eso la parte "y los limpian" del criterio 2 de §7 no tiene evidencia. | En el mismo ámbito, leer la entidad que sigue el `ContextoDatos` (`ChangeTracker.Entries<Reserva>().Single().Entity.Eventos`) y afirmar `BeEmpty()`. Hacer lo mismo en los demás casos de uso, o al menos en `CrearReserva` y `DarDeBajaEmpresa`. |
| 2 | MENOR | arquitectura | tests/PlataformaReservas.PruebasUnitarias/Entidades/PruebasReserva.cs:144-150 | AGENTS §11 bis, catálogo "Método de entidad" (repetir la operación y su evento) | Para `CancelarPorEmpresa` sobre una reserva ya cancelada, no se comprueba que la segunda llamada, la que lanza, no registre otro `ReservaCancelada`. En `CancelarPorCliente` sí se comprueba. | Afirmar que `reserva.Eventos` tiene un solo elemento (`ContainSingle()`) después de la excepción. |
| 3 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Eventos/ProcesadorEventos.cs:33 | sin regla (observación) | `catch (Exception)` también captura la `OperationCanceledException` que salta al detener el host con un manejador en curso, y la registra como `Error`. Es un falso positivo en el registro. | Opcional: `catch (Exception error) when (!ct.IsCancellationRequested)`. |
| 4 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/CasosUso/Empresas/DarDeBajaEmpresa.cs:5; src/PlataformaReservas.Dominio/Entidades/Empresa.cs:3 (y los `using` nuevos en 9 ficheros de pruebas) | CLAUDE.md, "diff limpio" (observación) | Los `using` nuevos van al final del bloque, fuera del orden alfabético del resto. | Moverlos a su sitio alfabético sin tocar los que ya estaban. |

**Lo que el revisor de seguridad no pudo verificar** (sin hallazgos en esta fase):
- `ProcesadorEventos` registra la excepción entera. Cuando existan los manejadores de la 16b, hay que comprobar que sus mensajes no lleven datos personales (RN-172), por ejemplo un error SMTP con la dirección de destino.
- Tampoco pudo comprobar cómo resolverán esos manejadores el contexto de empresa o de usuario en un ámbito sin `ClaimsPrincipal`. Ambas cosas quedan para la revisión de la Fase 16b.

### Criterios de aceptación (§7 de la especificación)

- [x] Las entidades registran los eventos y los casos de uso publican después del commit. Evidencia:
  - `grep PublicarAsync` en `Infraestructura/Persistencia` no da resultados;
  - las pruebas del publicador que falla pasan en `PruebasCrearReserva` y `PruebasDarDeBajaEmpresa`;
  - el sabotaje de RN-140 se ejecutó.
- [ ] Los cinco casos de uso publican sus eventos **y los limpian**, y un fallo no publica nada. La publicación tiene evidencia (pruebas de T5) y el "no publica" también (`Una_validacion_o_un_hueco_ocupado_no_publican_nada` y equivalentes). **La limpieza no tiene evidencia** (hallazgo 1).
- [x] CU-23 paso 7, N + 1 eventos con origen `BajaEmpresa`. Evidencia: `Con_N_reservas_futuras_publica_la_baja_y_N_cancelaciones_por_baja_de_la_empresa` y el sabotaje de RN-150.
- [x] El procesador sobrevive a un manejador que lanza (R-6). Evidencia: `PruebasProcesadorEventos` y el sabotaje 2.
- [x] `ColaEventos` es singleton y el procesador crea un ámbito por evento. Evidencia: `PruebasInyeccionDependencias` y `PruebasProcesadorEventos`.
- [x] Sin paquetes nuevos ni migración. Evidencia: `Directory.Packages.props` sin cambios y `has-pending-model-changes` responde "No changes", según lo anotado en la especificación.
- [x] Los sabotajes de §6 hacen fallar sus pruebas. Evidencia: los tres anotan en §6 la prueba que falló.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta revisión (327 unitarias y 231 de integración).

## Revisión 2026-10-13 00:00

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-11b.md (APROBADA)
**Commit base:** f647a1d
**Ficheros revisados:** 40 de código y pruebas, más 4 documentos en `docs/` (están en `.gitignore`, así que no tienen diff)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 1 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 327 | 327 | 0 |
| Integración | 231 | 231 | 0 |

`dotnet build`: 0 avisos y 0 errores. Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=0`.

**Ronda anterior.** El hallazgo 1 (IMPORTANTE, limpieza de eventos sin prueba) está **resuelto**:
- las cinco pruebas de éxito comprueban, con `EscenarioEmpresa.EntidadSeguida`, que la entidad seguida queda sin eventos;
- hay un sabotaje nuevo en §6: al quitar `LimpiarEventos()` de `CrearReserva`, falla `Tras_confirmar_publica_un_unico_evento_de_reserva_confirmada`.

Los menores 2 a 4 siguen abiertos y aparecen abajo como 3 a 5.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance | tests/PlataformaReservas.PruebasIntegracion/Infraestructura/EscenarioEmpresa.cs:41-44 | fase-11b §4 ("Todo lo que no figure como 'sí' no se toca"); AGENTS §3.2 | La corrección del hallazgo 1 de la ronda anterior añade `EntidadSeguida<T>` a `EscenarioEmpresa.cs`. Ese fichero no figura en §4: en `Infraestructura/` solo se nombran `PublicadorCaptura` y `BaseDatosFixture`. El cambio lo aprobó el desarrollador en el plan, pero §4 no se actualizó, que es lo que se hizo con `PruebasAislamientoEmpresas`. | Añadir `EscenarioEmpresa.cs` (`EntidadSeguida`, ver §11) a la fila de `PruebasIntegracion` en §4. Alternativa: mover el método a `PublicadorCaptura` o a `BaseDatosFixture`, que sí están en el radio. |
| 2 | IMPORTANTE | arquitectura | docs/arquitectura.md:1255; tests/PlataformaReservas.PruebasIntegracion/Eventos/PruebasProcesadorEventos.cs:84 | fase-11b §5 T6 ("ningún documento contradice el código"); duda 9 | §13.9 dice, sin salvedad, que en pruebas "el procesador no se levanta como servicio alojado". Sin embargo, la prueba de humo `Arrancado_procesa_lo_que_se_publica_en_la_cola_y_se_detiene_limpio` llama a `StartAsync`. La duda 9 la autoriza, pero el documento no lo recoge. | Añadir en §13.9 la excepción de la duda 9: una única prueba de humo con `StartAsync` sobre una `ColaEventos` propia; el resto invoca `ProcesarAsync` directamente. |
| 3 | MENOR | arquitectura | tests/PlataformaReservas.PruebasUnitarias/Entidades/PruebasReserva.cs:144-176 | AGENTS §11 bis, catálogo "Método de entidad" | Las pruebas de rechazo de `CancelarPorEmpresa` (ya cancelada, `Kind` no UTC, completada) no comprueban que la reserva quede sin eventos. `CancelarPorCliente` sí lo comprueba. | Comprobar `reserva.Eventos` en esas pruebas, o añadir una de repetición sin duplicado. |
| 4 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Eventos/ProcesadorEventos.cs:33 | sin regla (observación) | `catch (Exception)` también atrapa la `OperationCanceledException` de una parada ordenada y la registra como `Error`. | Opcional: `catch (Exception error) when (!ct.IsCancellationRequested)`. |
| 5 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/CasosUso/Empresas/DarDeBajaEmpresa.cs:5; src/PlataformaReservas.Dominio/Entidades/Empresa.cs:3 (y los `using` nuevos de las pruebas) | CLAUDE.md, "diff limpio" (observación) | Los `using` nuevos van al final del bloque, fuera del orden alfabético. | Moverlos a su sitio alfabético. |

**Revisor de seguridad:** sin hallazgos. Lo que no pudo verificar, sin cambios respecto a la ronda anterior, queda para la Fase 16b:
- que los mensajes de excepción de los manejadores no lleven datos personales (RN-172);
- cómo obtendrán los manejadores el contexto de empresa o de usuario en un ámbito sin `ClaimsPrincipal`.

### Criterios de aceptación (§7 de la especificación)

- [x] Las entidades registran y los casos de uso publican después del commit. Evidencia:
  - `grep PublicarAsync` en `Infraestructura/Persistencia` no da resultados;
  - las pruebas del publicador que falla;
  - el sabotaje de RN-140.
- [x] Los cinco casos de uso publican y limpian sus eventos, y un fallo no publica nada. Evidencia: las pruebas de T5, más `EntidadSeguida(...).Eventos` vacío en las cinco pruebas de éxito y el sabotaje de limpieza.
- [x] CU-23 paso 7, N + 1 eventos con origen `BajaEmpresa`. Evidencia: `Con_N_reservas_futuras_publica_la_baja_y_N_cancelaciones_por_baja_de_la_empresa` y el sabotaje de RN-150.
- [x] El procesador sobrevive a un manejador que lanza (R-6). Evidencia: `PruebasProcesadorEventos` y el sabotaje 2.
- [x] `ColaEventos` es singleton y el procesador crea un ámbito por evento. Evidencia: `PruebasInyeccionDependencias` y `PruebasProcesadorEventos`.
- [x] Sin paquetes nuevos ni migración. Evidencia: `Directory.Packages.props` sin cambios y "No changes", según lo anotado en la especificación.
- [x] Los sabotajes de §6 hacen fallar sus pruebas. Evidencia: los cuatro anotan la prueba que falló.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta revisión (327 unitarias y 231 de integración).

## Revisión 2026-10-13 00:30

**Veredicto:** APTO
**Especificación:** docs/fases/fase-11b.md (APROBADA)
**Commit base:** f647a1d
**Ficheros revisados:** 40 de código y pruebas, más 4 documentos en `docs/` (están en `.gitignore`, así que no tienen diff)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 327 | 327 | 0 |
| Integración | 232 | 232 | 0 |

`dotnet build`: 0 avisos y 0 errores. Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=0`.

**Ronda anterior.** Los cinco hallazgos están **resueltos**:
1. **Radio de cambio:** `EscenarioEmpresa.cs` ya figura en §4 y su motivo está en §11.
2. **§13.9 de `arquitectura.md`:** recoge la excepción de la prueba de humo (duda 9).
3. **`CancelarPorEmpresa`:** sus tres rechazos comprueban que no se registra ningún `ReservaCancelada` nuevo.
4. **Parada del host:** el `catch` de `ProcesadorEventos` deja pasar la cancelación cuando el host se detiene. Lo cubre `La_cancelacion_por_la_parada_del_host_se_propaga_sin_registrar_error`, que falló con el filtro quitado y pasó al restaurarlo. §13.2 recoge el mismo filtro.
5. **`using`:** los nuevos están en su sitio alfabético.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | src/PlataformaReservas.Dominio/Compartido/EntidadBase.cs:11 (consumida en CrearReserva.cs:96-97, CancelarReservaCliente.cs:43-44, CancelarReservaEmpresa.cs:31-32 y RegistrarEmpresa.cs:75-76) | Observación sin regla (AGENTS §8 ter) | `Eventos` devuelve `_eventos.AsReadOnly()`, una vista viva. Los casos de uso pasan esa vista a `PublicarAsync` y después llaman a `LimpiarEventos()`. Hoy funciona porque `ColaEventos` y `PublicadorCaptura` copian de forma síncrona. Un publicador que guardara la referencia perdería los eventos. El patrón coincide con el código de referencia de §13.1. | Que `Eventos` devuelva una copia (`[.. _eventos]`), o dejar documentado este acoplamiento. Lo decide el desarrollador. |
| 2 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Eventos/ProcesadorEventos.cs:28-37 | Observación sin regla (coincide con `arquitectura.md` §13.2) | Los manejadores de un mismo evento comparten un solo `try/catch`: si uno lanza, los siguientes de ese evento no se ejecutan. RN-141 y R-6 se cumplen igualmente. Con un manejador por evento (§13.5) no tiene efecto. | Nada en esta fase. Tenerlo presente en la 16b si un evento llega a tener más de un manejador. |

**Revisor de seguridad:** sin hallazgos. Hay que revisar en la 16b que las excepciones de los manejadores no lleven datos personales (RN-172) y cómo obtendrán el contexto en un ámbito sin `ClaimsPrincipal`.

### Criterios de aceptación (§7 de la especificación)

- [x] **Las entidades registran y los casos de uso publican después del commit.** Evidencia:
  - `grep PublicarAsync` en `Infraestructura/Persistencia` no da resultados;
  - las pruebas del publicador que falla;
  - el sabotaje de RN-140.
- [x] **Los cinco casos de uso publican y limpian sus eventos, y un fallo no publica nada.** Evidencia:
  - las pruebas de T5;
  - `EntidadSeguida(...).Eventos` vacío en las cinco pruebas de éxito;
  - el sabotaje de limpieza.
- [x] **CU-23 paso 7, N + 1 eventos con origen `BajaEmpresa`.** Evidencia:
  - la prueba N + 1 de `PruebasDarDeBajaEmpresa`;
  - el sabotaje de RN-150.
- [x] **El procesador sobrevive a un manejador que lanza (R-6).** Evidencia:
  - `PruebasProcesadorEventos`;
  - el sabotaje 2.
- [x] **`ColaEventos` es singleton y el procesador crea un ámbito por evento.** Evidencia: `PruebasInyeccionDependencias` y `PruebasProcesadorEventos`.
- [x] **Sin paquetes nuevos ni migración.** Evidencia:
  - `Directory.Packages.props` sin cambios;
  - "No changes", según lo anotado en la especificación.
- [x] **Los sabotajes de §6 hacen fallar sus pruebas.** Evidencia: los cuatro anotan la prueba que falló.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión, con 327 unitarias y 232 de integración.
