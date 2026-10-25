# Fase 19 — Endurecimiento (6 medidas), refactorización, revisión de seguridad y `docs/seguridad.md`

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 7,25 h (SPEC §19, l.1111): 2,75 h de las seis medidas, 1,5 h de refactorización, 2,5 h de revisión de seguridad y 0,5 h de `docs/seguridad.md` (SPEC l.944). **No se recorta** (SPEC l.1147) |
| **Semana** | 6 (`PROJECT_PLAN.md` §2, l.129-140) |
| **Cierra** | **Control 5** (`PROJECT_PLAN.md` l.205-215) |
| **Generada** | 2026-10-25 con `/fase`, con un enfoque doble: arquitectura .NET (Clean Architecture, SOLID, Clean Code) y auditoría de seguridad de código y de aplicación |
| **Aprobada por** | — |

> **Nota de historial:** la Fase 18 se confirmó primero como `ad4ed97`, titulado "fase 19" por error. El desarrollador lo renombró: hoy es `b941d79 "fase 18"` en `main`, y la Fase 19 parte de ahí.

---

## 1. Objetivo

Al terminar, la aplicación está endurecida y su seguridad documentada:

- **Las seis medidas de D-23**, implementadas y probadas:
  - E-1, bloqueo;
  - E-2, auditoría de dependencias;
  - E-3, escapado;
  - E-4, registro de intentos fallidos;
  - E-5, limitación de frecuencia;
  - E-6, cabeceras y CSP.
- **Las vulnerabilidades confirmadas** en la revisión de seguridad, corregidas. El resto de hallazgos queda documentado como limitación, con su motivo.
- **La deuda menor de las fases anteriores**, saldada o registrada como deuda técnica (`PROJECT_PLAN.md` l.275).
- **`docs/seguridad.md`**, con la tabla OWASP de SPEC §15.3 respaldada fila a fila por una prueba o un fichero (SPEC l.971).

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| D-23 (SPEC l.130) | "Las seis medidas de §15.2 **entran todas** en el alcance, incluida la CSP" |
| SPEC §15.2 (l.931-946) | E-1 a E-6 con sus horas (0,25 + 0,25 + 0,5 + 0,25 + 0,75 + 0,75). La CSP es "la medida arriesgada": va la última, se prueba a mano y, si pasa de 1 h, se saca y se documenta |
| SPEC §15.3 (l.948-971) | Tabla OWASP Top 10 (2021). `docs/seguridad.md` la desarrolla citando la prueba o el fichero de cada fila (l.971) |
| SPEC §18 (l.1076), §19 (l.1111, l.1133, l.1147) | `seguridad.md` es de la Fase 19; reparto de las 7,25 h; la fase no se recorta |
| SPEC §21 (l.1215, l.1222-1227) | Nomenclatura de §3 en todo el código; bloqueo registrado; `429`; cabeceras con la CSP activa; `<script>` que no se ejecuta; auditoría de dependencias; `seguridad.md` |
| SPEC §21bis (l.1255, l.1269) | La CSP se saca si no es compatible; RN-174 como limitación declarada |
| A-16 (`arquitectura.md` l.30) | E-1 a E-6 todas dentro; `AddRateLimiter` del marco, sin paquetes; `MarkupString` prohibido |
| `arquitectura.md` §10.1 (l.828) | `builder.Host.UseSerilog(...)` en el esqueleto de `Program.cs`. Hoy no está conectado (H-14) |
| `arquitectura.md` §11.2 | Qué se registra; "nunca se registran contraseñas, sellos, tokens ni datos personales innecesarios" |
| `arquitectura.md` §14.1-§14.6 (l.1314-1429) | Configuración concreta de E-1 a E-6 |
| `arquitectura.md` §15: R-12, R-13 y R-14 | Ver §8 |
| AGENTS §8 quinquies | `MarkupString` prohibido; `lockoutOnFailure: true`; mismo mensaje con la cuenta bloqueada; correo enmascarado; `UseRateLimiter` antes de `UseAuthentication`; CSP probada a mano |
| RN-169 ⚑, RN-170 | Bloqueo de 5 intentos durante 15 min; mismo mensaje genérico (I) |
| RN-171 ⚑ | Sin `MarkupString` en plantillas ni en la ficha; texto de usuario codificado (I) |
| RN-172 | Intento fallido con nivel `Warning`, correo enmascarado e IP (I) |
| RN-173 ⚑, RN-174 | 5 intentos en 5 min en autenticación; 60 por minuto en el buscador; `429`; en memoria y por instancia (I y —) |
| RN-175 | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP y HSTS fuera de desarrollo (I) |
| RN-176 | La auditoría de dependencias vulnerables hace fallar la compilación, incluidas las transitivas (—) |
| RN-158 ⚑, RN-165 | Respuesta indistinguible en la recuperación; nada de datos personales en los registros |
| `reglas-negocio.md` §12 (l.384-386) | RN-169: "la sexta con la contraseña correcta también falla"; RN-171: `<script>alert(1)</script>` escapado en la ficha y en el correo; RN-173: `429` en `/login` y `/businesses` |
| Control 5 (`PROJECT_PLAN.md` l.205-215) | Las cinco comprobaciones (ver §7) |
| `PROJECT_PLAN.md` §6 (l.275-276) | Menores abiertos dentro de la 1,5 h de refactorización (lo que no quepa, como deuda técnica con su motivo); compatibilidad de la CSP |
| Pendientes heredados | `fase-06-revision.md:161`, `CancellationToken` en `RegistroCliente`. `fase-07-revision.md:107`, comentario en `RepositorioMiembros`. `fase-08-revision.md:173`, `@using` de objetos de valor; `:159`, `/app` con la CSP. `fase-13.md`, duda 14, `catalogo` también en `/businesses/{slug}`. `fase-16.md:299`, motivo de "no cancelable" en el DTO. `fase-16b.md:357-358` y `:396`, revisión E-3 completa, limitación en `/Account/ForgotPassword`, `AllowedHosts` y tiempo de respuesta. `fase-17.md`, duda 6, RN-171 en la ficha. Auditoría 14, caso 7: empresa inexistente en `CrearReserva`. `fase-18.md`, duda 1, E-2 como `NuGetAudit` |
| Auditoría de seguridad previa, H-01 a H-24 (§11, "Entrada de la revisión") | Revisión de código y de aplicación (OWASP Top 10 2021 y ASVS) hecha al generar esta especificación |
| Revisión de arquitectura previa, R1 a R23 (§11, "Entrada de la refactorización") | Candidatos de refactorización priorizados |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| Fase 18 en el repositorio (`ad4ed97`) y suites en verde: 448 unitarias y 439 de integración | ✔ comprobado |
| `TreatWarningsAsErrors` y `CentralPackageTransitivePinningEnabled` (`Directory.Build.props`, `Directory.Packages.props:4`) | ✔ comprobado |
| Enmascarado de correo reutilizable: `ServicioEmailRegistro.Enmascarar` (`Infraestructura/Correo/ServicioEmailRegistro.cs:16-20`, `internal`) | ✔ comprobado (duda 1) |
| Constantes de política `RolesIdentidad.PoliticaPropietario` y `PoliticaCliente` | ✔ comprobado |
| `HtmlRenderer`, ya usado por `RenderizadorPlantillas` | ✔ comprobado |
| `Serilog.AspNetCore` y `Serilog.Sinks.Console` referenciados en `Web` | ✔ comprobado; **sin conectar** (duda 4) |
| Opciones de bloqueo en `AddIdentityCore` (`InyeccionDependencias.cs:103-109`) | ✘ no existen (T1) |
| `lockoutOnFailure: true` en el inicio de sesión | ✘ `Login.razor:80` usa `false` (T1) |
| `NuGetAudit`, limitación de frecuencia, cabeceras de seguridad | ✘ no existen (T2, T4, T5) |
| `docs/seguridad.md` | ✘ no existe (T12) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| Raíz del repositorio | sí | `Directory.Build.props` (`NuGetAudit`, T2). `docker-compose.yml`: puerto de `postgres` solo en `127.0.0.1` (H-11, T11) |
| `src/PlataformaReservas.Dominio` | no | — |
| `src/PlataformaReservas.Aplicacion` | sí | `Dtos/Reservas/DetalleReservaClienteDto.cs` (R4) y `Dtos/Reservas/PanelEmpresaDto.cs` (R7) |
| `src/PlataformaReservas.Infraestructura` | sí | `InyeccionDependencias.cs` (bloqueo, T1). `Identidad/InicioSesion.cs` (nuevo, duda 1). `Identidad/RegistroCliente.cs` (R1). `Correo/ServicioEmailRegistro.cs` (el enmascarado pasa a un sitio compartido, duda 1). `Repositorios/RepositorioMiembros.cs` (R2). `Consultas/ConsultasReservasEmpresa.cs` (R6, R7). `Consultas/ConsultasCatalogoPublico.cs` (R10). Los puntos de registro de la duda 4: los repositorios de empresa en la defensa en profundidad, el traductor del `23P01`, `SolicitudRestablecimientoClave` y `ServicioCuentas` (bajas) |
| `src/PlataformaReservas.Web` | sí | `Program.cs` (Serilog, limitación, cabeceras, R11). `Cuenta/Pages/Login.razor` (usa `InicioSesion`). `Cuenta/Pages/Register.razor` (R1, R9). `Cuenta/Pages/ResetPassword.razor` (T9). `Cuenta/IdentityRedirectManager.cs` (T8). `Components/App.razor` (duda 3). `Components/App/_Imports.razor`, `Components/MiCuenta/_Imports.razor` y `Components/Layout/MainLayout.razor` (R3, R8). `Components/MiCuenta/DetalleReserva.razor` y las páginas que necesiten el `@using` de R3. `LimitacionFrecuencia.cs` y `CabecerasSeguridad.cs` (nuevos, dudas 2 y 3). `appsettings*.json`, solo la sección de Serilog (duda 4) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Dtos/Reservas/PruebasDetalleReservaClienteDto.cs` (R4) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Identidad/PruebasInicioSesion.cs`, `PruebasLimitacionFrecuencia.cs`, `PruebasCabecerasSeguridad.cs`, `Cuenta/PruebasIdentityRedirectManager.cs` y `Components/Publico/PruebasFichaEstablecimiento.cs` (nuevos). `CasosUso/Reservas/PruebasCrearReserva.cs` (R5). `Identidad/PruebasRegistroCliente.cs` y las demás llamadas a `RegistrarAsync` (R1). `Consultas/PruebasConsultasReservasEmpresa.cs` y `PruebasConsultasCatalogoPublico.cs`, si cambian por R6, R7 o R10. Las pruebas de los puntos de registro de la duda 4 |
| `docs/` | sí | `seguridad.md` (nuevo, T12). `arquitectura.md` §10.1, §11.2, §14.4, §14.5 (`/businesses/{slug}`, duda 2), §14.6 (duda 3) y §15. `TFM_PlataformaReservas_SPEC.md` §15.3 y §21 (criterios con evidencia). `pruebas.md`. Este fichero |

