# Pruebas — PlataformaReservas

Estrategia, organización y evidencia de las pruebas del proyecto (SPEC §16 y §18). Este documento lo crea la Fase 17. El detalle de cada fase está en §6 de su `docs/fases/fase-NN.md`.

## 1. Dos suites

| Proyecto | Qué contiene | Base de datos |
|---|---|---|
| `tests/PlataformaReservas.PruebasUnitarias` | Objetos de valor, entidades, motor de disponibilidad (los 13 casos de SPEC §16.1), DTO, manejadores de correo con dobles y las pruebas de arquitectura | No |
| `tests/PlataformaReservas.PruebasIntegracion` | Casos de uso completos, repositorios y consultas, Identity y cifrado, correo y eventos, aislamiento, autorización, concurrencia | PostgreSQL real (Testcontainers) |

```bash
dotnet test tests/PlataformaReservas.PruebasUnitarias      # rápidas, sin Docker
dotnet test tests/PlataformaReservas.PruebasIntegracion    # requieren Docker
```

### Base de datos de integración (A-10)

- **Un contenedor de PostgreSQL** para toda la suite (`BaseDatosFixture`, colección `BaseDatos`).
- **El esquema sale de las migraciones**, nunca de `EnsureCreated`. Si no, la restricción `EXCLUDE`, escrita a mano en la migración, no existiría y la prueba de concurrencia pasaría sin probar nada (R-3).
- **No se limpia entre pruebas.** El paquete `Respawn` está referenciado, pero no se usa: cada prueba crea sus propios datos con identificadores, slugs, correos y ciudades únicos, y filtra por ellos. Las pruebas que usan datos fijos, como los de demostración, los recorren en un solo caso por pasos.
- **El cifrado es real** en el proveedor de servicios de la fixture (`ServiciosIdentidad`), con claves aleatorias por ejecución. `ProtectorFalso` solo lo usa `CrearContexto()`, para las pruebas de configuración de EF.
- **El correo nunca sale.** La fixture registra `CorreoCaptura` como `IServicioEmail` y `RemitenteCaptura` como remitente de Identity. `Correo:Habilitado` está en `false` en los dos `appsettings*.json`, y una prueba lo comprueba (R-7).

## 2. Organización (AGENTS §11 bis)

- **Un fichero por clase:** `Pruebas<Clase>.cs`, en la misma carpeta relativa que la clase en `src/`, uno por suite.
- **Dobles compartidos:** en `Infraestructura/` de cada proyecto de pruebas. Un doble que solo usa un fichero vive junto a él.
- **Transversales**, lista cerrada:

| Fichero | Suite | Qué demuestra |
|---|---|---|
| `Arquitectura/PruebasArquitectura.cs` | Unitarias | Las cinco reglas de forma: sin `set`/`init` públicos, sin colecciones mutables, constructor privado sin parámetros, `Dominio` sin EF/Npgsql/ASP.NET, métodos públicos sin primitivos validables |
| `Persistencia/PruebasEsquema.cs` | Integración | La restricción `ex_reservas_sin_solape` existe en `pg_constraint` (Control 3, R-3) y otras restricciones del esquema |
| `Persistencia/PruebasDatosIniciales.cs` | Integración | Categorías y datos de demostración: idempotentes y desactivados por defecto |
| `Persistencia/PruebasCifradoEnReposo.cs` | Integración | **Fase 17.** Con datos de todo el sistema, SQL crudo: nada personal en claro en `asp_net_users` ni en `reservas`; `empresas` en claro y el buscador por ciudad funciona; inicio de sesión, duplicado y restablecimiento con el cifrado activo (RN-162 a RN-167) |
| `Multiempresa/PruebasAislamientoEmpresas.cs` | Integración | Las cinco pruebas de SPEC §10.4: el recurso de otra empresa es "no encontrado" y queda intacto (RN-02, RN-03) |
| `Autorizacion/PruebasAutorizacionRutas.cs` | Integración | **Fase 17.** Toda ruta `/app/*` exige `EsPropietario`, toda ruta `/my-account/*` exige `EsCliente`, y ninguna página queda sin proteger fuera de la lista de públicas (SPEC §16.2) |
| `Reservas/PruebasConcurrenciaReservas.cs` | Integración | 10 tareas, cada una con su ámbito, sobre el mismo hueco, en 5 iteraciones: 1 éxito y 9 `HuecoYaNoDisponible`. También sobre un hueco recién cancelado (RN-76, RN-77, RN-80, RN-97) |

## 3. Reglas críticas (⚑) y su prueba

Trazabilidad de `reglas-negocio.md` §12, con el fichero que la sostiene.

