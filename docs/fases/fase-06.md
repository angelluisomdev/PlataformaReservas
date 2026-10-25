# Fase 06 — Identity: registro, sesión, roles, políticas, cuentas inactivas, recuperación de contraseña, cifrado de datos personales

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 8,5 h (SPEC §19) |
| **Semana** | 2 (`PROJECT_PLAN.md` §2) |
| **Cierra** | Ninguno. Prepara las tres últimas líneas del Control 1, que se cierra al final de la Fase 7 (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-03 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar, un visitante puede registrarse como cliente, iniciar y cerrar sesión, y recuperar su contraseña con las páginas SSR de la plantilla de Identity. Una cuenta inactiva no entra. Las áreas se protegen con las políticas `EsCliente` y `EsPropietario`. Los datos personales del usuario y las copias de `Reserva` se guardan **cifrados desde el primer registro** con la implementación real de las tres interfaces de Identity (D-22).

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 | Contenido y 8,5 h. Del presupuesto, 1 h es recuperación de contraseña (l.1145) y 3 h son cifrado: "las tres interfaces, la opción y los atributos" (l.1147) |
| SPEC §9.1 | `Usuario`: `NombreCompleto` string(120) obligatorio, `Telefono` string(20) opcional, `FechaAlta`, `Activo`. Roles `Cliente` y `Propietario`, acumulables |
| SPEC §11 | Rutas SSR `/login` y `/register` |
| SPEC §15, §15.1 | Hash de Identity, autorización por rol y política, antifalsificación. Qué se cifra, qué no, cómo se busca el correo y gestión de la clave |
| SPEC §21 | "Un `SELECT` directo sobre `asp_net_users` y sobre `reservas` no muestra ningún nombre, correo ni teléfono en claro". "Iniciar sesión, registrar un correo duplicado y recuperar la contraseña siguen funcionando con el cifrado activo" |
| D-18 · A-03 · A-06 | Claim `empresa_id`. `SecurityStamp` con revalidación cada 5 min |
| D-19 · A-04 | `ContextoDatos` scoped desde la fábrica, con el puente ya hecho en la Fase 5 |
| D-22 · A-15 | `ProtectPersonalData` con AES-256-GCM e índice determinista propios, sin paquetes nuevos |
| `arquitectura.md` §8.1 | `AddIdentityCore<Usuario>` con `RequireConfirmedAccount = false`, `RequireUniqueEmail = true`, `RequiredLength = 8`, roles, `AddSignInManager`, `AddClaimsPrincipalFactory<FabricaClaimsUsuario>` y `AddDefaultTokenProviders`. Páginas de cuenta de la plantilla `dotnet new blazor --auth Individual` |
| `arquitectura.md` §8.2 | `FabricaClaimsUsuario` añade `empresa_id` si hay pertenencia |
| `arquitectura.md` §8.3 | Políticas `EsCliente` (rol `Cliente`) y `EsPropietario` (rol `Propietario` y claim `empresa_id`) |
| `arquitectura.md` §8.4 | `RevalidadorIdentidad`, con un `RevalidationInterval` de 5 min. El `SignInManager` comprueba `Activo` |
| `arquitectura.md` §8.5 | `IPersonalDataProtector` (AES-256-GCM, `nonce ‖ tag ‖ texto cifrado` en base64), `ILookupProtector` (HMAC-SHA256 con otra clave) e `ILookupProtectorKeyRing`. Clases en `Infraestructura/Identidad/` |
| `arquitectura.md` §10.1 | `AddCascadingAuthenticationState`, `AuthenticationStateProvider` = `RevalidadorIdentidad`, `UseAuthentication` → `UseAuthorization` → `UseAntiforgery`, `MapAdditionalIdentityEndpoints` |
| `arquitectura.md` §13.6 | Recuperación de contraseña mediante `IEmailSender<Usuario>` |
| `fase-05.md` §11 | "Con el protector real registrado, `ObtenerProtector()` lanzará si falta, salvo en el tiempo de diseño de `dotnet ef`" |
| RN-81 | Teléfono opcional al registrarse |
| RN-135 ⚑ | Un usuario inactivo no puede iniciar sesión. Se comprueba en la autenticación |
| RN-157 | Mismo flujo de recuperación para clientes y propietarios |
| RN-158 ⚑ | La solicitud de restablecimiento responde siempre lo mismo |
| RN-159 | El token caduca y es de un solo uso; lo gestiona Identity |
| RN-160 | Una cuenta inactiva no recibe correo de restablecimiento |
| RN-161 | Restablecer la contraseña renueva el sello de seguridad |
| RN-162 ⚑ | Nombre, correo, nombre de acceso y teléfono del usuario, cifrados con `ProtectPersonalData` |
| RN-163 ⚑ | Las copias de `Reserva`, cifradas con conversores. El conversor existe desde la Fase 5; aquí recibe el protector real |
| RN-164 | Los datos de la empresa no se cifran |
| RN-165 | Las contraseñas llevan hash, no cifrado |
| RN-166 | La búsqueda por correo usa un valor determinista (HMAC) en `NormalizedEmail` |
| RN-167 | Una consulta LINQ sobre un campo protegido devuelve vacío sin error |
| RN-168 | Claves de cifrado y de búsqueda distintas, de 256 bits y fuera del repositorio |
| CU-04 | Registro de cliente con nombre, correo, contraseña y teléfono opcional. Se asigna el rol `Cliente` |
| CU-05 | Inicio de sesión. Una cuenta inactiva se rechaza con un mensaje genérico |
| CU-06 | Cierre de sesión |
| CU-24 | Recuperación de contraseña en `/Account/ForgotPassword` y `/Account/ResetPassword` |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `Infraestructura/Identidad/Usuario.cs` con `[ProtectedPersonalData]` en `NombreCompleto` y `Telefono` | ✔ comprobado |
| `ContextoDatos : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>` y tablas `asp_net_*` | ✔ comprobado |
| Fábrica más puente scoped en `AgregarInfraestructura` (R-1) | ✔ comprobado |
| `ConversorCifrado` y `ConfiguracionReserva` con protector opcional | ✔ comprobado |
| Paquete `Microsoft.AspNetCore.Identity.EntityFrameworkCore` en `Infraestructura` (`arquitectura.md` §2.2) | ✔ comprobado |
| `BaseDatosFixture` con migraciones y `ProtectorFalso` | ✔ comprobado |
| `UserSecretsId` en `PlataformaReservas.Web.csproj` (parte del desarrollador en T0 de la Fase 5) | ✔ hecho por el desarrollador.  |
| Las dos claves de 256 bits, creadas por el desarrollador y respaldadas fuera de la máquina (R-9) | ✔ hecho por el desarrollador. |
| Páginas de cuenta en `Web` | ✘ ausente, como se esperaba: el proyecto se creó con `--empty` (`arquitectura.md` §17). Las aporta T1 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | Nada |
| `src/PlataformaReservas.Aplicacion` | no | Nada. `IContextoEmpresa` y los repositorios son de la Fase 7 |
| `src/PlataformaReservas.Infraestructura` | sí | `Identidad/`: `Usuario`, `ProtectorDatosPersonales`, `ProtectorBusqueda`, `AnilloClaves`, `FabricaClaimsUsuario`, `RevalidadorIdentidad`, `ConfirmacionUsuarioActivo` (RN-135), `SolicitudRestablecimientoClave`, `RegistroCliente` y `RolesIdentidad` (§11). Además, `PlataformaReservas.Infraestructura.csproj` (`FrameworkReference`, §11), `InyeccionDependencias.cs`, `Persistencia/ContextoDatos.cs`, la configuración de roles (`Persistencia/Configuraciones/ConfiguracionesIdentidad.cs`, clase de `IdentityRole<Guid>` ya existente) y `Persistencia/Migraciones/` (migración `RolesIdentidad`) |
| `src/PlataformaReservas.Web` | sí | `Program.cs`, `Cuenta/` (páginas SSR de la plantilla, `arquitectura.md` §2.3), `Components/Routes.razor`, `Components/_Imports.razor`, `Components/Layout/` (enlaces de sesión) y `appsettings.json` (secciones sin secretos) |
| `tests/PlataformaReservas.PruebasUnitarias` | no | Los protectores viven en `Infraestructura`, que este proyecto no referencia |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Identidad/` (pruebas nuevas) y `Infraestructura/BaseDatosFixture.cs` (protector real con claves de prueba) |
| `docs/` | sí | `docs/base-datos.md` §7, `docs/arquitectura.md` §3.3 (fila de la supresión de `Web/Cuenta/**`) y este fichero. Por las decisiones de §11, también `docs/arquitectura.md` §2.1, §2.2, §2.3, §8.1, §8.3, §8.4, §10.1 y §13.6 |
| Raíz | sí | `README.md` (aviso de R-9), `.editorconfig` (solo la sección `generated_code` de `src/PlataformaReservas.Web/Cuenta/**`, duda 5), `docker-compose.yml` (volumen en `/var/lib/postgresql`, `arquitectura.md` §11.4) y `PlataformaReservas.slnx` (`README.md` en *Solution Items*). Estos dos últimos, autorizados por el desarrollador (§11) |

**Todo lo que no figure como "sí" no se toca en esta fase.**

## 5. Tareas

### T1 — Páginas de cuenta de la plantilla (≈ 1 h)

- **Ficheros:** crea `Web/Cuenta/**`, con las páginas `Login`, `Register`, `Logout`, `ForgotPassword`, `ForgotPasswordConfirmation`, `ResetPassword`, `ResetPasswordConfirmation` y los componentes compartidos que necesiten, más `IdentityComponentsEndpointRouteBuilderExtensions` (`MapAdditionalIdentityEndpoints`) y el redirector. Modifica `Components/_Imports.razor` y `Components/Routes.razor` (`AuthorizeRouteView`).
- **Implementa:** `arquitectura.md` §8.1 y §2.3, R-4, CU-05, CU-06.
- **Hecho cuando:**
  - Las páginas vienen de `dotnet new blazor --auth Individual` generado **en una carpeta temporal fuera del repositorio**, y solo se copian las necesarias.
  - Se sustituyen `ApplicationUser` por `Usuario` y `ApplicationDbContext` por `ContextoDatos`.
  - **No entra ningún paquete nuevo**: ni `Identity.UI`, ni `Diagnostics.EntityFrameworkCore`, ni SQLite o SQL Server (`arquitectura.md` §2.2).
  - **No se copian** las páginas de gestión de cuenta (`Manage/*`, 2FA, inicios de sesión externos, `passkeys`), porque no están en el alcance (SPEC §6, `reglas-negocio.md` §13).
  - `Login` y `Register` llevan una segunda directiva `@page "/login"` y `@page "/register"` (duda 4).
  - `src/PlataformaReservas.Web/Cuenta/**` se marca como `generated_code = true` en `.editorconfig`, con su fila en `arquitectura.md` §3.3 (A-18). Solo se traducen al castellano los textos visibles (duda 5).
  - `PasswordSignInAsync` conserva `lockoutOnFailure: false` hasta la Fase 19 (duda 10).
- **Verificación:** `dotnet build` sin avisos. `/login` y `/register` se renderizan en SSR.

### T2 — Alta de `Usuario` (≈ 0,5 h)

- **Ficheros:** modifica `Infraestructura/Identidad/Usuario.cs` y `Persistencia/Configuraciones/ConfiguracionUsuario.cs`.
- **Implementa:** SPEC §9.1, `modelo-dominio.md` §4.1 (invariantes), RN-81.
- **Hecho cuando:**
  - Existe `public static Usuario Crear(string email, string nombreCompleto, string? telefono, DateTime ahoraUtc)`, que asigna `Id = Guid.CreateVersion7()` (duda 7). Valida que `NombreCompleto` no esté vacío y no pase de 120 caracteres, que `Telefono` sea nulo o no vacío de 20 como máximo, y que la fecha sea UTC.
  - Fija `UserName = Email`, `Activo = true` y `FechaAlta`.
  - Las propiedades siguen sin `set` público.
  - `ActualizarPerfil`, `EstablecerTelefono` y `DarDeBaja` **no** se implementan: son de las fases 14 y 15.
- **Verificación:** prueba de integración que crea un usuario con `UserManager.CreateAsync` y lo vuelve a leer.

### T3 — `AnilloClaves` y `ProtectorBusqueda` (≈ 0,75 h)

- **Ficheros:** crea `Infraestructura/Identidad/AnilloClaves.cs` y `Infraestructura/Identidad/ProtectorBusqueda.cs`.
- **Implementa:** A-15, `arquitectura.md` §8.5, RN-166, RN-168.
- **Hecho cuando:**
  - `AnilloClaves` implementa `ILookupProtectorKeyRing` y lee `Cifrado:ClaveDatos` y `Cifrado:ClaveBusqueda` (duda 3), las valida (256 bits, distintas entre sí) y lanza al arrancar si faltan o no son válidas.
  - `ProtectorBusqueda` implementa `ILookupProtector` y aplica HMAC-SHA256 con la clave de búsqueda y es determinista.
  - Usa solo `System.Security.Cryptography`.
- **Verificación:**
  - la misma entrada da siempre la misma salida, y dos entradas distintas dan salidas distintas;
  - una clave de búsqueda igual a la de cifrado, o de longitud distinta de 32 bytes, se rechaza.

### T4 — `ProtectorDatosPersonales` (≈ 0,75 h)

- **Ficheros:** crea `Infraestructura/Identidad/ProtectorDatosPersonales.cs`.
- **Implementa:** A-15, `arquitectura.md` §8.5, RN-162, RN-163.
- **Hecho cuando:** implementa `IPersonalDataProtector` (duda 2). Cifra con AES-256-GCM y un nonce aleatorio por llamada, guarda `nonce ‖ tag ‖ texto cifrado` en base64 y descifra lo que ha cifrado. `null` entra y sale como `null`.
- **Verificación:**
  - ida y vuelta da el original;
  - cifrar dos veces el mismo texto da resultados distintos;
  - un texto cifrado alterado lanza al descifrar.

### T5 — Registro de Identity con cifrado y protector obligatorio (≈ 1 h)

- **Ficheros:** modifica `Infraestructura/InyeccionDependencias.cs` y `Infraestructura/Persistencia/ContextoDatos.cs`. Si el modelo cambia, crea una migración en `Persistencia/Migraciones/` con `dotnet ef migrations add` (`AGENTS.md` §11).
- **Implementa:** `arquitectura.md` §8.1 y §8.5, A-15, `fase-05.md` §11 (#5), R-11.
- **Hecho cuando:**
  - `AddIdentityCore<Usuario>` lleva literalmente las opciones de §8.1, más `Stores.ProtectPersonalData = true`, `AddRoles`, `AddEntityFrameworkStores<ContextoDatos>`, `AddSignInManager`, `AddClaimsPrincipalFactory<FabricaClaimsUsuario>` y `AddDefaultTokenProviders`.
  - Se registran las tres piezas de T3 y T4.
  - `ContextoDatos.ObtenerProtector()` **lanza** si no hay `IPersonalDataProtector`, salvo en el tiempo de diseño de `dotnet ef`. Así deja de guardar en claro sin avisar.
- **Verificación:**
  - `dotnet ef migrations add` funciona sin claves configuradas, o no genera cambios;
  - la aplicación no arranca sin claves.

### T6 — `FabricaClaimsUsuario` (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Identidad/FabricaClaimsUsuario.cs`.
- **Implementa:** A-03, D-18, `arquitectura.md` §8.2.
- **Hecho cuando:** extiende `UserClaimsPrincipalFactory<Usuario, IdentityRole<Guid>>` y añade `Claim("empresa_id", …)` solo si el usuario tiene `MiembroEmpresa`. La pertenencia se lee con una consulta directa a `ContextoDatos.MiembrosEmpresa` por `UsuarioId`; en la Fase 7 pasa a `IRepositorioMiembros` (duda 9).
- **Verificación:** prueba de integración: con pertenencia el principal lleva `empresa_id`; sin ella, no.

### T7 — Roles, políticas, revalidación y canalización (≈ 1 h)

- **Ficheros:** crea `Infraestructura/Identidad/RevalidadorIdentidad.cs`. Modifica `Web/Program.cs` y `Persistencia/Configuraciones/ConfiguracionesIdentidad.cs` (`HasData` de los dos roles con identificadores fijos, duda 6). Crea la migración `RolesIdentidad` con el comando de `AGENTS.md` §11.
- **Implementa:** SPEC §9.1 y §15, `arquitectura.md` §8.3, §8.4 y §10.1, A-06.
- **Hecho cuando:**
  - Existen los roles `Cliente` y `Propietario`, creados por migración.
  - La cookie de la aplicación usa `/login` como ruta de inicio de sesión (duda 4).
  - Las políticas `EsCliente` (`RequireRole("Cliente")`) y `EsPropietario` (`RequireRole("Propietario")` más `RequireClaim("empresa_id")`) están registradas con `AddAuthorizationBuilder`.
  - `RevalidadorIdentidad` usa `RevalidationInterval = TimeSpan.FromMinutes(5)` y está registrado como `AuthenticationStateProvider`.
  - `AddCascadingAuthenticationState`.
  - Canalización en el orden de §10.1: `UseAuthentication` → `UseAuthorization` → `UseAntiforgery`, y `MapAdditionalIdentityEndpoints`.
  - **No se crean** las carpetas `App/` ni `MiCuenta/`: sus pantallas son de fases posteriores.
- **Verificación:** prueba de integración: un principal con rol `Propietario` sin `empresa_id` no cumple `EsPropietario` (`IAuthorizationService`).

### T8 — Una cuenta inactiva no inicia sesión (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Identidad/ConfirmacionUsuarioActivo.cs`, que implementa `IUserConfirmation<Usuario>` (duda 8). Modifica `InyeccionDependencias.cs`.
- **Implementa:** RN-135 ⚑, CU-05, `arquitectura.md` §8.4 (punto 4), `modelo-dominio.md` §6.
- **Hecho cuando:** con `Activo = false`, el inicio de sesión falla con el **mismo** resultado genérico que una contraseña incorrecta. La comprobación está en la autenticación, no en la página.
- **Verificación:** prueba de integración de RN-135 (`reglas-negocio.md` §12). `Activo` se pone a `false` con SQL en la prueba, porque `DarDeBaja` es de la Fase 15.

### T9 — Registro de cliente (≈ 0,5 h)

- **Ficheros:** modifica la página `Register` de `Web/Cuenta/`.
- **Implementa:** CU-04, RN-81, SPEC §9.1.
- **Hecho cuando:**
  - El formulario pide nombre, correo, contraseña y teléfono opcional.
  - Crea el usuario con la fábrica de T2 y le asigna el rol `Cliente`.
  - Un correo duplicado se rechaza (`RequireUniqueEmail`).
  - No se encola ningún correo de bienvenida (RN-154, Fase 11b/16b).
- **Verificación:** prueba de integración: el registro con un correo duplicado falla con el cifrado activo (RN-166).

### T10 — Recuperación de contraseña (≈ 1 h)

- **Ficheros:** copia `IdentityNoOpEmailSender` de la plantilla a `Web/Cuenta/` y lo registra como `IEmailSender<Usuario>` en `Web/Program.cs`. Crea en `PruebasIntegracion/Identidad/` un doble que captura el enlace (duda 1). Si hace falta, modifica `ForgotPassword`.
- **Implementa:** CU-24, RN-157, RN-158 ⚑, RN-159, RN-160, RN-161, `arquitectura.md` §13.6.
- **Hecho cuando:**
  - `/Account/ForgotPassword` responde siempre igual.
  - Solo se genera el enlace para una cuenta existente **y activa**.
  - `/Account/ResetPassword` cambia la contraseña con el token y renueva el sello de seguridad.
- **Verificación:**
  - un correo inexistente y uno válido producen la misma respuesta (RN-158);
  - una cuenta inactiva no genera enlace (RN-160);
  - tras restablecer, el sello ha cambiado (RN-161).

### T11 — Pruebas de cifrado con Identity real (≈ 0,75 h)

- **Ficheros:** crea `PruebasIntegracion/Identidad/PruebasCifradoIdentidad.cs`. Modifica `PruebasIntegracion/Infraestructura/BaseDatosFixture.cs` para que use los protectores reales con claves de prueba generadas en la propia prueba.
- **Implementa:** RN-162, RN-163, RN-164, RN-166, RN-167, R-10, R-11, SPEC §21.
- **Hecho cuando:**
  - Un `SELECT` crudo sobre `asp_net_users` (email, nombre de acceso, nombre y teléfono) y sobre `reservas` no contiene ningún valor en claro.
  - `empresas` sí está en claro.
  - `FindByEmailAsync` encuentra al usuario.
  - `Users.Where(u => u.Email == x)` devuelve vacío: así se documenta RN-167.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion`.

### T12 — Documentación (≈ 0,25 h)

- **Ficheros:** modifica `docs/base-datos.md` §7 y `README.md`.
- **Implementa:** R-9, RN-168, `arquitectura.md` §8.5 ("Gestión de la clave").
- **Hecho cuando:**
  - `base-datos.md` §7 refleja el cifrado activo y el protector obligatorio.
  - El README avisa de que perder la clave equivale a perder las cuentas, explica dónde se configura (sin valores) y que hay que respaldarla fuera de la máquina.
- **Verificación:** lectura.

**Suma: 8,5 h de 8,5 h presupuestadas.** No queda margen. Los riesgos de desviación están en T1 (R-4, aunque `generated_code` evita adaptar el estilo de la plantilla) y en T5 (el tiempo de diseño de `dotnet ef`).

## 6. Pruebas

| Prueba | Tipo | Regla que verifica | Tarea |
|---|---|---|---|
| `ProtectorBusqueda` es determinista y distingue entradas | I | RN-166 | T3 |
| `AnilloClaves` rechaza claves iguales o que no son de 256 bits | I | RN-168 | T3 |
| `ProtectorDatosPersonales`: ida y vuelta, nonce distinto en cada cifrado y fallo si se altera | I | RN-162 | T4 |
| Sin protector registrado, `ContextoDatos` falla en lugar de guardar en claro | I | RN-163, R-11 | T5 |
| `FabricaClaimsUsuario` añade `empresa_id` solo con pertenencia | I | A-03 | T6 |
| `EsPropietario` exige rol y claim | I | SPEC §15, `arquitectura.md` §8.3 | T7 |
| Un usuario inactivo no puede iniciar sesión | I | RN-135 ⚑ | T8 |
| Registrar un correo duplicado falla con el cifrado activo | I | RN-166 | T9 |
| La solicitud de restablecimiento responde igual exista o no la cuenta | I | RN-156, RN-158 ⚑ | T10 |
| Una cuenta inactiva no genera enlace de restablecimiento | I | RN-160 | T10 |
| Restablecer la contraseña renueva el sello de seguridad | I | RN-161 | T10 |
| SQL crudo sobre `asp_net_users` y `reservas`: nada en claro; `empresas` en claro | I | RN-162 ⚑, RN-163 ⚑, RN-164 | T11 |
| `FindByEmailAsync` funciona e iniciar sesión funciona con el cifrado activo; el LINQ directo devuelve vacío | I | RN-166, RN-167 | T11 |
| Las 4 pruebas de arquitectura siguen en verde | A | D-14 | todas |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

La prueba final de cifrado de la Fase 17 (SPEC §19) repetirá la de SQL crudo con los datos completos del sistema.

## 7. Criterios de aceptación

- [x] Registro de cliente, inicio y cierre de sesión funcionan con las páginas SSR de la plantilla, sin paquetes fuera de `arquitectura.md` §2.2. *(comprobado el 2026-10-04: comprobado a mano con formularios reales el 2026-10-04. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] Un usuario inactivo no puede iniciar sesión, y el mensaje es el genérico (RN-135). *(comprobado el 2026-10-04: `Un_usuario_inactivo_no_puede_iniciar_sesion` y comprobación manual. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] `EsCliente` y `EsPropietario` registradas; `EsPropietario` exige rol y claim. *(comprobado el 2026-10-04: `EsPropietario_exige_el_rol_y_el_claim_de_empresa`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] La revalidación del principal es cada 5 minutos (A-06). *(comprobado el 2026-10-04: `La_cookie_revalida_el_sello_cada_cinco_minutos`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] La recuperación de contraseña responde de forma indistinguible y renueva el sello (RN-158, RN-161). *(comprobado el 2026-10-04: `PruebasSolicitudRestablecimientoClave` (RN-158, RN-160 y RN-161) y comprobación manual. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] **Un `SELECT` directo sobre `asp_net_users` y sobre `reservas` no muestra ningún nombre, correo ni teléfono en claro** (SPEC §21). *(comprobado el 2026-10-04: `PruebasCifradoIdentidad`, hoy repartida en `Identidad/PruebasUsuario` y `Persistencia/Configuraciones/PruebasConfiguracionReserva`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] **Iniciar sesión, registrar un correo duplicado y recuperar la contraseña siguen funcionando con el cifrado activo** (SPEC §21). *(comprobado el 2026-10-04: `PruebasUsuario`, `PruebasRegistroCliente` y `PruebasSolicitudRestablecimientoClave`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] Los datos de `empresas` siguen en claro (RN-164). *(comprobado el 2026-10-04: `Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] `ContextoDatos` ya no puede construirse sin protector fuera del tiempo de diseño (`fase-05.md` §11). *(comprobado el 2026-10-04: `Sin_protector_registrado_el_contexto_no_construye_el_modelo`. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] Ninguna clave en el repositorio; las dos claves son distintas (RN-168). *(comprobado el 2026-10-04: pruebas de `AnilloClaves` y de arranque sin claves; el revisor de seguridad no encontró secretos. `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] `dotnet build` sin avisos y `dotnet test` en verde. *(comprobado el 2026-10-04: 0 avisos; 46 pruebas unitarias y 27 de integración en verde. `fase-06-revision.md`, ronda 2026-10-04 16:43)*

**Esta fase no cierra ningún punto de control.** Deja preparadas estas tres líneas del Control 1 (`PROJECT_PLAN.md` §3), que se comprueba al final de la Fase 7:

```text
Un SELECT directo sobre asp_net_users no muestra correos ni telefonos en claro
Iniciar sesion, registrar correo duplicado y recuperar contrasena funcionan
La clave de cifrado esta respaldada FUERA de la maquina
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | `AddDbContextFactory` a secas rompe el inicio de sesión | El puente scoped ya existe desde la Fase 5. T5 no lo toca |
| R-4 | Formularios que no envían, antiforgery, bucles de redirección | T1 parte de la plantilla oficial y no reimplementa páginas. Orden de middleware literal de §10.1 (T7) |
| R-9 ⚠ | Pérdida de la clave: nadie puede iniciar sesión, de forma irreversible | Claves solo en User Secrets o variables de entorno. Respaldo fuera de la máquina a cargo del desarrollador. Aviso en el README (T12) |
| R-10 | Una consulta LINQ sobre un campo protegido devuelve vacío sin error | Siempre `FindByEmailAsync`. Una prueba de T11 deja documentado el comportamiento |
| R-11 | Usuario cifrado y copias de `Reserva` en claro | T5 hace obligatorio el protector y T11 comprueba `reservas` con SQL crudo |
| R-13 | `lockoutOnFailure: false` deja sin efecto el bloqueo | Decidido: se mantiene `false` hasta la Fase 19, que lo cambia junto con las opciones de `Lockout` (duda 10) |

Los demás (R-2, R-3, R-5 a R-8, R-12 y R-14) no aplican: tratan de fechas, concurrencia, `Guid` v7, eventos, correo, CSP o limitación de frecuencia.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `IContextoEmpresa`, `ContextoEmpresa`, repositorios y pruebas de aislamiento | Fase 7 |
| Alta de empresa con propietario (CU-11, RN-15) | Fase 8 |
| Teléfono pedido al reservar (`EstablecerTelefono`, RN-81 segunda mitad) | Fase 14 |
| Perfil y baja de cuenta (CU-22, `ActualizarPerfil`, `DarDeBaja`, RN-136 a RN-139) | Fase 15 |
| Eventos `CuentaRegistrada` y `CuentaDadaDeBaja` | Fase 11b |
| `RemitenteIdentity` real, correo de bienvenida y plantillas (RN-154, RN-156) | Fase 16b |
| Bloqueo por intentos (E-1, RN-169, RN-170), registro de intentos fallidos (E-4, RN-172) y limitación de frecuencia (E-5, RN-173) | Fase 19 |
| Prueba final de cifrado con todo el sistema | Fase 17 |
| Gestión de cuenta de la plantilla (`Manage/*`), 2FA, inicios de sesión externos y verificación de correo | Fuera del alcance del MVP (SPEC §6, `reglas-negocio.md` §13, `arquitectura.md` §8.1) |
| Usuarios y empresas de demostración | Sin fase asignada (pendiente desde `fase-05.md`, duda 6) |
| Rotación de claves | Trabajo futuro: el anillo la deja preparada (SPEC §15.1) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-81 (registro) | T2, T9 | Registro con correo duplicado (T9) y alta con `UserManager` (T2) |
| RN-135 ⚑ | T8 | Un usuario inactivo no puede iniciar sesión (`IsNotAllowed`). Que el mensaje sea el mismo que con contraseña incorrecta lo garantiza la rama `else` de `Login.razor`; comprobado a mano con formularios reales el 2026-10-03 |
| RN-157 | T10 | Lo cubre la de RN-158: no hay distinción por rol en el flujo |
| RN-158 ⚑ | T10 | Respuesta indistinguible: `SolicitarAsync` no devuelve nada y solo envía para cuentas existentes y activas. La redirección idéntica la garantiza `ForgotPassword.razor`, que redirige siempre; comprobado a mano el 2026-10-03 |
| RN-159 | T10 | Identity (`AddDefaultTokenProviders`); sin prueba propia, porque no se implementa a mano |
| RN-160 | T10 | Una cuenta inactiva no genera enlace |
| RN-161 | T10 | El sello cambia tras restablecer |
| RN-162 ⚑ | T4, T5, T11 | SQL crudo sobre `asp_net_users` |
| RN-163 ⚑ | T5, T11 | SQL crudo sobre `reservas`, y fallo sin protector |
| RN-164 | T11 | `empresas` en claro |
| RN-165 | T5 | Identity con su hash por defecto; sin prueba propia |
| RN-166 | T3, T9, T11 | Determinismo del HMAC, `FindByEmailAsync` y correo duplicado |
| RN-167 | T11 | El LINQ directo devuelve vacío |
| RN-168 | T3 | El anillo rechaza claves iguales o inválidas |
| CU-04 | T9 | Correo duplicado |
| CU-05 | T1, T8 | Usuario inactivo; inicio de sesión con cifrado (T11) |
| CU-06 | T1 | Sin prueba automática: lo trae la plantilla (`/Account/Logout`) |
| CU-24 | T10 | Las tres de T10 |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-10-03, con la opción recomendada. Se conservan para dejar rastro de cada decisión.

### Duda 1 — Remitente de la recuperación de contraseña

- **Pregunta:** `arquitectura.md` §13.6 define `RemitenteIdentity(IServicioEmail, IRenderizadorPlantillas)`, pero esas dos dependencias son de la Fase 16b. SPEC §19 pone 1 h de recuperación en esta fase, y el Control 1 exige que funcione. ¿Qué `IEmailSender<Usuario>` se registra ahora?
- **Opciones:**
  - (a) en la aplicación, el `IdentityNoOpEmailSender` que trae la plantilla; en las pruebas, un doble que captura el enlace. `RemitenteIdentity` llega en la 16b y sustituye al de la plantilla;
  - (b) un remitente provisional que escriba el enlace en el registro. Choca con SPEC §15 y `arquitectura.md` §11.2: "nunca se registran […] tokens";
  - (c) adelantar `RemitenteIdentity` con un `IServicioEmail` mínimo. Amplía el alcance hacia la 16b.
- **Recomendación y motivo:** (a). No inventa piezas, no registra tokens y deja el flujo probado de punta a punta con el doble. Lo único que falta hasta la 16b es el envío real, que RN-145 tampoco quiere en desarrollo.
- **Bloquea:** T10.
- **Resolución:** opción (a), el 2026-10-03. En la aplicación, `IdentityNoOpEmailSender` de la plantilla; en las pruebas, un doble que captura el enlace. `RemitenteIdentity` llega en la Fase 16b.

### Duda 2 — Qué interfaz implementa cada clase

- **Pregunta:** `arquitectura.md` §2.3 nombra `ProtectorDatosPersonales`, `ProtectorBusqueda` y `AnilloClaves`, y §8.5 enumera tres interfaces, pero no las empareja.
- **Opciones:** (a) `ProtectorDatosPersonales` implementa `IPersonalDataProtector`, `ProtectorBusqueda` implementa `ILookupProtector` y `AnilloClaves` implementa `ILookupProtectorKeyRing`; (b) otro emparejamiento.
- **Recomendación y motivo:** (a). Es el orden en que se citan los dos documentos y lo que sugiere cada nombre.
- **Bloquea:** T3, T4.
- **Resolución:** opción (a), el 2026-10-03. `ProtectorDatosPersonales` → `IPersonalDataProtector`, `ProtectorBusqueda` → `ILookupProtector`, `AnilloClaves` → `ILookupProtectorKeyRing`.

### Duda 3 — Nombre de configuración de las dos claves

- **Pregunta:** RN-168 y §8.5 piden dos claves de 256 bits desde User Secrets o variables de entorno, pero no dicen cómo se llaman ni en qué formato van.
- **Opciones:** (a) sección `Cifrado` con `ClaveDatos` y `ClaveBusqueda`, cada una en base64 de 32 bytes (en entorno: `Cifrado__ClaveDatos` y `Cifrado__ClaveBusqueda`); (b) otros nombres que indique el desarrollador.
- **Recomendación y motivo:** (a). Castellano, coherente con `Correo:Habilitado` y fácil de generar (`openssl rand -base64 32`). Habría que anotarlo en `arquitectura.md` §11.3, en la tabla de secretos.
- **Bloquea:** T3, T5, T11, T12.
- **Resolución:** opción (a), el 2026-10-03. Sección `Cifrado` con `ClaveDatos` y `ClaveBusqueda`, 32 bytes en base64 cada una (`Cifrado__ClaveDatos` y `Cifrado__ClaveBusqueda` como variables de entorno). **Documento actualizado:** `arquitectura.md` §8.5 (tabla de claves), §11.3 y §11.4 (tabla de secretos y procedimiento de generación y copia de seguridad).

### Duda 4 — Rutas de inicio de sesión y registro

- **Pregunta:** SPEC §11 y `casos-uso.md` usan `/login` y `/register`. La plantilla genera `/Account/Login` y `/Account/Register`. CU-24 sí usa las rutas de la plantilla (`/Account/ForgotPassword`).
- **Opciones:** (a) añadir una segunda directiva `@page "/login"` y `@page "/register"` a esas dos páginas, y fijar la ruta de inicio de sesión de la cookie en `/login`; (b) renombrar las rutas de la plantilla; (c) dejar las de la plantilla y corregir SPEC §11.
- **Recomendación y motivo:** (a). Cumple el SPEC sin tocar los enlaces internos de la plantilla, que apuntan a `/Account/...` (R-4). Las demás páginas de la plantilla se quedan en `/Account/...`, como ya asume CU-24.
- **Bloquea:** T1, T7.
- **Resolución:** opción (a), el 2026-10-03. Segunda directiva `@page "/login"` y `@page "/register"`; la cookie usa `/login` como ruta de inicio de sesión. El resto de páginas siguen en `/Account/...`.

### Duda 5 — Estilo e idioma del código de la plantilla

- **Pregunta:** el código de la plantilla choca con `.editorconfig`: usa `var`, comentarios (AGENTS §3.10) y nombres en inglés. Con `TreatWarningsAsErrors` no compilará. Además, los textos de las páginas están en inglés.
- **Opciones:**
  - (a) adaptar a mano las páginas copiadas a las reglas de estilo y traducir los textos visibles;
  - (b) marcar `Web/Cuenta/**` como código generado en `.editorconfig`, igual que las migraciones (A-18, con su fila en §3.3), y traducir solo los textos visibles;
  - (c) como (b), dejando los textos en inglés.
- **Recomendación y motivo:** (b). Unas 10 páginas de plantilla adaptadas línea a línea no caben en 1 h (T1), y el riesgo R-4 crece con cada cambio. Los nombres de tipos de la plantilla (`IdentityRedirectManager` y similares) se mantienen, como en `ValueObjects`. Traducir los textos visibles es lo coherente con una interfaz en castellano.
- **Bloquea:** T1, T9.
- **Resolución:** opción (b), el 2026-10-03. `src/PlataformaReservas.Web/Cuenta/**` como `generated_code = true` en `.editorconfig`, con su fila en `arquitectura.md` §3.3. Se traducen al castellano solo los textos visibles.

### Duda 6 — Cómo se crean los roles `Cliente` y `Propietario`

- **Pregunta:** los roles tienen que existir en cualquier entorno, no solo en desarrollo. Los datos iniciales son solo de `Development` (RN-123).
- **Opciones:** (a) `HasData` en la configuración de `IdentityRole<Guid>`, con dos identificadores fijos, que entra en una migración; (b) crearlos al arrancar, de forma idempotente, en todos los entornos.
- **Recomendación y motivo:** (a). El esquema es la fuente de verdad, igual que el `EXCLUDE`, y no añade trabajo al arrancar. `NormalizedName` va en mayúsculas porque no es un dato personal.
- **Bloquea:** T7, T9.
- **Resolución:** opción (a), el 2026-10-03. `HasData` con dos identificadores fijos en la configuración de `IdentityRole<Guid>` y migración `RolesIdentidad`.

### Duda 7 — Fábrica de `Usuario`

- **Pregunta:** `modelo-dominio.md` §4.1 no define cómo se crea un `Usuario`, y sus propiedades tienen `set` privado (resolución de la duda 3 de la Fase 5). El registro necesita construirlo.
- **Opciones:** (a) `public static Usuario Crear(string email, string nombreCompleto, string? telefono, DateTime ahoraUtc)`, que valida las invariantes de §4.1, como las demás fábricas; (b) constructor público con esos parámetros.
- **Recomendación y motivo:** (a). Mismo patrón que el resto de entidades (AGENTS §7). El `Id` lo asigna `IdentityUser<Guid>` salvo que se fije: se propone `Guid.CreateVersion7()` dentro de la fábrica (D-16).
- **Bloquea:** T2, T9.
- **Resolución:** opción (a), el 2026-10-03. `public static Usuario Crear(string email, string nombreCompleto, string? telefono, DateTime ahoraUtc)`, con `Id = Guid.CreateVersion7()`.

### Duda 8 — Dónde se comprueba `Activo` en la autenticación

- **Pregunta:** `arquitectura.md` §8.4 dice que "`SignInManager` comprueba además `Activo` al iniciar sesión", pero no nombra la pieza.
- **Opciones:**
  - (a) una clase `SignInManager<Usuario>` derivada, que sobrescribe `CanSignInAsync`, por ejemplo `GestorInicioSesion`;
  - (b) una implementación de `IUserConfirmation<Usuario>`, por ejemplo `ConfirmacionUsuarioActivo`, que Identity consulta dentro de `CanSignInAsync`;
  - (c) otro nombre que indique el desarrollador.
- **Recomendación y motivo:** (b). Es el punto de extensión previsto por Identity para "¿puede este usuario entrar?" y no hereda de una clase grande. Hay que fijar el nombre.
- **Bloquea:** T8.
- **Resolución:** opción (b), el 2026-10-03. `ConfirmacionUsuarioActivo : IUserConfirmation<Usuario>`, en `Infraestructura/Identidad/`.

### Duda 9 — `FabricaClaimsUsuario` antes que `IRepositorioMiembros`

- **Pregunta:** §8.2 obtiene la pertenencia con `_miembros.ObtenerPorUsuarioAsync`, pero `IRepositorioMiembros` es de la Fase 7.
- **Opciones:** (a) en esta fase la fábrica consulta `ContextoDatos.MiembrosEmpresa` filtrando por `UsuarioId`, y en la Fase 7 pasa a usar el repositorio; (b) la fábrica entera se mueve a la Fase 7.
- **Recomendación y motivo:** (a). La política `EsPropietario` de esta fase necesita a alguien que emita el claim. Además, la consulta es por `UsuarioId`, no por empresa, así que no es una consulta de inquilino. Deja 0,5 h en esta fase tal como está presupuestada.
- **Bloquea:** T6.
- **Resolución:** opción (a), el 2026-10-03. Consulta directa a `ContextoDatos.MiembrosEmpresa` por `UsuarioId`; en la Fase 7 pasa a `IRepositorioMiembros`.

### Duda 10 — `lockoutOnFailure` en la página de inicio de sesión

- **Pregunta:** la plantilla llama a `PasswordSignInAsync(..., lockoutOnFailure: false)`. El bloqueo (E-1, RN-169) es de la Fase 19, pero R-13 avisa de que ese parámetro se olvida.
- **Opciones:** (a) dejarlo en `false` y que la Fase 19 lo cambie junto con las opciones de `Lockout`; (b) ponerlo ya en `true`, lo que activa desde ahora el bloqueo por defecto de Identity (5 intentos y 5 minutos) antes de configurarlo.
- **Recomendación y motivo:** (a). No adelanta un comportamiento de la Fase 19 con valores que no son los documentados (15 min). La prueba de RN-169 de esa fase detecta el olvido.
- **Bloquea:** T1.
- **Resolución:** opción (a), el 2026-10-03. `lockoutOnFailure: false` hasta la Fase 19, que lo cambia junto con las opciones de `Lockout`.

### Decisiones tomadas durante la implementación (2026-10-03)

- **`FrameworkReference` a `Microsoft.AspNetCore.App` en `Infraestructura` (T5, T7).** `AddSignInManager`, `SignInManager` y `RevalidatingServerAuthenticationStateProvider` son del marco de ASP.NET Core. No es un paquete NuGet. Así Identity y `RevalidadorIdentidad` quedan donde dicen `arquitectura.md` §2.3 y §10.1.
- **Políticas `EsCliente` y `EsPropietario` registradas en `AgregarInfraestructura`, no en `Program.cs` (T7).** Se aparta de `arquitectura.md` §10.1 para que la prueba de integración ejercite el registro real. Junto a ellas van `AddAuthentication` con las cookies de Identity y la ruta `/login`.

- **`RequireConfirmedAccount = true` con `ConfirmacionUsuarioActivo` (T8).** Identity solo consulta `IUserConfirmation<Usuario>` cuando la opción está activa. "Confirmada" pasa a significar "activa"; el correo sigue sin verificarse. Se corrigen `arquitectura.md` §8.1, §8.4 y §13.6.
- **`SolicitudRestablecimientoClave` en `Infraestructura/Identidad/` (T10).** La página `ForgotPassword` delega en ella para que las pruebas de integración ejerciten RN-158 y RN-160 sin un host web, y para que la regla no viva en `Web` (AGENTS §6). Nombre nuevo, añadido a `arquitectura.md` §2.3.
- **`BaseDatosFixture.CrearContexto` sigue con `ProtectorFalso` (T11).** Lo usa la prueba de ida y vuelta de la Fase 5 (`Persistencia/PruebasEsquema.cs`, fuera del radio). Las pruebas de Identity y de cifrado usan `ServiciosIdentidad`, con los protectores reales y claves aleatorias.
- **`README.md` se crea (T12)**: no existía en la raíz. Solo lleva el título y la sección de claves (R-9). El 2026-10-03 el desarrollador lo amplía con la descripción del proyecto, la arquitectura y la puesta en marcha; la sección de claves y el aviso de R-9 se conservan en "Puesta en marcha → Claves de cifrado".

### Decisiones tomadas tras la revisión 1 (2026-10-04)

- **`docker-compose.yml` y `PlataformaReservas.slnx`, autorizados por el desarrollador.** El primero corrige el volumen para PostgreSQL 18 (`arquitectura.md` §11.4, revisión de la Fase 5); el segundo añade `README.md` a la solución.
- **`RegistroCliente` en `Infraestructura/Identidad/` (T9, hallazgo 8).** Crea el usuario y le asigna el rol `Cliente` en una sola transacción de `ContextoDatos`. `Register.razor` delega en ella, y la asignación del rol pasa a tener prueba.
- **La cookie revalida el sello cada 5 minutos (hallazgo 3).** `SecurityStampValidatorOptions.ValidationInterval` toma el mismo intervalo que `RevalidadorIdentidad` (A-06).
- **`ComprobarClavesCifrado` al arrancar (hallazgo 4).** `Program.cs` resuelve `AnilloClaves` justo después de `Build()`: sin claves, la aplicación no arranca en ningún entorno (`arquitectura.md` §8.5).
- **`IdentityNoOpEmailSender` sin HTML (hallazgo 10).** Los tres métodos devuelven `Task.CompletedTask`.

### Decisiones tomadas tras la revisión 2 (2026-10-04)

- **`RolesIdentidad` en `Infraestructura/Identidad/` (hallazgo 2).** Constantes con los nombres de los roles `Cliente` y `Propietario` y de las políticas `EsCliente` y `EsPropietario`, usadas en `RegistroCliente`, en el registro de políticas, en el `HasData` de roles y en las pruebas. No es un enum en `Dominio`: son roles de Identity, que los maneja como cadenas, y `Dominio` ya tiene `RolMiembro`, que es otro concepto. Las migraciones generadas no cambian.
- **El BOM de `PlataformaReservas.Web.csproj` se restaura (hallazgo 1).** Quitarlo fue un cambio cosmético fuera del radio; el fichero vuelve a ser idéntico al del commit `a0b6f81`.

### Informativas (no bloquean)

- `reglas-negocio.md` cruza las referencias de CU: RN-130, RN-131 y RN-153 citan CU-22/CU-23 al revés que `casos-uso.md` (CU-22 es perfil y baja de cuenta; CU-23, baja de empresa), y RN-136, RN-137 y RN-155 también.
- `reglas-negocio.md` l.397 todavía dice "consecuencia de no tener notificaciones", lo que contradice RN-150 y RN-151.
- `casos-uso.md` l.53 habla de 21 CU y 15 rutas; l.693, de 24 CU y 16 vistas.
- `arquitectura.md` §14.5 (E-5) usa `/login` y `/register`. Si se elige la opción (a) de la duda 4, encaja.
- `arquitectura.md` §8.5 muestra `Usuario` con `set` públicos, contra la resolución de la duda 3 de la Fase 5.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos *(2026-10-04: 0 avisos, 0 errores; `fase-06-revision.md`, ronda 2026-10-04 16:43)*
- [x] `dotnet test` en verde *(2026-10-04: 46 unitarias y 27 de integración)*
- [x] `/revisar` sin hallazgos bloqueantes *(2026-10-04 16:43: `APTO` en la tercera ronda. Queda 1 menor abierto: `CancellationToken` en `RegistroCliente.RegistrarAsync`; ver el seguimiento de 2026-10-12 en `fase-06-revision.md`)*
- [x] Commit hecho *(`a0b6f81` y `85aeb98`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Punto de control superado, si esta fase cierra alguno (no cierra ninguno) *(no aplica)*
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna) *(no aplica)*