**Ampliaciones del radio, autorizadas por el desarrollador el 2026-10-25** (`/revisar 19`, hallazgos 4 y 6). Todas caben dentro de las tareas y están anotadas en §11. Las dos primeras ya estaban en el plan aprobado antes de implementar; el resto se ratifica en esa fecha:

- `Infraestructura/Identidad/RestablecimientoClave.cs` y su `PruebasRestablecimientoClave.cs` (T9): la duda 5 prevé extraer la lógica a una clase.
- `Web/PlataformaReservas.Web.csproj`: `InternalsVisibleTo` para `PruebasIntegracion`, como ya tiene `Infraestructura` (T8, `IdentityRedirectManager` es `internal`).
- `Infraestructura/EventosSeguridad.cs` y `Infraestructura/Correo/EnmascaradoCorreo.cs` (T1, T10); `Persistencia/UnidadTrabajo.cs`, donde se traduce el `23P01` (T10).
- Pruebas: `Correo/PruebasEnmascaradoCorreo.cs`, `Infraestructura/RegistroCaptura.cs` y `NavegacionPrueba.cs` (dobles compartidos), `Multiempresa/PruebasAislamientoEmpresas.cs` (nuevo parámetro de los repositorios), `PruebasCifradoEnReposo.cs` (R1), `PruebasUnidadTrabajo.cs`, `PruebasRepositorioServicios.cs`, `PruebasRepositorioProfesionales.cs`, `PruebasServicioCuentas.cs` (T10), `Correo/PruebasServicioEmailRegistro.cs` (la prueba del enmascarado sale a su propio fichero, duda 1; usa el doble compartido) y `PruebasUnitarias/Dtos/Reservas/PruebasPanelEmpresaDto.cs` (R7).
- `Web/Components/App/Configuracion.razor`, `Servicios.razor` y `MiCuenta/Perfil.razor`: reciben el `@using` de R3.
- `docs/arquitectura.md` §3.2 (`comprobaciones.sh`, C17), §2.1 (excepción de las constantes de política de R8; `PruebasIntegracion` prueba clases de `Web`; `InternalsVisibleTo` de `Web`) y §2.3 (ficheros nuevos), y `docs/seguridad.md`.
- `AGENTS.md` §6: la línea de `PruebasIntegracion -> Web` recoge el uso de la Fase 19.

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- ningún paquete (Serilog ya está referenciado);
- el dominio;
- la CSP más allá de lo que permite R-12: 1 h, y si no cabe se saca;
- nada de despliegue: proxy inverso, certificados, cabeceras reenviadas, roles de PostgreSQL ni almacén de claves (duda 6);
- la deuda R12 a R23 (duda 7).

## 5. Tareas

### Medidas de endurecimiento (2,75 h)

#### T1 — E-1 bloqueo y E-4 registro de intentos fallidos (≈ 0,5 h)

- **Ficheros:** modifica `Infraestructura/InyeccionDependencias.cs`, `Correo/ServicioEmailRegistro.cs` y `Web/Cuenta/Pages/Login.razor`; crea `Infraestructura/Identidad/InicioSesion.cs` y `tests/.../Identidad/PruebasInicioSesion.cs`.
- **Implementa:** RN-169 ⚑, RN-170, RN-172, R-13, `arquitectura.md` §14.1 y §14.4, H-01.
- **Contenido:**
  - `Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min` y `AllowedForNewUsers = true` en `AddIdentityCore`.
  - `InicioSesion` (duda 1) llama a `PasswordSignInAsync(..., lockoutOnFailure: true)` y devuelve un único resultado de fallo para la contraseña incorrecta, la cuenta bloqueada, inexistente o inactiva (RN-170).
  - Registra con `LogWarning` el correo enmascarado y la IP en cada fallo (RN-172), y con `LogInformation` cada acierto, sin datos personales.
  - `Login.razor` solo llama a `InicioSesion` y muestra el mensaje genérico de siempre.
- **Hecho cuando:** las pruebas de RN-169, RN-170 y RN-172 de §6 están en verde y su sabotaje las hace fallar.
- **Verificación:** `dotnet test --filter PruebasInicioSesion` y el sabotaje.

#### T2 — E-2 auditoría de dependencias vulnerables (≈ 0,25 h)

- **Ficheros:** modifica `Directory.Build.props`.
- **Implementa:** RN-176, `arquitectura.md` §14.2, `fase-18.md` duda 1.
- **Contenido:** `<NuGetAudit>true</NuGetAudit>` y `<NuGetAuditMode>all</NuGetAuditMode>`. Si hoy aparece algún `NU1901`-`NU1904`, se corrige fijando la versión parcheada en `Directory.Packages.props` (como ya se hizo con `System.Security.Cryptography.Pkcs`). Nunca se silencia (A-18).
- **Hecho cuando:** `dotnet build` y `docker build` terminan sin avisos con la auditoría activa, y el sabotaje de §6 la hace fallar.
- **Verificación:** `dotnet build`, `docker build -t prueba .` y el sabotaje.

#### T3 — E-3 revisión y escapado en plantillas y ficha (≈ 0,5 h)

- **Ficheros:** revisa las 11 plantillas de `Infraestructura/Correo/Plantillas/` y `Web/Components/Publico/FichaEstablecimiento.razor`; crea `tests/.../Components/Publico/PruebasFichaEstablecimiento.cs`.
- **Implementa:** RN-171 ⚑, `arquitectura.md` §14.3, `fase-16b.md:357`, `fase-17.md` duda 6, `fase-13.md` duda 10.
- **Contenido:**
  - Recorrer cada plantilla y la ficha buscando `MarkupString`, `AddMarkupContent`, `Html.Raw` e interpolación en HTML. Anotar el resultado en §11.
  - Prueba de integración (duda 8): una empresa con `Descripcion` y `Nombre` que contienen `<script>alert(1)</script>`. El HTML de la ficha los muestra escapados (`&lt;script&gt;`) y no contiene la etiqueta.
  - La parte del correo ya la cubre `PruebasRenderizadorPlantillas` (Fase 16b).
