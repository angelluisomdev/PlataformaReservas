# Revisión de la Fase 17

## Revisión 2026-10-24 18:49

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-17.md (APROBADA)
**Commit base:** f1f6ed6
**Ficheros revisados:** 25, más la documentación (`docs/`, `AGENTS.md`) y `.claude/skills/revisar/comprobaciones.sh`, que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 2 | 2 | 4 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 448 | 448 | 0 |
| Integración | 437 | 437 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.
- Revisión de seguridad: **sin hallazgos**. Confirma que:
  - los datos de demostración están desactivados por defecto, no hay clave en el repositorio y los usuarios se crean con `UserManager`;
  - `UrlBaseCorreo` rechaza `javascript:` y `file:`, y ningún registro lleva el token ni el correo;
  - el SQL crudo de las pruebas va parametrizado.

**Contexto de los dos bloqueantes.** Ambos son cambios que el desarrollador **pidió expresamente** en esta sesión:
- corregir los menores de `/revisar 16b`;
- corregir los hallazgos de `/auditar-pruebas 17`.

Están documentados en `fase-16b.md` (§4 y decisiones), en `fase-16b-revision.md` y en `fase-17.md` §11. Pero `fase-17.md` §4, el contrato con el que se revisa esta fase, no los nombra, y §9 manda expresamente a la Fase 19 parte de lo que ya se ha hecho. La corrección es documental. Mientras no se haga, el radio de esta fase no coincide con su diff.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance; arquitectura (#2, como IMPORTANTE) | `src/PlataformaReservas.Infraestructura/Correo/UrlBaseCorreo.cs` (nuevo), `Correo/RenderizadorPlantillas.cs:104-114`, `Identidad/SolicitudRestablecimientoClave.cs:29`; `tests/.../Correo/PruebasUrlBaseCorreo.cs` (nuevo), `tests/.../Correo/PruebasRenderizadorPlantillas.cs:163-179`; `AGENTS.md` §8 l.216; `docs/arquitectura.md` §11.3; `docs/fases/fase-16b.md`; `docs/fases/fase-17.md:289` | AGENTS §3.2 y §13; `fase-17.md` §4 y §9 | Los menores de `/revisar 16b` se corrigieron en el árbol de la Fase 17, pero §4 no nombra ninguno de estos ficheros: en Infraestructura solo autoriza `SembradorDatos.cs`, en `AGENTS.md` solo §11 bis, y en `arquitectura.md` solo §6, §12 y §13.6. Además, §9 l.289 dice que los menores 2-5 y 7 de `/revisar 16b` van a la Fase 19 (duda 7), así que la especificación contradice al diff. También cambia el comportamiento: con `UrlBase: ""` (el valor por defecto de `appsettings.json`), los correos de RN-152 y RN-153 ahora fallan al componerse en lugar de salir con enlaces relativos | Anotar en `fase-17.md` §4 una **excepción al radio autorizada por el desarrollador el 2026-10-24** que nombre estos ficheros, como hizo la 16b con `ForgotPassword.razor`. Corregir §9 l.289 y la duda 7 para decir que esos menores se resolvieron aquí. Otra opción: hacer un commit aparte con estos ficheros como corrección de la 16b, antes del de la 17 |
| 2 | BLOQUEANTE | alcance; arquitectura (#3, como IMPORTANTE) | `tests/PlataformaReservas.PruebasIntegracion/Infraestructura/BaseDatosFixture.cs:22-25,65-76`; `tests/.../CasosUso/Horarios/PruebasAgregarIntervalo.cs:35-53`; `docs/casos-uso.md` (criterio de CU-16) | AGENTS §3.2; `fase-17.md` §4 | Las correcciones de `/auditar-pruebas 17` tocan dos ficheros de pruebas que la fila `tests/PlataformaReservas.PruebasIntegracion` de §4 no nombra. Solo aparecen en las decisiones de §11 | Añadirlos a esa fila de §4, citando la auditoría 17 (menores 3 y 5) |
| 3 | IMPORTANTE | arquitectura (#1) | `tests/PlataformaReservas.PruebasIntegracion/PlataformaReservas.PruebasIntegracion.csproj:10` | `docs/arquitectura.md` §2.1; AGENTS §6 | La referencia de `PruebasIntegracion` a `Web` está autorizada (duda 2), pero el grafo de §2.1 solo muestra `PI --> I` y `PI --> A`, y el árbol de AGENTS §6 dice "PruebasIntegracion -> Aplicacion, Infraestructura". El grafo documentado no coincide con el código | Añadir `PI --> W` a §2.1 y a AGENTS §6, citando la Fase 17, duda 2, y limitarlo a la reflexión sobre los atributos de ruta |
| 4 | IMPORTANTE | alcance | `docs/fases/fase-17.md` §7 | `/revisar` §3 (criterio sin evidencia) | El criterio de SPEC §16.2 sigue desmarcado y parcial: falta el `curl` del desarrollador (302 en `/app/*` y `/my-account/*` sin sesión), que es lo que demuestra que `AuthorizeRouteView` aplica los atributos. Lo señala también el revisor de seguridad como no verificable | Hacer el `curl` contra una instancia con el PostgreSQL de desarrollo y marcar el criterio con la evidencia |
| 5 | MENOR | arquitectura (#4) | `tests/.../Autorizacion/PruebasAutorizacionRutas.cs:38-50` | `fase-17.md` T2 | La política se comprueba por el prefijo de la ruta (`/app`, `/my-account`), no por el espacio de nombres del área como dice T2. Una página de `App/` con una ruta sin `/app` y `[Authorize(Policy = "EsCliente")]` pasaría las pruebas | Filtrar también por `t.Namespace` (`App` → `EsPropietario`, `MiCuenta` → `EsCliente`), o anotar en §11 que se eligió el prefijo |
| 6 | MENOR | arquitectura (#5) | `src/PlataformaReservas.Infraestructura/Persistencia/DatosIniciales/SembradorDatos.cs:144-157` | `fase-17.md` T5 ("idempotente") | El propietario (`CrearPropietarioAsync`) y su `MiembroEmpresa` se confirman por separado. Si falla entre las dos cosas, la siguiente siembra encuentra el usuario y vuelve sin crear la pertenencia, y el propietario se queda sin empresa | Comprobar la pertenencia, no solo el usuario, antes de volver; o crear las dos cosas en una transacción |
| 7 | MENOR | arquitectura (#6) | `SembradorDatos.cs:67-68`; `RenderizadorPlantillas.cs:104-105`; `UrlBaseCorreo.cs:5` | AGENTS §3.10 | Comentarios nuevos que explican la intención o citan el origen, sin marcar un punto crítico | Quitarlos o dejar solo los críticos |
| 8 | MENOR | alcance | `docs/fases/fase-17.md:318`, `:413`, §12 | AGENTS §2 | §11 y §12 dicen "421 de integración", y hoy son 437 tras las dos rondas de correcciones. §12 deja `/auditar-pruebas 17` sin marcar, aunque se hizo y sus hallazgos se corrigieron | Actualizar las cifras y marcar la auditoría |

**Falsos positivos:** ninguno.

**Notas de los revisores (no verificables, no son hallazgos)**
- Si `DatosDemostracion:Clave` está en alguna configuración de despliegue no versionada. Está fuera del repositorio.
- RN-159 (el token caduca y no se reutiliza) es una comprobación manual del desarrollador.
- `.claude/skills/revisar/comprobaciones.sh` cambió (lista `TRANSVERSALES`). Es una herramienta del proceso, anotada en `fase-17.md` §11; no se valora como alcance.

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1224), SQL crudo sin datos personales en claro.** Evidencia: `PruebasCifradoEnReposo` y el sabotaje de RN-163.
- [x] **SPEC §21 (l.1225), inicio de sesión, duplicado y restablecimiento con cifrado.** Evidencia: `PruebasCifradoEnReposo`, que ahora comprueba `DuplicateEmail`.
- [x] **SPEC §21 (l.1226), empresa en claro y buscador por ciudad.** Evidencia: `PruebasCifradoEnReposo`.
- [x] **SPEC §21 (l.1214), sin solapes y concurrencia contra PostgreSQL real.** Evidencia: `PruebasConcurrenciaReservas`, en verde.
- [x] **SPEC §21 (l.1215), aislamiento entre empresas.** Evidencia: `PruebasAislamientoEmpresas`, en verde.
- [ ] **SPEC §16.2, autorización de rutas privadas.** Falta el `curl` (hallazgo 4). La reflexión y los sabotajes sí tienen evidencia.
- [x] **Concurrencia de RN-97.** Evidencia: `PruebasConcurrenciaReservas` y el sabotaje de §6.
- [x] **`docs/pruebas.md` existe.** Evidencia: el fichero.
- [x] **SPEC §21 y `casos-uso.md` marcados con evidencia.** Evidencia: las marcas, incluida CU-16 tras la auditoría.
- [x] **Sin paquetes nuevos ni secretos, y `DatosDemostracion:Habilitado` en `false`.** Evidencia: el diff del `.csproj` (solo una `ProjectReference`), los `appsettings*.json` y la revisión de seguridad.
- [x] **Sabotajes de §6.** Evidencia: `fase-17.md` §6 y §11, y `docs/pruebas.md` §4.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 448/448 y 437/437).

## Seguimiento 2026-10-24: corrección de los hallazgos

Corregidos a petición del desarrollador. No es una ronda nueva de `/revisar`.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (BLOQUEANTE) | Radio: correcciones de los menores de 16b fuera de §4; §9 los mandaba a la Fase 19 | Resuelto | `fase-17.md` §4 recoge una **excepción al radio autorizada por el desarrollador el 2026-10-24** que nombra `UrlBaseCorreo.cs`, `RenderizadorPlantillas.cs`, `SolicitudRestablecimientoClave.cs`, sus pruebas y los documentos, y el cambio de comportamiento con `UrlBase` vacía. §9 ya no lleva esa fila, y la duda 7 tiene una actualización |
| 2 (BLOQUEANTE) | Radio: `BaseDatosFixture.cs` y `PruebasAgregarIntervalo.cs` fuera de §4 | Resuelto | Nombrados en la fila de pruebas de integración de §4, citando la auditoría 17 (menores 3 y 5). La fila `docs/` nombra `casos-uso.md` y los informes |
| 3 (IMPORTANTE) | Grafo sin `PI --> W` | Resuelto | `arquitectura.md` §2.1 (arista y párrafo: solo reflexión de rutas) y AGENTS §6 |
| 4 (IMPORTANTE) | Falta el `curl` de SPEC §16.2 | Resuelto | Instancia propia en el puerto 5199 contra un PostgreSQL desechable, ya borrado. Las 9 rutas privadas dan 302 a `/login?ReturnUrl=…` y `/` y `/businesses`, 200. Criterio marcado en §7 |
| 5 | Política comprobada solo por prefijo | Resuelto | `Toda_pagina_del_espacio_de_nombres_del_area_de_empresa_exige_la_politica_de_propietario` y la de `MiCuenta`. Sabotaje: `EsCliente` en `App/_Imports.razor` hace fallar las dos de empresa |
| 6 | Propietario de demostración sin pertenencia tras una siembra interrumpida | Resuelto | `SembrarPropietarioAsync` crea la pertenencia si falta. `PruebasDatosIniciales` crea antes el propietario sin empresa. Sabotaje: volver al `return` temprano la hace fallar |
| 7 | Comentarios que no marcan puntos críticos | Resuelto | Quitados de `UrlBaseCorreo`, `RenderizadorPlantillas`, `SembradorDatos`, la cabecera de `PruebasAutorizacionRutas` y de `PruebasCifradoEnReposo`, y el de `PruebasDatosIniciales`. Quedan los críticos: la carrera de `EstablecerTelefono`, las mismas claves de la fixture y `[AllowAnonymous]` |
| 8 | Cifras desactualizadas y auditoría sin marcar | Resuelto | `fase-17.md` §11 y §12: 448 y 439, y `/auditar-pruebas 17` marcado |

**Verificación:** `dotnet build` sin avisos; unitarias 448/448; integración 439/439; `comprobaciones.sh` 0 bloqueantes, 0 importantes. Sabotajes ejecutados y restaurados: espacio de nombres, pertenencia del propietario y `[AllowAnonymous]`.

## Revisión 2026-10-24 19:07

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-17.md (APROBADA)
**Commit base:** f1f6ed6
**Ficheros revisados:** 25, más la documentación (`docs/`, `AGENTS.md`) y `.claude/skills/revisar/comprobaciones.sh`, que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 0 | 0 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 448 | 448 | 0 |
| Integración | 439 | 439 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.
- **Radio:** los 25 ficheros de `git status` y los documentos cambiados figuran en `fase-17.md` §4, incluidas las excepciones autorizadas por el desarrollador.
- **Alcance:** todo corresponde a una tarea de §5 o a una corrección autorizada y anotada en §11.
- Revisión de seguridad: **sin hallazgos**.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Correo/UrlBaseCorreo.cs:9` (llamado desde `RenderizadorPlantillas.cs:106`, `Identidad/SolicitudRestablecimientoClave.cs:29` y `tests/.../Correo/PruebasUrlBaseCorreo.cs`) | AGENTS §3.3 y §5 (métodos en castellano; solo tres excepciones) | El método nuevo `TryObtener` lleva el prefijo inglés `Try`. Es el único método propio de `src/` con ese prefijo, comprobado con `grep`; los demás `Try*` son del BCL (`Uri.TryCreate`, `Guid.TryParse`) | Renombrarlo en castellano (por ejemplo `IntentarObtener`) y actualizar las dos llamadas, las pruebas y las menciones en `fase-16b.md`, `fase-16b-revision.md` y `arquitectura.md` §11.3 si lo nombran |

**Notas de los revisores (no verificables, no son hallazgos)**
- Ninguno de los dos revisores ejecuta `dotnet build` ni `dotnet test`, por su rol. Los resultados de arriba son de esta revisión.
- No se puede comprobar si hay User Secrets o configuración no versionada con `DatosDemostracion:Clave`.
- El `curl` y los sabotajes solo constan en `fase-17.md`; los revisores no los han reproducido.

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1224), SQL crudo sin datos personales en claro.** Evidencia: `PruebasCifradoEnReposo` y el sabotaje de RN-163.
- [x] **SPEC §21 (l.1225), inicio de sesión, duplicado y restablecimiento con cifrado.** Evidencia: `PruebasCifradoEnReposo` (`DuplicateEmail`).
- [x] **SPEC §21 (l.1226), empresa en claro y buscador por ciudad.** Evidencia: `PruebasCifradoEnReposo`.
- [x] **SPEC §21 (l.1214), sin solapes y concurrencia contra PostgreSQL real.** Evidencia: `PruebasConcurrenciaReservas`, en verde.
- [x] **SPEC §21 (l.1215), aislamiento entre empresas.** Evidencia: `PruebasAislamientoEmpresas`, en verde.
- [x] **SPEC §16.2, autorización de rutas privadas.** Evidencia: `PruebasAutorizacionRutas` (prefijo, espacio de nombres y `[AllowAnonymous]`, con sabotajes) y el `curl` del 2026-10-24: 9 rutas privadas con 302 a `/login` y las públicas con 200.
- [x] **Concurrencia de RN-97.** Evidencia: `PruebasConcurrenciaReservas` y su sabotaje.
- [x] **`docs/pruebas.md` existe.** Evidencia: el fichero.
- [x] **SPEC §21 y `casos-uso.md` marcados con evidencia.** Evidencia: las marcas, CU-16 incluido.
- [x] **Sin paquetes nuevos ni secretos, y `DatosDemostracion:Habilitado` en `false`.** Evidencia: el `.csproj` (solo una `ProjectReference`), los `appsettings*.json` y la revisión de seguridad.
- [x] **Sabotajes de §6.** Evidencia: `fase-17.md` §6 y `docs/pruebas.md` §4.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 448/448 y 439/439).
