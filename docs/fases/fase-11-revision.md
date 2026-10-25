# Revisión de la Fase 11

## Revisión 2026-10-12 18:39

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-11.md (APROBADA)
**Commit base:** 7693bc9
**Ficheros revisados:** 42. Son 14 modificados y 24 nuevos en `src/` y `tests/`, más 4 `.md`; los `.md` están excluidos por `.gitignore`, así que no salen en `git status`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 1 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 314 | 314 | 0 |
| Integración | 210 | 210 | 0 |

- **Comprobaciones mecánicas:** `RESUMEN|bloqueantes=0|importantes=0`, sin hallazgos.
- **Compilación:** `dotnet build` termina con 0 avisos y 0 errores.
- **Alcance:** todos los ficheros están dentro del radio de §4 y corresponden a tareas de §5. `EscenarioReserva.cs` es una utilidad de pruebas en `PruebasIntegracion/Infraestructura/`, que está en el radio, y viene anotada en §11.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Persistencia/UnidadTrabajo.cs:23-26`, `src/PlataformaReservas.Aplicacion/CasosUso/Reservas/CrearReserva.cs:88-91` | RN-80; `arquitectura.md` §4.2 y §4.3 (A-04, el ámbito es el circuito); A-05 | **La reserva rechazada se queda pendiente de guardar.** Cuando el guardado choca con `ex_reservas_sin_solape`, la `Reserva` rechazada se queda en el `ChangeTracker` como `Added`: la transacción se deshace, el rastreador no. En `InteractiveServer` el `ContextoDatos` dura lo que el circuito, y RN-80 prevé que el usuario recargue los huecos y vuelva a intentarlo en ese mismo circuito. Entonces `SaveChangesAsync` reinserta la reserva rechazada junto a la nueva, y el cliente recibe `HuecoYaNoDisponible` sin fin. También fallaría cualquier otro guardado del circuito. Ninguna prueba lo detecta, porque todas crean un ámbito por llamada | En `UnidadTrabajo.GuardarCambiosAsync`, antes de lanzar `HuecoOcupadoException`, desenganchar las entradas `Reserva` en estado `Added` (`EntityState.Detached`). Añadir a `PruebasCrearReserva` una prueba que, en un mismo ámbito, pierda un hueco por la restricción y después reserve otro hueco libre con éxito |
| 2 | IMPORTANTE | arquitectura | `docs/arquitectura.md:312-343` (§4.2) | Fase 11, T9 ("ningún documento contradice el código"); dudas 6 y 7 | **El diagrama de "Crear reserva" contradice el código.** Sigue mostrando `validar (FluentValidation)`, el participante `IRepositorioReservas` y la traducción del 23P01 en el repositorio. En el código no hay `IValidator`, el repositorio es `IRepositorioReservasCliente` y la traducción ocurre en `UnidadTrabajo` + `TraductorErroresPostgres` + `HuecoOcupadoException` | Actualizar el diagrama de §4.2: quitar FluentValidation, cambiar el participante a `IRepositorioReservasCliente` / `IUnidadTrabajo` y que el 23P01 o el 40P01 llegue como `HuecoOcupadoException` desde `GuardarCambiosAsync` |
| 3 | MENOR | arquitectura | `docs/arquitectura.md:773-783` (§10) | `arquitectura.md` §10 | La tabla de ciclos de vida no incluye `IContextoUsuario` ni `IRepositorioReservasCliente`, que se registran como scoped (lo verifica `PruebasInyeccionDependencias`) | Añadir una fila para `IContextoUsuario` (Scoped, "se lee del principal del ámbito") |
| 4 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/Identidad/ContextoUsuario.cs:16-35` | Duplicación leve | `ObtenerId` es una copia literal de `ContextoEmpresa.ObtenerId`, salvo el tipo de claim | Opcional: un ayudante `internal` en `Identidad` que lea un `Guid` de un claim |

El revisor de seguridad no encuentra hallazgos. Comprobó:
- el aislamiento entre empresas en `CancelarReservaEmpresa`, `CompletarReserva` y las consultas de `CrearReserva`;
- el filtro por `UsuarioId` en origen y la defensa en profundidad en `RepositorioReservasCliente`;
- que el usuario sale del claim y no del comando;
- que el contacto se lee con `UserManager`;
- que no hay secretos.

**Lo que los revisores no pudieron verificar:**
- **Seguridad:**
  - `ObtenerContactoAsync` no comprueba que el usuario esté activo. Depende de la revalidación de la sesión (RN-83 y RN-135, Fase 6).
  - Las políticas `EsCliente` y `EsPropietario` en las páginas son de las fases 14 a 16.