- **Hecho cuando:** la revisión está anotada y la prueba está en verde y falla con el sabotaje.
- **Verificación:** `dotnet test --filter PruebasFichaEstablecimiento` y el sabotaje.

#### T4 — E-5 limitación de frecuencia (≈ 0,75 h)

- **Ficheros:** crea `Web/LimitacionFrecuencia.cs` y `tests/.../PruebasLimitacionFrecuencia.cs`; modifica `Web/Program.cs`.
- **Implementa:** RN-173 ⚑, RN-174, `arquitectura.md` §14.5, `fase-13.md` duda 14, `fase-16b.md:358`, H-01.
- **Contenido** (duda 2):
  - Limitador global (`AddRateLimiter` del marco, sin paquetes) particionado por grupo y por IP (`RemoteIpAddress`).
  - Grupo `autenticacion`: 5 intentos en 5 min, ventana fija. Cuenta solo los `POST` a `/login`, `/Account/Login`, `/register`, `/Account/Register` y `/Account/ForgotPassword`.
  - Grupo `catalogo`: 60 peticiones por minuto. Cuenta todas las peticiones a `/businesses` y `/businesses/{slug}`.
  - Cualquier otra ruta, sin límite.
  - `RejectionStatusCode = 429`. `UseRateLimiter` va **antes** de `UseAuthentication`.
- **Hecho cuando:**
  - las pruebas de RN-173 están en verde y fallan con el sabotaje;
  - `curl` contra una instancia propia devuelve `429` en el sexto `POST` a `/login` y en la petición 61 a `/businesses`.
- **Verificación:** `dotnet test --filter PruebasLimitacionFrecuencia` y `curl`.

#### T5 — E-6 cabeceras de seguridad y CSP (≈ 0,75 h)

- **Ficheros:** crea `Web/CabecerasSeguridad.cs` y `tests/.../PruebasCabecerasSeguridad.cs`; modifica `Web/Program.cs` y `Web/Components/App.razor` (duda 3).
- **Implementa:** RN-175, `arquitectura.md` §14.6, R-12, `fase-08-revision.md:159`, H-10.
- **Contenido:**
  - Un *middleware* añade `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` y `Referrer-Policy: strict-origin-when-cross-origin` en todas las respuestas.
  - HSTS se mantiene fuera de `Development`.
  - **La CSP de §14.6 va la última**, después de las cuatro anteriores y de comprobarlas. Antes se quita `<ImportMap />` (duda 3).
  - **Límite de 1 h** (R-12): si la aplicación no funciona con la CSP en las tres áreas, se saca la CSP, se dejan las otras cabeceras y se documenta en `seguridad.md` como trabajo futuro con su motivo.
- **Hecho cuando:**
  - las pruebas de RN-175 están en verde;
  - `curl -I` muestra las cabeceras;
  - el **desarrollador** navega por las tres áreas (pública, cliente y empresa) con la consola del navegador abierta, sin errores de CSP. Esto incluye el modo interactivo de `/app` (`fase-08-revision.md:159`) y la reconexión.
- **Verificación:** `dotnet test --filter PruebasCabecerasSeguridad`, `curl -I` y la navegación manual.

### Refactorización (1,5 h)

#### T6 — Menores heredados de las fases anteriores (≈ 1 h)

- **Ficheros:**
  - `Infraestructura/Identidad/RegistroCliente.cs` y `Web/Cuenta/Pages/Register.razor` (R1);
  - `Infraestructura/Repositorios/RepositorioMiembros.cs` (R2);
  - `Web/Components/App/_Imports.razor` y `MiCuenta/_Imports.razor`, y las páginas que usen objetos de valor (R3);
  - `Aplicacion/Dtos/Reservas/DetalleReservaClienteDto.cs` y `Web/Components/MiCuenta/DetalleReserva.razor` (R4);
  - `tests/.../CasosUso/Reservas/PruebasCrearReserva.cs` (R5);
  - `Infraestructura/Consultas/ConsultasReservasEmpresa.cs` y `Aplicacion/Dtos/Reservas/PanelEmpresaDto.cs` (R6, R7).
- **Implementa:** `PROJECT_PLAN.md` l.275; `fase-06-revision.md:161`; `fase-07-revision.md:107`; `fase-08-revision.md:173`; `fase-16.md:299` (origen `fase-15-revision.md:83`); auditoría 14, caso 7; menores 1 y 2 de `/revisar 16`.
- **Contenido:**
  - **R1:** `RegistrarAsync` recibe `CancellationToken` y lo propaga a la transacción.
  - **R2:** se quita el comentario que repite `arquitectura.md` §8.2.
  - **R3:** el `@using PlataformaReservas.Dominio.ValueObjects` sale de los dos `_Imports.razor` y va solo a las páginas que lo usan.
  - **R4:** `DetalleReservaClienteDto.ConvertirADto` resuelve el motivo de "no cancelable" (RN-93) y la página solo lo muestra. Lleva su prueba unitaria.
  - **R5:** `Una_empresa_inexistente_da_validacion`.
  - **R6:** se quita el `AsNoTracking()` redundante.
  - **R7:** el orden del panel queda en un solo sitio.
- **Hecho cuando:** las suites siguen en verde, hay prueba de R4 y R5, y `comprobaciones.sh` no da hallazgos.
- **Verificación:** `dotnet build` y `dotnet test`.

#### T7 — Limpieza de bajo riesgo (≈ 0,5 h)

- **Ficheros:**
  - `Web/Components/App/_Imports.razor`, `MiCuenta/_Imports.razor` y `Layout/MainLayout.razor` (R8);
  - `Web/Cuenta/Pages/Register.razor` (R9);
  - `Infraestructura/Consultas/ConsultasCatalogoPublico.cs` (R10);
  - `Web/Program.cs` (R11).
- **Implementa:** AGENTS §3.10 (comentarios), §8 bis (reloj inyectado), A-09 (una regla en un solo sitio).
- **Contenido:**
  - **R8:** `"EsPropietario"` y `"EsCliente"` pasan a usar `RolesIdentidad.Politica*`.
  - **R9:** `Register.razor` usa `IRelojSistema` en lugar de `DateTime.UtcNow`, y las longitudes de `NombrePersona` y `Telefono`.
  - **R10:** la búsqueda de la empresa visible por slug y de sus servicios activos, compartida entre la ficha y los datos de reserva.
  - **R11:** se quitan los comentarios en inglés de la plantilla en `Program.cs`.
  - R12 a R23 quedan como deuda técnica, con su motivo, en §9 (duda 7).
- **Hecho cuando:** las suites siguen en verde, sin avisos.
- **Verificación:** `dotnet build`, `dotnet test` y `comprobaciones.sh`.

### Revisión de seguridad (2,5 h)

#### T8 — Redirección abierta en `ReturnUrl` (H-02, OWASP A01) (≈ 0,5 h)

- **Ficheros:** modifica `Web/Cuenta/IdentityRedirectManager.cs`; crea `tests/.../Cuenta/PruebasIdentityRedirectManager.cs`.
- **Implementa:** H-02 (OWASP A01, ASVS V5.1.5), SPEC §15.3 A01.
- **Contenido:**
  - Hoy `Uri.IsWellFormedUriString(uri, UriKind.Relative)` acepta `//evil.com`, que el navegador resuelve contra otro dominio tras un inicio de sesión real.
  - La comprobación pasa a aceptar solo rutas locales: empiezan por `/` y no por `//` ni `/\`. Cualquier otra cosa redirige a `/`.
- **Hecho cuando:**
  - `//evil.com`, `/\evil.com`, `https://evil.com` y `javascript:alert(1)` acaban en `/`;
  - `/app` y `/my-account/bookings?x=1` se conservan;
  - quitar la comprobación hace fallar la prueba.
- **Verificación:** `dotnet test --filter PruebasIdentityRedirectManager`.

#### T9 — Enumeración y errores en el restablecimiento (H-03, H-22) (≈ 0,5 h)

- **Ficheros:** modifica `Web/Cuenta/Pages/ResetPassword.razor`; las pruebas van en `tests/.../Identidad/PruebasSolicitudRestablecimientoClave.cs` o en el fichero de la clase que reciba la lógica (duda 5).
- **Implementa:** H-03 y H-22 (OWASP A07), RN-158 ⚑ (extensión del criterio, duda 5), RN-159.
- **Contenido:**
  - Con un correo inexistente, `ResetPassword` muestra hoy la página de "contraseña restablecida", y con uno existente y un token inválido muestra los errores de Identity: se distingue si la cuenta existe.
  - Pasa a dar **la misma respuesta** en los dos casos: "el enlace no es válido o ha caducado". Los errores de la política de contraseñas solo se muestran cuando el token es válido.
  - Un `code` que no es Base64Url válido (`FormatException`) redirige a `InvalidPasswordReset` en lugar de dar 500.