| Regla | Prueba |
|---|---|
| RN-02, RN-03 | `PruebasAislamientoEmpresas`; además, la consulta de cada caso de uso de empresa ("recurso de otra empresa") |
| RN-05 | `PruebasRepositorioReservasCliente`, `PruebasConsultasReservasCliente`, `PruebasAutorizacionRutas` |
| RN-16, RN-17 | `PruebasConsultasCatalogoPublico`, `PruebasConsultasReservasEmpresa` (requisitos del panel) |
| RN-26 a RN-29 | `PruebasPoliticasReserva` y los objetos de valor de las políticas |
| RN-50 a RN-53, RN-104 | `PruebasMotorDisponibilidad` (13 casos, incluido el cambio de hora) |
| RN-76, RN-77, RN-80 | `PruebasConcurrenciaReservas` |
| RN-97 | `PruebasConcurrenciaReservas` (hueco cancelado), `PruebasCancelarReservaCliente`, `PruebasCancelarReservaEmpresa`, `PruebasDarDeBajaCuenta` |
| RN-132, RN-137 | `PruebasDarDeBajaEmpresa`, `PruebasDarDeBajaCuenta` |
| RN-135 | `PruebasConfirmacionUsuarioActivo`, `PruebasDarDeBajaCuenta` (sello renovado) |
| RN-140, RN-141 | `PruebasProcesadorEventos` (manejador que lanza, correo caído), `PublicadorCaptura.Fallar` en los casos de uso |
| RN-150 | `PruebasDarDeBajaEmpresa` (N eventos), `PruebasEnviarCorreoReservaCancelada` |
| RN-156, RN-158 | `PruebasSolicitudRestablecimientoClave`: misma respuesta con correo inexistente, con el servidor de correo caído y con `UrlBase` mal configurada; el enlace usa `Correo:UrlBase`, nunca el `Host` |
| RN-162 a RN-167 | `PruebasCifradoEnReposo`, `PruebasUsuario`, `PruebasConfiguracionReserva` |
| RN-158 (extensión, Fase 19) | `PruebasRestablecimientoClave`: cuenta inexistente y enlace inválido dan la misma respuesta; `code` malformado sin excepción |
| RN-169, RN-170, RN-172 | `PruebasInicioSesion`: cinco fallos bloquean y la sexta con la clave correcta falla; bloqueo de 15 min; mismo resultado para bloqueada, clave mala, inexistente e inactiva; `Warning` con correo enmascarado e IP |
| RN-171 | `PruebasRenderizadorPlantillas` (correo) y `Components/Publico/PruebasFichaEstablecimiento` (ficha, con `HtmlRenderer`, Fase 19) |
| RN-173 | `PruebasLimitacionFrecuencia`: sexto `POST` a las rutas de cuenta, petición 61 al catálogo, rutas sin grupo, y la respuesta de rechazo |
| RN-175 | `PruebasCabecerasSeguridad` |
| H-02 (redirección abierta) | `Cuenta/PruebasIdentityRedirectManager` |

## 4. Sabotajes

Cada fase sabotea dos o tres reglas críticas y anota en §6 de su especificación qué prueba falló. Los de la Fase 17 son estos:

| Regla | Sabotaje | Prueba que falló |
|---|---|---|
| RN-163 | Quitar `ConversorCifrado` de `NombreCliente` | `Ni_asp_net_users_ni_reservas_guardan_en_claro_nombres_correos_ni_telefonos` |
| RN-02, RN-05 | Quitar `[Authorize(Policy = "EsCliente")]` de `MiCuenta/_Imports.razor` | `Toda_ruta_del_area_de_cliente_exige_la_politica_de_cliente` y `Ninguna_ruta_queda_sin_proteger…` |
| RN-02 | Añadir `[AllowAnonymous]` a `App/Configuracion.razor` | `Ninguna_ruta_fuera_de_las_publicas_declaradas_admite_anonimos` |
| RN-02 | Cambiar la política de `App/_Imports.razor` a `EsCliente` | `Toda_pagina_del_espacio_de_nombres_del_area_de_empresa_exige_la_politica_de_propietario` y la de prefijo |
| SPEC l.873 | Volver al `return` temprano si el propietario de demostración ya existe | `Los_datos_de_demostracion_solo_se_siembran_habilitados_desde_el_arranque…` |
| SPEC l.873 | Quitar la siembra de demostración de `MigrarYSembrarAsync` | `Los_datos_de_demostracion_solo_se_siembran_habilitados_desde_el_arranque…` |
| RN-97 | Que el motor cuente también las reservas canceladas | `Tras_cancelar_diez_peticiones_simultaneas…` y `El_hueco_que_cancela_la_empresa_se_vuelve_a_ofrecer…` |

