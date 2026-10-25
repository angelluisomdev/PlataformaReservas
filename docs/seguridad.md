# Seguridad — PlataformaReservas

Estado de la seguridad del MVP al cerrar la Fase 19. Desarrolla la tabla OWASP de SPEC §15.3 citando, en cada fila, la prueba o el fichero que la sostiene (SPEC l.971), y reúne en un solo sitio las medidas, el modelo de amenaza, las limitaciones y el trabajo futuro.

Fuentes: SPEC §15 (D-22, D-23), `docs/arquitectura.md` §8.5, §11.2 y §14, `docs/reglas-negocio.md` (RN-155 a RN-176) y la revisión de seguridad de la Fase 19 (`docs/fases/fase-19.md` §11, hallazgos H-01 a H-24).

---

## 1. OWASP Top 10 (2021)

Lo parcial se declara como parcial (SPEC §15.3).

| | Riesgo | Estado | Cómo | Prueba o fichero que lo sostiene |
|---|---|---|---|---|
| **A01** | Control de acceso roto | **Cubierto** | Repositorios filtrados por empresa en origen; validación explícita de `EmpresaId` antes de persistir (defensa en profundidad, con aviso en el registro); "no encontrado" en vez de "prohibido"; políticas `EsPropietario` y `EsCliente`; eje cliente filtrado por `UsuarioId`; redirección tras iniciar sesión solo a rutas locales (H-02) | `Multiempresa/PruebasAislamientoEmpresas` (las 5 de SPEC §10.4), `Autorizacion/PruebasAutorizacionRutas`, `PruebasRepositorioReservasCliente`, `PruebasInyeccionDependencias.La_matriz_de_politicas_completa_separa_anonimo_cliente_y_propietario`, `Cuenta/PruebasIdentityRedirectManager`, y el caso "recurso de otra empresa" de cada caso de uso |
| **A02** | Fallos criptográficos | **Cubierto** en reposo y contraseñas; **TLS, del despliegue** | Contraseñas con el hash PBKDF2 de Identity; datos personales con AES-256-GCM (`ProtectorDatosPersonales`) y valor de búsqueda HMAC-SHA256 (`ProtectorBusqueda`), con dos claves distintas de 256 bits fuera del repositorio (D-22); HSTS fuera de desarrollo (E-6). La aplicación sirve HTTP: el TLS lo pone el proxy del despliegue (`fase-18.md`, duda 4) | `PruebasCifradoEnReposo`, `PruebasProtectorDatosPersonales`, `PruebasProtectorBusqueda`, `PruebasAnilloClaves`, `PruebasUsuario`, `PruebasConfiguracionReserva`; `Program.cs` (`UseHsts`) |
| **A03** | Inyección | **Cubierto** | EF Core parametriza todo; el único SQL a mano es la restricción `EXCLUDE`, estática. Blazor codifica por defecto y no hay `MarkupString`, `AddMarkupContent` ni `Html.Raw` en las 11 plantillas de correo ni en la ficha (E-3, revisado en la Fase 19) | `Correo/PruebasRenderizadorPlantillas`, `Components/Publico/PruebasFichaEstablecimiento`, `Persistencia/PruebasEsquema` |
| **A04** | Diseño inseguro | **Parcial** | Limitación de frecuencia (E-5), validación en servidor, `FinUtc` calculado en servidor, restricción `EXCLUDE` contra el doble agendamiento. **Sin límite de reservas por cliente** (§4) | `PruebasLimitacionFrecuencia`, `Reservas/PruebasConcurrenciaReservas`, validadores de `PruebasUnitarias` |
| **A05** | Configuración insegura | **Cubierto** | Cabeceras y CSP (E-6); trazas nunca al usuario; datos iniciales solo en desarrollo o con `BaseDatos:MigrarAlArrancar`; correo desactivado por defecto; sin secretos en Git; `postgres` y `web` publicados solo en `127.0.0.1` (H-11); `code` malformado sin 500 (H-22) | `PruebasCabecerasSeguridad`, `PruebasInyeccionDependencias.El_servicio_de_correo_solo_envia_de_verdad_si_esta_habilitado`, `PruebasRestablecimientoClave`, `PruebasDatosIniciales`; `docker-compose.yml` |
| **A06** | Componentes vulnerables | **Cubierto** | Auditoría de NuGet en la compilación, incluidas las transitivas (`NuGetAudit`, `NuGetAuditMode=all`), que con `TreatWarningsAsErrors` hace fallar `dotnet build` y `docker build` (E-2) | `Directory.Build.props`; sabotaje de la Fase 19 (`Pkcs` 10.0.12: `NU1902` y `NU1903`) |
| **A07** | Identificación y autenticación | **Parcial** | Identity con política de contraseña; bloqueo de 5 intentos durante 15 min (E-1) con mensaje único (RN-170); limitación de frecuencia en las rutas de cuenta; token de restablecimiento de un solo uso; sello de seguridad revalidado cada 5 min; respuesta indistinguible al solicitar (RN-158) y al usar el restablecimiento (H-03). **Sin verificación de correo ni segundo factor** | `Identidad/PruebasInicioSesion`, `PruebasSolicitudRestablecimientoClave`, `PruebasRestablecimientoClave`, `PruebasRevalidadorIdentidad`, `PruebasConfirmacionUsuarioActivo`, `PruebasLimitacionFrecuencia` |
| **A08** | Fallos de integridad | **Parcial** | Gestión centralizada de paquetes con versiones fijadas y transitivas fijadas (`CentralPackageTransitivePinningEnabled`); compilación en contenedor multietapa; sin deserialización de datos no confiables. **Sin firma de artefactos** | `Directory.Packages.props`, `Dockerfile` |
| **A09** | Registro y supervisión | **Cubierto** | Serilog conectado (`UseSerilog`, sección `Serilog`). Eventos de seguridad: inicio de sesión correcto, fallido y bloqueado con el correo enmascarado y la IP (E-4); solicitud de restablecimiento; bajas; acceso entre empresas; violaciones de solapamiento. Nunca contraseñas, tokens ni correos enteros | `PruebasInicioSesion`, `PruebasSolicitudRestablecimientoClave`, `PruebasServicioCuentas`, `PruebasRepositorioServicios`, `PruebasRepositorioProfesionales`, `PruebasUnidadTrabajo`, `PruebasServicioEmailRegistro`, `PruebasEnmascaradoCorreo` |
| **A10** | SSRF | **No aplica** | La aplicación no hace peticiones salientes a direcciones de entrada del usuario. El único enlace saliente, el del correo, sale de `Correo:UrlBase` y nunca del `Host` de la petición | `PruebasSolicitudRestablecimientoClave.El_enlace_se_construye_con_la_url_base_configurada…`, `PruebasUrlBaseCorreo` |

