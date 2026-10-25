# Revisión de la Fase 16b

## Revisión 2026-10-24 17:09

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-16b.md (APROBADA)
**Commit base:** c8f15cf
**Ficheros revisados:** 63, más la documentación (`docs/`), que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 4 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 447 | 447 | 0 |
| Integración | 393 | 393 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | seguridad | src/PlataformaReservas.Infraestructura/Identidad/RemitenteIdentity.cs:14 (llamado desde `SolicitudRestablecimientoClave.cs:23`) | RN-158 ⚑, RN-160 | El restablecimiento se envía de forma síncrona. Con `Correo:Habilitado = true` y el SMTP caído, `CorreoNoEnviadoException` sube hasta `ForgotPassword.razor`. Una cuenta existente y activa produce una página de error; un correo inexistente o una cuenta inactiva produce la redirección normal. Las respuestas dejan de ser iguales y el formulario revela qué correos están registrados. Hay contradicción entre `arquitectura.md` §13.6 ("si falla debe saberlo") y RN-158, que tiene más autoridad (AGENTS §2). | Abrir una duda (AGENTS §14). La opción coherente con RN-158: capturar `CorreoNoEnviadoException` en `SolicitudRestablecimientoClave`, registrarla sin el correo y responder igual. Añadir una prueba con un `IServicioEmail` que lance |
| 2 | IMPORTANTE | seguridad | src/PlataformaReservas.Web/Cuenta/Pages/ForgotPassword.razor:43-44 y src/PlataformaReservas.Web/appsettings.json:8 | RN-156 ⚑, RN-159, SPEC §15.3 (A07) | El enlace de restablecimiento sale de `NavigationManager.ToAbsoluteUri`, es decir, del `Host` de la petición, y `AllowedHosts` es `"*"`. Hasta ahora no se enviaba nada; esta fase activa el envío real. Con un `Host` manipulado, la víctima recibe un enlace auténtico hacia el dominio del atacante y, si lo pulsa, le entrega el token. El código de la página es anterior, pero la vía de ataque la abre esta fase. Depende del despliegue: un proxy que fije el `Host` la cierra | Construir el enlace con `Correo:UrlBase` (duda 9) en lugar del host de la petición, o restringir `AllowedHosts` en producción. `ForgotPassword.razor` está fuera del radio (§4): decidirlo como duda o llevarlo a la Fase 19 (endurecimiento) |
| 3 | IMPORTANTE | arquitectura | tests/PlataformaReservas.PruebasIntegracion/CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs:135 | `fase-16b.md` §6 (RN-142, Control 4b) | `Con_300_reservas_futuras_publica_301_eventos_sin_enviar_ningun_correo_dentro_de_la_operacion` no comprueba nada sobre `IServicioEmail`: en el fixture se resuelve `ServicioEmailRegistro`, que no captura. La mitad de lo que dice el nombre no está verificada | Registrar en el ámbito un `IServicioEmail` que capture o que lance, y comprobar que no recibe ninguna llamada durante `EjecutarAsync` |
| 4 | IMPORTANTE | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasSolicitudRestablecimientoClave.cs:70 y Infraestructura/BaseDatosFixture.cs:72 | `fase-16b.md` §6 (RN-157, RN-158, "con el remitente real"); T9, "Hecho cuando" | El fixture sustituye `IEmailSender<Usuario>` por `RemitenteCaptura`. Por eso las pruebas de propietario y de respuesta idéntica nunca pasan por `RemitenteIdentity`, el renderizador ni `IServicioEmail`. `PruebasRemitenteIdentity` cubre el remitente aislado, pero no el flujo completo | Añadir un caso con `SolicitudRestablecimientoClave` y el `RemitenteIdentity` real, con un `IServicioEmail` de captura: el enlace llega para cliente y propietario, y con un correo inexistente no se envía nada. Otra opción: ajustar el texto de §6 |
| 5 | MENOR | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasRegistroCliente.cs:82 | `fase-16b.md` §6 ("después de guardar"); AGENTS §8 ter | La prueba comprueba que se publica `CuentaRegistrada`, pero no que sea después del commit. El código está bien (`RegistroCliente.cs:40-41`) | Un publicador que, al publicar, compruebe que el usuario ya es legible desde otro ámbito |
| 6 | MENOR | alcance | src/PlataformaReservas.Infraestructura/Consultas/CondicionPublicable.cs y ConsultasReservasEmpresa.cs | `CLAUDE.md` (radio de cambio) | Dos clases de la Fase 16 que §4 no nombraba al aprobarse. Su modificación figuraba en el plan aprobado por el desarrollador (2026-10-24) y está anotada en §4 como "Añadido al implementar T6" | Que el desarrollador confirme la anotación de §4. No hace falta cambiar el código |
| 7 | MENOR | alcance | tests/PlataformaReservas.PruebasUnitarias/Dtos/Notificaciones/PruebasDatosReservaNotificacionDto.cs; Correo/Plantillas/FormatoCorreo.cs y _Imports.razor | `fase-16b.md` §4 | Ficheros que §4 no nombra: la prueba unitaria del DTO y dos auxiliares de las plantillas. Están anotados en las decisiones de implementación (T1-T3) | Añadir los nombres a §4 para que el radio coincida con el diff |

