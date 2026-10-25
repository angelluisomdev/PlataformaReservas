# Fase 16b — Notificaciones: servicio de correo, 11 plantillas, manejadores y pruebas

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 9 h (SPEC §19) |
| **Semana** | 5 (`PROJECT_PLAN.md` §2, l.113-125), la última de las funcionales: "necesita que **todos** los casos de uso existan" |
| **Cierra** | Control 4b (`PROJECT_PLAN.md` §3, l.194-203) |
| **Generada** | 2026-10-18 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar, cada evento de dominio que ya se publica produce su correo **después del commit y en segundo plano**:

- los correos salen de 11 plantillas Razor renderizadas con `HtmlRenderer`, de RN-146 a RN-156;
- por defecto se usa `ServicioEmailRegistro`, que no envía nada, y solo con `Correo:Habilitado = true` se usa `ServicioEmailSmtp` (MailKit);
- un fallo de correo nunca deshace la operación ni mata el procesador;
- la recuperación de contraseña funciona de verdad, para cliente y para propietario, con `RemitenteIdentity` en lugar del `IdentityNoOpEmailSender` de la plantilla.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §16 bis (l.1011-1042) | Qué se notifica y a quién, y "Las tres reglas que no se negocian": nada dentro de una transacción, un fallo no deshace nada y el envío real está desactivado por defecto |
| SPEC §21 (l.1220-1223, l.1230) | Correo caído y reserva creada; baja de empresa igual de rápida con muchas reservas; recuperación para cliente y propietario; `<script>` no se ejecuta en el correo |
| D-20, D-21 | Notificaciones por correo dentro del MVP; eventos publicados por el caso de uso después del commit, con cola en memoria |
| E-3 (SPEC l.940) | `Empresa.Descripcion` y `Empresa.Nombre` acaban en HTML de correo. La revisión completa es de la Fase 19 |
| A-11 a A-14, A-16 (`arquitectura.md` l.25-30) | Publicación después del commit; `Channel<T>` más `BackgroundService`; `IServicioEmail` en `Aplicacion` y MailKit en `Infraestructura`; plantillas Razor con `HtmlRenderer`; `MarkupString` prohibido |
| `arquitectura.md` §11.3 (l.895-911) | Sección `Correo` con `Habilitado` en `false`, `Servidor`, `Puerto`, `Usuario`, `Contrasena`, `Remitente` y `NombreRemitente`. Las credenciales SMTP van en User Secrets |
| `arquitectura.md` §13.3-§13.9 | `IServicioEmail.EnviarAsync(MensajeCorreo, ct)`, `MensajeCorreo(Para, Asunto, CuerpoHtml)`, `ServicioEmailSmtp` y `ServicioEmailRegistro`, el `LayoutCorreo` común, los manejadores que vuelven a leer con `IConsultasNotificacion` (sin contexto de empresa), `RemitenteIdentity`, los ciclos de vida y las pruebas |
| RN-140 ⚑, RN-141 ⚑, RN-142, RN-143, RN-144, RN-145 | El mecanismo: después del commit; un fallo se registra y se sigue; en segundo plano; el manejador vuelve a leer; la cola es volátil; nada se envía en desarrollo ni en pruebas |
| RN-146 a RN-149 | Reserva confirmada (al cliente y al negocio), cancelada por el cliente (al negocio) y cancelada por la empresa (al cliente) |
| RN-150 ⚑, RN-151 | Avisos de las cascadas: baja de la empresa (al cliente) y baja de la cuenta (al negocio) |
| RN-152, RN-153 | Empresa registrada (enlace a la ficha y pasos que faltan) y empresa dada de baja (recuento y cómo reactivar) |
| RN-154, RN-155 | Cuenta registrada (bienvenida) y cuenta dada de baja (recuento) |
| RN-156 ⚑, RN-157, RN-158 ⚑, RN-159, RN-160, RN-161 | Recuperación de contraseña: el mismo flujo para cliente y propietario, la misma respuesta siempre, token de Identity, nada para una cuenta inactiva y sello renovado |
| RN-171 ⚑ | `MarkupString` prohibido en las plantillas; todo texto del usuario se codifica |
| RN-172 | Nunca datos personales enteros en el registro |
| CU-04 (l.560) | "se encola el correo de bienvenida" |
| CU-07, pasos 11-13 y 13a (l.225-238) | Después del commit salen dos correos, y con el correo caído "**La reserva sigue creada.**" |
| CU-10, paso 8 (l.280) | El negocio recibe el aviso (RN-148) |
| CU-11, paso 8 (l.322) | El propietario recibe el enlace y los pasos pendientes (RN-152) |
| CU-20 (l.587) | "avisa al cliente por correo" |
| CU-22, paso 9 (l.418) | Confirmación al usuario (RN-155) y aviso a cada negocio (RN-151) |
| CU-23, pasos 7-8 (l.464-465) | N + 1 eventos: confirmación al propietario (RN-153) y aviso a cada cliente (RN-150) |
| CU-24 | Recuperación de contraseña (RN-156 a RN-161) |
| `modelo-dominio.md` §10 bis (l.832-854) | Los seis eventos y `OrigenCancelacion`. `CuentaRegistrada` "se registrará en la Fase 16b" |
| `casos-uso.md` §6 (l.677-684) | Desglose de las 9 h: 2 h de servicio y configuración, 3,5 h de plantillas, 2 h de manejadores y 1,5 h de pruebas |
| `fase-06.md` duda 1, `fase-06-revision.md` menor 10 | `RemitenteIdentity` llega en la 16b. El `IdentityNoOpEmailSender` de la plantilla no sirve como base |
| `fase-11b.md` l.262-266 y duda 7 | Lo que se dejó para la 16b, también publicar `CuentaRegistrada` |
| `fase-11b-revision.md` l.33-34 y l.132 | Que los fallos no registren datos personales, y que los manejadores de un mismo evento comparten hoy un único `try/catch` |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `ColaEventos` (Singleton, `IPublicadorEventos`) y `ProcesadorEventos` (hosted service, un ámbito por evento, `try/catch`) | ✔ comprobado (Fase 11b) |
| `IManejadorEvento<in TEvento>`, resuelto por el tipo cerrado del evento; hoy no hay ningún manejador en `src` | ✔ comprobado |
| Los seis eventos y `OrigenCancelacion` en `Dominio/Eventos` | ✔ comprobado |
| Casos de uso que publican después del commit: `CrearReserva`, `CancelarReservaCliente`, `CancelarReservaEmpresa`, `RegistrarEmpresa`, `DarDeBajaEmpresa` y `DarDeBajaCuenta` | ✔ comprobado |
| `CuentaRegistrada`, publicado por alguien | ✘ no se publica en ningún sitio: duda 7 |
| MailKit 4.18.0 en `Directory.Packages.props` y referenciado por `Infraestructura` | ✔ comprobado (sin uso) |
| `FrameworkReference Microsoft.AspNetCore.App` en `Infraestructura` (`HtmlRenderer` disponible) | ✔ comprobado |
| SDK Razor en `Infraestructura`, para compilar las plantillas `.razor` | ✘ el proyecto usa `Microsoft.NET.Sdk`: duda 10 |
| `SolicitudRestablecimientoClave`, que usa `IEmailSender<Usuario>`, y `ForgotPassword.razor` | ✔ comprobado |
| `IdentityNoOpEmailSender`, registrado como Singleton en `Program.cs:18` | ✔ comprobado (se sustituye) |
| `CondicionPublicable` (RN-16), para los pasos que faltan de RN-152 | ✔ comprobado (Fase 16) |
| `RemitenteCaptura`, `PublicadorCaptura` y `BaseDatosFixture` en las pruebas | ✔ comprobado |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | Los eventos ya existen |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IServicioEmail.cs` y `MensajeCorreo.cs`. `Abstracciones/IRenderizadorPlantillas.cs` (duda 4). `Abstracciones/IConsultasNotificacion.cs`. `Dtos/Notificaciones/` (`DatosReservaNotificacionDto`, `DatosEmpresaNotificacionDto`, `DatosCuentaNotificacionDto`). `Notificaciones/` (los seis manejadores de §5). `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | **Añadido al implementar T6 (2026-10-24):** `Consultas/CondicionPublicable.cs` (los tres indicadores parciales de RN-19 pasan aquí, junto a RN-16) y `Consultas/ConsultasReservasEmpresa.cs` (los usa en lugar de repetirlos), para que el panel y el correo de RN-152 no tengan dos copias. `Correo/` (`ServicioEmailRegistro`, `ServicioEmailSmtp`, `CorreoNoEnviadoException`, `RenderizadorPlantillas` y `Plantillas/`, con `LayoutCorreo.razor`, las 11 plantillas, `_Imports.razor` y `FormatoCorreo.cs`). **Añadido tras `/revisar 16b` (menores 4 y 7):** `Correo/UrlBaseCorreo.cs`, la única validación de `Correo:UrlBase`. `Consultas/ConsultasNotificacion.cs`. `Identidad/RemitenteIdentity.cs`. `Identidad/RegistroCliente.cs` (duda 7). `Eventos/ProcesadorEventos.cs` (duda 8). `InyeccionDependencias.cs`. `PlataformaReservas.Infraestructura.csproj`, solo el SDK (duda 10) |
| `src/PlataformaReservas.Web` | sí | **Excepción al radio, autorizada por el desarrollador el 2026-10-24 al corregir `/revisar 16b` (hallazgos 1 y 2):** `Cuenta/Pages/ForgotPassword.razor` deja de construir el enlace con el `Host` de la petición, e `Infraestructura/Identidad/SolicitudRestablecimientoClave.cs` lo construye con `Correo:UrlBase` y absorbe los fallos de envío. `Program.cs` (registro del remitente de Identity). `appsettings.json` y `appsettings.Development.json` (sección `Correo`, sin credenciales). Se borra `Cuenta/IdentityNoOpEmailSender.cs` |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Notificaciones/Pruebas<Manejador>.cs` (uno por manejador) y sus dobles. `Dtos/Notificaciones/PruebasDatosReservaNotificacionDto.cs` (añadido tras `/revisar 16b`, menor 7) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Correo/PruebasServicioEmailRegistro.cs`, `Correo/PruebasRenderizadorPlantillas.cs`, `Correo/PruebasUrlBaseCorreo.cs` (tras `/revisar 16b`, menor 4), `Consultas/PruebasConsultasNotificacion.cs`, `Identidad/PruebasRemitenteIdentity.cs`, `Identidad/PruebasRegistroCliente.cs`, `Identidad/PruebasSolicitudRestablecimientoClave.cs`, `Eventos/PruebasProcesadorEventos.cs`, `CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs` (Control 4b), `PruebasInyeccionDependencias.cs` y `Infraestructura/BaseDatosFixture.cs` |
| `docs/` | sí | `casos-uso.md` (criterios de CU-07, CU-10, CU-23 y CU-24, y CU-24 paso 3, duda 2). `arquitectura.md` §10.1 (duda 11), §11.3 (`UrlBase`, duda 9), §13.3-§13.7 (las firmas que fija esta fase) y l.176. `reglas-negocio.md`, solo si una duda lo exige. Este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- el dominio ni los eventos;
- los casos de uso que ya publican;
- ninguna página Razor salvo el borrado del remitente de la plantilla;
- ningún secreto, credencial ni fichero `*.local.json` (las credenciales SMTP las pone el desarrollador en User Secrets);
- ningún paquete nuevo.

