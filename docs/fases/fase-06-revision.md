# Revisión de la Fase 06

## Revisión 2026-10-03 19:57

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-06.md (APROBADA)
**Commit base:** 31c073d
**Ficheros revisados:** 44, más los documentos ignorados por git `docs/arquitectura.md`, `docs/base-datos.md` y `docs/fases/fase-06.md`

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 2 | 7 | 6 | 4 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 46 | 46 | 0 |
| Integración | 24 | 24 | 0 |

`dotnet build`: 0 avisos, 0 errores.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance · arquitectura | docker-compose.yml:19 | AGENTS §3.2, fase-06 §4 (raíz: solo `README.md` y `.editorconfig`) | El volumen pasa de `/var/lib/postgresql/data` a `/var/lib/postgresql`. Es el cambio que exige `arquitectura.md` §11.4 y que pedía la revisión de la Fase 5, y lo hizo el desarrollador. Aun así, el fichero está fuera del radio de esta fase. | Hacer un commit aparte, o anotar en fase-06 §4 y §11 que se autoriza |
| 2 | BLOQUEANTE | alcance · arquitectura | PlataformaReservas.slnx:2-4 | AGENTS §3.2, fase-06 §4 | Se añade la carpeta `/Solution Items/` con `README.md`. La solución no está en el radio. | Hacer un commit aparte o revertir, o anotar en fase-06 que se autoriza |
| 3 | IMPORTANTE | seguridad | src/PlataformaReservas.Infraestructura/InyeccionDependencias.cs:41-43 | A-06, `arquitectura.md` §8.4, RN-135 ⚑, RN-161 | Los 5 minutos solo se aplican en `RevalidadorIdentidad`, es decir, dentro de un circuito interactivo. La cookie conserva el `SecurityStampValidatorOptions.ValidationInterval` por defecto, que es de 30 minutos. Por eso, en las peticiones SSR y al abrir cada circuito nuevo, un usuario dado de baja o con la contraseña restablecida puede conservar la sesión hasta 30 minutos si recarga antes de que pasen 5. | `servicios.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5))` en `AgregarIdentidad` |
| 4 | IMPORTANTE | arquitectura | src/PlataformaReservas.Infraestructura/InyeccionDependencias.cs:31 | `arquitectura.md` §8.5 ("la aplicación **no arranca**"), T5 | `AnilloClaves` se crea perezosamente, la primera vez que alguien lo resuelve. En `Development` la aplicación falla al arrancar porque `MigrarYSembrarAsync` construye el modelo. En otros entornos arrancaría sin claves y fallaría en la primera petición. Ninguna prueba cubre que no arranque sin claves. | Validar `Cifrado:*` al arrancar (`ValidateOnStart`, o resolver `AnilloClaves` justo después de `Build()`) |
| 5 | IMPORTANTE | arquitectura | src/PlataformaReservas.Infraestructura/PlataformaReservas.Infraestructura.csproj:7-9 | AGENTS §14, `arquitectura.md` §2.2 | Nueva `FrameworkReference Microsoft.AspNetCore.App` en `Infraestructura`. La aprobó el desarrollador al empezar T2 a T7 y consta en fase-06 §11, pero `arquitectura.md` §2.2 no la recoge. | Anotarla en `arquitectura.md` §2.2 con su motivo |
| 6 | IMPORTANTE | arquitectura | src/PlataformaReservas.Infraestructura/InyeccionDependencias.cs:59-63 | `arquitectura.md` §10.1 y §8.3 | Las políticas, `AddAuthentication` y las cookies se registran en `AgregarInfraestructura`. La desviación consta en fase-06 §11, pero el esqueleto de §10.1 sigue poniéndolas en `Program.cs`. | Actualizar `arquitectura.md` §10.1 y §8.3 |
| 7 | IMPORTANTE | arquitectura | src/PlataformaReservas.Web/Cuenta/Pages/ForgotPassword.razor:6 (también Login.razor:8, Register.razor:7, ResetPassword.razor:9, Cuenta/_Imports.razor:5) | `arquitectura.md` §2.1 ("Ningún componente Blazor usa un tipo de `Infraestructura`") | Las páginas de `Web/Cuenta/` inyectan `Usuario`, `UserManager<Usuario>`, `SignInManager<Usuario>` y `SolicitudRestablecimientoClave`. Con las páginas de Identity es inevitable (T1), pero §2.1 no recoge la excepción. | Añadir a §2.1 la excepción acotada a `Web/Cuenta/**` |
| 8 | IMPORTANTE | arquitectura | src/PlataformaReservas.Web/Cuenta/Pages/Register.razor:72-77 | A-04, `arquitectura.md` §9.4 | `CreateAsync` y `AddToRoleAsync` son dos `SaveChanges` sin transacción común. Si falla la asignación del rol, el usuario queda guardado sin rol `Cliente`, y al reintentar el registro el correo ya figura como duplicado. | Envolver las dos llamadas en una transacción de `ContextoDatos`, o anotar en la fase que se acepta el riesgo |
| 9 | IMPORTANTE | alcance | docs/arquitectura.md §2.3, §8.1, §8.4, §13.6 | fase-06 §4 (`arquitectura.md`: solo la fila de §3.3) | Se cambian secciones fuera de las previstas: dónde están las clases de `Identidad/`, `RequireConfirmedAccount = true` y la recuperación de contraseña. Reflejan decisiones del desarrollador anotadas en fase-06 §11. | Ampliar §4 de la fase para incluirlas |
| 10 | MENOR | seguridad | src/PlataformaReservas.Web/Cuenta/IdentityNoOpEmailSender.cs:12,15,18 | AGENTS §8 quinquies, RN-171 | Copiado de la plantilla, monta HTML con interpolación y `resetCode` sin codificar. Hoy no abre ninguna vía, porque el mensaje se descarta (duda 1). | No tomarlo como base de `RemitenteIdentity` en la Fase 16b |
| 11 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Identidad/AnilloClaves.cs:30 | Encapsulación | `internal byte[] ClaveDatos` expone un array mutable con la clave a todo el ensamblado. | `ReadOnlySpan<byte>`, `ReadOnlyMemory<byte>` o una copia |
| 12 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Persistencia/Migraciones/20261003171203_RolesIdentidad.cs:6 | AGENTS §13, `arquitectura.md` §3.3 | `#pragma warning disable CA1814`, generado por `dotnet ef`. La fila de §3.3 solo menciona `612, 618`. | Ampliar la nota de §3.3 a los `#pragma` que genera EF |
| 13 | MENOR | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasIdentidad.cs:113-126 | T8 | La prueba de RN-135 comprueba `IsNotAllowed`. Que el mensaje sea el mismo depende de la rama `else` de `Login.razor`, que no tiene prueba automática. Se comprobó a mano el 2026-10-03. | Dejarlo documentado en la prueba |
| 14 | MENOR | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasIdentidad.cs:143-157 | fase-06 §6 (RN-158) | "Responde igual" se comprueba como "no lanza" a nivel de servicio. La redirección idéntica de `ForgotPassword.razor` no tiene prueba automática. Se comprobó a mano el 2026-10-03. | Dejar constancia en fase-06 §10 |
| 15 | MENOR | arquitectura | src/PlataformaReservas.Web/PlataformaReservas.Web.csproj:1 | CLAUDE.md, "Radio de cambio" | Además del `UserSecretsId`, que es del desarrollador según §3, aparece un BOM en la primera línea. | Quitar el BOM |
| 16 | FALSO POSITIVO | mecánica (C07) | src/PlataformaReservas.Web/Cuenta/Pages/Login.razor:80 | RN-169, R-13 | `lockoutOnFailure: false`. No es un descuido: es la decisión de la duda 10, con la mitigación de R-13 en fase-06 §8, y lo cambia la Fase 19. | Ninguna en esta fase |
| 17 | FALSO POSITIVO | mecánica (C08) | tests/PlataformaReservas.PruebasIntegracion/Infraestructura/BaseDatosFixture.cs:37 | R-3 | La palabra `EnsureCreated` aparece en el comentario que prohíbe usarlo. El código usa `MigrateAsync`. | Afinar C08 para que ignore los comentarios |
| 18 | FALSO POSITIVO | mecánica (C13) | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasCifradoIdentidad.cs:92 | RN-167, R-10 | El LINQ sobre `Users` es deliberado: la prueba de T11 documenta que devuelve vacío. | Ninguna |
| 19 | FALSO POSITIVO | mecánica (C16) | .editorconfig | A-18 | La única supresión nueva (`generated_code` en `Web/Cuenta/**`) ya tiene su fila en `arquitectura.md` §3.3. | Ninguna |