---

## 2. Las seis medidas de endurecimiento (D-23)

| # | Medida | Dónde | Cómo se verifica |
|---|---|---|---|
| E-1 | Bloqueo: 5 intentos, 15 min, `lockoutOnFailure: true` | `InyeccionDependencias.AgregarIdentidad`, `Identidad/InicioSesion.cs` | `PruebasInicioSesion` (la sexta con la clave correcta falla) y su sabotaje (R-13) |
| E-2 | Auditoría de dependencias vulnerables | `Directory.Build.props` | Sabotaje: una versión vulnerable rompe la compilación |
| E-3 | Escapado en correo y ficha; `MarkupString` prohibido | Plantillas de `Infraestructura/Correo/Plantillas/`, `Web/Components/Publico/FichaEstablecimiento.razor` | `PruebasRenderizadorPlantillas`, `PruebasFichaEstablecimiento` y su sabotaje |
| E-4 | Registro de intentos fallidos con correo enmascarado e IP | `InicioSesion`, `Correo/EnmascaradoCorreo.cs` | `PruebasInicioSesion` |
| E-5 | Limitación de frecuencia: `autenticacion` (5 `POST` en 5 min por IP a inicio de sesión, registro, alta de empresa y recuperación, con o sin barra final) y `catalogo` (60 peticiones por min por IP); `429` | `Web/LimitacionFrecuencia.cs`, `UseRateLimiter` antes de `UseAuthentication` | `PruebasLimitacionFrecuencia` y su sabotaje; `curl` contra una instancia propia (`fase-19.md` §11) |
| E-6 | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP y HSTS fuera de desarrollo | `Web/CabecerasSeguridad.cs`, `Program.cs`, `App.razor` sin `<ImportMap />` | `PruebasCabecerasSeguridad`; `curl`; navegación manual por las tres áreas con la consola abierta, sin errores (desarrollador, 2026-10-25, Control 5) |