- **Hecho cuando:** la respuesta es indistinguible entre cuenta inexistente y token inválido, y el `code` malformado no da 500.
- **Verificación:** prueba de integración (duda 5) y `curl` con un `code` malformado.

#### T10 — Registro de eventos de seguridad (H-14, OWASP A09) (≈ 0,75 h)

- **Ficheros:** modifica `Web/Program.cs` y `appsettings*.json` (sección de Serilog), y los puntos de registro de la duda 4 en `Infraestructura`.
- **Implementa:** `arquitectura.md` §10.1 (l.828) y §11.2, SPEC §15.3 A09, RN-165, H-14.
- **Contenido** (duda 4):
  - `builder.Host.UseSerilog(...)` leyendo de configuración, con salida a consola.
  - Se registran estos eventos:
    - inicio de sesión correcto, fallido y bloqueado (T1);
    - solicitud de restablecimiento, sin el correo;
    - bajas de cuenta y de empresa;
    - intentos de acceso entre empresas en la defensa en profundidad de los repositorios, con `Warning` y el identificador de usuario;
    - violaciones de solapamiento (`23P01`), con `Warning`.
  - Nunca contraseñas, sellos, tokens ni correos enteros (RN-165).
- **Hecho cuando:**
  - la aplicación arranca con Serilog;
  - cada evento tiene una prueba que comprueba su nivel y que no lleva datos personales;
  - la suite sigue en verde.
- **Verificación:** las pruebas de la duda 4 y el arranque en la instancia propia.

#### T11 — Configuración del entorno y repaso final (≈ 0,75 h)

- **Ficheros:** modifica `docker-compose.yml`. El repaso no tiene fichero propio: se anota en §11 y en `seguridad.md`.
- **Implementa:** H-11, la revisión de seguridad de SPEC l.944 (superficie nueva: correo, eventos y criptografía) y la duda 6.
- **Contenido:**
  - **H-11:** `postgres` se publica en `127.0.0.1:${POSTGRES_PUERTO:-5432}:5432`, como ya se hizo con `web` en la Fase 18. Docker se salta el cortafuegos del sistema, y hoy la base de datos queda abierta a la red local.
  - **Repaso por lectura**, con `revisor-seguridad`, de lo que haya cambiado en T1 a T10 y de la superficie de correo, eventos y criptografía.
  - Cada hallazgo de la auditoría que no se corrige (duda 6) pasa a `seguridad.md` como limitación o trabajo futuro, con su motivo.
- **Hecho cuando:** el repaso no deja nada sin clasificar, y `docker compose config` publica `postgres` solo en el bucle local.
- **Verificación:** `docker compose config` y la lista de §11.

### Documentación (0,5 h)

#### T12 — `docs/seguridad.md` (≈ 0,5 h)

- **Ficheros:** crea `docs/seguridad.md`; modifica `TFM_PlataformaReservas_SPEC.md` §15.3 y §21, `arquitectura.md` §10.1, §11.2, §14.4, §14.5, §14.6 y §15, y `docs/pruebas.md`.
- **Implementa:** SPEC l.971, §18 l.1076 y §21 l.1227; `PROJECT_PLAN.md` l.276.
- **Contenido:**
  - la tabla OWASP Top 10 (2021) de §15.3, con la **prueba o el fichero concreto** de cada fila;
  - las seis medidas y cómo se verifican;
  - el modelo de amenaza del cifrado (SPEC §15.1);
  - las limitaciones declaradas: RN-174, la CSP si se sacó y los hallazgos de la duda 6 con su motivo;
  - el trabajo futuro.
- **Hecho cuando:** ninguna fila de la tabla queda sin evidencia, y los criterios de §21 que cierra la fase están marcados.
- **Verificación:** lectura cruzada con §7 y `/revisar 19`.