### Criterios de aceptación (§7 de la especificación)

- [x] Registro de cliente, inicio y cierre de sesión con las páginas SSR de la plantilla, sin paquetes nuevos. Comprobado a mano con formularios reales contra la aplicación en marcha el 2026-10-03.
- [x] Un usuario inactivo no puede iniciar sesión, con el mensaje genérico. Prueba `Un_usuario_inactivo_no_puede_iniciar_sesion` y comprobación manual.
- [x] `EsCliente` y `EsPropietario` registradas; `EsPropietario` exige rol y claim. Prueba `EsPropietario_exige_el_rol_y_el_claim_de_empresa`.
- [x] Revalidación cada 5 minutos (A-06) en `RevalidadorIdentidad`. Verificado por lectura de código; ver el hallazgo 3 sobre la cookie.
- [x] La recuperación de contraseña responde igual y renueva el sello. Pruebas de RN-158, RN-160 y RN-161, más la comprobación manual de la redirección idéntica.
- [x] Un `SELECT` directo sobre `asp_net_users` y `reservas` no muestra nada en claro. Pruebas de `PruebasCifradoIdentidad`.
- [x] Inicio de sesión, correo duplicado y recuperación funcionan con el cifrado activo. Pruebas de `PruebasCifradoIdentidad` y `PruebasIdentidad`.
- [x] `empresas` sigue en claro. Prueba `Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro`.
- [x] `ContextoDatos` no se construye sin protector fuera del tiempo de diseño. Prueba `Sin_protector_registrado_el_contexto_no_construye_el_modelo`.
- [x] Ninguna clave en el repositorio, y las dos claves son distintas. Pruebas de `AnilloClaves`; el revisor de seguridad no encontró secretos.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

