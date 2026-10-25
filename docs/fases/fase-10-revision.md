# Revisión de la Fase 10

## Revisión 2026-10-12 17:14

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-10.md (APROBADA)
**Commit base:** 6b1ac13
**Ficheros revisados:** 55 de código y pruebas (29 modificados, 8 DTO movidos a subcarpetas y 18 nuevos). Además cambian `AGENTS.md`, `docs/arquitectura.md` y `docs/fases/fase-10.md`, ignorados por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 3 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 288 | 288 | 0 |
| Integración | 145 | 145 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos (bloqueantes=0, importantes=0).

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Consultas/ConsultasDisponibilidad.cs:46` | AGENTS §3.7, RN-51 ⚑ | `.Where(r => r.Franja.InicioUtc < hastaUtc && r.Franja.FinUtc > desdeUtc)` repite literalmente la condición de `FranjaHoraria.SeSolapaCon`. La única justificación está en fase-10 §11, "Decisiones de implementación", escrita por quien implementó. No es una duda resuelta por el desarrollador, y AGENTS §3.7 no recoge esta excepción. No es BLOQUEANTE porque aquí la condición solo filtra y no decide: el descarte del hueco lo hace `SeSolapaCon` en el motor. | Dos opciones. Que el desarrollador apruebe la excepción por escrito, en una duda de la fase o en AGENTS §3.7 ("el prefiltro SQL de rango no cuenta como reimplementación"). O sustituir el filtro por uno grueso que no sea de solape (por ejemplo, reservas que empiezan antes de `hasta` y después de `desde − duración máxima`, o el día local completo), y dejar la decisión solo a `SeSolapaCon`. |
| 2 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Dominio/ValueObjects/PoliticasReserva.cs:30-31` | AGENTS §7 ("los métodos de dominio reciben objetos de valor, nunca el primitivo que validan"), M-05 | La nueva sobrecarga pública `Resolver(PoliticasReserva, int?, int?, int?)` recibe primitivos y los valida dentro, a través de `IntervaloHuecos`, `AntelacionMinimaReserva` y `AntelacionMaximaReserva`. La prueba de arquitectura no lo detecta porque solo mira entidades. La firma la fijó fase-10 §4, así que la especificación contradice AGENTS §7. | Cambiar la firma a `Resolver(PoliticasReserva, IntervaloHuecos?, AntelacionMinimaReserva?, AntelacionMaximaReserva?)` y que el motor construya los objetos de valor a partir del DTO. O que el desarrollador ratifique la excepción. |
| 3 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasUnitarias/Disponibilidad/PruebasMotorDisponibilidad.cs:105, :364, :411, :539` | §3 de `/revisar`: la prueba verifica la regla que dice verificar; fase-10 §6 casos 12 y 14 | Desde que el puerto devuelve DTOs, el motor ya no filtra por actividad del profesional, asociación al servicio, empresa del servicio ni estado de la reserva. Las partes de los casos 5 (reserva cancelada), 12 (inactivos, no asociados, otra empresa) y 14 (servicio de otra empresa) ejercitan los filtros del doble `ConsultasDisponibilidadEnMemoria` (líneas 59 y 71), no el motor. RN-56 sigue cubierta por `PruebasConsultasDisponibilidad`, por eso no es BLOQUEANTE. Pero la tabla de §6 y la trazabilidad de §10 atribuyen RN-56 al caso 12 del motor. | Corregir §6 y §10 de la fase. RN-56, el estado cancelado y la empresa del servicio se verifican en `PruebasConsultasDisponibilidad`. El caso 12 del motor verifica la agregación y el orden (RN-59). |
| 4 | MENOR | seguridad | `src/PlataformaReservas.Infraestructura/Consultas/ConsultasDisponibilidad.cs:43-48` | D-22, AGENTS §8 quater | `ObtenerReservasActivasAsync` carga entidades `Reserva` completas y después las convierte a `ReservaOcupadaDto`. Al cargarlas, los conversores descifran los datos personales del cliente de cada reserva del rango, aunque el DTO solo usa `ProfesionalId`, `InicioUtc` y `FinUtc`. No salen del método, pero se descifran sin necesidad en una ruta que también usará el flujo público. | Proyectar en la consulta: `.Select(r => new ReservaOcupadaDto(r.ProfesionalId, r.Franja.InicioUtc, r.Franja.FinUtc))`. Si se quiere mantener la convención de `ConvertirADto`, decidir antes cómo encaja una proyección en SQL con ella. |
| 5 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Disponibilidad/MotorDisponibilidad.cs:96` | AGENTS §3.10 | El comentario de `SuperaLaVentana` explica una equivalencia que el nombre del método ya da a entender, y no es un punto crítico. El de `ALocalUtc` (línea 101) sí está justificado. | Quitar el comentario de la línea 96. |
| 6 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Disponibilidad/MotorDisponibilidad.cs:68` | RN-59 | El desempate por nombre usa `StringComparer.Ordinal`, mientras que `ListarProfesionales` ordena con `StringComparer.CurrentCulture`. Con ordinal, "Álvaro" queda detrás de "Zoe". | Usar el mismo comparador que el resto de listados, o dejar escrita la elección en §11. |

**No verificable, según los revisores:**
- **Origen del `empresaId` que recibe el motor.** Ninguna pantalla ni caso de uso usa todavía el motor. Hay que comprobar en las fases 11, 14 y 16 dos cosas. En el área de empresa, el `empresaId` debe salir de `IContextoEmpresa.EmpresaActualId` (A-03). En el flujo público, de una empresa activa y publicable resuelta por slug.
- **Lectura sin seguimiento.** Se cumple por la configuración global (`UseQueryTrackingBehavior(NoTracking)`), no consulta a consulta.
- **SQL del catálogo.** `ConsultasCatalogoPublico` llama a `CategoriaDto.ConvertirADto(c)` en la proyección final. EF la evalúa en cliente con la entidad entera: el SQL que se genera cambia respecto al anterior, aunque no rompe ninguna regla.

**Radio de cambio.** Todos los ficheros están en §4 de la especificación, incluidas las ampliaciones del 2026-10-12 que el desarrollador autorizó al pedir que el puerto devolviera DTOs. Hay una excepción a medias:
- `src/PlataformaReservas.Aplicacion/Abstracciones/IConsultasCatalogoPublico.cs` solo cambia su `using` por el nuevo namespace de `CategoriaDto`. §4 recoge "los `using` de los demás" para los casos de uso, pero no nombra este fichero de `Abstracciones`.
- Es consecuencia obligada de mover los DTOs, y no lo cuento como hallazgo.
- **Si el desarrollador no lo considera autorizado, este punto sería BLOQUEANTE.**

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas del motor se escribieron antes que `MotorDisponibilidad` y el desarrollador las confirmó. Evidencia: 40 pruebas en rojo contra el esqueleto con `NotImplementedException`, confirmadas el 2026-10-12.
- [x] Los 13 casos de SPEC §16.1, más el 14, en verde. Evidencia: las 40 pruebas de `PruebasMotorDisponibilidad`. Ver el hallazgo 3 sobre qué parte de los casos 5, 12 y 14 ejercita el doble.
- [x] Una sola implementación de `IMotorDisponibilidad` (RN-60). Evidencia: `grep ": IMotorDisponibilidad" src/` da un único resultado.
- [x] Conversión local → UTC en un único método (`ALocalUtc`), solo con `"Europe/Madrid"`, sin `TimeZoneInfo.Local` ni `DateTime.Now`. Evidencia: `grep`.
- [x] El solape con reservas usa `FranjaHoraria.SeSolapaCon` en el motor. Ver el hallazgo 1 sobre el prefiltro SQL.
- [x] Las políticas efectivas salen de `PoliticasReserva.Resolver`, sin duplicar la regla `??`. Ver el hallazgo 2 sobre la firma de la sobrecarga.
- [x] `IConsultasDisponibilidad` no recibe `IContextoEmpresa`, filtra por `empresaId` en cada consulta y devuelve DTOs. Evidencia: `PruebasConsultasDisponibilidad`, 5 pruebas, con aislamiento entre empresas.
- [x] Los sabotajes de T7 hacen fallar sus pruebas. Evidencia: fase-10 §11, repetidos tras pasar el puerto a DTOs.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta ronda, con 288 pruebas unitarias y 145 de integración.

**Control 2** (`PROJECT_PLAN.md` §3):
- [x] Las 13 pruebas del motor en verde.
- [x] El motor no depende de la interfaz ni de `Infraestructura`. `Aplicacion` solo referencia `Dominio`, y el motor recibe `IConsultasDisponibilidad` e `IRelojSistema`.
- [x] Las políticas rechazan un intervalo de 0, negativo o mayor de 480. Evidencia: `PruebasIntervaloHuecos`.

## Revisión 2026-10-12 17:30

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-10.md (APROBADA)
**Commit base:** 6b1ac13
**Ficheros revisados:** 55 de código y pruebas (29 modificados, 8 DTO movidos a subcarpetas y 18 nuevos). Además cambian `AGENTS.md`, `docs/arquitectura.md` y `docs/fases/fase-10.md`, ignorados por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 288 | 288 | 0 |
| Integración | 145 | 145 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos (bloqueantes=0, importantes=0).

**Estado de los hallazgos de la ronda anterior (17:14):**

| Hallazgo | Estado |
|---|---|
| Importante 1 (prefiltro SQL de solape) | Resuelto: excepción en AGENTS §3.7 y duda 5 de la fase, resuelta por el desarrollador |
| Importante 2 (`Resolver` con primitivos) | Resuelto: la sobrecarga recibe `IntervaloHuecos?`, `AntelacionMinimaReserva?` y `AntelacionMaximaReserva?` |
| Importante 3 (casos del motor que prueban el doble) | Resuelto: §6 y §10 de la fase atribuyen RN-56 y el filtrado a `PruebasConsultasDisponibilidad` |
| Menor 4 (descifrado innecesario) | Resuelto: la consulta solo lee `profesional_id`, `inicio_utc` y `fin_utc`. El revisor de seguridad lo confirma |
| Menor 5 (comentario de `SuperaLaVentana`) | Resuelto |
| Menor 6 (comparador del desempate) | Resuelto: `CurrentCulture`, con una prueba con tilde |
| Radio de `IConsultasCatalogoPublico.cs` | Resuelto: nombrado en §4 |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasDisponibilidad.cs` | AGENTS §11 bis, catálogo "Repositorio" (lectura sin seguimiento); `docs/arquitectura.md` §4.1; fase-10 T5 ("Todas las consultas van sin seguimiento") | Las pruebas cubren dos casos del catálogo, filtrar por empresa y no encontrar lo ajeno, pero no el tercero: que la lectura va sin seguimiento. §6 no explica la omisión. Si alguien añade un `AsTracking()` o cambia el comportamiento global, ninguna prueba falla. | Añadir una prueba que, tras las consultas, compruebe que `ChangeTracker.Entries()` del `ContextoDatos` del ámbito está vacío. O anotar en §6 el motivo de la omisión. |
| 2 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Disponibilidad/MotorDisponibilidad.cs:29-33` | SPEC §4.1 | El paso de los `int?` del servicio a `IntervaloHuecos?`, `AntelacionMinimaReserva?` y `AntelacionMaximaReserva?` está copiado de `PoliticasReserva.Resolver(Empresa, Servicio)`. La regla `??` sigue en un único sitio, pero la preparación de sus argumentos está duplicada. | Que `ServicioDisponibilidadDto` dé los objetos de valor ya construidos, o aceptar la duplicación. |
| 3 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Disponibilidad/MotorDisponibilidad.cs:96` | AGENTS §3.10 | Queda el comentario `// Unico punto de conversion local -> UTC (RN-102)...`. En la misma fase se quitó el de `SuperaLaVentana` por esta misma regla. Ojo: en la ronda anterior el mismo revisor lo dio por justificado como punto crítico. | Decidirlo: quitarlo o mantenerlo como punto crítico (RN-102 y la duda 2). |
| 4 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Dtos/Reservas/ReservaOcupadaDto.cs:13` | AGENTS §8 bis | La sobrecarga `ConvertirADto(Guid, FranjaHoraria)` no recibe ninguna entidad. La decisión está en fase-10 §11 (D-22), pero AGENTS §8 bis no recoge esa excepción. | Añadir a AGENTS §8 bis que se admite una sobrecarga de proyección para consultas EF, o dejarlo documentado solo en la fase. |

El revisor de seguridad no encuentra nada. Confirma que `ObtenerReservasActivasAsync` ya no carga ni descifra datos personales.

**No verificable, según los revisores:** el `empresaId` que recibe el motor tendrá que salir del claim en el área de empresa (fases 11 y 16) y de una empresa activa y publicable en el flujo público (fase 14). Esas pantallas todavía no existen.

**Radio de cambio:** todos los ficheros están dentro de §4 de la especificación, incluidas las ampliaciones autorizadas el 2026-10-12.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas del motor se escribieron antes que `MotorDisponibilidad` y el desarrollador las confirmó.
- [x] Los 13 casos de SPEC §16.1, más el 14, en verde. Evidencia: 41 pruebas de `PruebasMotorDisponibilidad`, contando la de desempate con tilde.
- [x] Una sola implementación de `IMotorDisponibilidad` (RN-60).
- [x] La conversión local → UTC está en un único método, solo con `"Europe/Madrid"`.
- [x] El solape lo decide `FranjaHoraria.SeSolapaCon`. El prefiltro SQL está cubierto por la duda 5 y AGENTS §3.7.
- [x] Las políticas efectivas salen de `PoliticasReserva.Resolver`, que recibe objetos de valor.
- [x] `IConsultasDisponibilidad` no recibe `IContextoEmpresa`, filtra por `empresaId` y devuelve DTOs, con prueba de aislamiento. Ver el hallazgo 1 sobre la lectura sin seguimiento.
- [x] Los sabotajes de T7 y el de `Resolver` hacen fallar sus pruebas (fase-10 §11).
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 288 pruebas unitarias y 145 de integración.

**Control 2:** sus tres evidencias siguen cumplidas, igual que en la ronda de las 17:14.

## Revisión 2026-10-12 17:39

**Veredicto:** APTO
**Especificación:** docs/fases/fase-10.md (APROBADA)
**Commit base:** 6b1ac13
**Ficheros revisados:** 61 de código y pruebas (43 modificados o renombrados y 18 nuevos). Además cambian `AGENTS.md`, `docs/arquitectura.md` y `docs/fases/fase-10.md`, ignorados por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 297 | 297 | 0 |
| Integración | 146 | 146 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos (bloqueantes=0, importantes=0).

**Estado de los hallazgos de la ronda anterior (17:30):**

| Hallazgo | Estado |
|---|---|
| Importante 1 (no se prueba la lectura sin seguimiento) | Resuelto: `Las_consultas_no_dejan_entidades_en_seguimiento`; un `AsTracking()` temporal la hace fallar |
| Menor 2 (conversión de `int?` a objeto de valor duplicada) | Resuelto: `Desde(int?)` en los tres objetos de valor, con pruebas |
| Menor 3 (comentario de `ALocalUtc`) | Se mantiene como punto crítico por decisión del desarrollador (fase-10 §11) |
| Menor 4 (sobrecarga de `ConvertirADto` sin entidad) | Resuelto: excepción recogida en AGENTS §8 bis |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/Consultas/ConsultasDisponibilidad.cs:35` | Observación, sin regla. Decisión registrada en fase-10 §11 | El filtro de asociación usa `EF.Property<List<ProfesionalServicio>>(p, "_servicios")`, con el nombre del campo privado como cadena. Si se renombra el campo, sigue compilando y solo falla al ejecutarse. Lo cubre `PruebasConsultasDisponibilidad.Solo_devuelve_los_profesionales_activos_...`. | Una constante compartida con `ConfiguracionProfesional`, o dejarlo así porque la prueba de integración ya lo vigila. |
| 2 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/Dtos/Profesionales/HorarioProfesionalDto.cs:10-18` | Observación, sin regla. AGENTS §8 bis solo fija la forma `ConvertirADto(entidad, ...)` | `HorarioProfesionalDto.ConvertirADto` ordena él mismo intervalos y excepciones, mientras que en `ProfesionalDto` y `ServicioDto` el orden lo pone el caso de uso. Ordenar es tarea del DTO en un caso y del caso de uso en los otros. El resultado no cambia respecto al anterior. | Elegir un único sitio para ordenar. |

El revisor de seguridad no encuentra nada. Confirma el aislamiento por `empresaId` en las cuatro consultas y que no se descifran datos personales.

**No verificable, según los revisores:** quién pasa el `empresaId` al motor. En el área de empresa, en las fases 11 y 16, deberá ser `EmpresaActualId`. En el flujo público, en la fase 14, deberá salir de una ficha activa y publicable. Hoy no hay ningún llamador.

**Radio de cambio:** todos los ficheros están dentro de §4 de la especificación, incluidas las ampliaciones autorizadas el 2026-10-12.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas del motor se escribieron antes que `MotorDisponibilidad` y el desarrollador las confirmó.
- [x] Los 13 casos de SPEC §16.1, más el 14, están en verde. Evidencia: `PruebasMotorDisponibilidad`.
- [x] Hay una sola implementación de `IMotorDisponibilidad` (RN-60).
- [x] La conversión de hora local a UTC está en un único método y solo usa `"Europe/Madrid"` (RN-102 y RN-103).
- [x] El solape lo decide `FranjaHoraria.SeSolapaCon`. El prefiltro SQL está cubierto por AGENTS §3.7 (duda 5).
- [x] Las políticas efectivas salen de `PoliticasReserva.Resolver`, que recibe objetos de valor.
- [x] `IConsultasDisponibilidad` no recibe `IContextoEmpresa`, filtra por `empresaId`, devuelve DTOs y lee sin seguimiento. Lo cubren las pruebas de aislamiento y de seguimiento.
- [x] Los sabotajes de T7, el de `Resolver` y el de `AsTracking()` hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 297 unitarias y 146 de integración.

**Control 2** (`PROJECT_PLAN.md` §3): cumplido. Las 13 pruebas del motor están en verde, el motor no depende de la interfaz ni de `Infraestructura`, y las políticas rechazan un intervalo de 0, negativo o mayor de 480.