**Suma: 7,25 h de 7,25 h presupuestadas** (2,75 h de medidas + 1,5 h de refactorización + 2,5 h de revisión + 0,5 h de documentación).

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Cinco contraseñas incorrectas bloquean la cuenta, y la **sexta, con la contraseña correcta, también falla** | I | `Identidad/PruebasInicioSesion.cs` | RN-169 ⚑, R-13 | T1 |
| El bloqueo dura 15 min: pasado ese tiempo (con el reloj del almacén de Identity) se vuelve a poder entrar | I | `Identidad/PruebasInicioSesion.cs` | RN-169 | T1 |
| Cuenta bloqueada, contraseña incorrecta, cuenta inexistente e inactiva dan el mismo resultado | I | `Identidad/PruebasInicioSesion.cs` | RN-170 | T1 |
| Un fallo registra un `Warning` con el correo enmascarado y la IP; el registro no contiene el correo entero ni la contraseña | I | `Identidad/PruebasInicioSesion.cs` | RN-172, RN-165 | T1 |
| La ficha pública de una empresa con `<script>alert(1)</script>` en `Nombre` y `Descripcion` lo muestra escapado | I | `Components/Publico/PruebasFichaEstablecimiento.cs` | RN-171 ⚑ | T3 |
| El sexto `POST` a `/login` desde la misma IP se rechaza; desde otra IP, no; los `GET` a `/login` no cuentan | I | `PruebasLimitacionFrecuencia.cs` | RN-173 ⚑ | T4 |
| Lo mismo en `/register`, `/Account/Register`, `/Account/Login` y `/Account/ForgotPassword` (cuentan en el mismo grupo) | I | `PruebasLimitacionFrecuencia.cs` | RN-173 | T4 |
| La petición 61 en un minuto a `/businesses` o `/businesses/{slug}` se rechaza | I | `PruebasLimitacionFrecuencia.cs` | RN-173 | T4 |
| Una ruta sin grupo (`/`, `/app`) no se limita | I | `PruebasLimitacionFrecuencia.cs` | RN-173 | T4 |
| Toda respuesta lleva `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y la CSP de §14.6 | I | `PruebasCabecerasSeguridad.cs` | RN-175 | T5 |
| `ReturnUrl` externo, protocolo-relativo o con barra invertida redirige a `/`; una ruta local se conserva | I | `Cuenta/PruebasIdentityRedirectManager.cs` | H-02 (A01) | T8 |
| Restablecimiento con correo inexistente y con token inválido: misma respuesta. `code` malformado: no da 500 | I | Según la duda 5 | RN-158, H-03, H-22 | T9 |
| Cada evento de seguridad de la duda 4 se registra con su nivel y sin datos personales | I | El fichero de cada clase que registra | A09, RN-165 | T10 |
| `DetalleReservaClienteDto` resuelve el motivo de "no cancelable" | U | `Dtos/Reservas/PruebasDetalleReservaClienteDto.cs` | RN-93 (R4) | T6 |
| `CrearReserva` con una empresa inexistente da `Validacion` | I | `CasosUso/Reservas/PruebasCrearReserva.cs` | RN-03 (R5) | T6 |
| `curl -I` muestra las cabeceras; `curl` repetido da `429`; navegación por las tres áreas sin errores de CSP | Manual | — (anotado en §11) | RN-173, RN-175, R-12 | T4, T5 |

Un fichero por clase probada y por suite (`AGENTS.md` §11 bis). Las clases nuevas de `Web` (`LimitacionFrecuencia`, `CabecerasSeguridad`, `IdentityRedirectManager` y `FichaEstablecimiento`) se prueban en `PruebasIntegracion`, que ya referencia a `Web` (`fase-17.md`, duda 2), en la misma carpeta relativa.

Casos del catálogo que no aplican, con su motivo:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `Program.cs` | Todos | Arranque sin lógica propia. Se comprueba con la aplicación levantada y con las pruebas de las clases que registra |
| `LimitacionFrecuencia`, `CabecerasSeguridad` | "Caso de uso" y "repositorio" | No son casos de uso ni repositorios. Se aplican los casos de "regla con números": extremos (5 y 6, 60 y 61) y valores contiguos |
| RN-174 | Prueba | Verificación "—": es una limitación declarada (por instancia y en memoria), documentada en `seguridad.md` |
| RN-176 | Prueba automática | Verificación "—". Se comprueba con el sabotaje de §6, porque la auditoría actúa en la restauración, no en una prueba |
| `IdentityRedirectManager` | Pruebas de los métodos que se borran | R18 (código muerto) es deuda de la duda 7 y no se toca aquí |
| `EventosSeguridad` | Fichero de pruebas propio | Solo define mensajes de registro compartidos. Se prueba a través de las clases que lo usan: `PruebasRepositorioServicios`, `PruebasRepositorioProfesionales` y `PruebasServicioCuentas` (menor 2 de `/revisar 19`, anotado en la Fase 20) |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-169 | En `InicioSesion`, `lockoutOnFailure: false` | La sexta contraseña correcta también falla | **Falló**, con otras tres de `PruebasInicioSesion` (4 de 8). Restaurado |
| RN-171 | En `FichaEstablecimiento.razor`, mostrar la descripción con `(MarkupString)` | La ficha muestra la descripción escapada | **Falló** `El_texto_del_propietario_con_script_se_muestra_escapado`. Restaurado |
| RN-173 | En `LimitacionFrecuencia`, sacar `/login` del grupo `autenticacion` | El sexto `POST` a `/login` se rechaza | **Fallaron** `El_sexto_envio_a_login…`, `Las_rutas_de_cuenta_comparten…` y un caso de `Asigna_cada_peticion_a_su_grupo`. Restaurado |
| RN-176 | En `Directory.Packages.props`, bajar `System.Security.Cryptography.Pkcs` a una versión con aviso conocido | `dotnet build` falla con `NU1902` o `NU1903` | **Falló** con 10.0.12: `NU1902` (GHSA-2fmv-rv2c-59x7) y `NU1903` (GHSA-846f-f764-76mx). Restaurado |
| H-02 (extra) | Navegar sin `DestinoLocal` en `IdentityRedirectManager` | Los destinos externos acaban en `/` | **Fallaron** 10 de 16 casos de `PruebasIdentityRedirectManager`. Restaurado |
| H-02 (extra, `/revisar 19`) | Quitar solo `uri.StartsWith("/\\")` de `DestinoLocal` | `/\evil.com` acaba en `/` | **Fallaron** los casos escritos en C# como `"/\\evil.com"` y `"/\\/evil.com"` (2 de 19). Restaurado. El caso anterior, escrito `"/\evil.com"` con una sola barra, era en C# 14 un `\e` (ESC) y no probaba esa rama |
| H-02 (extra, segunda ronda de `/revisar 19`) | En la rama de URI absoluta propia, devolver `PathAndQuery` sin volver a validarlo | Una URI del mismo origen con barras de más acaba en `/` | **Fallaron** los 5 casos del mismo origen (`Base + "/evil.com"`, `Base + "//evil.com"`, con barras invertidas y con el host en mayúsculas). Restaurado |

## 7. Criterios de aceptación

- [x] **SPEC §21 (l.1215):** "La nomenclatura de §3 se respeta en todo el código." Lo que queda en inglés es la excepción de plantilla de `Web/Cuenta/` (AGENTS §5).
- [x] **SPEC §21 (l.1222):** "Cinco intentos fallidos de inicio de sesión bloquean la cuenta 15 minutos, y quedan registrados."
- [x] **SPEC §21 (l.1223):** "La limitación de frecuencia responde `429` en `/login` y en `/businesses` al superar el umbral."
- [x] *(cabeceras: `PruebasCabecerasSeguridad` y `curl`; navegación manual por las tres áreas con la CSP activa, sin errores: desarrollador, 2026-10-25)* **SPEC §21 (l.1224):** "Las cabeceras de seguridad están presentes y **la aplicación funciona con la CSP activa**." Si R-12 obliga a sacar la CSP, el criterio queda parcial y documentado en `seguridad.md`.
- [x] **SPEC §21 (l.1225):** "Una descripción de empresa con `<script>` **no se ejecuta** ni en la ficha pública ni en el correo."
- [x] **SPEC §21 (l.1226):** "La auditoría de dependencias vulnerables hace fallar la compilación si encuentra alguna."
- [x] **SPEC §21 (l.1227):** "`docs/seguridad.md` existe y su tabla OWASP cita, en cada fila, la prueba o el fichero que la sostiene."
- [x] **Revisión de seguridad:** H-02, H-03, H-11, H-14 y H-22 corregidos con prueba o comprobación; el resto de H-xx, clasificados en `seguridad.md`.
- [x] **Refactorización:** R1 a R11 hechos; R12 a R23 documentados como deuda con su motivo.
- [x] **Los sabotajes de §6** hacen fallar su prueba.
- [x] **`dotnet build`** sin avisos y **`dotnet test`** en verde.

**Control 5** (`PROJECT_PLAN.md` l.205-213, literal). Esta fase lo cierra:

```text
Cinco intentos fallidos bloquean la cuenta 15 min, y quedan registrados
Superar el umbral en /login y /businesses devuelve 429
Las cabeceras estan presentes y la aplicacion FUNCIONA con la CSP activa
Una descripcion con <script> no se ejecuta ni en la ficha ni en el correo
La auditoria de dependencias vulnerables hace fallar la compilacion
```

La tercera exige **navegación manual por las tres áreas** revisando la consola del navegador (`PROJECT_PLAN.md` l.215). La hace el desarrollador.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-12 ⚠ | La CSP deja la aplicación inerte sin error visible; el *script* en línea de `<ImportMap />` se bloquea | La CSP va la última (T5), se quita `<ImportMap />` (duda 3), se prueba a mano en las tres áreas y, si pasa de 1 h, se saca y se documenta |
| R-13 | `lockoutOnFailure: false` hace que el bloqueo configurado no bloquee nada | `InicioSesion` lo fija en `true` y la prueba de la sexta contraseña correcta lo detecta, con sabotaje |
| R-14 | Con varias instancias, el límite real se multiplica | Una sola instancia (`docker-compose.yml`). RN-174 documentado como limitación |
| R-1 | Paralelismo nuevo en un componente | Ningún cambio añade llamadas concurrentes en componentes; `Login.razor` hace una sola llamada |
| R-10 | Buscar el usuario por LINQ sobre un campo cifrado | `InicioSesion` usa `SignInManager` y `UserManager`, nunca LINQ sobre `Users` |
| R-7 | Correos reales por descuido al tocar el restablecimiento o el registro | `Correo:Habilitado` sigue en `false`; las pruebas usan dobles |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace, o por qué no |
|---|---|
| Verificación del correo y segundo factor (H-07) | Descartados: SPEC §15.3 A07, "Sin verificación de correo y sin segundo factor" |
| Límite de reservas por cliente (H-08) | Descartado: `reglas-negocio.md` §13, SPEC §15.3 A04 |
| Despliegue tras un proxy: `ForwardedHeaders`, *cookie* `Secure` siempre, `AllowedHosts` restringido (H-09, H-24, `fase-16b.md:396`) | Configuración de despliegue, que no está en el SPEC. Se documenta en `seguridad.md` (duda 6) |
| Rol de PostgreSQL sin privilegios para la aplicación (H-12) | Despliegue. Se documenta (duda 6) |
| Cifrado de las claves de Data Protection en el volumen (H-13) | Despliegue. Ya está declarado en `arquitectura.md` §11.4; se documenta (duda 6) |
| Cifrar `Reserva.Observaciones` (H-15) | Cambiaría D-22 (SPEC §15.1, qué se cifra). Se documenta (duda 6) |
| Rotación de claves y AAD en el cifrado (H-16) | Trabajo futuro (duda 6) |
| Diferencia de tiempo en el inicio de sesión y en `ForgotPassword` (H-05, H-06, `fase-16b.md:396`) | Mitigada por E-5. Se documenta (duda 6) |
| Caracteres de control en los objetos de valor de texto (H-17), carrera del slug (H-18), *tokens* de concurrencia (H-19), cola acotada (H-20), sesión tras la baja (H-21) | Bajo riesgo o fuera de las medidas. Se documentan (duda 6) |
| Fijar las versiones de las imágenes de Docker (H-23) | `postgres:latest` lo decidió el desarrollador en la Fase 5 (`arquitectura.md` §11.4). Se documenta |
| Deuda R12 a R23 (duda 7) | Deuda técnica, con su motivo: R12 (`OrdenEspanol` en tres listados), R13 (`contacto is null` en `CrearReserva`), R14 (texto `"_servicios"`), R15 (formatos repetidos en `Web`), R16 (publicar y limpiar eventos), R17 (plantillas de correo con estilo repetido), R18 (código muerto de la plantilla de Identity), R19 (creación de usuarios repetida), R20 (`EsCancelablePorEmpresa`), R21 (`ElegirServicioAsync` síncrono), R22 (dobles de prueba duplicados), R23 (longitud de la contraseña en cuatro sitios). Todos son de bajo riesgo y ninguno afecta a la seguridad ni al aislamiento; se dejan para no comprometer las medidas, que no se recortan |
| Documentación técnica final | Fase 20 |

## 10. Trazabilidad

| RN · criterio · hallazgo | Tarea | Prueba |
|---|---|---|
| RN-169 ⚑ | T1 | `PruebasInicioSesion` (sexta contraseña correcta) y su sabotaje |
| RN-170 | T1 | `PruebasInicioSesion` (mismo resultado) |
| RN-171 ⚑ | T3 | `PruebasFichaEstablecimiento` y su sabotaje; `PruebasRenderizadorPlantillas` (correo, Fase 16b) |
| RN-172, RN-165 | T1, T10 | `PruebasInicioSesion` (registro enmascarado) |
| RN-173 ⚑ | T4 | `PruebasLimitacionFrecuencia` y su sabotaje; `curl` |
| RN-174 | T4, T12 | Documentado en `seguridad.md` |
| RN-175 | T5 | `PruebasCabecerasSeguridad`; `curl -I`; navegación manual |
| RN-176 | T2 | Sabotaje con `NU190x` |
| RN-158 (extensión) | T9 | Prueba de la duda 5 |
| H-02 | T8 | `PruebasIdentityRedirectManager` |
| H-11 | T11 | `docker compose config` |
| H-14 | T10 | Pruebas de la duda 4 |
| H-22 | T9 | Prueba de la duda 5 |
| SPEC §21 l.1215 | T6, T7 | `comprobaciones.sh` (C14) y revisión |
| SPEC §21 l.1227 | T12 | Lectura de `seguridad.md` |

## 11. Dudas

Resueltas a petición del desarrollador con la opción recomendada (2026-10-25).

### Entrada de la revisión: auditoría de seguridad previa (2026-10-25)

Auditoría de código y de aplicación en solo lectura (OWASP Top 10 2021 y ASVS). **No hay ningún acceso entre empresas (IDOR), ninguna inyección SQL ni ningún `MarkupString`.** Los repositorios y las consultas filtran por empresa o por usuario en origen, y la autorización por política funciona. Los hallazgos son de perímetro y de configuración:

| Id | Severidad | OWASP | Dónde | Resumen | Tratamiento |
|---|---|---|---|---|---|
| H-01 | Alta | A07 | `Login.razor:80`, `Program.cs` | Sin bloqueo efectivo ni limitación de frecuencia: fuerza bruta ilimitada | T1, T4 |
| H-02 | Alta (probable) | A01 | `IdentityRedirectManager.cs:23-28` | `ReturnUrl=//evil.com` redirige fuera tras un inicio de sesión real | T8 |
| H-03 | Media | A07 | `ResetPassword.razor:70-84` | Respuesta distinta si el correo existe | T9 |
| H-04 | Media | A07 | `Register.razor:61,76`, `ServicioCuentas.cs:37-38` | El registro revela si el correo existe | Duda 5 (documentado, mitigado por E-5) |
| H-05 | Media | A07 | `SolicitudRestablecimientoClave.cs:21-46` | Diferencia de tiempo con cuenta existente (envío síncrono) | Duda 6 |
| H-06 | Baja-media | A07 | `Login.razor:80` | Diferencia de tiempo con usuario inexistente | Duda 6 |
| H-07 | Media | A04, A07 | `Register.razor:82` | Sin verificación de correo | §9 (descartado por el SPEC) |
| H-08 | Media | A04 | `CrearReserva.cs` | Sin límite de reservas por cliente | §9 (descartado por el SPEC) |
| H-09 | Media | A05 | `Program.cs`, `InyeccionDependencias.cs:99` | Sin cabeceras reenviadas; la *cookie* no fuerza `Secure` tras un proxy | Duda 6 |
| H-10 | Media | A05 | `Program.cs` | Sin cabeceras de seguridad ni CSP | T5 |
| H-11 | Media | A05 | `docker-compose.yml:21-22` | `postgres` publicado en todas las interfaces | T11 |
| H-12 | Media | A05 | `docker-compose.yml` | La aplicación usa el superusuario de PostgreSQL | Duda 6 |
| H-13 | Baja-media | A02 | `docker-compose.yml:46-47` | Claves de Data Protection sin cifrar en el volumen | Duda 6 |
| H-14 | Baja-media | A09 | `Login.razor:84` | Sin eventos de seguridad; Serilog sin conectar | T10 |
| H-15 | Baja | A02 | `ConfiguracionReserva.cs:32` | `Observaciones` en claro (posibles datos de salud) | Duda 6 |
| H-16 | Baja | A02 | `ProtectorDatosPersonales.cs`, `AnilloClaves.cs` | Sin identificador de clave ni AAD: no hay rotación | Duda 6 |
| H-17 | Baja | A03 | Objetos de valor de texto | Caracteres de control en asuntos de correo (MimeKit los codifica) | Duda 6 |
| H-18 | Baja | A04 | `RegistrarEmpresa.cs:50-54` | Carrera del slug: 500 en lugar de reintento | Duda 6 |
| H-19 | Baja | A04 | Cancelaciones, `DarDeBajaEmpresa` | Sin *token* de concurrencia | Duda 6 |
| H-20 | Baja | A04 | `ColaEventos.cs:9-10` | Canal sin límite | Duda 6 |
| H-21 | Baja | A07 | `ServicioCuentas.cs:94-99` | Hasta 5 min de sesión tras la baja | Duda 6 |
| H-22 | Baja | A05 | `ResetPassword.razor:65` | `code` malformado da 500 | T9 |
| H-23 | Baja | A06, A08 | `docker-compose.yml`, `Dockerfile` | Imágenes sin versión fijada | §9 |
| H-24 | Baja | A05 | `appsettings.json:8` | `AllowedHosts: "*"` | Duda 6 |