## Revisión 2026-10-04 16:30

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-06.md (APROBADA)
**Commit base:** a0b6f81 (el desarrollador confirmó la fase como "fase 6"; esta ronda revisa las correcciones de la revisión anterior)
**Ficheros revisados:** 10, más los documentos ignorados por git `docs/arquitectura.md` y `docs/fases/fase-06.md`

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 0 | 1 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 46 | 46 | 0 |
| Integración | 27 | 27 | 0 |

`dotnet build`: 0 avisos, 0 errores. `comprobaciones.sh`: sin hallazgos (`bloqueantes=0`, `importantes=0`).

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | arquitectura | src/PlataformaReservas.Web/PlataformaReservas.Web.csproj:1 | AGENTS §3.2 y §13, fase-06 §4 | El único cambio del fichero es que se quita el BOM UTF-8 de la primera línea. El `.csproj` de `Web` no está en la tabla de §4. **Contexto:** se quitó para atender el hallazgo 15 (MENOR) de la revisión anterior, que lo señalaba como ruido. Las dos rondas se contradicen. | Elegir una de las dos: deshacer el cambio (`git checkout -- src/PlataformaReservas.Web/PlataformaReservas.Web.csproj`) y dejar el BOM; o mantenerlo y añadir el `.csproj` de `Web` a fase-06 §4, donde ya está implícito por el `UserSecretsId` de §3 |
| 2 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Identidad/RegistroCliente.cs:9 | Observación | El literal `"Cliente"` aparece en `RegistroCliente.RolCliente`, en la política `EsCliente` de `InyeccionDependencias.cs` y en el `HasData` de `ConfiguracionesIdentidad.cs`. Solo el primero es una constante. | Opcional: que la política use `RegistroCliente.RolCliente` |

### Estado de los hallazgos de la revisión anterior

| # anterior | Estado |
|---|---|
| 1, 2 (`docker-compose.yml`, `.slnx`) | Resuelto: autorizados por el desarrollador y anotados en fase-06 §4 y §11 |
| 3 (intervalo de la cookie) | Resuelto: `SecurityStampValidatorOptions.ValidationInterval` = 5 min, con prueba |
| 4 (arranque sin claves) | Resuelto: `ComprobarClavesCifrado` tras `Build()`, con prueba |
| 5, 6, 7, 9 (documentos) | Resuelto: `arquitectura.md` §2.1, §2.2, §2.3, §8.3, §10.1 y fase-06 §4 |
| 8 (registro sin transacción) | Resuelto: `RegistroCliente`, con transacción y pruebas del rol y del duplicado |
| 10 a 14 | Resuelto |
| 15 (BOM) | Resuelto, pero da lugar al hallazgo 1 de esta ronda |

### Criterios de aceptación (§7 de la especificación)