- **Arquitectura:** el hallazgo 1 sale del análisis del flujo; no hay prueba que lo reproduzca. Que `HuecoOcupadoException` compile sin CA1032 sí está verificado: `dotnet build` termina sin avisos.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas de T1 y T2 se escribieron antes que la implementación y el desarrollador las confirmó el 2026-10-12, con 75 en rojo contra los esqueletos.
- [x] Criterios de CU-07 de esta fase: `PruebasCrearReserva` (sin usuario, profesional que no presta el servicio, `FinUtc` en servidor, hueco ocupado, precio conservado) y `PruebasConcurrenciaReservas`, con 1 éxito y 9 `HuecoYaNoDisponible` en 5 iteraciones.
- [x] Criterios de CU-10 de esta fase: `PruebasCancelarReservaCliente` (reserva de otro, ya iniciada, cancelar dos veces, el hueco vuelve a ofrecerse).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96: `PruebasCancelarReservaEmpresa` (reserva pasada) y `PruebasCompletarReserva` (`FinUtc >= ahora`).
- [x] `"23P01"` aparece una sola vez en `src/`, en `TraductorErroresPostgres.cs:7`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion`: `grep IContexto` no da resultados, y lo confirma el revisor de seguridad.
- [x] RN-102: no hay `TimeZoneInfo` en `CasosUso/`.
- [x] Los sabotajes de §6 hacen fallar sus pruebas, anotado en §6 de la especificación.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (314 + 210).
- [x] **Control 3:** las pruebas de integración de reservas y la de concurrencia están en verde contra PostgreSQL real, y `ex_reservas_sin_solape` existe en la base de pruebas (`PruebasEsquema`, en verde).

## Revisión 2026-10-12 21:59

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-11.md (APROBADA)
**Commit base:** 7693bc9
**Ficheros revisados:** 44. Son 15 modificados y 25 nuevos en `src/` y `tests/`, más 4 `.md` excluidos por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 314 | 314 | 0 |
| Integración | 211 | 211 | 0 |

- **Comprobaciones mecánicas:** `RESUMEN|bloqueantes=0|importantes=0`.
- **Compilación:** `dotnet build` termina con 0 avisos y 0 errores.
- **Alcance:** todos los ficheros están dentro del radio de §4. El único nuevo respecto a la ronda anterior es `Identidad/ClaimsSesion.cs`, que corrige el menor 4.
- **Revisores:** la primera ejecución de esta ronda se cortó por el límite de uso de la sesión; se relanzaron y estos son sus resultados.

**Estado de los hallazgos de la ronda 18:39:**

| Hallazgo | Estado | Cómo se resolvió |
|---|---|---|
| 1 (bloqueante) | Resuelto | `UnidadTrabajo` desengancha las reservas rechazadas antes de lanzar `HuecoOcupadoException`. La prueba nueva `PruebasUnidadTrabajo.Tras_un_hueco_ocupado_el_mismo_ambito_puede_guardar_otra_reserva` se vio fallar antes de la corrección |
| 2 (importante) | Resuelto | Diagrama de §4.2 reescrito |
| 3 (menor) | Resuelto | Tabla de §10 completada |
| 4 (menor) | Resuelto | La lectura del claim pasa a `ClaimsSesion` |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `docs/fases/fase-11.md:264-273` (§6, tabla de omisiones) | AGENTS §11 bis, catálogo "Caso de uso": validación | **Falta el motivo de una omisión.** `CancelarReservaCliente`, `CancelarReservaEmpresa` y `CompletarReserva` no tienen el caso de validación del catálogo, y la omisión no está anotada con su motivo. El caso no aplica, porque el comando solo lleva `ReservaId`, pero el catálogo exige dejarlo escrito | Añadir a la tabla de omisiones de §6 la fila: «Los tres casos de uso · Validación · El comando solo lleva el identificador; si no existe, da `NoEncontrado`, que sí se prueba» |
| 2 | MENOR | arquitectura | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasCrearReserva.cs` | fase-11 §5 T6, paso 2 | **Falta una prueba.** T6 cita el profesional inexistente entre las comprobaciones previas que dan `Validacion`, pero no tiene prueba. Sí se prueban el profesional inactivo, el de otra empresa y el que no presta el servicio | Añadir `Un_profesional_inexistente_da_validacion` con `ProfesionalId = Guid.CreateVersion7()` |
| 3 | MENOR | arquitectura | `CancelarReservaCliente.cs:29-37`, `CompletarReserva.cs:20-28`, `CancelarReservaEmpresa.cs:19-22` | `arquitectura.md` §7 (A-09) frente a §6 | **Dos apartados de `arquitectura.md` se contradicen.** Los casos de uso repiten antes las condiciones de estado y hora que también comprueba el dominio. El código sigue lo que piden T7 y §6; según §7, en cambio, la invariante debería ir solo en el dominio | Sin cambio de código. Conciliar §6 y §7 de `arquitectura.md`, aceptando la comprobación previa como duplicación consciente |