Los de la Fase 19:

| Regla | Sabotaje | Prueba que falló |
|---|---|---|
| RN-169 | `lockoutOnFailure: false` en `InicioSesion` | `Cinco_fallos_bloquean_la_cuenta_y_la_sexta_con_la_clave_correcta_tambien_falla` y otras tres de `PruebasInicioSesion` |
| RN-171 | `@((MarkupString)ficha.Descripcion)` en `FichaEstablecimiento.razor` | `El_texto_del_propietario_con_script_se_muestra_escapado` |
| RN-173 | Quitar `/login` del grupo `autenticacion` | `El_sexto_envio_a_login_desde_la_misma_ip_se_rechaza`, `Las_rutas_de_cuenta_comparten_el_cupo_de_autenticacion` y un caso de `Asigna_cada_peticion_a_su_grupo` |
| RN-176 | Bajar `System.Security.Cryptography.Pkcs` a 10.0.12 | `dotnet build` falla con `NU1902` y `NU1903` |
| H-02 | Navegar sin `DestinoLocal` en `IdentityRedirectManager` | 10 de los 16 casos de `PruebasIdentityRedirectManager` |
| H-02 | Quitar solo la comprobación del prefijo `/\` | Los casos de `/\evil.com` y `/\/evil.com` (`/revisar 19`) |
| H-02 | No volver a validar la ruta de una URI absoluta propia | Los 5 casos del mismo origen con barras de más o con el host en mayúsculas (segunda ronda de `/revisar 19`) |

## 5. Lo que no tiene prueba automática

| Qué | Por qué | Cómo se comprueba |
|---|---|---|
| Render de las páginas Razor | Sin bUnit ni `WebApplicationFactory` en el stack (AGENTS §4) | La reflexión de `PruebasAutorizacionRutas` para la autorización; la ficha pública, con `HtmlRenderer` (Fase 19); el resto, en el navegador |
| Rutas privadas por HTTP | Igual | `curl` a `/app/*` y `/my-account/*` sin sesión: 302 a `login`. Hecho el 2026-10-24: las 9 rutas privadas dan 302 y `/` y `/businesses`, 200 (`fase-17.md` §7). Repetir si cambia `Routes.razor` o la autorización |
| Token de restablecimiento caducado o reutilizado (RN-159) | Lo gestiona Identity | A mano |
| Envío SMTP real y entregabilidad (R-8) | Necesita credenciales del desarrollador | Mailtrap |
| `429` de punta a punta y cabeceras en una respuesta real (RN-173, RN-175) | La canalización HTTP completa no se levanta en pruebas | `curl` contra una instancia propia (Fase 19, `fase-19.md` §11): sexto `POST` a `/login` y petición 61 a `/businesses`, `429`; cabeceras en `/` |
| CSP en el navegador (R-12) | Solo el navegador aplica la CSP | Navegación por las tres áreas con la consola abierta. Hecha por el desarrollador el 2026-10-25, sin errores (Control 5) |
| Auditoría de dependencias (RN-176) | Actúa en la restauración, no en una prueba | Sabotaje de la Fase 19 |

Una guarda conocida: `PruebasConsultasReservasEmpresa.Las_consultas_no_dejan_entidades_en_seguimiento` no puede fallar con el código actual, porque las consultas proyectan y el contexto ya no hace seguimiento. Se mantiene como protección frente a un cambio que pase a cargar entidades.

Otra guarda del mismo tipo: `PruebasConsultarDisponibilidad.Dos_consultas_simultaneas_del_mismo_hueco_lo_muestran_disponible_a_ambas` (CU-03 l.200). Dos lecturas sin escrituras siempre coinciden, así que hoy no puede fallar. Se mantiene frente a una consulta que llegue a bloquear o reservar el hueco.

## 6. Evidencia al cierre (Fase 20)

Ejecución desde un clon limpio del commit `96f05dd`, el 2026-10-25 (`fase-20.md`, T8): `dotnet build` sin avisos ni errores.

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 449 | 449 | 0 | 0 |
| Integración | 536 | 536 | 0 | 0 |

Evolución por fase, de cada `fase-NN-revision.md`, en `PROJECT_PLAN.md` §7. Las 9 pruebas de integración añadidas tras la Fase 19 son de la tarea de interfaz fuera de fase (`96f05dd`): `PruebasDialogo` (3), `PruebasDestinoInicioSesion` (5) y un caso nuevo en `PruebasInicioSesion`.