- [x] Registro de cliente, inicio y cierre de sesión con las páginas SSR, sin paquetes nuevos. Comprobado de nuevo a mano con formularios reales el 2026-10-04; la base registra el rol `Cliente`.
- [x] Un usuario inactivo no puede iniciar sesión, con el mensaje genérico. Prueba `Un_usuario_inactivo_no_puede_iniciar_sesion` y comprobación manual del 2026-10-03.
- [x] `EsCliente` y `EsPropietario` registradas; `EsPropietario` exige rol y claim. Prueba `EsPropietario_exige_el_rol_y_el_claim_de_empresa`.
- [x] Revalidación cada 5 minutos (A-06) en el circuito y en la cookie. Prueba `La_cookie_revalida_el_sello_cada_cinco_minutos`.
- [x] La recuperación responde igual y renueva el sello. Pruebas de RN-158, RN-160 y RN-161; redirección idéntica comprobada a mano el 2026-10-04.
- [x] Un `SELECT` directo sobre `asp_net_users` y `reservas` no muestra nada en claro. `PruebasCifradoIdentidad`.
- [x] Inicio de sesión, correo duplicado y recuperación funcionan con el cifrado activo. `PruebasCifradoIdentidad` y `PruebasIdentidad`.
- [x] `empresas` sigue en claro. `Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro`.
- [x] `ContextoDatos` no se construye sin protector fuera del tiempo de diseño. `Sin_protector_registrado_el_contexto_no_construye_el_modelo`.
- [x] Ninguna clave en el repositorio y claves distintas. Pruebas de `AnilloClaves` y de arranque sin claves; el revisor de seguridad no encontró secretos.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

## Revisión 2026-10-04 16:43

**Veredicto:** APTO
**Especificación:** docs/fases/fase-06.md (APROBADA)
**Commit base:** a0b6f81
**Ficheros revisados:** 11, más los documentos ignorados por git `docs/arquitectura.md` y `docs/fases/fase-06.md`

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 1 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 46 | 46 | 0 |
| Integración | 27 | 27 | 0 |

`dotnet build`: 0 avisos, 0 errores. `comprobaciones.sh`: sin hallazgos. `dotnet ef migrations has-pending-model-changes`: sin cambios en el modelo.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Identidad/RegistroCliente.cs:9-24 | Observación | `RegistrarAsync` no recibe `CancellationToken`, así que `BeginTransactionAsync` y `CommitAsync` se llaman sin él. | Añadir `CancellationToken ct` y pasarlo a la transacción |

### Estado de los hallazgos de la revisión anterior

| # anterior | Estado |
|---|---|
| 1 (BOM de `Web.csproj`) | Resuelto: BOM restaurado; el fichero es idéntico al commit y no aparece en el diff |
| 2 (literales de rol) | Resuelto: `RolesIdentidad` con roles y nombres de política, usado en `RegistroCliente`, en las políticas, en el `HasData` y en las pruebas. Las migraciones no cambian |

### Criterios de aceptación (§7 de la especificación)

- [x] Registro, inicio y cierre de sesión con las páginas SSR, sin paquetes nuevos. Comprobado a mano con formularios reales el 2026-10-04.
- [x] Un usuario inactivo no puede iniciar sesión, con el mensaje genérico. `Un_usuario_inactivo_no_puede_iniciar_sesion` y comprobación manual.
- [x] `EsCliente` y `EsPropietario` registradas; `EsPropietario` exige rol y claim. `EsPropietario_exige_el_rol_y_el_claim_de_empresa`.
- [x] Revalidación cada 5 minutos en el circuito y en la cookie. `La_cookie_revalida_el_sello_cada_cinco_minutos`.
- [x] La recuperación responde igual y renueva el sello. Pruebas de RN-158, RN-160 y RN-161, más la comprobación manual.
- [x] Un `SELECT` directo sobre `asp_net_users` y `reservas` no muestra nada en claro. `PruebasCifradoIdentidad`.
- [x] Inicio de sesión, correo duplicado y recuperación funcionan con el cifrado activo. `PruebasCifradoIdentidad` y `PruebasIdentidad`.
- [x] `empresas` sigue en claro. `Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro`.
- [x] `ContextoDatos` no se construye sin protector fuera del tiempo de diseño. `Sin_protector_registrado_el_contexto_no_construye_el_modelo`.
- [x] Ninguna clave en el repositorio y claves distintas. Pruebas de `AnilloClaves` y de arranque sin claves; el revisor de seguridad no encontró secretos.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

## Seguimiento 2026-10-12

No es una ronda de revisión: no se ha vuelto a compilar ni a ejecutar las pruebas. Solo registra en qué estado están los hallazgos que la última ronda dejó abiertos, comprobado contra el código el 2026-10-12.

| Hallazgo | Estado | Evidencia |
|---|---|---|
| Menor 1 de la ronda 16:43 (`CancellationToken` en `RegistroCliente.RegistrarAsync`) | **Abierto**: se cierra en la Fase 19 (refactorización) | `src/PlataformaReservas.Infraestructura/Identidad/RegistroCliente.cs:11`: la firma sigue sin `CancellationToken` |