El revisor de seguridad no encuentra hallazgos. Comprobó:
- **aislamiento:** entre empresas y en el eje cliente;
- **contexto:** que el usuario sale del claim;
- **cifrado:** que el contacto se lee con `UserManager`;
- **inyección y secretos:** ninguno de los dos;
- **concurrencia:** `FinUtc` calculado en servidor, traducción del 23P01 y del 40P01, y desenganche de la reserva rechazada;
- **errores:** se convierten en `Resultado`.

Lo que no pudo verificar: el manejador global de errores de `Web`, que está fuera del radio de esta fase.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas de T1 y T2 se escribieron primero y el desarrollador las confirmó.
- [x] Criterios de CU-07 de esta fase (`PruebasCrearReserva`, `PruebasConcurrenciaReservas`).
- [x] Criterios de CU-10 de esta fase (`PruebasCancelarReservaCliente`).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96 (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`).
- [x] `"23P01"` aparece una sola vez en `src/`, en `TraductorErroresPostgres.cs`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion`.
- [x] RN-102: no hay `TimeZoneInfo` en `CasosUso/`.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (314 + 211).
- [x] **Control 3:** las pruebas de reservas y la de concurrencia están en verde contra PostgreSQL real, y `ex_reservas_sin_solape` existe en la base de pruebas (`PruebasEsquema`).

## Revisión 2026-10-12 22:26

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-11.md (APROBADA)
**Commit base:** 7693bc9
**Ficheros revisados:** 44. Son 15 modificados y 25 nuevos en `src/` y `tests/`, más 4 `.md` excluidos por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 314 | 314 | 0 |
| Integración | 212 | 212 | 0 |

- **Comprobaciones mecánicas:** `RESUMEN|bloqueantes=0|importantes=0`.
- **Compilación:** `dotnet build` termina con 0 avisos y 0 errores.

**Estado de los hallazgos de la ronda 21:59:**

| Hallazgo | Estado | Cómo se resolvió |
|---|---|---|
| 1 (importante) | Resuelto | Fila añadida a la tabla de omisiones de §6 |
| 2 (menor) | Resuelto | Prueba `Un_profesional_inexistente_da_validacion` |
| 3 (menor) | Resuelto | `arquitectura.md` §7 concilia §6 y §7 ("duplicación consciente") |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Identidad/ContextoEmpresa.cs:14`, `src/PlataformaReservas.Infraestructura/Identidad/ClaimsSesion.cs:6`, `docs/arquitectura.md` §4.2, §7 y §10 | AGENTS §3.2; fase-11 §4 y T9 | **Cambios fuera del radio escrito de la fase.** En `Identidad/`, §4 solo autoriza el contexto de usuario y el método de contacto, pero se ha cambiado `ContextoEmpresa` y se ha creado `ClaimsSesion`. T9 solo autoriza §2.3, §5.1 y §9.5 de `arquitectura.md`, y se han tocado también §4.2, §7 y §10. Los cambios vienen de corregir las rondas de revisión anteriores y constan en §11 de la fase, pero §4 y T9 no se actualizaron. *Nota de quien escribe el informe:* el desarrollador pidió estas correcciones en el chat ("vamos a darle solución al hallazgo bloqueante y al importante", "vamos a corregir los hallazgos menores", "haz todas las correcciones necesarias…"). Lo que falta es dejarlo escrito en la especificación | Que el desarrollador lo ratifique y se añadan a §4 y a T9 los ficheros y secciones afectados |
| 2 | MENOR | arquitectura | `docs/modelo-dominio.md:438` | fase-11 T9; duda 3 | **El párrafo se contradice con el siguiente.** Sigue diciendo que `Crear` recibe "el `Servicio` completo" y que es "la única referencia que se pasa como objeto", mientras el párrafo siguiente describe la sobrecarga con `servicioId`, `Duracion` y `Precio` | Reformular la línea 438 para que hable solo de la primera sobrecarga, o fundirla con el párrafo siguiente |
| 3 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/Identidad/ClaimsSesion.cs:6` | AGENTS §11 bis | **La omisión no está donde toca.** `ClaimsSesion` no tiene `PruebasClaimsSesion.cs`; lo cubren de forma indirecta `PruebasContextoEmpresa` y `PruebasContextoUsuario`. El motivo está escrito en §11 de la fase, no en la tabla de omisiones de §6 | Añadir la fila de `ClaimsSesion` a la tabla de omisiones de §6 |

El revisor de seguridad no encuentra hallazgos: revisó las áreas 1, 2, 3, 5, 6, 7, 8, 10 y 11. No pudo verificar dos cosas:
- que las páginas de las fases 14 a 16 apliquen las políticas `EsPropietario` y `EsCliente`;
- si existe una clave foránea compuesta que impida, en la propia base de datos, una reserva con un servicio o un profesional de otra empresa. Hoy lo garantiza `CrearReserva`, que comprueba ambos con `ConsultasDisponibilidad` filtrando por empresa.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas de T1 y T2 se escribieron primero y el desarrollador las confirmó.
- [x] Criterios de CU-07 de esta fase (`PruebasCrearReserva`, `PruebasConcurrenciaReservas`).
- [x] Criterios de CU-10 de esta fase (`PruebasCancelarReservaCliente`).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96 (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`).
- [x] `"23P01"` aparece una sola vez en `src/`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion`.
- [x] RN-102: no hay `TimeZoneInfo` en `CasosUso/`.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (314 + 212).
- [x] **Control 3:** las pruebas de reservas y la de concurrencia están en verde contra PostgreSQL real, y `ex_reservas_sin_solape` existe en la base de pruebas (`PruebasEsquema`).

## Revisión 2026-10-12 23:11

**Veredicto:** APTO
**Especificación:** docs/fases/fase-11.md (APROBADA)
**Commit base:** 7693bc9
**Ficheros revisados:** 44. Son 15 modificados y 25 nuevos en `src/` y `tests/`, más 4 `.md` excluidos por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 314 | 314 | 0 |
| Integración | 212 | 212 | 0 |

- **Comprobaciones mecánicas:** `RESUMEN|bloqueantes=0|importantes=0`.
- **Compilación:** `dotnet build` termina con 0 avisos y 0 errores.
- **Alcance:** todos los ficheros entran en §4. Desde la ronda de las 22:26 la ampliación está ratificada por el desarrollador.

**Estado de los hallazgos de la ronda 22:26:**

| Hallazgo | Estado | Detalle |
|---|---|---|
| 1 (importante) | Resuelto | §4 y T9 recogen `ContextoEmpresa`, `ClaimsSesion` y `arquitectura.md` §4.2, §7 y §10, con la ratificación del desarrollador |
| 2 (menor) | Abierto | Sigue como menor 1 de esta ronda |
| 3 (menor) | Abierto | El motivo de `ClaimsSesion` sigue en §11 y no en §6. Esta ronda no lo vuelve a señalar, pero no se ha corregido |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `docs/modelo-dominio.md:438` | fase-11 T9; duda 3 | **El párrafo se contradice con el siguiente.** Sigue diciendo que `Crear` recibe el `Servicio` completo y que es "la única referencia que se pasa como objeto", mientras el párrafo siguiente describe la sobrecarga con `servicioId`, `Duracion` y `Precio` | Reformular la línea 438 para que hable solo de `Crear(Servicio)`, o fundirla con la 440 |
| 2 | MENOR | arquitectura | `AGENTS.md:205-211` | AGENTS §8; fase-11 T9 | **El texto y la tabla no casan.** El texto anuncia "Tres familias de acceso a datos", pero la tabla sigue con dos columnas; la del eje cliente solo está en un párrafo aparte | Añadir a la tabla una columna para el eje cliente |
| 3 | MENOR | arquitectura | `docs/fases/fase-11.md:269-279` | AGENTS §11 bis (catálogo: recurso de otra empresa da `NoEncontrado`) | **Desviación del catálogo sin anotar.** En `CrearReserva`, un servicio o un profesional de otra empresa da `Validacion` y no `NoEncontrado`. Lo justifican T6 y CU-07 6a, y está probado, pero no figura en la tabla de §6 | Añadir la fila: `CrearReserva` · recurso de otra empresa · da `Validacion` (CU-07 6a, T6) |

El revisor de seguridad no encuentra hallazgos de esta fase. Avisa de algo **fuera del diff**: `src/PlataformaReservas.Web/Cuenta/Pages/Login.razor:80` usa `PasswordSignInAsync` con `lockoutOnFailure: false`, lo que contradice AGENTS §8 quinquies y RN-169. Es anterior a esta fase y `Web` no está en su radio, así que no cuenta como hallazgo de la Fase 11; hay que confirmar que una fase posterior lo corrige.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas de T1 y T2 se escribieron primero y el desarrollador las confirmó.
- [x] Criterios de CU-07 de esta fase (`PruebasCrearReserva`, `PruebasConcurrenciaReservas`).
- [x] Criterios de CU-10 de esta fase (`PruebasCancelarReservaCliente`).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96 (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`).
- [x] `"23P01"` aparece una sola vez en `src/`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion`.
- [x] RN-102: no hay `TimeZoneInfo` en `CasosUso/`.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (314 + 212).
- [x] **Control 3:** las pruebas de reservas y la de concurrencia están en verde contra PostgreSQL real, y `ex_reservas_sin_solape` existe en la base de pruebas (`PruebasEsquema`).

## Revisión 2026-10-12 23:17

**Veredicto:** APTO
**Especificación:** docs/fases/fase-11.md (APROBADA)
**Commit base:** 7693bc9
**Ficheros revisados:** 44. Son 15 modificados y 25 nuevos en `src/` y `tests/`, más 4 `.md` excluidos por `.gitignore`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 314 | 314 | 0 |
| Integración | 212 | 212 | 0 |

- **Comprobaciones mecánicas:** `RESUMEN|bloqueantes=0|importantes=0`.
- **Compilación:** `dotnet build` termina con 0 avisos y 0 errores. Eso incluye `ConsultasDisponibilidadEnMemoria.cs`, que el revisor de arquitectura no pudo comprobar.
- **Alcance:** todos los ficheros entran en §4.

**Estado de los hallazgos de las rondas anteriores:** los tres menores de las 23:11 y el menor 3 de las 22:26 están resueltos:
- `modelo-dominio.md:438` reformulado;
- tabla de AGENTS §8 con tres columnas;
- filas de `CrearReserva` y de `ClaimsSesion` en la tabla de omisiones de §6.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `src/PlataformaReservas.Aplicacion/CasosUso/Reservas/CrearReserva.cs:60-64` | `arquitectura.md` §6 (A-02) | **Un estado imposible se trata como validación.** Si `ObtenerContactoAsync` devuelve `null` porque el usuario de la sesión no existe, el resultado es `Fallo(Validacion, "Hace falta un telefono…")`. Un usuario autenticado que no existe es un estado imposible, no un desenlace previsto, y el mensaje confunde porque culpa a la falta de teléfono | Tratar `contacto is null` aparte: lanzar una excepción o devolver `NoAutorizado`. Dejar `Validacion` solo para el caso de RN-82, con contacto pero sin teléfono |
| 2 | MENOR | arquitectura | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasCrearReserva.cs:52-59` | fase-11 §5 T6, paso 2 (RN-13) | **Falta una prueba.** T6 dice "empresa inactiva o inexistente", pero solo se prueba la inactiva | Añadir `Una_empresa_inexistente_da_validacion` |

El revisor de seguridad no encuentra hallazgos. Comprobó:
- el filtro por usuario y por empresa en la consulta;
- que el contrato de `Dominio` está limpio;
- que el usuario sale del claim;
- que `FinUtc` se calcula en servidor;
- la recomprobación dentro de la transacción y la traducción única del 23P01;
- que el contacto se lee con `UserManager`;
- que no aparece nada prohibido.

### Criterios de aceptación (§7 de la especificación)

- [x] Las pruebas de T1 y T2 se escribieron primero y el desarrollador las confirmó.
- [x] Criterios de CU-07 de esta fase (`PruebasCrearReserva`, `PruebasConcurrenciaReservas`).
- [x] Criterios de CU-10 de esta fase (`PruebasCancelarReservaCliente`).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96 (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`).
- [x] `"23P01"` aparece una sola vez en `src/`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion`.
- [x] RN-102: no hay `TimeZoneInfo` en `CasosUso/`.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (314 + 212).
- [x] **Control 3:** las pruebas de reservas y la de concurrencia están en verde contra PostgreSQL real, y `ex_reservas_sin_solape` existe en la base de pruebas (`PruebasEsquema`).