---

## 3. Modelo de amenaza del cifrado en reposo

Tomado de SPEC §15.1. Se cifran el correo, el nombre de usuario, el nombre completo y los teléfonos de `asp_net_users`, y el nombre y el teléfono del cliente en `reservas`. Los datos de la empresa van en claro porque son públicos y `Ciudad` es el filtro del buscador.

| Escenario | ¿Protegido? |
|---|---|
| Copia de seguridad o volcado robado | **Sí** |
| Puerto 5432 expuesto o acceso de solo lectura mal concedido | **Sí** (y el puerto ya solo se publica en `127.0.0.1`) |
| Inyección SQL que vuelca tablas | **Sí** (además, EF parametriza) |
| Compromiso del servidor de aplicación | **No**: las claves viven ahí, porque la aplicación necesita descifrar |

Contrapartidas declaradas: el índice determinista del correo revela si dos filas comparten correo; perder las claves equivale a perder las cuentas.

El ciclo de vida de las dos claves (generar, guardar, copiar, validar al arrancar, desplegar, pérdida y compromiso) está en un solo sitio: `docs/arquitectura.md` §8.5, "Gestión de la clave".

---

## 4. Limitaciones declaradas

Cada una con su motivo. Las que vienen de la revisión de la Fase 19 llevan su identificador H-xx.

| Limitación | Motivo |
|---|---|
| Limitación de frecuencia en memoria y por instancia (RN-174, R-14) | Despliegue de una sola instancia. Con varias haría falta un almacén compartido |
| La limitación es por IP | Tras un proxy, todas las peticiones llegarían con la IP del proxy: hay que configurar las cabeceras reenviadas en el despliegue (ver H-09) |
| Sin límite de reservas por cliente (A04, H-08) | Exige decidir umbral y periodo; descartado en `reglas-negocio.md` §13 |
| Sin verificación de correo ni segundo factor (A07, H-07) | La verificación rompe el flujo de CU-07 alt. 1a; el segundo factor queda fuera de un MVP |
| Sin firma de artefactos (A08) | Infraestructura de claves que no aporta al objeto del TFM |
| El registro de cliente y el alta de empresa revelan que un correo ya tiene cuenta (H-04) | El usuario necesita saberlo; cambiarlo exige una regla nueva y tocar CU-01 y CU-04. Mitigado por E-5: el registro y el alta comparten el cupo de 5 envíos en 5 min por IP (`fase-19.md`, duda 5) |
| Diferencia de tiempo de respuesta al solicitar el restablecimiento con una cuenta existente (H-05) y al iniciar sesión con un usuario inexistente (H-06) | Permiten, en teoría, enumerar cuentas midiendo tiempos. Mitigado por E-5, que limita el número de muestras. Igualar tiempos exigiría encolar el envío del restablecimiento, fuera de esta fase |
| Sin cabeceras reenviadas ni *cookie* `Secure` forzada tras un proxy; `AllowedHosts: "*"` (H-09, H-24) | Configuración del despliegue real, que el SPEC no define. Se hace al desplegar tras un proxy con TLS |
| La aplicación se conecta a PostgreSQL con el superusuario (H-12) | Un rol de aplicación con permisos mínimos es tarea de despliegue |
| Las claves de Data Protection se guardan sin cifrar en el volumen `claves-web` (H-13) | Ya declarado en `arquitectura.md` §11.4. Cifrarlas exige un certificado o un almacén de claves del despliegue |
| `Reserva.Observaciones` va en claro (H-15) | Es texto libre que podría contener datos sensibles, pero D-22 no lo incluye. Cifrarlo cambiaría una decisión cerrada |
| El cifrado no lleva identificador de clave ni datos asociados (H-16) | La rotación de claves queda como trabajo futuro |
| Los objetos de valor de texto admiten caracteres de control (H-17) | Bajo riesgo: Blazor y MimeKit los codifican en HTML y en las cabeceras del correo |
| Dos altas simultáneas con el mismo nombre pueden chocar en el slug y dar 500 en lugar de reintentar (H-18) | Bajo riesgo; el índice único impide el duplicado |
| Las cancelaciones y la baja de empresa no usan *token* de concurrencia optimista (H-19) | Bajo riesgo: el dominio rechaza las transiciones ilegales, y dos cancelaciones simultáneas del mismo recurso dejan el mismo estado final. Lo que se puede perder es la detección del conflicto, no la coherencia |
| La cola de eventos no tiene límite (H-20) | Un único proceso, y cada evento lo genera una operación de un usuario autenticado o del propietario. Es volátil por diseño (RN-144) |
| Tras la baja de una cuenta, la sesión dura hasta 5 min (H-21) | Es el intervalo de revalidación del sello (A-06) |
| Las imágenes de Docker no fijan versión (H-23) | `postgres:latest` lo decidió el desarrollador en la Fase 5 |
| La aplicación sirve HTTP; HSTS solo actúa detrás de TLS | `fase-18.md`, duda 4: HTTPS queda para el despliegue |

