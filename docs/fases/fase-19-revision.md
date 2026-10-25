# Revisión de la Fase 19

## Revisión 2026-10-25 13:35

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-19.md (APROBADA)
**Commit base:** b941d79
**Ficheros revisados:** 58 versionados (43 modificados y 15 nuevos), más la documentación que git ignora (`docs/seguridad.md`, `arquitectura.md`, el SPEC, `pruebas.md` y `fase-19.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 5 | 2 | 2 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 449 | 449 | 0 |
| Integración | 519 | 519 | 0 |

- `dotnet build`: 0 avisos y 0 errores, con `NuGetAudit` activo.
- `comprobaciones.sh`: 0 bloqueantes y 2 importantes. Los dos son falsos positivos (#9 y #10).
- Integración ejecutada: los cambios tocan `Infraestructura` y `PruebasIntegracion`, y la fase cierra el Control 5.
- **Revisión de seguridad:** sin hallazgos en las 11 áreas. La ronda previa de T11 dejó tres hallazgos (`/login/` con barra final, `/for-business/register` sin límite y `ReturnUrl` con espacios), y esta revisión los da por corregidos.
- **Navegación manual con la CSP activa:** el desarrollador la confirma el 2026-10-25 ("el punto 1 de mis pruebas está ok").

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | arquitectura | `src/PlataformaReservas.Web/CabecerasSeguridad.cs:11`; usos en `Program.cs:41`, `PruebasCabecerasSeguridad.cs:55` y `docs/arquitectura.md:854` | AGENTS §3.3 y §5 (métodos en castellano) | El método de extensión propio se llama `UseCabecerasSeguridad`, medio en inglés. No es un tipo del BCL ni de la plantilla de `Web/Cuenta`. En la misma fase, su gemelo es `AgregarLimitacionFrecuencia`, como `AgregarInfraestructura`. `comprobaciones.sh` (C14) no lo detecta | Renombrarlo, por ejemplo a `UsarCabecerasSeguridad`, y actualizar `Program.cs`, la prueba y `arquitectura.md` §10.1 |
| 2 | IMPORTANTE | arquitectura | `Web/Components/App/_Imports.razor:18-19`, `MiCuenta/_Imports.razor:12-13`, `Layout/MainLayout.razor:2,11,15` | `arquitectura.md` §2.1 (l.96-98): "Ningún componente Blazor usa un tipo de `Infraestructura` [...] Única excepción: `Web/Cuenta/**`"; AGENTS §2 y §14 | R8 lleva `@using PlataformaReservas.Infraestructura.Identidad` y `RolesIdentidad.Politica*` a componentes de fuera de `Web/Cuenta`. La especificación lo pide, pero contradice a `arquitectura.md`, que tiene más autoridad. Era motivo para parar y preguntar | Que decida el desarrollador: (a) volver a los literales `"EsPropietario"` y `"EsCliente"` y dar R8 por descartado con su motivo; o (b) anotar en `arquitectura.md` §2.1 una excepción explícita para las constantes de política de `RolesIdentidad`, con fecha y fase |
| 3 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasIntegracion/Cuenta/PruebasIdentityRedirectManager.cs:12` | `fase-19.md` T8 y §6 (H-02, "`/\evil.com` acaba en `/`"); AGENTS §11 bis (sabotajes) | `[InlineData("/\evil.com")]` no es la cadena `/\evil.com`. En C# 14, `\e` es el carácter ESC (U+001B), así que el caso entra por la rama `char.IsControl`. Ningún caso prueba el prefijo `/\`: si se quita `uri.StartsWith("/\\")` de `DestinoLocal`, no falla ninguna prueba y `/\evil.com` pasa tal cual porque empieza por `/`. Confirmado leyendo el fichero | Cambiarlo a `"/\\evil.com"` (o `@"/\evil.com"`), repetir el sabotaje quitando solo esa comprobación y anotarlo en `fase-19.md` §6 |
| 4 | IMPORTANTE | arquitectura | `docs/arquitectura.md:94` y l.117; `AGENTS.md` §6 | `arquitectura.md` §2.1; AGENTS §6 ("Web (solo reflexion de rutas; Fase 17, duda 2)") | `PruebasIntegracion` usa ahora tipos de `Web` directamente: `LimitacionFrecuencia`, `CabecerasSeguridad`, `IdentityRedirectManager` (con el nuevo `InternalsVisibleTo`) y `FichaEstablecimiento` renderizado. Además monta una canalización parcial con `ApplicationBuilder`. La especificación lo prevé (§6, "las clases nuevas de `Web` se prueban en `PruebasIntegracion`"), pero §2.1 sigue diciendo "no arranca la aplicación ni usa otros tipos de `Web`", y la nota de `InternalsVisibleTo` solo menciona `Infraestructura` | Actualizar `arquitectura.md` §2.1, la nota de l.117 y AGENTS §6 con el uso autorizado en la Fase 19 |
| 5 | IMPORTANTE | arquitectura | `docs/arquitectura.md:169-197` (§2.3) | `arquitectura.md` §2.3 (estructura interna) | El árbol no recoge los ficheros nuevos de la fase: `Infraestructura/EventosSeguridad.cs`, `Identidad/InicioSesion.cs`, `Identidad/RestablecimientoClave.cs`, `Correo/EnmascaradoCorreo.cs`, `Web/LimitacionFrecuencia.cs` y `Web/CabecerasSeguridad.cs` | Añadirlos a §2.3 |
| 6 | IMPORTANTE | alcance | `docs/fases/fase-19.md` §4, l.89-97 ("Ampliaciones del radio hechas al implementar") | Skill `revisar` §3; AGENTS §3.2; `CLAUDE.md` ("para, dilo y espera") | El desarrollador aprobó antes de implementar `RestablecimientoClave` y el `InternalsVisibleTo` de `Web.csproj`, que estaban en el plan. El resto de la lista de §4 la añadió quien implementaba sobre una especificación ya APROBADA, sin ratificación expresa: `EventosSeguridad`, `EnmascaradoCorreo`, `UnidadTrabajo`, los dobles `RegistroCaptura` y `NavegacionPrueba`, `Configuracion.razor`, `Servicios.razor`, `Perfil.razor` y `arquitectura.md` §3.2. Todo corresponde a T1, T6, T8 y T10, así que no hay ampliación de alcance, pero el radio no lo fijó el desarrollador. Además falta `Correo/PruebasServicioEmailRegistro.cs`, del que sale la prueba del enmascarado | Que el desarrollador ratifique la lista de §4 (fecha y "autorizado por el desarrollador", como en la Fase 18) y añada `PruebasServicioEmailRegistro.cs` |
| 7 | MENOR | arquitectura | `tests/.../Identidad/PruebasSolicitudRestablecimientoClave.cs:230`, `tests/.../Correo/PruebasServicioEmailRegistro.cs:35` | AGENTS §11 bis (dobles compartidos en `Infraestructura/`) | El fichero usa ya el doble compartido `RegistroCaptura<T>` en el caso nuevo, pero conserva su `RegistroCaptura` anidado para los antiguos: dos dobles equivalentes en el mismo fichero. Igual en `PruebasServicioEmailRegistro`. Es la deuda R22, y la fase la ha agravado | Usar solo `RegistroCaptura<T>` en esos dos ficheros, o anotarlo en R22 (`fase-19.md` §9) |
| 8 | MENOR | alcance | `docs/fases/fase-19.md:332` (§7, l.1224) y `:599` (§12); `TFM_PlataformaReservas_SPEC.md` §21 l.1224 | `fase-19.md` §7 y §12 | La navegación manual con la CSP activa ya está hecha y es correcta (desarrollador, 2026-10-25), pero §7, §12 y el SPEC siguen marcándola como pendiente o parcial | Anotar la comprobación con fecha y marcar SPEC l.1224, §7 l.1224, el Control 5 y la casilla de §12 |
| 9 | Falso positivo | mecánica (C18) | `tests/.../Components/Publico/PruebasFichaEstablecimiento.cs` | AGENTS §11 bis | "No existe FichaEstablecimiento.cs en src". La clase probada es el componente `Web/Components/Publico/FichaEstablecimiento.razor`. Es un único fichero por clase, en su misma carpeta relativa | C18 debería buscar también `.razor` |
| 10 | Falso positivo | mecánica (C17) | `Directory.Build.props` | `arquitectura.md` §2.2, §3.1 y §3.2 | "Cambian los ajustes comunes: contrastar". Contrastado: los dos ajustes nuevos (`NuGetAudit` y `NuGetAuditMode=all`) son E-2 (`arquitectura.md` §14.2) y ya figuran en §3.2. No hay desajuste | — |

**Notas de los revisores (no verificables, no son hallazgos)**
- Ninguno de los dos puede ejecutar `dotnet` ni los sabotajes. Las suites se han ejecutado en esta revisión. Los sabotajes constan en `fase-19.md` §6, salvo el del prefijo `/\`, que no demuestra nada (hallazgo 3).
- El revisor de seguridad no revisa el contenido de `appsettings.Development.json` ni de `.env`, que no han cambiado.

### Criterios de aceptación (§7 de la especificación)

- [ ] **SPEC §21 (l.1215), nomenclatura de §3:** `UseCabecerasSeguridad` incumple §3 (hallazgo 1). `comprobaciones.sh` no da hallazgos de C14, pero no detecta ese caso.
- [x] **SPEC §21 (l.1222), bloqueo de 15 min registrado.** Evidencia: `Identidad/PruebasInicioSesion`, con 8 casos, y su sabotaje (`lockoutOnFailure: false`: fallan 4).
- [x] **SPEC §21 (l.1223), `429` en `/login` y `/businesses`.** Evidencia: `PruebasLimitacionFrecuencia`, incluidas la barra final y la respuesta de rechazo; `curl` contra una instancia propia (`fase-19.md` §11); sabotaje.
- [x] **SPEC §21 (l.1224), cabeceras y aplicación funcionando con la CSP activa.** Evidencia: `PruebasCabecerasSeguridad`, `curl` con una sola CSP, y la navegación manual del desarrollador del 2026-10-25. Falta anotarla (hallazgo 8).
- [x] **SPEC §21 (l.1225), `<script>` que no se ejecuta.** Evidencia: `PruebasFichaEstablecimiento` y su sabotaje con `MarkupString`; `PruebasRenderizadorPlantillas` para el correo.
- [x] **SPEC §21 (l.1226), auditoría de dependencias.** Evidencia: sabotaje con `Pkcs` 10.0.12, que da `NU1902` y `NU1903`.
- [x] **SPEC §21 (l.1227), `docs/seguridad.md`.** Evidencia: existe, y cada fila de la tabla OWASP cita su prueba o su fichero.
- [x] **Revisión de seguridad.** Evidencia:
  - H-02: `PruebasIdentityRedirectManager`, con el hueco del caso `/\` (hallazgo 3);
  - H-03 y H-22: `PruebasRestablecimientoClave` y `curl`;
  - H-11: `docker compose config`;
  - H-14: las pruebas de registro de cada clase;
  - el resto de H-xx: clasificados en `seguridad.md` §4.
- [ ] **Refactorización R1 a R11:** hechos, pero R8 contradice `arquitectura.md` §2.1 (hallazgo 2). R12 a R23 están documentados en §9.
- [ ] **Los sabotajes de §6:** los cuatro planificados fallan como se esperaba. El sabotaje adicional de H-02 no cubre la rama `/\` (hallazgo 3).
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión (0 avisos; 449/449 y 519/519).

## Seguimiento 2026-10-25: corrección de los hallazgos

Corregidos a petición del desarrollador. No es una ronda nueva de `/revisar`. El desarrollador decidió los hallazgos 2 y 6.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (BLOQUEANTE) | `UseCabecerasSeguridad` medio en inglés | Resuelto | Renombrado a `UsarCabecerasSeguridad` en `CabecerasSeguridad.cs`, `Program.cs`, `PruebasCabecerasSeguridad.cs` y `arquitectura.md` §10.1. Ningún otro método propio de `src` empieza por `Use`, `Add`, `Map`, `Get`, `Set`, `Try` ni `Is`. Solo `MapAdditionalIdentityEndpoints`, de la plantilla de `Web/Cuenta` (AGENTS §5) |
| 2 | R8 lleva `RolesIdentidad` a componentes de fuera de `Web/Cuenta` | Resuelto | Decisión del desarrollador: se mantiene R8, con una excepción acotada a las constantes de política de `RolesIdentidad` en `arquitectura.md` §2.1, con fecha y fase |
| 3 | El caso `/\evil.com` era un `\e` (ESC) | Resuelto | Casos escritos en C# como `"/\evil.com"` y `"/\/evil.com"`. Sabotaje: al quitar solo la comprobación del prefijo `/\`, fallan esos dos casos (2 de 19). Restaurado y anotado en `fase-19.md` §6 y en `pruebas.md` §4 |
| 4 | `PruebasIntegracion` usa tipos de `Web` sin reflejarlo | Resuelto | `arquitectura.md` §2.1 (párrafo de `PruebasIntegracion` y nota de `InternalsVisibleTo`) y `AGENTS.md` §6, autorizados por el desarrollador |
| 5 | §2.3 sin los ficheros nuevos | Resuelto | Añadidos `EventosSeguridad`, `InicioSesion`, `RestablecimientoClave`, `EnmascaradoCorreo`, `LimitacionFrecuencia` y `CabecerasSeguridad` |
| 6 | Ampliaciones del radio sin ratificar | Resuelto | El desarrollador las ratifica el 2026-10-25. `fase-19.md` §4 lo anota y añade `PruebasServicioEmailRegistro.cs`, `arquitectura.md` §2.1 y §2.3, y `AGENTS.md` §6 |
| 7 | Dobles de registro duplicados | Resuelto | `PruebasSolicitudRestablecimientoClave` y `PruebasServicioEmailRegistro` usan solo `RegistroCaptura<T>`, que gana `LineasDe(nivel)`. Ya no queda ningún `RegistroCaptura` anidado en los ficheros de la fase |
| 8 | CSP manual sin anotar | Resuelto | Anotada con fecha en `fase-19.md` §7, §11 y §12 (Control 5 marcado), en SPEC §21 l.1224, en `pruebas.md`, en `seguridad.md` y en `arquitectura.md` (§14.6 y R-12) |
| 9 y 10 | C18 y C17 | Falsos positivos, sin cambios | C18 no reconoce que la clase probada es un `.razor`. C17 está contrastado: `arquitectura.md` §3.2 recoge `NuGetAudit` |

Verificación tras las correcciones:
- `dotnet build`: 0 avisos y 0 errores.
- `dotnet test`: 449/449 unitarias y 520/520 de integración.
- `comprobaciones.sh`: 0 bloqueantes, y solo los falsos positivos C18 y C17.

## Revisión 2026-10-25 13:58

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-19.md (APROBADA)
**Commit base:** b941d79
**Ficheros revisados:** 58 versionados (43 modificados y 15 nuevos), más la documentación que git ignora (`AGENTS.md`, `docs/seguridad.md`, `arquitectura.md`, el SPEC, `pruebas.md` y `fase-19.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 0 | 2 | 2 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 449 | 449 | 0 |
| Integración | 520 | 520 | 0 |

- `dotnet build`: 0 avisos y 0 errores, con `NuGetAudit` activo.
- `comprobaciones.sh`: 0 bloqueantes y 2 importantes. Son los mismos falsos positivos de la ronda anterior (#4 y #5).
- **Radio:** los 58 ficheros figuran en `fase-19.md` §4, con las ampliaciones ratificadas por el desarrollador el 2026-10-25.
- **Alcance:** todo corresponde a T1-T12 o a las correcciones de la ronda de las 13:35.
- Los 8 hallazgos de la ronda anterior están resueltos. Los dos revisores lo confirman: el renombrado, la excepción de R8, el caso `/\`, `arquitectura.md` §2.1 y §2.3, la ratificación del radio, los dobles y la CSP anotada.
- **Novedad de esta ronda:** el revisor de seguridad encuentra una variante de H-02 que la corrección de T8 no cubría (hallazgo 1).

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | seguridad | `src/PlataformaReservas.Web/Cuenta/IdentityRedirectManager.cs:51` (rama `IsBaseOf` → `ToBaseRelativePath` de `DestinoLocal`) | H-02, SPEC §15.3 A01 y criterio de T8 (`fase-19.md` §5 y §7, "Revisión de seguridad") | **Sigue habiendo redirección abierta.** Las comprobaciones de `//`, `/\` y `\` solo se aplican a la cadena de entrada. Si llega una URI absoluta del mismo origen, el resultado de `ToBaseRelativePath` se devuelve sin volver a validar, y esa función solo recorta el prefijo `BaseUri` como texto (comparación ordinal). Con `ReturnUrl=https://<host-real>///evil.com`, `IsBaseOf` es verdadero y `ToBaseRelativePath` devuelve `//evil.com`, que el navegador lleva a `evil.com` después de un inicio de sesión real (`Login.razor:83`, `Register.razor:90`). Igual con barras invertidas tras el host. Es viable porque `RedirectToLogin.razor:6` ya hace que el `returnUrl` legítimo sea absoluto. Efecto secundario: con la base en mayúsculas (`HTTPS://<HOST>/app`), `IsBaseOf` es verdadero pero `ToBaseRelativePath` lanza `ArgumentException`, y da un 500 después de iniciar sesión. Confirmado leyendo `DestinoLocal` y el comportamiento documentado de `NavigationManager`, sin ejecutarlo. `PruebasIdentityRedirectManager` no tiene ningún caso del mismo origen con barras extra | Validar también el resultado de la rama absoluta: construir el destino con `absoluta.PathAndQuery` (y `Fragment`) del `Uri` ya analizado, que no distingue mayúsculas en el host y no lanza, y pasarlo por la misma comprobación de ruta local (`//`, `/\`, `\`). Añadir a `Un_destino_fuera_de_la_aplicacion_redirige_a_la_raiz` los casos `NavegacionPrueba.Base + "/evil.com"` (que da `///evil.com`), `Base + "\\evil.com"` y la base en mayúsculas, y un sabotaje que quite esa segunda validación |
| 2 | MENOR | arquitectura | `Web/Components/App/_Imports.razor:18`, `MiCuenta/_Imports.razor:12`, `Layout/MainLayout.razor:2` | `arquitectura.md` §2.1, excepción de R8: "Ningún otro tipo de `Infraestructura` entra en esos componentes" | Para usar las dos constantes se importa el espacio de nombres entero `PlataformaReservas.Infraestructura.Identidad`. Con eso, `InicioSesion`, `ServicioCuentas` y el resto quedan accesibles en todas las páginas de `App/` y `MiCuenta/`. Hoy no se usan, pero la excepción solo se cumple por disciplina | Usar un alias de tipo: `@using RolesIdentidad = PlataformaReservas.Infraestructura.Identidad.RolesIdentidad` |
| 3 | MENOR | arquitectura | `docs/arquitectura.md:1419-1436` (§14.5) | `fase-19.md`, duda 2 (a), y T12 | §14.5 conserva la tabla y el ejemplo de políticas con nombre (`AddFixedWindowLimiter("autenticacion", ...)`), que es la opción (b), la descartada. La tabla solo lista `/login`, `/register` y `/Account/ForgotPassword`, y contradice a la nota de implementación que tiene debajo y a `LimitacionFrecuencia.cs` | Sustituir el ejemplo por el limitador global (`GlobalLimiter = LimitacionFrecuencia.CrearLimitador()`) y completar la tabla con `/Account/Login`, `/Account/Register` y `/for-business/register` (solo `POST`) |
| 4 | Falso positivo | mecánica (C18) | `tests/.../Components/Publico/PruebasFichaEstablecimiento.cs` | AGENTS §11 bis | "No existe FichaEstablecimiento.cs en src". La clase probada es `Web/Components/Publico/FichaEstablecimiento.razor`. Es un único fichero por clase, en su misma carpeta relativa | C18 debería buscar también `.razor` |
| 5 | Falso positivo | mecánica (C17) | `Directory.Build.props` | `arquitectura.md` §2.2, §3.1 y §3.2 | "Cambian los ajustes comunes: contrastar". Contrastado: `NuGetAudit` y `NuGetAuditMode=all` (E-2) figuran en §3.2 y §14.2 | — |

**Notas de los revisores (no verificables, no son hallazgos)**
- El comportamiento exacto de `HttpNavigationManager` en .NET 10 al resolver `//evil.com` se deduce del código del marco, no de una ejecución. La prueba sugerida en el hallazgo 1 lo confirma o lo descarta.
- Ninguno de los dos revisores puede ejecutar `dotnet`, los sabotajes ni la navegación con la CSP. Las suites se han ejecutado en esta revisión; lo demás consta en `fase-19.md` §6, §7 y §11.

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1215), nomenclatura.** Evidencia: `UsarCabecerasSeguridad` corregido; el revisor de arquitectura no ve nada más en inglés salvo la excepción de plantilla de `Web/Cuenta`.
- [x] **SPEC §21 (l.1222), bloqueo de 15 min registrado.** Evidencia: `PruebasInicioSesion` y su sabotaje.
- [x] **SPEC §21 (l.1223), `429`.** Evidencia: `PruebasLimitacionFrecuencia`, `curl` y el sabotaje.
- [x] **SPEC §21 (l.1224), cabeceras y aplicación funcionando con la CSP.** Evidencia: `PruebasCabecerasSeguridad`, `curl` y la navegación manual del desarrollador del 2026-10-25.
- [x] **SPEC §21 (l.1225), `<script>` que no se ejecuta.** Evidencia: `PruebasFichaEstablecimiento` y `PruebasRenderizadorPlantillas`, con sabotaje.
- [x] **SPEC §21 (l.1226), auditoría de dependencias.** Evidencia: sabotaje con `Pkcs` 10.0.12 (`NU1902` y `NU1903`).
- [x] **SPEC §21 (l.1227), `docs/seguridad.md`.** Evidencia: la tabla OWASP con su prueba o su fichero en cada fila.
- [ ] **Revisión de seguridad:** H-02 no está cerrado del todo. La variante del mismo origen con barras extra sigue abierta (hallazgo 1), y `seguridad.md` §5 lo da por corregido. H-03, H-11, H-14 y H-22 sí están corregidos.
- [x] **Refactorización R1 a R11.** Evidencia: hechos; R8 con la excepción autorizada; R12 a R23 en §9.
- [x] **Sabotajes de §6.** Evidencia: anotados, incluido el de la rama `/\`. Falta el de la segunda validación que pide el hallazgo 1.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 449/449 y 520/520).

## Seguimiento 2026-10-25: corrección del bloqueante de la ronda de las 13:58

Corregido a petición del desarrollador. No es una ronda nueva de `/revisar`.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (BLOQUEANTE) | Redirección abierta por la rama de URI absoluta propia de `DestinoLocal`; 500 con el host en mayúsculas | Resuelto | `DestinoLocal` ya no usa `ToBaseRelativePath`. Una URI absoluta del mismo origen (`IsBaseOf`) se reduce a `PathAndQuery` y `Fragment` del `Uri` analizado, que convierte `\` en `/` y no distingue mayúsculas en el host, y pasa por la misma comprobación de ruta local que las relativas (`EsRutaLocal`: ni `//`, ni `/\`, ni `\`). Casos nuevos: `Base + "/evil.com"`, `Base + "//evil.com"`, `Base + "\evil.com"`, `Base + "\\evil.com"` y `HTTPS://RESERVAS.EXAMPLE//evil.com`, que van a `/`; `Base + "app#seccion"` y `HTTPS://RESERVAS.EXAMPLE/app` se conservan como `/app#seccion` y `/app`. Un destino absoluto propio sale ahora desde la raíz (`/my-account/...`), equivalente con `<base href="/">`, y se ajustaron sus dos expectativas. Sabotaje: devolver la ruta sin validarla hace fallar los 5 casos del mismo origen. Restaurado y anotado en `fase-19.md` §6 y §11, en `pruebas.md` §4 y en `seguridad.md` §5 |
| 2 y 3 (MENORES) | Alias de `RolesIdentidad` y ejemplo de §14.5 | Sin cambios | No se pidió corregirlos |

## Revisión 2026-10-25 14:12

**Veredicto:** APTO
**Especificación:** docs/fases/fase-19.md (APROBADA)
**Commit base:** b941d79
**Ficheros revisados:** 58 versionados (43 modificados y 15 nuevos), más la documentación que git ignora (`AGENTS.md`, `docs/seguridad.md`, `arquitectura.md`, el SPEC, `pruebas.md` y `fase-19.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 4 | 2 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 449 | 449 | 0 |
| Integración | 527 | 527 | 0 |

- `dotnet build`: 0 avisos y 0 errores, con `NuGetAudit` activo.
- `comprobaciones.sh`: 0 bloqueantes y 2 importantes. Son los mismos falsos positivos de las rondas anteriores (#5 y #6).
- **Radio:** los 58 ficheros figuran en `fase-19.md` §4, con las ampliaciones ratificadas por el desarrollador el 2026-10-25.
- **Alcance:** todo corresponde a T1-T12 o a las correcciones de `/revisar 19`.
- **Bloqueante de la ronda de las 13:58 resuelto.** El revisor de seguridad confirma que `DestinoLocal` vuelve a validar la ruta de una URI absoluta propia. `PruebasIdentityRedirectManager` tiene 26 casos, con su sabotaje.
- **Revisión de seguridad:** sin hallazgos en ninguna área.
- Los dos subagentes se cortaron por el límite de sesión y se reanudaron con su contexto, sin empezar de nuevo.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/Identidad/ServicioCuentas.cs` (`RegistrarSesionesEmpresaInvalidadas`) | `fase-19.md`, duda 4 (a): registrar las "bajas de cuenta y de empresa" | La baja de empresa se registra solo de forma indirecta: "Sesiones de los miembros de la empresa {EmpresaId} invalidadas", desde `RenovarSelloMiembrosAsync`, que hoy solo invoca `DarDeBajaEmpresa`. Si otro caso de uso renovara el sello, el registro dejaría de significar "baja de empresa" | Nombrar el mensaje por su evento, o anotar en §11 que la invalidación de sesiones es la prueba de la baja |
| 2 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/EventosSeguridad.cs` | AGENTS §11 bis (omisiones con motivo en §6) | Clase nueva sin fichero de pruebas propio, y su omisión no figura en la tabla "Casos del catálogo que no aplican" de §6. La cubren `PruebasRepositorioServicios`, `PruebasRepositorioProfesionales` y `PruebasServicioCuentas` | Añadir a §6 una fila: se prueba a través de las clases que lo usan |
| 3 | MENOR | arquitectura (pendiente de la ronda de las 13:58) | `Web/Components/App/_Imports.razor:18`, `MiCuenta/_Imports.razor:12`, `Layout/MainLayout.razor:2` | `arquitectura.md` §2.1, excepción de R8 | Sin corregir: se importa el espacio de nombres `Infraestructura.Identidad` entero para dos constantes | Alias de tipo: `@using RolesIdentidad = PlataformaReservas.Infraestructura.Identidad.RolesIdentidad` |
| 4 | MENOR | arquitectura (pendiente de la ronda de las 13:58) | `docs/arquitectura.md` §14.5 | `fase-19.md`, duda 2 (a), y T12 | Sin corregir: la tabla y el ejemplo de §14.5 describen políticas con nombre (la opción descartada) y no listan todas las rutas de `autenticacion` | Sustituir el ejemplo por el limitador global y completar la tabla |
| 5 | Falso positivo | mecánica (C18) | `tests/.../Components/Publico/PruebasFichaEstablecimiento.cs` | AGENTS §11 bis | "No existe FichaEstablecimiento.cs en src". La clase probada es `FichaEstablecimiento.razor` | C18 debería buscar también `.razor` |
| 6 | Falso positivo | mecánica (C17) | `Directory.Build.props` | `arquitectura.md` §2.2, §3.1 y §3.2 | "Contrastar". Contrastado: `NuGetAudit` figura en §3.2 y §14.2 | — |

**Notas de los revisores (no verificables, no son hallazgos)**
- Ninguno de los dos puede ejecutar `dotnet`, los sabotajes ni la navegación con la CSP. Las suites se han ejecutado en esta revisión; lo demás consta en `fase-19.md` §6, §7 y §11.
- **Contraseñas literales en pruebas fuera del cambio.** `UsuariosPrueba.cs` y `PruebasRegistrarEmpresa.cs` tienen las contraseñas de los usuarios de prueba escritas en el código. No son de esta fase ni son secretos reales.

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1215), nomenclatura.** Evidencia: el revisor de arquitectura no ve identificadores en inglés fuera de la excepción de `Web/Cuenta`; `comprobaciones.sh` sin C14.
- [x] **SPEC §21 (l.1222), bloqueo de 15 min registrado.** Evidencia: `PruebasInicioSesion` y su sabotaje.
- [x] **SPEC §21 (l.1223), `429`.** Evidencia: `PruebasLimitacionFrecuencia`, `curl` y el sabotaje.
- [x] **SPEC §21 (l.1224), cabeceras y aplicación funcionando con la CSP.** Evidencia: `PruebasCabecerasSeguridad`, `curl` y la navegación manual del desarrollador del 2026-10-25.
- [x] **SPEC §21 (l.1225), `<script>` que no se ejecuta.** Evidencia: `PruebasFichaEstablecimiento` y `PruebasRenderizadorPlantillas`, con sabotaje.
- [x] **SPEC §21 (l.1226), auditoría de dependencias.** Evidencia: sabotaje con `Pkcs` 10.0.12 (`NU1902` y `NU1903`).
- [x] **SPEC §21 (l.1227), `docs/seguridad.md`.** Evidencia: la tabla OWASP con su prueba o su fichero en cada fila.
- [x] **Revisión de seguridad.** Evidencia: H-02 (26 casos y tres sabotajes), H-03 y H-22, H-11 y H-14 corregidos; el resto de H-xx clasificados en `seguridad.md` §4.
- [x] **Refactorización R1 a R11.** Evidencia: hechos, R8 con su excepción autorizada; R12 a R23 en §9.
- [x] **Sabotajes de §6.** Evidencia: los cuatro planificados y los tres adicionales de H-02, con la prueba que falló.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 449/449 y 527/527).