## 5. Tareas

### T1 — Servicio de correo y configuración (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IServicioEmail.cs` y `MensajeCorreo.cs`;
  - crea `Infraestructura/Correo/ServicioEmailRegistro.cs`, `ServicioEmailSmtp.cs` y `CorreoNoEnviadoException.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs`, `Web/appsettings.json` y `Web/appsettings.Development.json`;
  - pruebas en `Correo/PruebasServicioEmailRegistro.cs` y `PruebasInyeccionDependencias.cs`.
- **Implementa:** RN-145, RN-172, A-13, R-7, `arquitectura.md` §11.3 y §13.3.
- **Contenido:**
  - **Firmas** de `arquitectura.md` §13.3: `Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)` y `MensajeCorreo(string Para, string Asunto, string CuerpoHtml)`.
  - **`ServicioEmailRegistro`** registra con nivel `Information` el destinatario **enmascarado** y el asunto. **Nunca el cuerpo**, que puede llevar el token de restablecimiento (duda 3).
  - **`ServicioEmailSmtp`** envía con MailKit usando la sección `Correo`. Envuelve las excepciones de MailKit en `CorreoNoEnviadoException`, sin la dirección de destino (duda 12).
  - **`Correo`** en `appsettings.json` lleva `Habilitado: false`, el resto de claves vacías y `UrlBase` (duda 9). Nunca credenciales.
  - **En DI**, `IServicioEmail` es Scoped: `ServicioEmailSmtp` solo si `Correo:Habilitado`, y si no, `ServicioEmailRegistro`.
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Renderizador y plantilla común (≈ 0,75 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IRenderizadorPlantillas.cs`;
  - crea `Infraestructura/Correo/RenderizadorPlantillas.cs` y `Infraestructura/Correo/Plantillas/LayoutCorreo.razor`;
  - modifica `PlataformaReservas.Infraestructura.csproj`, solo el SDK (duda 10), e `Infraestructura/InyeccionDependencias.cs`;
  - pruebas en `Correo/PruebasRenderizadorPlantillas.cs`.
- **Implementa:** A-14, RN-171, `arquitectura.md` §13.4 y §13.7.
- **Contenido:**
  - **`IRenderizadorPlantillas`** tiene un método por correo y cada uno devuelve un `MensajeCorreo` completo (duda 4). Los once métodos se nombran en T3-T5.
  - **`RenderizadorPlantillas`** usa `HtmlRenderer` en Scoped (§13.7: "no es seguro para hilos"). Todas las plantillas comparten `LayoutCorreo`.
  - Ningún `MarkupString` y ningún HTML montado con cadenas: Blazor codifica.
- **Hecho cuando:** el renderizador produce un HTML con el layout y una prueba lo comprueba.
- **Verificación:** `dotnet test`.

### T3 — Plantillas del ciclo de reserva (≈ 1 h)

- **Ficheros:**
  - crea, en `Infraestructura/Correo/Plantillas/`, `ReservaConfirmadaCliente.razor`, `ReservaConfirmadaNegocio.razor`, `ReservaCanceladaPorCliente.razor` y `ReservaCanceladaPorEmpresa.razor`;
  - modifica `RenderizadorPlantillas.cs` e `IRenderizadorPlantillas.cs`;
  - pruebas en `PruebasRenderizadorPlantillas.cs`.
- **Implementa:** RN-146, RN-147, RN-148, RN-149, RN-171.
- **Contenido:** el contenido mínimo de cada regla, con la fecha y la hora en hora local con `MotorDisponibilidad.ALocal`.

  | Regla | Destinatario | Contenido mínimo |
  |---|---|---|
  | RN-146 | Cliente | Establecimiento, dirección, servicio, profesional, fecha, hora, duración y precio |
  | RN-147 | Negocio | Servicio, profesional, fecha, hora, y nombre y teléfono del cliente |
  | RN-148 | Negocio | Los mismos datos, indicando que la cancela el cliente |
  | RN-149 | Cliente | Los mismos datos, indicando que la cancela el establecimiento |

- **Hecho cuando:** las pruebas de T3 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T4 — Plantillas de las cascadas y de la empresa (≈ 1 h)

- **Ficheros:**
  - crea `ReservaCanceladaPorBajaEmpresa.razor`, `ReservaCanceladaPorBajaCuenta.razor`, `EmpresaRegistrada.razor` y `EmpresaDadaDeBaja.razor`;
  - modifica `RenderizadorPlantillas.cs` e `IRenderizadorPlantillas.cs`;
  - pruebas en `PruebasRenderizadorPlantillas.cs`.
- **Implementa:** RN-150, RN-151, RN-152, RN-153, RN-171.
- **Contenido:**

  | Regla | Destinatario | Contenido |
  |---|---|---|
  | RN-150 | Cliente | Cita anulada porque el establecimiento ha cerrado |
  | RN-151 | Negocio | Cita anulada porque el cliente ha dado de baja su cuenta |
  | RN-152 | Propietario | Enlace absoluto a su ficha pública, con `Correo:UrlBase` (duda 9), y los pasos que faltan para publicarla |
  | RN-153 | Propietario | Confirmación, número de reservas canceladas (duda 6) y cómo reactivar (enlace a `/app/settings`) |

  `Empresa.Nombre` y `Empresa.Descripcion` siempre van codificados (E-3).
- **Hecho cuando:** las pruebas de T4 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T5 — Plantillas de la cuenta y de la recuperación (≈ 0,75 h)

- **Ficheros:**
  - crea `CuentaRegistrada.razor`, `CuentaDadaDeBaja.razor` y `RestablecimientoClave.razor`;
  - modifica `RenderizadorPlantillas.cs` e `IRenderizadorPlantillas.cs`;
  - pruebas en `PruebasRenderizadorPlantillas.cs`.
- **Implementa:** RN-154, RN-155, RN-156, RN-171.
- **Contenido:**
  - RN-154: bienvenida.
  - RN-155: confirmación y número de reservas canceladas.
  - RN-156: el enlace con el token de un solo uso que da Identity, sin tocarlo.
- **Hecho cuando:** las pruebas de T5 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T6 — Consulta de datos para notificar (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IConsultasNotificacion.cs`;
  - crea `Aplicacion/Dtos/Notificaciones/DatosReservaNotificacionDto.cs`, `DatosEmpresaNotificacionDto.cs` y `DatosCuentaNotificacionDto.cs`;
  - crea `Infraestructura/Consultas/ConsultasNotificacion.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs`;
  - pruebas en `Consultas/PruebasConsultasNotificacion.cs`.
- **Implementa:** RN-143, RN-152, RN-153, RN-155, D-22, AGENTS §8 quater, `arquitectura.md` §13.5 (l.1222).
- **Contenido:**
  - **Sin contexto.** No recibe `IContextoEmpresa` ni `IContextoUsuario`: en segundo plano no hay usuario (§13.5). Filtra solo por el identificador del evento.
  - **Datos personales.** El correo y el nombre del usuario se leen con `UserManager.FindByIdAsync`, nunca con LINQ sobre `Users` (RN-167, R-10). Las copias de la reserva se descifran con sus conversores.
  - **Destinatarios.** El negocio es `Empresa.Email` y el propietario, el usuario de `MiembroEmpresa` (duda 5).
  - **Recuentos (RN-153, RN-155).** Son las reservas canceladas con `FechaCancelacion == OcurridoEnUtc` del evento (duda 6).
  - **Pasos que faltan (RN-152).** Con `CondicionPublicable` y los indicadores de la Fase 16.
  - **Inexistente.** Si el recurso no existe, devuelve `null` y el manejador no envía nada (§13.5).
- **Hecho cuando:** las pruebas de T6 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T7 — Manejadores de reserva (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Notificaciones/EnviarCorreosReservaConfirmada.cs` (el nombre de `arquitectura.md` §13.5) y `EnviarCorreoReservaCancelada.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs` (Scoped, §13.7);
  - pruebas unitarias en `Notificaciones/PruebasEnviarCorreosReservaConfirmada.cs` y `PruebasEnviarCorreoReservaCancelada.cs`.
- **Implementa:** RN-143, RN-146 a RN-151, CU-07 paso 13, CU-10 paso 8, CU-20, CU-22 paso 9, CU-23 paso 8.
- **Contenido:**
  - **`ReservaConfirmada`.** Vuelve a leer y envía dos correos: al cliente (RN-146) y al negocio (RN-147).
  - **`ReservaCancelada`.** Elige el destinatario y la plantilla según `Origen`:

    | Origen | Destinatario | Regla |
    |---|---|---|
    | `Cliente` | Negocio | RN-148 |
    | `Empresa` | Cliente | RN-149 |
    | `BajaEmpresa` | Cliente | RN-150 |
    | `BajaCuenta` | Negocio | RN-151 |

  - Si la consulta devuelve `null`, no envía nada.
- **Hecho cuando:** las pruebas de T7 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T8 — Manejadores de empresa y cuenta, y publicar `CuentaRegistrada` (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Notificaciones/EnviarCorreoEmpresaRegistrada.cs`, `EnviarCorreoEmpresaDadaDeBaja.cs`, `EnviarCorreoCuentaRegistrada.cs` y `EnviarCorreoCuentaDadaDeBaja.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs` e `Infraestructura/Identidad/RegistroCliente.cs` (duda 7);
  - pruebas unitarias en `Notificaciones/Pruebas<Manejador>.cs` (una por manejador) y de integración en `Identidad/PruebasRegistroCliente.cs`.
- **Implementa:** RN-152 a RN-155, CU-04, CU-11 paso 8, CU-22 paso 9, CU-23 paso 8, `modelo-dominio.md` l.846.
- **Contenido:**
  - **Los cuatro manejadores** vuelven a leer y envían su correo: RN-152 al propietario, RN-153 con recuento, RN-154 de bienvenida y RN-155 con recuento.
  - **`RegistroCliente`** publica `CuentaRegistrada(usuarioId, ahora)` después de crear el usuario y guardar (duda 7). El propietario no recibe RN-154: recibe RN-152.
- **Hecho cuando:** las pruebas de T8 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T9 — `RemitenteIdentity` (≈ 0,5 h)

- **Ficheros:**
  - crea `Infraestructura/Identidad/RemitenteIdentity.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs` y `Web/Program.cs`;
  - borra `Web/Cuenta/IdentityNoOpEmailSender.cs`;
  - pruebas en `Identidad/PruebasRemitenteIdentity.cs` y `Identidad/PruebasSolicitudRestablecimientoClave.cs`.
- **Implementa:** RN-156, RN-157, RN-158, RN-160, CU-24, `arquitectura.md` §13.6.
- **Contenido:**
  - **`RemitenteIdentity(IServicioEmail, IRenderizadorPlantillas) : IEmailSender<Usuario>`**, Scoped (duda 11).
  - **`SendPasswordResetLinkAsync`** envía la plantilla RN-156 **de forma síncrona, sin pasar por la cola** (duda 2).
  - **Los otros dos métodos** (`SendConfirmationLinkAsync` y `SendPasswordResetCodeAsync`) lanzan `NotSupportedException`: no tienen plantilla y nada los llama (§13.6). *Corregido tras `/revisar 16b`, menor 3; la redacción original decía "implementados y sin uso".*
  - **El enlace** lo construye `SolicitudRestablecimientoClave` con `Correo:UrlBase` y llega sin codificar; la plantilla lo codifica una sola vez. *Corregido tras `/revisar 16b`; la redacción original decía que llegaba codificado desde `ForgotPassword.razor`.*
- **Hecho cuando:** las pruebas de T9 de §6 están en verde y el enlace llega al correo capturado tanto para un cliente como para un propietario.
- **Verificación:** `dotnet test`.

### T10 — Robustez del procesador, Control 4b y documentación (≈ 1 h)

- **Ficheros:**
  - modifica `Infraestructura/Eventos/ProcesadorEventos.cs` (duda 8);
  - pruebas en `Eventos/PruebasProcesadorEventos.cs` y `CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs`;
  - documentos de §4.
- **Implementa:** RN-140, RN-141, RN-142, R-6, Control 4b, AGENTS §11 bis (sabotajes).
- **Contenido:**
  - **Un `try/catch` por manejador**, dentro del bucle de manejadores. Si uno lanza, se registra con nivel `Error` (tipo de evento y tipo de excepción) y se sigue con el siguiente. El `try/catch` que envuelve el bucle se mantiene (R-6).
  - **RN-141.** El manejador real de `ReservaConfirmada`, con un `IServicioEmail` que siempre lanza: el procesador registra el fallo, sigue vivo y procesa el evento siguiente, y la reserva sigue creada.
  - **Control 4b.** Dar de baja una empresa con 300 reservas futuras publica 301 eventos sin enviar ningún correo dentro del caso de uso (RN-142): la respuesta no depende de N.
  - **Sabotajes y documentos.**
- **Hecho cuando:** las pruebas de T10 están en verde, los sabotajes de §6 han fallado su prueba y los documentos están corregidos.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 9 h de 9 h presupuestadas** (1 + 0,75 + 1 + 1 + 0,75 + 1 + 1 + 1 + 0,5 + 1).

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `ServicioEmailRegistro` registra el destinatario enmascarado y el asunto; el registro no contiene el cuerpo ni el enlace | I | `Correo/PruebasServicioEmailRegistro.cs` | RN-145, RN-172 | T1 |
| Sin `Correo:Habilitado` (o en `false`), `IServicioEmail` resuelve `ServicioEmailRegistro`; con `true`, `ServicioEmailSmtp`; Scoped | I | `PruebasInyeccionDependencias.cs` | RN-145, R-7 | T1 |
| `appsettings.json` y `appsettings.Development.json` tienen `Correo:Habilitado = false` | I | `PruebasInyeccionDependencias.cs` | R-7, AGENTS §13 | T1 |
| El renderizador envuelve cada correo en `LayoutCorreo` y devuelve `Para`, `Asunto` y `CuerpoHtml` | I | `Correo/PruebasRenderizadorPlantillas.cs` | A-14 | T2 |
| Un `<script>alert(1)</script>` en el nombre o la descripción de la empresa sale **escapado** en el HTML | I | `Correo/PruebasRenderizadorPlantillas.cs` | RN-171 | T2-T4 |
| Cada una de las 11 plantillas lleva el contenido mínimo de su regla y el destinatario correcto (una prueba por regla) | I | `Correo/PruebasRenderizadorPlantillas.cs` | RN-146 a RN-156 | T3-T5 |
| La hora del correo es la hora local de Madrid, no la UTC | I | `Correo/PruebasRenderizadorPlantillas.cs` | AGENTS §9 | T3 |
| Los datos de la reserva: establecimiento, dirección, servicio, profesional, inicio, duración, precio, cliente descifrado, correo del cliente (por `UserManager`) y `Empresa.Email` | I | `Consultas/PruebasConsultasNotificacion.cs` | RN-143, D-22, R-10 | T6 |
| Datos de la empresa: correo del propietario, slug, los pasos que faltan y el recuento de canceladas en el instante del evento | I | `Consultas/PruebasConsultasNotificacion.cs` | RN-152, RN-153 | T6 |
| Datos de la cuenta: correo y nombre del usuario, aunque esté inactivo, y el recuento de canceladas en el instante del evento | I | `Consultas/PruebasConsultasNotificacion.cs` | RN-154, RN-155 | T6 |
| Una reserva, empresa o cuenta inexistente devuelve `null` | I | `Consultas/PruebasConsultasNotificacion.cs` | §13.5 | T6 |
| `ReservaConfirmada` envía dos correos, al cliente y al negocio; si no hay datos, ninguno | U | `Notificaciones/PruebasEnviarCorreosReservaConfirmada.cs` | RN-146, RN-147 | T7 |
| `ReservaCancelada`: con cada uno de los cuatro orígenes, el destinatario y la plantilla correctos; sin datos, ninguno | U | `Notificaciones/PruebasEnviarCorreoReservaCancelada.cs` | RN-148 a RN-151 | T7 |
| Los cuatro manejadores de empresa y cuenta envían su correo al destinatario correcto; sin datos, ninguno | U | `Notificaciones/PruebasEnviarCorreoEmpresaRegistrada.cs`, `…EmpresaDadaDeBaja.cs`, `…CuentaRegistrada.cs`, `…CuentaDadaDeBaja.cs` | RN-152 a RN-155 | T8 |
| Registrar un cliente publica `CuentaRegistrada` con su identificador después de guardar; un registro fallido no publica nada | I | `Identidad/PruebasRegistroCliente.cs` | RN-154, CU-04 | T8 |
| `RemitenteIdentity` envía la plantilla RN-156 al correo indicado con el enlace intacto | I | `Identidad/PruebasRemitenteIdentity.cs` | RN-156 | T9 |
| Un propietario solicita el restablecimiento y recibe el enlace, igual que un cliente | I | `Identidad/PruebasSolicitudRestablecimientoClave.cs` | RN-157, CU-24 | T9 |
| La respuesta es la misma con un correo inexistente (sigue en verde con el remitente real) | I | `Identidad/PruebasSolicitudRestablecimientoClave.cs` | RN-158 | T9 |
| Un manejador que lanza no impide que se ejecute el siguiente manejador del mismo evento; el fallo se registra como `Error` | I | `Eventos/PruebasProcesadorEventos.cs` | RN-141, R-6 | T10 |
| Con un `IServicioEmail` que siempre lanza, el manejador real de `ReservaConfirmada` registra el fallo, el procesador sigue vivo y procesa el siguiente evento | I | `Eventos/PruebasProcesadorEventos.cs` | RN-140, RN-141, Control 4b | T10 |
| Dar de baja una empresa con 300 reservas futuras publica 301 eventos y no llama a `IServicioEmail` | I | `CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs` | RN-142, RN-150, Control 4b | T10 |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `ServicioEmailSmtp` | Prueba automática | No hay servidor SMTP en las pruebas ni credenciales (las pone el desarrollador). Se cubre con la verificación manual, con `Correo:Habilitado` en `true` en sus User Secrets y Mailtrap (R-8) |
| Manejadores | Validación; recurso de otra empresa; transacción; eventos | No tienen comando ni contexto, no escriben y no publican. El "inexistente" se prueba (consulta `null`) |
| `ConsultasNotificacion` | Filtro por empresa en origen | Es deliberado: no hay contexto en segundo plano (§13.5, l.1222). Filtra por el identificador del evento, y se prueba que no mezcla datos de otra reserva o empresa |
| Plantillas `.razor` | Prueba por fichero | Son componentes sin lógica. Se prueban a través de `PruebasRenderizadorPlantillas` (una prueba por regla) |
| `MensajeCorreo`, DTO de notificación | Casos de objeto de valor | Son records sin validación ni `ConvertirADto` con reglas |

**Verificación manual** (la hace el desarrollador, porque exige credenciales):

- [ ] Con `Correo:Habilitado` en `true` en sus User Secrets y Mailtrap, una reserva produce los dos correos y se ven bien.
- [ ] Una recuperación de contraseña de cliente y otra de propietario llegan y el enlace funciona.
- [ ] Con `Habilitado` en `false` no sale nada y el registro solo muestra destinatarios enmascarados.
- [ ] Entregabilidad, SPF y DKIM (`PROJECT_PLAN.md` §6, l.278): sin horas presupuestadas; se anota el resultado.

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-141 | En `ProcesadorEventos`, quitar el `try/catch` por manejador y dejar solo el del bucle | `Un manejador que lanza no impide que se ejecute el siguiente…` | Hecho el 2026-10-24: fallaron `Un_manejador_que_lanza_no_impide_que_se_ejecute_el_siguiente_manejador_del_mismo_evento` y `Con_el_correo_caido_el_manejador_real_registra_el_fallo_y_el_procesador_sigue_con_el_siguiente_evento`. Restaurado |
| RN-150 | En `EnviarCorreoReservaCancelada`, enviar el caso `BajaEmpresa` al negocio en lugar de al cliente | `ReservaCancelada: con cada uno de los cuatro orígenes…` | Hecho el 2026-10-24 (plantilla de baja de cuenta, que va al negocio): falló `Elige_destinatario_y_plantilla_segun_el_origen(BajaEmpresa, …)`. Restaurado |
| RN-171 | En una plantilla, pintar la descripción con `(MarkupString)` | `Un <script> … sale escapado` (y la comprobación mecánica C06 de `/revisar`) | Hecho en T3 (2026-10-18): `@((MarkupString)Datos.Establecimiento)` en `ReservaConfirmadaCliente.razor` hizo fallar `Un_script_en_el_nombre_del_establecimiento_sale_escapado_en_el_html`, y `comprobaciones.sh` dio BLOQUEANTE C06. Restaurado |

## 7. Criterios de aceptación

- [x] CU-07: "Salen dos correos: al cliente y al negocio." (`PruebasEnviarCorreosReservaConfirmada`, `PruebasRenderizadorPlantillas`)
- [x] CU-07: "**Con el servicio de correo caído, la reserva se crea igualmente** y el fallo queda registrado." (`PruebasProcesadorEventos`, con el manejador real y un correo que siempre falla; la reserva se confirma antes de encolar, `PruebasCrearReserva`)
- [x] CU-10: "El negocio recibe el aviso de cancelación." (`PruebasEnviarCorreoReservaCancelada`, origen `Cliente`)
- [x] CU-23: "Cada cliente con cita futura recibe su aviso." (`PruebasDarDeBajaEmpresa` publica un `ReservaCancelada(BajaEmpresa)` por cita, y `PruebasEnviarCorreoReservaCancelada` lo envía al cliente)
- [x] CU-23: "**Con 300 reservas futuras, el botón responde igual de rápido que con 3.**" (`Con_300_reservas_futuras_publica_301_eventos…`: el caso de uso solo encola; ningún correo se envía dentro)
- [x] CU-24: "Un cliente y un propietario usan el mismo flujo con idéntico resultado." (`Un_propietario_usa_el_mismo_flujo…`, `PruebasRemitenteIdentity`)
- [x] SPEC §21: "Una descripción de empresa con `<script>` **no se ejecuta** ni en la ficha pública ni en el correo" (la parte del correo). Ninguna plantilla incluye la descripción; el nombre de la empresa y del cliente salen escapados (`Un_script_en_el_nombre_del_establecimiento_sale_escapado_en_el_html`).
- [x] Ningún `MarkupString` en `Infraestructura/Correo/` y ninguna credencial en el repositorio (`comprobaciones.sh` limpio; `La_configuracion_del_repositorio_no_habilita_el_envio_de_correo`).
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (447 unitarias, 393 de integración).

**Control 4b** (`PROJECT_PLAN.md` §3, literal):

```text
Con el servicio de correo caido, la reserva se crea igualmente
Dar de baja una empresa con N reservas responde igual de rapido con 3 que con 300
El procesador sigue vivo tras un manejador que lanza
Recuperacion de contrasena funciona para cliente y para propietario
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-6 | Una excepción de un manejador (por ejemplo, SMTP caído) escapa y mata el `BackgroundService`; no vuelve a salir ningún correo | Un `try/catch` por manejador más el que envuelve el bucle (duda 8), y las dos pruebas de T10 |
| R-7 | El desarrollo o las pruebas envían correos reales | `Habilitado` en `false` en `appsettings.json` y en `appsettings.Development.json`, con prueba. Las pruebas usan dobles. Las credenciales nunca en el repositorio |
| R-8 | Los correos llegan a spam | Verificación manual del desarrollador con Mailtrap y su proveedor; sin horas (`PROJECT_PLAN.md` l.278) |
| R-10 | `ConsultasNotificacion` busca el correo del usuario con LINQ y obtiene vacío sin error | `UserManager.FindByIdAsync`; la prueba de T6 comprueba el correo descifrado |
| R-1 | `HtmlRenderer` compartido entre ámbitos o hilos | `IRenderizadorPlantillas` Scoped (§13.7); un ámbito por evento en el procesador |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Patrón *outbox* y reintento de correos fallidos | Trabajo futuro (SPEC §6.2; `reglas-negocio.md` §13, l.410-411; RN-144) |
| Verificación del correo al registrarse y recordatorios antes de la cita | Fuera del MVP (`reglas-negocio.md` l.307-312) |
| Correo por excepción de horario (RN-46) o por reserva completada (CU-21) | No se notifican (`reglas-negocio.md` l.307-312) |
| Revisión E-3 completa de las once plantillas y de la ficha | Fase 19 (`arquitectura.md` l.1331) |
| Limitación de frecuencia en `/Account/ForgotPassword` | Fase 19 (`arquitectura.md` §14.5) |
| SPF, DKIM y alta en el proveedor transaccional | El desarrollador, sin horas |
| Actualizar `PROJECT_PLAN.md` §7 | No asignado a ninguna fase |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-140, RN-141 | T10 | `PruebasProcesadorEventos` (correo que siempre falla, manejador que lanza); sabotaje |
| RN-142 | T10 | `PruebasDarDeBajaEmpresa` (300 reservas) |
| RN-143 | T6, T7, T8 | `PruebasConsultasNotificacion`; los manejadores solo reciben identificadores |
| RN-144 | — | Limitación declarada; sin prueba (verificación `—`) |
| RN-145 | T1 | `PruebasServicioEmailRegistro`, `PruebasInyeccionDependencias` |
| RN-146, RN-147 | T3, T7 | `PruebasRenderizadorPlantillas`, `PruebasEnviarCorreosReservaConfirmada` |
| RN-148 a RN-151 | T3, T4, T7 | `PruebasRenderizadorPlantillas`, `PruebasEnviarCorreoReservaCancelada`; sabotaje RN-150 |
| RN-152, RN-153 | T4, T6, T8 | `PruebasRenderizadorPlantillas`, `PruebasConsultasNotificacion`, manejadores de empresa |
| RN-154, RN-155 | T5, T6, T8 | `PruebasRenderizadorPlantillas`, `PruebasConsultasNotificacion`, manejadores de cuenta, `PruebasRegistroCliente` |
| RN-156 a RN-158, RN-160 | T5, T9 | `PruebasRemitenteIdentity`, `PruebasSolicitudRestablecimientoClave` |
| RN-159, RN-161 | — (ya implementadas, Fase 6) | `PruebasSolicitudRestablecimientoClave` |
| RN-171 | T2-T5 | `PruebasRenderizadorPlantillas` (escapado); sabotaje |
| RN-172 | T1 | `PruebasServicioEmailRegistro` |
| CU-04 | T8 | `PruebasRegistroCliente` |
| CU-07, CU-10, CU-20, CU-22, CU-23 | T7, T8, T10 | Manejadores; `PruebasProcesadorEventos`; `PruebasDarDeBajaEmpresa` |
| CU-11 | T4, T8 | `PruebasEnviarCorreoEmpresaRegistrada` |
| CU-24 | T9 | `PruebasSolicitudRestablecimientoClave`; manual |

## 11. Dudas

Resueltas a petición del desarrollador con la opción recomendada (2026-10-18).

### Decisiones de implementación

- **Correcciones de `/revisar 16b` (2026-10-24), seguridad.**
  - **RN-158 frente a `arquitectura.md` §13.6.** §13.6 decía que, si el envío del restablecimiento falla, el usuario "debe saberlo"; con el SMTP caído eso hacía que una cuenta existente diera error y una inexistente no, revelando qué correos están registrados. Manda RN-158 (⚑, `reglas-negocio.md` está por encima de `arquitectura.md`, AGENTS §2). `SolicitudRestablecimientoClave` captura cualquier fallo del remitente (salvo la cancelación), lo registra como `Error` **solo con el tipo de excepción**, sin el correo ni la excepción, y responde igual. Se corrige §13.6.
  - **Enlace con `Correo:UrlBase`, nunca con el `Host` de la petición.** `ForgotPassword.razor` montaba el enlace con `NavigationManager.ToAbsoluteUri` y `AllowedHosts` es `"*"`: con un `Host` manipulado, la víctima habría recibido un enlace auténtico hacia otro dominio con su token. Ahora la página solo pasa el correo y `SolicitudRestablecimientoClave` construye `{UrlBase}/Account/ResetPassword?code=…`. Si `UrlBase` no es una URL `http` o `https` absoluta, no se envía nada, se registra el error y la respuesta es la misma. Se exige el esquema porque en Linux `/ruta` es un URI absoluto (`file://`).
  - `RemitenteIdentity` ya no descodifica el enlace: llega sin codificar para HTML, y la plantilla lo codifica una vez.
  - **Pruebas:** `El_enlace_se_construye_con_la_url_base_configurada…`, `Sin_una_url_base_absoluta_no_envia_nada_y_responde_igual` (seis valores, también `file://` y `javascript:`), `Con_el_remitente_real_cliente_y_propietario_reciben_el_mismo_correo…` y `Con_el_servidor_de_correo_caido_responde_igual_que_sin_cuenta_y_no_registra_el_correo`.
  - **Sabotajes:** propagar el fallo de envío, quitar la validación de `UrlBase` y quitar la exigencia de esquema hicieron fallar sus pruebas. Restaurado.
  - **Queda para la Fase 19 (endurecimiento):** restringir `AllowedHosts` en producción, como segunda barrera; la diferencia de tiempo de respuesta entre una cuenta existente (genera el token y envía) y una inexistente; y la limitación de frecuencia de `/Account/ForgotPassword` (`arquitectura.md` §14.5).
- **Correcciones de `/revisar 16b`, pruebas.** `Con_300_reservas…` hace que el correo de la fixture falle durante la baja y comprueba que la baja termina bien y que no se envía nada. La fixture registra `IConfiguration`, `Correo:UrlBase` y un `CorreoCaptura` (`Infraestructura/CorreoCaptura.cs` de las pruebas de integración) como `IServicioEmail`.
- **T4-T10 hechas el 2026-10-24.**
  - **Pruebas nuevas:** `PruebasConsultasNotificacion`, `PruebasRemitenteIdentity`, las seis `Notificaciones/PruebasEnviarCorreo*` (unitarias, con los dobles compartidos `CorreoCaptura`, `ConsultasNotificacionFalsas` y `RenderizadorFalso` en `PruebasUnitarias/Infraestructura/`).
  - **Pruebas ampliadas:** `PruebasRenderizadorPlantillas` (RN-150 a RN-156), `PruebasRegistroCliente`, `PruebasSolicitudRestablecimientoClave`, `PruebasProcesadorEventos`, `PruebasDarDeBajaEmpresa` (300 reservas) y `PruebasInyeccionDependencias`.
- ~~**El enlace de restablecimiento se descodifica una vez en `RemitenteIdentity`.**~~ **Sustituida** por la corrección de `/revisar 16b` (hallazgo 2, más arriba): el enlace lo construye `SolicitudRestablecimientoClave` con `Correo:UrlBase`, llega sin codificar y `RemitenteIdentity` ya no lo descodifica. Texto original, solo como historial: `ForgotPassword.razor` (plantilla de Identity) lo entrega ya codificado para HTML, y la plantilla Razor lo vuelve a codificar al escribirlo en `href`. Sin el `WebUtility.HtmlDecode`, un `&` llegaría como `&amp;amp;` y el enlace no funcionaría. Lo fija `Restablecimiento_lleva_el_enlace_con_el_token_codificado_una_sola_vez`.
- **`SendConfirmationLinkAsync` y `SendPasswordResetCodeAsync` lanzan `NotSupportedException`.** No hay plantilla para ellos (no están entre las 11) y nada los llama; un correo vacío sería peor que un fallo visible (§13.6).
- **`ConsultasNotificacion` devuelve `null` también si falta el propietario o el correo del usuario**, y el manejador no envía nada. El destinatario "negocio" es `Empresa.Email` (duda 5).
- **`RenderizadorPlantillas` recibe `IConfiguration`** para `Correo:UrlBase` (duda 9); quita la barra final. *Desde la corrección del menor 4 de `/revisar 16b`*, la validación de `UrlBase` vive en un único sitio, `Correo/UrlBaseCorreo.cs`, que usan el renderizador y `SolicitudRestablecimientoClave`. Sin una URL `http(s)` absoluta, el renderizador lanza al componer los correos con enlace (RN-152, RN-153) y el procesador registra el fallo, en lugar de enviar enlaces relativos rotos.
- **Los manejadores reciben `OcurridoEnUtc` como instante del recuento** (duda 6). Un origen de cancelación desconocido lanza `ArgumentOutOfRangeException` y el procesador lo registra.
- **El registro del procesador por manejador** nombra el tipo de evento y el del manejador, nunca el mensaje de la excepción en el texto; `CorreoNoEnviadoException` ya no lleva la dirección (duda 12).
- **Arranque de la aplicación:** la instancia propia en el puerto 5199 resolvió la inyección de dependencias y se detuvo al migrar, porque el PostgreSQL de desarrollo no estaba levantado. No lo he arrancado; queda para la verificación manual del desarrollador.

- **T1-T3 hechas el 2026-10-18.** Pruebas: `PruebasServicioEmailRegistro`, `PruebasInyeccionDependencias` (servicio según `Habilitado` y `appsettings*.json` con `false`), `PruebasRenderizadorPlantillas` (layout, RN-146 a RN-149, hora local en invierno, escapado) y `PruebasDatosReservaNotificacionDto`.
- **`DatosReservaNotificacionDto` se crea en T3, no en T6.** Las firmas de `IRenderizadorPlantillas` lo necesitan. Lleva la dirección ya unida (`Calle, CP Ciudad`) y su `ConvertirADto` recibe objetos de valor y el correo del cliente como `Email`. T6 solo lo rellena.
- **`Correo/Plantillas/_Imports.razor` y `FormatoCorreo`** (interno) no estaban nombrados en §4. Son los usings de las plantillas y el único sitio que formatea fecha, hora y precio en `es-ES`, con la hora local de `MotorDisponibilidad.ALocal` (AGENTS §9).
- **`ServicioEmailSmtp` lee la sección `Correo` de `IConfiguration`**, sin clase de opciones. Usa `StartTlsWhenAvailable`, autentica solo si `Usuario` no está vacío y envuelve cualquier fallo (salvo la cancelación) en `CorreoNoEnviadoException`, con el tipo de la excepción original y sin la excepción interna (duda 12).
- **`appsettings.Development.json`** lleva `Habilitado: false` y `UrlBase: https://localhost:7285` (`launchSettings.json`). `appsettings.json` deja `UrlBase` vacía.

### Duda 1 — ¿Cuántos correos?

- **Pregunta:** SPEC §16 bis (l.1013) y D-20 dicen "Once correos más la recuperación de contraseña". `arquitectura.md` l.1200 dice "Once plantillas, una por regla de RN-146 a RN-156", y RN-156 *es* la recuperación.
- **Opciones:**
  - (a) 11 plantillas, de RN-146 a RN-156, incluida la recuperación.
  - (b) 12 plantillas, con una regla nueva sin origen.
- **Recomendación y motivo:** (a). El título de la fase, el desglose de horas ("11 plantillas", `casos-uso.md` l.680) y la numeración de las reglas cuadran. (b) inventa un correo sin regla.
- **Bloquea:** T3, T4, T5.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 2 — ¿La recuperación de contraseña pasa por la cola?

- **Pregunta:** CU-24 paso 3 (l.517) dice "se encola el correo". `arquitectura.md` §13.6 (l.1226) dice "No pasa por la cola: es una acción síncrona que el usuario está esperando".
- **Opciones:**
  - (a) Síncrona, por `IEmailSender<Usuario>`, y se corrige el texto de CU-24.
  - (b) Por la cola, con un evento nuevo.
- **Recomendación y motivo:** (a). Es el diseño de §13.6, `SolicitudRestablecimientoClave` ya funciona así y (b) exige un evento que no está en el modelo.
- **Bloquea:** T9.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 3 — `ServicioEmailRegistro` frente a "nunca se registran tokens"

- **Pregunta:** §13.3 dice que "Escribe el correo en el registro", pero §11.2 (l.888) y SPEC l.885 prohíben registrar tokens y datos personales innecesarios, y el cuerpo de RN-156 lleva el token (ya rechazado en `fase-06.md`, duda 1b).
- **Opciones:**
  - (a) Registrar solo el destinatario enmascarado y el asunto.
  - (b) Registrar el correo entero.
- **Recomendación y motivo:** (a). Cumple RN-145 ("se registra o se captura") sin romper §11.2 ni RN-172. Para ver el contenido en desarrollo está Mailtrap (§13.3, l.1187).
- **Bloquea:** T1.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 4 — Forma de `IRenderizadorPlantillas`

- **Pregunta:** §13.4 la usa como `RenderizarAsync<PlantillaReservaConfirmada>(new { … })`, que devuelve HTML. §13.5 la usa como `render.CorreoClienteAsync(datos)`, que devuelve un mensaje. Además, `Aplicacion`, donde viven los manejadores, no puede nombrar componentes Razor de `Infraestructura`.
- **Opciones:**
  - (a) Un método por correo que devuelve un `MensajeCorreo` completo (destinatario, asunto y HTML), como en §13.5: `ReservaConfirmadaClienteAsync`, `ReservaConfirmadaNegocioAsync`, `ReservaCanceladaPorClienteAsync`, `ReservaCanceladaPorEmpresaAsync`, `ReservaCanceladaPorBajaEmpresaAsync`, `ReservaCanceladaPorBajaCuentaAsync`, `EmpresaRegistradaAsync`, `EmpresaDadaDeBajaAsync`, `CuentaRegistradaAsync`, `CuentaDadaDeBajaAsync` y `RestablecimientoClaveAsync`.
  - (b) Un método genérico con una enumeración de plantillas.
- **Recomendación y motivo:** (a). Respeta la dirección de dependencias, cada correo es tipado y se prueba por separado, y el manejador no sabe nada de Razor.
- **Bloquea:** T2 a T9.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 5 — ¿Quién es el "negocio"?

- **Pregunta:** RN-147, RN-148 y RN-151 envían al "Negocio", y RN-152 y RN-153 al "Propietario".
- **Opciones:**
  - (a) Negocio es `Empresa.Email`, el contacto público del establecimiento; propietario es el usuario de `MiembroEmpresa`.
  - (b) Los dos son el propietario.
- **Recomendación y motivo:** (a). Las reglas distinguen los dos destinatarios a propósito, y `Empresa.Email` no está cifrado ni necesita `UserManager`.
- **Bloquea:** T6, T7.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 6 — Recuento de canceladas en RN-153 y RN-155

- **Pregunta:** los correos piden "número de reservas canceladas", pero `EmpresaDadaDeBaja` y `CuentaDadaDeBaja` solo llevan el identificador y el instante.
- **Opciones:**
  - (a) Contar las reservas de esa empresa o de ese usuario con `Estado == Cancelada` y `FechaCancelacion == OcurridoEnUtc`. La cascada usa el mismo `ahora` para todo.
  - (b) Añadir el número a los eventos, lo que toca el dominio y `modelo-dominio.md`.
- **Recomendación y motivo:** (a). No toca el dominio y sigue RN-143 (el manejador vuelve a leer). La igualdad exacta es fiable porque las dos marcas salen del mismo `ahora`.
- **Bloquea:** T6, T8.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 7 — Publicar `CuentaRegistrada`

- **Pregunta:** `Usuario` no hereda de `EntidadBase` y `RegistroCliente` no es un caso de uso. La duda 7 de la Fase 11b lo dejó para esta fase.
- **Opciones:**
  - (a) `RegistroCliente` recibe `IPublicadorEventos` y publica `CuentaRegistrada(usuarioId, ahora)` después de crear el usuario. El propietario recibe solo RN-152, porque CU-04 es el registro de cliente.
  - (b) Que lo publique `Register.razor`.
  - (c) Crear un caso de uso nuevo.
- **Recomendación y motivo:** (a). Es el punto donde se crea la cuenta, igual que `DarDeBajaCuenta` crea `CuentaDadaDeBaja` (Fase 15). (b) mete lógica en `Web` (AGENTS §6). (c) amplía el alcance.
- **Bloquea:** T8.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 8 — Varios manejadores comparten un `try/catch`

- **Pregunta:** hoy, si un manejador lanza, los siguientes del mismo evento no se ejecutan (`fase-11b-revision.md` l.132). Con un correo por manejador, un fallo de uno se llevaría los demás.
- **Opciones:**
  - (a) Un `try/catch` por manejador en `ProcesadorEventos`, sin cambiar nada más.
  - (b) Un `try/catch` en cada manejador.
- **Recomendación y motivo:** (a). Se arregla en un solo sitio, cumple RN-141 para cualquier manejador futuro y conserva la protección de R-6.
- **Bloquea:** T7, T10.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 9 — URL base para los enlaces

- **Pregunta:** RN-152 pide un enlace a la ficha pública y RN-153, cómo reactivar. En segundo plano no hay petición HTTP de la que sacar el host, y §11.3 no tiene ninguna clave para eso.
- **Opciones:**
  - (a) Una clave nueva, `Correo:UrlBase`, documentada en `arquitectura.md` §11.3.
  - (b) Sin enlaces, solo la ruta en texto.
- **Recomendación y motivo:** (a). RN-152 dice "enlace", y es configuración, no un secreto.
- **Bloquea:** T1, T4, T6.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 10 — SDK de `Infraestructura` para compilar `.razor`

- **Pregunta:** las plantillas son componentes Razor (A-14) y viven en `Infraestructura/Correo/` (§2.3, l.182), pero el proyecto usa `Microsoft.NET.Sdk`, que no compila `.razor`.
- **Opciones:**
  - (a) Cambiar el SDK del proyecto a `Microsoft.NET.Sdk.Razor`. No es un paquete.
  - (b) Llevar las plantillas y el renderizador a `Web`.
- **Recomendación y motivo:** (a). Mantiene la ubicación de §2.3 y A-13/A-14, y no añade paquetes. (b) rompe la estructura documentada.
- **Bloquea:** T2.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 11 — Ciclo de vida de `IEmailSender<Usuario>`

- **Pregunta:** §13.7 dice Scoped ("Lo exige Identity"), pero §10.1 (l.828) y `Program.cs:18` lo registran como Singleton.
- **Opciones:**
  - (a) Scoped, corrigiendo §10.1.
  - (b) Singleton.
- **Recomendación y motivo:** (a). `RemitenteIdentity` depende de `IServicioEmail` y del renderizador, que son Scoped. Como Singleton los capturaría.
- **Bloquea:** T9.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 12 — Datos personales en el registro de fallos

- **Pregunta:** el procesador registra la excepción, y una excepción de MailKit puede llevar la dirección de destino (`fase-11b-revision.md` l.33; RN-172).
- **Opciones:**
  - (a) `ServicioEmailSmtp` envuelve las excepciones de MailKit en `CorreoNoEnviadoException`, con un mensaje sin la dirección y la excepción original fuera del mensaje.
  - (b) Que el procesador registre solo el tipo de excepción.
- **Recomendación y motivo:** (a). El problema se corta donde nace y el procesador conserva un registro útil.
- **Bloquea:** T1.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo en las fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [x] Punto de control superado: Control 4b. Las cuatro líneas tienen prueba automática, y la comprobación manual con la aplicación y Mailtrap la hizo el desarrollador el 2026-10-25 (anotado en la Fase 20)
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6: la de entregabilidad (l.278), anotada por el desarrollador