### Entrada de la refactorización: revisión de arquitectura previa (2026-10-25)

La arquitectura está sana:
- repositorios filtrados en origen, sin `SaveChanges` fuera de los casos de uso y con los eventos publicados después del commit;
- casos de uso con un único `EjecutarAsync` y DTOs con `ConvertirADto`;
- dominio rico;
- ningún `async void`, `.Wait()` ni `Task.WhenAll` en componentes.

Los candidatos están priorizados como R1 a R23 (tareas T6 y T7, y deuda en §9). Se desaconseja hacer ahora tres cambios, porque tocan decisiones cerradas o firmas públicas:
- sacar la conversión de hora del motor (AGENTS §9);
- cambiar `HorarioDiaDto`;
- quitar la consulta duplicada entre `CrearReserva` y el motor (firma de `IMotorDisponibilidad`).

### Duda 1 — Inicio de sesión que se pueda probar

- **Pregunta:** RN-169, RN-170 y RN-172 son de integración, y R-13 exige una prueba que detecte `lockoutOnFailure: false`. Hoy la lógica está dentro de `Login.razor`, que no se puede probar sin infraestructura HTTP (`WebApplicationFactory` no está en el stack).
- **Opciones:**
  - (a) Sacar la lógica a `Infraestructura/Identidad/InicioSesion.cs`, igual que `SolicitudRestablecimientoClave` y `RegistroCliente`. Llama a `PasswordSignInAsync` con `lockoutOnFailure: true`, unifica el resultado de fallo y registra con el correo enmascarado. El enmascarado pasa de `internal` en `ServicioEmailRegistro` a un ayudante compartido de `Infraestructura` (nombre propuesto: `EnmascaradoCorreo.Enmascarar`), que usan las dos clases.
  - (b) Dejarla en la página y probar solo las opciones de bloqueo.
- **Recomendación y motivo:** (a). Es el patrón que ya sigue la cuenta (las páginas de `Web/Cuenta` delegan en `Infraestructura/Identidad`). Hace la regla comprobable y evita duplicar el enmascarado.
- **Bloquea:** T1.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 2 — Limitación de frecuencia: qué cuenta y cómo se prueba

- **Pregunta:** `arquitectura.md` §14.5 propone políticas con nombre de 5 en 5 min sobre `/login`, `/register` y `/Account/ForgotPassword`. En Blazor SSR la misma ruta sirve el formulario (`GET`) y recibe el envío (`POST`): si cuentan las dos, un usuario legítimo agota el cupo con dos o tres intentos. Además, sin infraestructura HTTP de pruebas, `429` solo se comprobaría a mano.
- **Opciones:**
  - (a) Un limitador global particionado por grupo e IP en `Web/LimitacionFrecuencia.cs`. `autenticacion` cuenta solo los `POST` a las rutas de cuenta (incluidas `/Account/Login` y `/Account/Register`, que son la misma página); `catalogo` cuenta todas las peticiones a `/businesses` y `/businesses/{slug}` (`fase-13.md`, duda 14). Se prueba directamente con `DefaultHttpContext`, sin paquetes, y además con `curl`.
  - (b) Políticas con nombre y `[EnableRateLimiting]` en cada componente, que cuentan también los `GET`, comprobadas solo con `curl`.