---

## 5. Hallazgos corregidos en la Fase 19

| Id | Qué | Corrección | Prueba |
|---|---|---|---|
| H-01 | Fuerza bruta sin límite | E-1 y E-5 | `PruebasInicioSesion`, `PruebasLimitacionFrecuencia` |
| H-02 | `ReturnUrl=//evil.com` redirigía fuera tras iniciar sesión | `IdentityRedirectManager` solo acepta destinos locales, también con espacios o caracteres de control delante, y una URI absoluta propia se reduce a su ruta y se valida igual (`https://host///evil.com`) | `PruebasIdentityRedirectManager` |
| H-03 | `ResetPassword` distinguía cuenta inexistente de enlace inválido | `RestablecimientoClave`: misma respuesta, y los errores de la política solo con un enlace válido | `PruebasRestablecimientoClave` |
| H-10 | Sin cabeceras de seguridad | E-6 | `PruebasCabecerasSeguridad` |
| H-11 | `postgres` publicado en todas las interfaces | `127.0.0.1:${POSTGRES_PUERTO:-5432}:5432` | `docker compose config` |
| H-14 | Serilog sin conectar y sin eventos de seguridad | `UseSerilog` y los eventos de `arquitectura.md` §11.2 | Pruebas de A09 en §1 |
| H-22 | Un `code` malformado daba 500 | Redirección a `InvalidPasswordReset` | `PruebasRestablecimientoClave`; `curl` |

---

## 6. Trabajo futuro

- Almacén compartido para la limitación de frecuencia si se despliega más de una instancia.
- Despliegue tras un proxy con TLS: cabeceras reenviadas, *cookie* `Secure`, `AllowedHosts` restringido, rol de base de datos sin privilegios y claves de Data Protection protegidas.
- Gestión de claves externa con cifrado en sobre y rotación (identificador de clave en el texto cifrado).
- Límite de reservas por cliente, verificación de correo y segundo factor.
- Encolar el envío del restablecimiento para igualar los tiempos de respuesta.
- Registrar también los eventos de negocio de `arquitectura.md` §11.2 (alta de empresa, creación y cancelación de reservas).
- Deuda técnica de la refactorización, R12 a R23 (`fase-19.md` §9): sin impacto en la seguridad ni en el aislamiento.