**Notas de los revisores (no verificables, no son hallazgos):**
- El hallazgo 2 depende del despliegue: no hay configuración de producción ni de proxy en el repositorio.
- No se comprobó la aplicación con Mailtrap ni con el PostgreSQL de desarrollo (no estaba levantado). Queda para la verificación manual del desarrollador (§6).

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-07, dos correos.** Evidencia: `PruebasEnviarCorreosReservaConfirmada` y `PruebasRenderizadorPlantillas`.
- [x] **CU-07, correo caído.** Evidencia: `PruebasProcesadorEventos` (manejador real con un correo que siempre falla) y el sabotaje de RN-141.
- [x] **CU-10, aviso al negocio.** Evidencia: `PruebasEnviarCorreoReservaCancelada` (origen `Cliente`).
- [x] **CU-23, aviso a cada cliente.** Evidencia: `PruebasDarDeBajaEmpresa` (N eventos `BajaEmpresa`) y `PruebasEnviarCorreoReservaCancelada`; sabotaje de RN-150.
- [ ] **CU-23, "300 igual de rápido que 3".** Evidencia parcial: se comprueba que se publican 301 eventos, pero no que no se envíe ningún correo dentro de la operación (hallazgo 3).
- [ ] **CU-24, cliente y propietario con el mismo flujo.** Evidencia parcial: se prueba con `RemitenteCaptura`, no con el remitente real (hallazgo 4).
- [x] **SPEC §21, `<script>` en el correo.** Evidencia: `Un_script_en_el_nombre_del_establecimiento_sale_escapado_en_el_html`, el sabotaje de RN-171 y C06. Ninguna plantilla incluye la descripción.
- [x] **Sin `MarkupString` ni credenciales.** Evidencia: `comprobaciones.sh` y la revisión de seguridad.
- [x] **Sabotajes.** Evidencia: los tres anotados en §6.
- [x] **Compilación y pruebas.** Evidencia: esta revisión.
- [ ] **Control 4b completo.** Hay prueba automática de las cuatro líneas (con las reservas de los hallazgos 3 y 4), pero falta la comprobación manual del desarrollador con la aplicación y Mailtrap.