- **Recomendación y motivo:** (a). Respeta los umbrales de §14.5 midiendo intentos reales y no visitas, cubre `/businesses/{slug}` y deja una prueba automática para la regla ⚑. Se actualiza `arquitectura.md` §14.5.
- **Bloquea:** T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 3 — La CSP frente a `<ImportMap />`

- **Pregunta:** `App.razor:11` emite `<ImportMap />`, un `<script type="importmap">` en línea. La CSP de §14.6 (`script-src 'self' 'wasm-unsafe-eval'`, sin *nonce* ni *hash*) lo bloquea.
- **Opciones:**
  - (a) Quitar `<ImportMap />` tras comprobar que la aplicación no usa mapas de importación (`blazor.web.js` y `ReconnectModal.razor.js` se cargan por URL), y mantener la CSP de §14.6.
  - (b) Generar un *nonce* por petición y pasarlo a la CSP y al `<ImportMap />`.
  - (c) Añadir `'unsafe-inline'` a `script-src`. Se descarta: anula la protección contra *scripts* inyectados.
- **Recomendación y motivo:** (a). Es lo más simple y no debilita la CSP. Si al probarlo algo deja de funcionar y se pasa de 1 h, se aplica R-12: se saca la CSP y se documenta. También se comprueba que `connect-src 'self'` admite el WebSocket del circuito en los navegadores actuales.
- **Bloquea:** T5.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 4 — Serilog y eventos de seguridad

- **Pregunta:** `arquitectura.md` §10.1 pone `UseSerilog` en `Program.cs` y §11.2 enumera lo que se registra, pero Serilog no está conectado y los únicos registros son el acierto del inicio de sesión y el registro de cliente. SPEC §15.3 da A09 por "cubierto" con Serilog y eventos de seguridad.
- **Opciones:**
  - (a) Conectar `UseSerilog` (configuración y consola) y registrar los **eventos de seguridad**:
    - inicio de sesión correcto, fallido y bloqueado;
    - solicitud de restablecimiento (sin el correo);
    - bajas de cuenta y de empresa;
    - intentos de acceso entre empresas en la defensa en profundidad de los repositorios (`Warning`, con identificador de usuario);
    - violaciones de solapamiento (`Warning`).
  - (b) Todos los eventos de §11.2, incluidos el alta de empresa y la creación y cancelación de reservas.
  - (c) No conectarlo y corregir la documentación.
- **Recomendación y motivo:** (a). Es lo que sostiene A09 y las dos señales que §11.2 destaca (aislamiento y concurrencia), y cabe en 0,75 h. Los eventos de negocio de (b) quedan como deuda documentada.
- **Bloquea:** T10.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 5 — Enumeración en el restablecimiento (H-03) y en el registro (H-04)

- **Pregunta:** RN-158 exige una respuesta indistinguible al **solicitar** el restablecimiento, pero `ResetPassword` revela si la cuenta existe al **usarlo**. El registro de cliente y el alta de empresa también revelan que el correo ya está registrado.
- **Opciones:**
  - (a) `ResetPassword` da la misma respuesta para una cuenta inexistente y para un token inválido, extendiendo el criterio de RN-158, y el `code` malformado no da 500. El registro y el alta se quedan como están: el usuario necesita saber que su correo ya tiene cuenta, y cambiarlo exigiría una regla nueva y tocar CU-01 y CU-04. Queda mitigado por E-5 y documentado.
  - (b) Además, mensajes genéricos en el registro y en el alta.
- **Recomendación y motivo:** (a). Corrige el hueco que contradice el espíritu de una regla ⚑ sin cambiar el comportamiento de negocio. La prueba va en el fichero de la clase que reciba la lógica: si se extrae una clase como en la duda 1, su `Pruebas<Clase>.cs`; si no, `PruebasSolicitudRestablecimientoClave` no aplica, y se anota en §11.
- **Bloquea:** T9.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 6 — Hallazgos de la auditoría que no se corrigen

- **Pregunta:** qué hacer con H-05, H-06, H-09, H-12, H-13, H-15, H-16, H-17, H-18, H-19, H-20, H-21 y H-24 (y H-07, H-08 y H-23, ya fuera por el SPEC o por decisión previa).
- **Opciones:**
  - (a) Documentarlos en `seguridad.md` como limitación o trabajo futuro, cada uno con su motivo:
    - varios dependen del despliegue real (proxy, roles de base de datos, almacén de claves, `AllowedHosts`);
    - H-15 cambiaría D-22;
    - H-05 y H-06 quedan mitigados por E-5;
    - el resto son de bajo riesgo.

    Solo se corrigen H-11 y H-22, triviales y sin decisión pendiente.
  - (b) Corregir además alguno, por ejemplo cerrar la sesión inmediatamente tras la baja (H-21) o capturar la carrera del slug (H-18).
- **Recomendación y motivo:** (a). La fase no se recorta y las seis medidas tienen prioridad. Documentar con motivo es lo que pide `PROJECT_PLAN.md` l.275 y deja trazabilidad para la memoria.
- **Bloquea:** T11, T12.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 7 — Alcance de la refactorización

- **Pregunta:** los candidatos R1 a R23 suman bastante más que 1,5 h.
- **Opciones:**
  - (a) R1 a R7 (menores heredados) en T6 y R8 a R11 (limpieza de bajo riesgo) en T7. R12 a R23 quedan como deuda técnica con su motivo en §9, como manda `PROJECT_PLAN.md` l.275.
  - (b) Ampliar a R12-R14 si sobra tiempo.
- **Recomendación y motivo:** (a). Ajusta la refactorización a su presupuesto y deja la deuda explícita. El comentario de `MotorDisponibilidad.cs:116` (RN-102) se mantiene como punto crítico.
- **Bloquea:** T6, T7.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Duda 8 — Prueba de RN-171 en la ficha pública

- **Pregunta:** `fase-17.md`, duda 6, dejó para la revisión E-3 de esta fase la prueba automática de que la ficha escapa el texto del propietario.
- **Opciones:**
  - (a) Prueba de integración en `Components/Publico/PruebasFichaEstablecimiento.cs`, renderizando `FichaEstablecimiento` con `HtmlRenderer` (ya usado en el correo) y los servicios de la fixture.
  - (b) Solo comprobación manual.
- **Recomendación y motivo:** (a). RN-171 es ⚑ con verificación I, y una prueba que nunca se ha visto fallar no demuestra nada (AGENTS §11 bis). Si el renderizado del componente exige servicios del marco que la fixture no tiene, se anota en §11 y se cubre con la comprobación manual.
- **Bloquea:** T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-25.

### Notas de implementación (2026-10-25)

**Resultado**
- `dotnet build`: 0 avisos y 0 errores, con `NuGetAudit` activo y sin ningún `NU190x`.
- `dotnet test`: 449 unitarias y 519 de integración, todas en verde, ya con las correcciones de la revisión (partía de 448 y 439).

**T3, revisión E-3.** Se buscaron `MarkupString`, `AddMarkupContent`, `Html.Raw` e interpolación de HTML en las plantillas de `Infraestructura/Correo/Plantillas/` y en `FichaEstablecimiento.razor`, y no aparece ninguno. Las plantillas solo interpolan con `@`. La prueba de la ficha usa `HtmlRenderer` con el `IConsultasCatalogoPublico` de la fixture y `NavegacionPrueba`, sin servicios del marco que falten (duda 8).

**T4, defecto encontrado con `curl`.** El limitador rechazaba el sexto `POST`, pero `UseStatusCodePagesWithReExecute` reejecutaba la petición contra `/not-found`. Esa página fallaba la validación *antiforgery*, y el cliente recibía `400` en lugar de `429`. Con `GET`, como en `/businesses`, no pasaba. Corrección: `OnRejected` desactiva `IStatusCodePagesFeature` y escribe un texto propio (`LimitacionFrecuencia.ResponderRechazoAsync`), con sus pruebas.

