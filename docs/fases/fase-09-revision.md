# Revisión de la Fase 9

## Revisión 2026-10-10 21:25

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-09.md (APROBADA)
**Commit base:** cdbac62
**Ficheros revisados:** 42 de código y pruebas (16 modificados y 26 nuevos). Además cambian `AGENTS.md`, `docs/modelo-dominio.md`, `docs/arquitectura.md` y `docs/fases/fase-09.md`, que están ignorados por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 3 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 234 | 234 | 0 |
| Integración | 139 | 139 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Web/Components/App/Horarios.razor:222` | AGENTS §9: `DateOnly` es una fecha local, y la conversión entre hora local y UTC solo ocurre en el motor | `Fecha = DateOnly.FromDateTime(DateTime.UtcNow)` usa la fecha UTC como si fuera la fecha local de Madrid, en la capa `Web`. Entre las 00:00 y la 01:00 o las 02:00 de Madrid, el formulario propone por defecto el día anterior. | Dejar `Fecha` sin valor inicial, anulable y obligatoria en el formulario. Otra opción es decidir de dónde sale "hoy local" fuera del motor, algo que ningún documento define. |
| 2 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Persistencia/Migraciones/ContextoDatosModelSnapshot.cs:513, :627` y el `.Designer.cs` de `IdsHijosProfesionalSinGenerar` | fase-09 §4 y §5 T2b; AGENTS §13 (sin ruido ajeno a la tarea) | Además de `ValueGeneratedNever`, la migración renombra en el snapshot `DuracionAplicadaMinutos` → `DuracionAplicada` (`Reserva`) y `DuracionMinutos` → `Duracion` (`Servicio`). Es un desfase heredado del commit `0ec9dd0`, que renombró esas propiedades sin migración. Las columnas no cambian y `Up` y `Down` están vacíos, pero el cambio no está declarado en la fase. Confirmado con `git diff` del snapshot. | Anotar en fase-09 §11 que `IdsHijosProfesionalSinGenerar` absorbe también ese desfase del snapshot. |
| 3 | IMPORTANTE | alcance | `docs/fases/fase-09.md` §7 | Tabla de §3 de `/revisar`: criterio de aceptación sin evidencia | Del criterio "`/app/schedules` funciona en InteractiveServer…" solo hay evidencia del smoke: carga en modo interactivo y redirige a `/login`. **Falta la prueba manual de T7 en el navegador:** añadir, rechazar y eliminar intervalos, y marcar y quitar excepciones. | El desarrollador hace la prueba manual y deja la evidencia en §7. |
| 4 | MENOR | arquitectura | `docs/arquitectura.md:705` | fase-09 §5 T8 | §9.3 recoge `ObtenerConHorarioAsync` pero no `ValueGeneratedNever`, que solo aparece en `modelo-dominio.md`. T8 queda incompleta en este punto. | Añadir una línea en §9.3, o remitir a `modelo-dominio.md` §4.6. |
| 5 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/CasosUso/Horarios/ObtenerHorarioProfesional.cs:19` | fase-09 §5 T6 ("ordenados por día y hora de inicio") | `OrderBy(DiaSemana)` sigue el orden de `DayOfWeek`, que pone el domingo primero. El DTO sale en un orden distinto al de la pantalla, que va de lunes a domingo. La prueba solo usa lunes y martes, así que no lo detecta. | Ordenar con el lunes primero, o dejar escrito en §11 que se sigue el orden de `DayOfWeek`. |
| 6 | MENOR | arquitectura | `src/PlataformaReservas.Web/Components/App/Horarios.razor:135` | Observación, sin regla | `profesionales = resultado.Valor ?? []` ignora un `Resultado` fallido: si `ListarProfesionales` falla, la página muestra "primero crea un profesional" en lugar del error. | Si `!resultado.EsExito`, mostrar `resultado.Mensaje`. |

El revisor de seguridad no encuentra nada. Señala como no verificable qué ve el usuario si dos `MarcarNoDisponible` simultáneos para la misma fecha chocan con el índice único: la excepción de la base de datos no se captura y llegaría a la interfaz de error de Blazor.

**Radio de cambio:** todos los ficheros están dentro del §4 de la fase, salvo `tests/PlataformaReservas.PruebasIntegracion/Multiempresa/PruebasAislamientoEmpresas.cs`, que es transversal. En él solo cambia una comparación, `Motivo == "Vacaciones"` por `new MotivoExcepcion("Vacaciones")`, imprescindible para compilar tras T2.
- El cambio está registrado en fase-09 §11 y se hizo con la instrucción general del desarrollador de aplicar las opciones recomendadas.
- No se cuenta como hallazgo, igual que los cambios autorizados de la Fase 8.
- **Si el desarrollador no lo considera autorizado, este punto sería BLOQUEANTE.**

### Criterios de aceptación (§7 de la especificación)

- [x] Dos intervalos el mismo día sin solaparse. Evidencia: `PruebasProfesional` y `PruebasAgregarIntervalo`.
- [x] Un intervalo solapado se rechaza en el dominio. Evidencia: `PruebasIntervaloHorario`, `PruebasProfesional` y `PruebasAgregarIntervalo`, con sabotaje.
- [x] Un intervalo con inicio posterior o igual al fin se rechaza. Evidencia: `PruebasIntervaloHorario` y `PruebasAgregarIntervalo`.
- [x] Eliminar un horario no cancela reservas y lo borra físicamente. Evidencia: `PruebasEliminarIntervalo`.
- [x] Una excepción por fecha, y crearla no cancela reservas. Evidencia: `PruebasProfesional` y `PruebasMarcarNoDisponible`.
- [x] Ningún método público de `Profesional` recibe `string`. Evidencia: `PruebasArquitectura`.
- [x] La regla de solape existe una vez, en `IntervaloHorario.SeSolapaCon`.
- [x] Un recurso de otra empresa da `NoEncontrado` en los cinco casos de uso. Evidencia: `CasosUso/Horarios/`, confirmado por el revisor de seguridad.
- [x] `/app/schedules` funciona en InteractiveServer y redirige a `/login` sin sesión. Hay evidencia de la carga y de la redirección por smoke, pero **falta la prueba manual en el navegador** (hallazgo 3).
- [x] Solo una migración de snapshot, y `has-pending-model-changes` limpio. Ver el hallazgo 2 sobre lo que además arrastra el snapshot.
- [x] Los hijos se guardan como alta sin `DbUpdateConcurrencyException`. Evidencia: pruebas con sabotaje.
- [x] La lectura va sin seguimiento y filtra por empresa. Evidencia: `PruebasRepositorioProfesionales`.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta ronda, 234 pruebas unitarias y 139 de integración.

## Revisión 2026-10-10 21:34

**Veredicto:** APTO
**Especificación:** docs/fases/fase-09.md (APROBADA)
**Commit base:** cdbac62
**Ficheros revisados:** 42 de código y pruebas. Además cambian `AGENTS.md`, `docs/modelo-dominio.md`, `docs/arquitectura.md` y `docs/fases/fase-09.md`, que están ignorados por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 1 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 234 | 234 | 0 |
| Integración | 139 | 139 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos.

**Estado de los hallazgos de la ronda anterior (21:25):**

| Hallazgo | Estado |
|---|---|
| Importante 1 (fecha UTC en `Horarios.razor`) | Resuelto: la fecha empieza vacía y es obligatoria |
| Importante 2 (el snapshot arrastra renombrados) | Resuelto: documentado en fase-09 §11 |
| Importante 3 (falta la prueba manual de T7) | Resuelto: el desarrollador la hizo el 2026-10-10, con resultado OK |
| Menor 4 (`ValueGeneratedNever` en §9.3) | Resuelto |
| Menor 5 (orden de la semana) | Resuelto: de lunes a domingo, y la prueba incluye el domingo |
| Menor 6 (error de `ListarProfesionales`) | Resuelto |
| `PruebasAislamientoEmpresas.cs`, fuera del radio | Se queda como está por decisión del desarrollador, registrada en fase-09 §11 |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `docs/modelo-dominio.md:875` | fase-09 T8 ("ningún documento contradice el código"); M-05 | La lista de comprobación de cierre aún dice "veintiuno desde la Fase 8". La línea 20, §5.7 y AGENTS §7 ya dicen veintidós. | Añadir "veintidós desde la Fase 9 (duda 2)". |

El revisor de seguridad no encuentra nada. Repite como no verificable qué ve el usuario si una violación del índice único llega al circuito; depende de `DetailedErrors` y del manejador global, que esta fase no toca.

**Radio de cambio:** todos los ficheros están dentro del §4 de la fase o tienen la autorización registrada en §11.

### Criterios de aceptación (§7 de la especificación)

Los 13 tienen evidencia. El de `/app/schedules` queda completado con la prueba manual del desarrollador. Esta ronda vuelve a pasar `dotnet build` sin avisos y `dotnet test` con 234 pruebas unitarias y 139 de integración.

## Seguimiento 2026-10-12

No es una ronda de revisión: no se ha vuelto a compilar ni a ejecutar las pruebas. Solo registra en qué estado están los hallazgos que la última ronda dejó abiertos, comprobado contra el código el 2026-10-12.

| Hallazgo | Estado | Evidencia |
|---|---|---|
| Menor 1 de la ronda 21:34 (`modelo-dominio.md` §12 dice "veintiuno") | **Resuelto** el 2026-10-12 | `modelo-dominio.md` §12 añade "veintidós desde la Fase 9 (duda 2)" |