## Revisión 2026-10-24 17:34

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-16b.md (APROBADA)
**Commit base:** c8f15cf
**Ficheros revisados:** 67, más la documentación (`docs/`), que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 7 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 447 | 447 | 0 |
| Integración | 402 | 402 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.
- Revisión de seguridad: **sin hallazgos**. Confirma cerrados los dos fallos de la recuperación de contraseña: misma respuesta en todos los casos y enlace construido con `Correo:UrlBase`.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | docs/arquitectura.md:1239 | AGENTS §2; `fase-16b.md` §4 y T10 | §13.6 dice que "`RemitenteIdentity` descodifica una vez el enlace que entrega `ForgotPassword.razor`, ya codificado para HTML". El código ya no hace eso: lo monta `SolicitudRestablecimientoClave` con `UrlBase` y llega sin codificar. El documento contradice al código en una regla de seguridad (RN-158) | Sustituir la frase por lo que hace el código: el enlace lo construye `SolicitudRestablecimientoClave` con `UrlBase`, llega sin codificar y la plantilla lo codifica una sola vez |
| 2 | MENOR | arquitectura | docs/fases/fase-16b.md:403 | AGENTS §2 | La decisión "El enlace de restablecimiento se descodifica una vez en `RemitenteIdentity`" contradice la l.395 de la misma especificación y el código | Marcarla como sustituida por la corrección de la revisión, o borrarla |
| 3 | MENOR | arquitectura | docs/arquitectura.md:1237; docs/fases/fase-16b.md:245 | AGENTS §2; `arquitectura.md` §13.6 | Dicen que `SendConfirmationLinkAsync` y `SendPasswordResetCodeAsync` "quedan implementados sin uso", pero lanzan `NotSupportedException` (decisión de `fase-16b.md` l.404) | Recoger en §13.6 que lanzan `NotSupportedException`, y por qué |
| 4 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Correo/RenderizadorPlantillas.cs:16 | A-09 (una regla en un solo sitio) | `Correo:UrlBase` se lee en dos sitios con reglas distintas. `SolicitudRestablecimientoClave` exige una URL `http(s)` absoluta; el renderizador acepta cualquier valor. Con `UrlBase: ""` (por defecto en `appsettings.json`), los enlaces de RN-152 y RN-153 salen relativos sin aviso | Validar `UrlBase` en un único punto que usen las dos clases |
| 5 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Consultas/ConsultasNotificacion.cs:12 | AGENTS §8 (tres familias) | `IConsultasNotificacion` lee datos privados sin contexto, filtrando por el identificador del evento. Lo permiten `arquitectura.md` §13.5 y `fase-16b.md` §6, pero no figura en la tabla de familias de AGENTS §8 | Añadirla en AGENTS §8 como excepción de segundo plano, citando §13.5 |
| 6 | MENOR | arquitectura (sigue abierto, ronda 1 #5) | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasRegistroCliente.cs:82 | AGENTS §8 ter | La prueba no fija que `CuentaRegistrada` se publique después del commit | Un publicador que compruebe, al publicar, que el usuario ya es legible desde otro ámbito |
| 7 | MENOR | alcance (sigue abierto, ronda 1 #6 y #7) | `fase-16b.md` §4 | `CLAUDE.md` | Ficheros que §4 no nombra en su redacción original: la prueba del DTO, `FormatoCorreo.cs` y `_Imports.razor`. `CondicionPublicable` y `ConsultasReservasEmpresa` ya están anotados, autorizados por el plan aprobado | Añadir los nombres que faltan a §4 |

**Estado de los hallazgos de la revisión de las 17:09**

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 | El fallo de envío revelaba qué correos están registrados (RN-158) | Resuelto | `SolicitudRestablecimientoClave` absorbe el fallo, registra solo el tipo de excepción y responde igual. Lo cubren una prueba con el correo caído y un sabotaje. §13.6 y CU-24 corregidos |
| 2 | Enlace construido con el `Host` de la petición | Resuelto | Se construye con `Correo:UrlBase`, que debe ser `http(s)` absoluta. Si no lo es, no se envía nada. Hay pruebas con seis valores (también `file://` y `javascript:`) y dos sabotajes. `ForgotPassword.razor` está anotado como excepción autorizada |
| 3 | La prueba de las 300 reservas no comprobaba el correo | Resuelto | El correo de la fixture falla durante la baja; la baja termina bien y no se envía nada |
| 4 | Pruebas de restablecimiento sin el remitente real | Resuelto | `Con_el_remitente_real_cliente_y_propietario_reciben_el_mismo_correo…` |
| 5 | Orden de publicación de `CuentaRegistrada` | Abierto | Hallazgo 6 de esta ronda |
| 6, 7 | Radio de §4 | Abierto en parte | Hallazgo 7 de esta ronda |

**Notas de los revisores (no verificables, no son hallazgos)**
- `ProcesadorEventos` registra el objeto de la excepción de cada manejador (duda 12). No se descarta que una excepción de terceros que no pase por `CorreoNoEnviadoException` lleve valores en su mensaje.
- No se comprobó cómo trata MimeKit un asunto con saltos de línea. El asunto lleva `Empresa.Nombre`, ya validado por `NombreEmpresa`.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-07, dos correos.** Evidencia: `PruebasEnviarCorreosReservaConfirmada`.
- [x] **CU-07, correo caído.** Evidencia: `PruebasProcesadorEventos` y el sabotaje de RN-141.
- [x] **CU-10, aviso al negocio.** Evidencia: `PruebasEnviarCorreoReservaCancelada`.
- [x] **CU-23, aviso a cada cliente.** Evidencia: `PruebasDarDeBajaEmpresa` y `PruebasEnviarCorreoReservaCancelada`; sabotaje de RN-150.
- [x] **CU-23, 300 reservas igual de rápido que 3.** Evidencia: 301 eventos, y ningún correo enviado con el correo de la fixture en fallo.
- [x] **CU-24, cliente y propietario con el mismo flujo.** Evidencia: el remitente real, y respuesta idéntica con el correo caído.
- [x] **SPEC §21, `<script>` en el correo.** Evidencia: la prueba de escapado, el sabotaje de RN-171 y la comprobación C06.
- [x] **Sin `MarkupString` ni credenciales.** Evidencia: `comprobaciones.sh` y la revisión de seguridad.
- [x] **Sabotajes.** Evidencia: §6 y las correcciones de la revisión.
- [x] **Compilación y pruebas.** Evidencia: esta revisión.
- [x] **Control 4b.** Las cuatro líneas tienen prueba automática. Falta la comprobación manual del desarrollador con la aplicación (PostgreSQL de desarrollo) y Mailtrap.

## Seguimiento 2026-10-24: menores de la revisión de las 17:34

Corregidos a petición del desarrollador. No es una ronda nueva de `/revisar`.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (IMPORTANTE) | §13.6 describía la descodificación del enlace en `RemitenteIdentity` | Resuelto en la Fase 17 (T6) | `arquitectura.md` §13.6 describe el enlace construido con `UrlBase`, sin codificar, y codificado una sola vez por la plantilla |
| 2 | Decisión obsoleta en `fase-16b.md` l.403 | Resuelto | Tachada y marcada como sustituida por la corrección de la revisión. También se corrige la l.246 de T9, que decía que el enlace llegaba codificado desde `ForgotPassword.razor` |
| 3 | "Implementados y sin uso" frente a `NotSupportedException` | Resuelto | `arquitectura.md` §13.6 (Fase 17) y `fase-16b.md` T9 dicen que lanzan `NotSupportedException`, y por qué |
| 4 | `Correo:UrlBase` validada en dos sitios con reglas distintas (A-09) | Resuelto | `Infraestructura/Correo/UrlBaseCorreo.cs`, única regla (`http` o `https` absoluta, sin barra final), usada por `SolicitudRestablecimientoClave` y `RenderizadorPlantillas`. El renderizador lanza `InvalidOperationException` al componer RN-152 y RN-153 sin una `UrlBase` válida; el procesador lo registra. Pruebas: `PruebasUrlBaseCorreo` (nuevo, 11 casos) y `PruebasRenderizadorPlantillas.Sin_una_url_base_valida_no_se_compone_un_correo_con_enlaces_rotos`. `arquitectura.md` §11.3 actualizado |
| 5 | `IConsultasNotificacion` fuera de la tabla de familias | Resuelto | AGENTS §8: excepción de segundo plano, citando `arquitectura.md` §13.5 |
| 6 | `CuentaRegistrada` después del commit sin prueba | Resuelto en la Fase 17 (T4) | `PruebasRegistroCliente.CuentaRegistrada_se_publica_cuando_el_usuario_ya_es_legible_desde_otro_ambito` |
| 7 | Ficheros sin nombrar en §4 | Resuelto | `fase-16b.md` §4 nombra `_Imports.razor`, `FormatoCorreo.cs`, `PruebasDatosReservaNotificacionDto.cs`, y los nuevos `UrlBaseCorreo.cs` y `PruebasUrlBaseCorreo.cs` |

**Sabotaje del menor 4:** con el renderizador sin lanzar ante una `UrlBase` no válida, fallan los dos casos de `Sin_una_url_base_valida_no_se_compone_un_correo_con_enlaces_rotos`. Restaurado.

**Verificación:** `dotnet build` sin avisos; unitarias 448/448; integración 434/434; `comprobaciones.sh` 0 bloqueantes, 0 importantes.