**T4, comprobación con `curl`** contra una instancia propia en el puerto 5199, sobre un PostgreSQL desechable que se borró al terminar:
- **Login:** del primer al quinto `POST`, `400` (falta el token *antiforgery*, lo esperado); el sexto y el séptimo, `429` con el mensaje propio. Un `GET /login` con el cupo agotado sigue en `200`.
- **Catálogo:** la petición 61 a `/businesses` da `429`, y también `/businesses/x`, porque comparten cupo. `/` sigue en `200`.
- **Restablecimiento:** `ResetPassword?code=%%%` y sin `code` redirigen a `InvalidPasswordReset`, sin `500`.

**T5, CSP.** La cabecera es la de §14.6, literal. Se quitó `<ImportMap />`. Blazor añadía una segunda cabecera `Content-Security-Policy: frame-ancestors 'self'`, y se desactiva con `ContentSecurityFrameAncestorsPolicy = null`: la propia ya lleva `'none'`. Con `curl`, ninguna página pública ni de cuenta emite *scripts* en línea, atributos `style` ni elementos `<style>`, y `blazor.web.js` responde `200` en `Development`. En `Production` sin publicar, `_framework` no se sirve, pero es un efecto del entorno de la prueba: la imagen de Docker publica. Se ha usado menos de 1 h y R-12 no ha obligado a sacar la CSP. **Navegación manual:** hecha por el desarrollador el 2026-10-25 por las tres áreas, con la consola abierta y sin errores de CSP.

**T6 y T7, decisiones de refactorización:**
- **R4:** `DetalleReservaClienteDto` gana `MotivoNoCancelable`. Es nulo si la reserva es cancelable y lleva el texto exacto que antes montaba la página.
- **R7:** el orden queda en la consulta (`ConsultasReservasEmpresa.Propias`), que comparten el listado y el panel. `PanelEmpresaDto` conserva el orden que recibe. La prueba unitaria se ajusta a eso, y una nueva prueba de integración comprueba el orden del panel.
- **R9:** en `Register.razor`, el alias `TelefonoValidado` evita el choque con la propiedad `Telefono` del formulario, como en `Usuario.cs`.
- **R10:** `VisibleConServiciosAsync` es el único sitio que busca la empresa visible por slug y sus servicios activos.

**T8.** `DestinoLocal`:
- recorta los espacios;
- manda a `/` lo que empieza por `//`, `/\` o `\`, lo que lleva caracteres de control y lo absoluto fuera de `BaseUri` (también `javascript:`);
- comprueba si un destino es absoluto solo cuando no empieza por `/`, porque en Linux `Uri` interpreta `/app` como `file:///app`.

**T9.** Para que H-03 y H-22 tengan prueba automática, la lógica pasa a `Infraestructura/Identidad/RestablecimientoClave.cs` (duda 5):
- comprueba el token con `VerifyUserTokenAsync` antes de `ResetPasswordAsync`;
- los errores de la política de contraseñas solo se ven con un enlace válido;
- el campo oculto guarda el código Base64Url sin decodificar.

**T10.** Los avisos de acceso entre empresas registran la empresa de la sesión y la del recurso, no el usuario, porque darles `IContextoUsuario` a los repositorios de empresa mezclaría familias (AGENTS §8). El `Warning` de solapamiento va en `UnidadTrabajo`, donde se traduce el `23P01`, porque `TraductorErroresPostgres` es estático. Las definiciones compartidas están en `EventosSeguridad` y el doble de prueba compartido es `RegistroCaptura<T>`.

**T11, repaso con `revisor-seguridad`.** Tres hallazgos, los tres corregidos con prueba:

| # | Severidad | Hallazgo | Corrección |
|---|---|---|---|
| 1 | Bloqueante | `POST /login/`, con barra final, llegaba a la página sin contar en `autenticacion` | `GrupoDe` normaliza la barra final; casos `/login/` y `/Account/Login//` |
| 2 | Importante | `/for-business/register` crea una cuenta y revela si el correo existe (H-04), pero no estaba limitado | Añadido al grupo `autenticacion`; casos en `PruebasLimitacionFrecuencia` |
| 3 | Menor | Un `ReturnUrl` con espacios delante (`" //evil.com"`) no estaba probado | `Trim()` antes de validar; casos `" //evil.com"` y una tabulación seguida de dos barras invertidas y `evil.com` |

Sin hallazgos en el aislamiento, las consultas públicas, el cifrado (AES-GCM con nonce de 12 bytes y etiqueta de 16; HMAC-SHA256; dos claves distintas), la inyección y salida, los secretos ni el registro. El resto de H-xx queda clasificado en `docs/seguridad.md` §4.

**`comprobaciones.sh`.** Da 0 bloqueantes y dos importantes:
- **C18, falso positivo:** la clase probada es `FichaEstablecimiento.razor`, no un `.cs`.
- **C17:** `arquitectura.md` §3.2 recoge ya `NuGetAudit`.

### Seguimiento de `/revisar 19` (2026-10-25)

Correcciones de la ronda de las 13:35, a petición del desarrollador. No es una ronda nueva de `/revisar`.

| # | Hallazgo | Cómo se resolvió |
|---|---|---|
| 1 | `UseCabecerasSeguridad` medio en inglés | Renombrado a `UsarCabecerasSeguridad` en `CabecerasSeguridad.cs`, `Program.cs`, `PruebasCabecerasSeguridad.cs` y `arquitectura.md` §10.1 |
| 2 | R8 contradice `arquitectura.md` §2.1 | El desarrollador elige mantener R8. `arquitectura.md` §2.1 recoge una excepción acotada a las constantes de política de `RolesIdentidad` |
| 3 | El caso `/\evil.com` era un `\e` (ESC) | Casos escritos en C# como `"/\\evil.com"` y `"/\\/evil.com"`. El sabotaje que quita solo esa comprobación hace fallar los dos (§6) |
| 4 | `PruebasIntegracion` usa tipos de `Web` sin reflejarlo en la arquitectura | `arquitectura.md` §2.1 y la nota de `InternalsVisibleTo`, y `AGENTS.md` §6 |
| 5 | §2.3 sin los ficheros nuevos | Añadidos `EventosSeguridad`, `InicioSesion`, `RestablecimientoClave`, `EnmascaradoCorreo`, `LimitacionFrecuencia` y `CabecerasSeguridad` |
| 6 | Ampliaciones del radio sin ratificar; falta `PruebasServicioEmailRegistro.cs` | El desarrollador las ratifica (§4), y el fichero se añade a la lista |
| 7 | Dobles de registro duplicados | `PruebasSolicitudRestablecimientoClave` y `PruebasServicioEmailRegistro` usan solo `RegistroCaptura<T>`, que gana `LineasDe(nivel)` |
| 8 | CSP manual sin anotar | Anotada en §7, §11 y §12, en SPEC §21 l.1224, en `pruebas.md`, en `seguridad.md` y en `arquitectura.md` (R-12 y §14.6) |
| 9 y 10 | Falsos positivos de C18 y C17 | Sin cambios: C18 no reconoce la clase `.razor`, y C17 ya está contrastado con §3.2 |

**Segunda ronda (13:58), bloqueante 1: redirección abierta por la rama absoluta.** Con `ReturnUrl=https://<host>///evil.com`, `ToBaseRelativePath` (un recorte de texto) devolvía `//evil.com` sin volver a validarlo. Con el host en mayúsculas lanzaba `ArgumentException` (500). Ahora `DestinoLocal` reduce la URI absoluta propia a `PathAndQuery` y `Fragment` del `Uri` ya analizado, que normaliza `\` a `/` y no distingue mayúsculas en el host, y pasa el resultado por la misma comprobación de ruta local (`EsRutaLocal`). Un destino absoluto propio sale ahora como ruta desde la raíz (`/my-account/...`), equivalente con `<base href="/">`. Hay casos nuevos en `PruebasIdentityRedirectManager` (26 en total) y su sabotaje (§6). Los dos menores de esa ronda no se tocan: no se pidió corregirlos.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos, con `NuGetAudit` activo
- [x] `dotnet test` en verde
- [x] `/revisar` sin hallazgos bloqueantes (`APTO`, ronda del 2026-10-25 14:12)
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo fases 10, 11, 16 y 17)
- [x] Commit hecho (`98aa768`)
- [x] **Control 5 superado**, incluida la navegación manual por las tres áreas con la CSP activa (desarrollador, 2026-10-25)
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6: menores abiertos (l.275) y compatibilidad de la CSP (l.276), resueltos o documentados. *Anotado en la Fase 20: los menores documentales de `/revisar 19` se corrigen allí y los de código quedan como deuda en `PROJECT_PLAN.md` §7*
