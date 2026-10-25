# Fase 17 — Pruebas finales: integración, autorización, concurrencia, regresión, cifrado

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 6 h (SPEC §19, l.1112; 1 h de ellas es del cifrado, l.1134) |
| **Semana** | 6 (`PROJECT_PLAN.md` §2, l.129-140) |
| **Cierra** | Ninguno. Vuelve a comprobar los Controles 1 y 3 y deja preparado el Control 6 (`PROJECT_PLAN.md` §3). No se recorta (l.250) |
| **Generada** | 2026-10-24 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar, el proyecto tiene las pruebas finales que exige el SPEC, y lo que ya estaba cubierto queda marcado con su evidencia:

- **Cifrado de extremo a extremo** con SQL crudo sobre un sistema con datos completos (RN-162 a RN-167).
- **Autorización de las rutas privadas** (SPEC §16.2).
- **Concurrencia de RN-97**: un hueco cancelado se vuelve a reservar una sola vez bajo carga.
- **Regresión** de los pendientes asignados a esta fase por las fases 5 a 16b.
- **Datos de demostración** (SPEC l.873).
- **El entregable `docs/pruebas.md`** (SPEC §18, l.1080).
- **Los criterios de §21** que ya tienen evidencia, marcados.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §10.4 (l.602-604) | Las cinco pruebas de aislamiento: leer, modificar y eliminar un recurso de otra empresa da "no encontrado" y el recurso queda intacto |
| SPEC §15 (l.893-911) | Qué se cifra: `asp_net_users` y las copias de `reservas`. Los datos de la empresa quedan en claro. `ILookupProtector` para buscar por correo |
| SPEC §16.1, §16.2, §16.3 (l.976-1007) | Las 13 del motor; integración con "autorización de rutas privadas" y "aislamiento entre empresas (5 pruebas)"; concurrencia: "exactamente 1 éxito y N−1 errores `HuecoYaNoDisponible`. Repetido varias iteraciones" |
| SPEC §18 (l.1080) | `docs/pruebas.md` (Fase 17) |
| SPEC §19 (l.1112, l.1134) | 6 h, de ellas 1 h de pruebas de cifrado |
| SPEC l.873 | Datos iniciales: "2 empresas de demostración con sus servicios, profesionales y horarios, y usuarios de prueba" |
| SPEC §21 (l.1207-1232) | Criterios del MVP; varios ya cubiertos siguen sin marcar (l.1214, l.1216, l.1220-1222) |
| A-10 (`arquitectura.md` §12.2) | Contenedor compartido y migraciones, nunca `EnsureCreated` |
| `arquitectura.md` §12.3 | Concurrencia con un ámbito de DI y un `ContextoDatos` por tarea |
| RN-02 ⚑, RN-03 ⚑ | Aislamiento entre empresas (trazabilidad l.363) |
| RN-05 | El cliente solo ve lo suyo; `/my-account/*` exige el rol `Cliente` |
| RN-76 ⚑, RN-77 ⚑, RN-80 | Concurrencia y restricción `EXCLUDE` (trazabilidad l.372) |
| RN-97 ⚑ (U, **C**) | El hueco cancelado se libera de inmediato, para el motor y para la restricción (trazabilidad l.373) |
| RN-162, RN-163 ⚑, RN-164 | SQL crudo: nada en claro en `asp_net_users` ni en `reservas`; `empresas` en claro y el buscador por ciudad funciona (trazabilidad l.381-382) |
| RN-166, RN-167 | Inicio de sesión, correo duplicado y restablecimiento con el cifrado activo (trazabilidad l.383) |
| RN-140, RN-141, RN-154 | Publicación después del commit, también de `CuentaRegistrada` (`/revisar 16b`, menor 6) |
| RN-159 | El token caduca y es de un solo uso; comprobación manual (`PROJECT_PLAN.md` l.305, `casos-uso.md` l.544) |
| CU-03 (l.200) | "Dos consultas simultáneas del mismo hueco lo muestran disponible a ambas", marcado sin evidencia (auditoría 14, caso 8) |
| CU-10 (l.301), CU-22 (l.443) | El hueco cancelado vuelve a ofrecerse |
| R-1, R-3, R-10, R-11 | Ver §8 |
| `fase-05.md` l.211, l.257; `fase-06.md` l.242, l.290; `fase-11.md` l.347 | Prueba final de cifrado con SQL crudo |
| `fase-07.md` l.241-242, l.300 | Autorización de rutas privadas y repaso del aislamiento |
| `fase-13.md` duda 13 | Empresas de demostración |
| `fase-15.md` duda 10 | Pendientes de la Fase 14: `EstablecerTelefono` (revisión 14, importante 1) |
| `fase-14-auditoria-pruebas.md` l.42-47, l.54 | Casos 3, 4, 5 y 8, y prueba débil 2 |
| `fase-16-auditoria-pruebas.md` l.91-95 | Caso 5 (Scoped), caso 6 (`DateOnly.MaxValue`), caso 7 y prueba débil 4 |
| `fase-16b-revision.md` (17:34) | Importante 1 (`arquitectura.md` §13.6) y menor 6 (`CuentaRegistrada` después del commit) |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| Las cinco pruebas transversales de AGENTS §11 bis | ✔ comprobado |
| `PruebasConcurrenciaReservas`: 5 iteraciones de 10 tareas, cada una con su ámbito | ✔ comprobado |
| `PruebasEsquema.La_restriccion_de_no_solapamiento_existe_en_la_base_de_datos` (catálogo `pg_constraint`) | ✔ comprobado |
| Pruebas de cifrado por clase (`PruebasUsuario`, `PruebasConfiguracionReserva`, protectores, anillo) | ✔ comprobado |
| Matriz de políticas parcial (`PruebasInyeccionDependencias.EsPropietario_exige_el_rol_y_el_claim_de_empresa`) | ✔ comprobado (faltan dos combinaciones) |
| `[Authorize(Policy = …)]` en `App/_Imports.razor` y `MiCuenta/_Imports.razor` | ✔ comprobado |
| Referencia de `PruebasIntegracion` a `Web` | ✘ no existe: duda 2 |
| Infraestructura de pruebas HTTP (`WebApplicationFactory`) | ✘ no hay paquete: duda 2 |
| `SembradorDatos` (categorías, idempotente) | ✔ comprobado; sin empresas de demostración: duda 5 |
| `docs/pruebas.md` | ✘ no existe (se crea en T6) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | — |
| `src/PlataformaReservas.Aplicacion` | sí | `CasosUso/Cuenta/EstablecerTelefono.cs` (duda 3). `CasosUso/Reservas/ConsultarReservasEmpresa.cs` (duda 4) |
| `src/PlataformaReservas.Infraestructura` | sí | `Persistencia/DatosIniciales/SembradorDatos.cs` (duda 5). **Excepción al radio, autorizada por el desarrollador el 2026-10-24** al pedir la corrección de los menores de `/revisar 16b` (`fase-16b-revision.md`, seguimiento): `Correo/UrlBaseCorreo.cs` (nuevo, única validación de `Correo:UrlBase`), `Correo/RenderizadorPlantillas.cs` e `Identidad/SolicitudRestablecimientoClave.cs`. Cambio de comportamiento aceptado: con `UrlBase` vacía, los correos de RN-152 y RN-153 fallan al componerse y el procesador lo registra (RN-141), en lugar de salir con enlaces relativos |
| `src/PlataformaReservas.Web` | sí | `appsettings.json` y `appsettings.Development.json`: solo la sección `DatosDemostracion` sin contraseñas (duda 5) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Dtos/Empresas/PruebasDatosReservaDto.cs` (prueba débil 2) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `PlataformaReservas.PruebasIntegracion.csproj` (referencia a `Web`, duda 2). Transversales nuevos: `Persistencia/PruebasCifradoEnReposo.cs` (duda 1) y `Autorizacion/PruebasAutorizacionRutas.cs` (duda 2). Ampliaciones de `Reservas/PruebasConcurrenciaReservas.cs`, `CasosUso/Reservas/PruebasCancelarReservaEmpresa.cs`, `PruebasConsultarDisponibilidad.cs`, `PruebasConsultarReservasEmpresa.cs`, `CasosUso/Cuenta/PruebasEstablecerTelefono.cs`, `Consultas/PruebasConsultasCatalogoPublico.cs`, `Identidad/PruebasRegistroCliente.cs`, `Persistencia/PruebasDatosIniciales.cs` y `PruebasInyeccionDependencias.cs`. **Añadidos con autorización del desarrollador (2026-10-24):** `Correo/PruebasUrlBaseCorreo.cs` (nuevo) y `Correo/PruebasRenderizadorPlantillas.cs`, por los menores de `/revisar 16b`; `Infraestructura/BaseDatosFixture.cs` (`CrearServicios` con configuración adicional y las mismas claves de cifrado; auditoría 17, menor 3) y `CasosUso/Horarios/PruebasAgregarIntervalo.cs` (CU-16; auditoría 17, menor 5), por `/auditar-pruebas 17` |
| `docs/` | sí | `pruebas.md` (nuevo). `TFM_PlataformaReservas_SPEC.md` §21 (marcar con evidencia). `casos-uso.md` (criterios con evidencia). `arquitectura.md` §6 (duda 3), §12 (cinco pruebas de arquitectura y estado real de la limpieza entre pruebas) y §13.6 (duda 7). `AGENTS.md` §11 bis (dudas 1 y 2). Este fichero. **Añadidos con autorización del desarrollador (2026-10-24):** `arquitectura.md` §2.1 (`PI --> W`, duda 2) y §11.3 (`UrlBase`); `AGENTS.md` §6 (árbol de `tests/`, duda 2) y §8 (`IConsultasNotificacion`, menor 5 de 16b); `casos-uso.md` (criterio de CU-16); `fase-16b.md`, `fase-16b-revision.md`, `fase-17-auditoria-pruebas.md` y `fase-17-revision.md`. Herramienta del proceso: `.claude/skills/revisar/comprobaciones.sh`, solo la lista `TRANSVERSALES` de C18 (dudas 1 y 2) |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- ningún paquete (sin `Microsoft.AspNetCore.Mvc.Testing` ni bUnit);
- el endurecimiento: bloqueo, limitación de frecuencia, cabeceras y CSP (Fase 19);
- el `Dockerfile` (Fase 18);
- ninguna página Razor;
- ningún secreto ni contraseña en el repositorio.

## 5. Tareas

### T1 — Cifrado de extremo a extremo (≈ 1 h)

- **Ficheros:** crea `tests/PlataformaReservas.PruebasIntegracion/Persistencia/PruebasCifradoEnReposo.cs` (duda 1).
- **Implementa:** RN-162, RN-163, RN-164, RN-166, RN-167, R-10, R-11, SPEC §15 y §21 (l.1224-1226).
- **Contenido:**
  - **Escenario completo:** un cliente con teléfono, un propietario con su empresa publicable y una reserva, creados por los casos de uso y servicios reales.
  - **SQL crudo sobre `asp_net_users`:** ninguna columna contiene en claro el correo, el nombre de usuario, el nombre completo ni el teléfono. Tampoco la columna normalizada.
  - **SQL crudo sobre `reservas`:** `nombre_cliente` y `telefono_cliente` no contienen el nombre ni el teléfono en claro (R-11).
  - **SQL crudo sobre `empresas`:** nombre, dirección y ciudad están en claro (RN-164), y `BuscarEstablecimientos` encuentra la empresa por su ciudad.
  - **Con el cifrado activo:**
    - `FindByEmailAsync` encuentra al usuario y LINQ sobre `Users` no (R-10);
    - el inicio de sesión funciona;
    - registrar el mismo correo otra vez falla;
    - la solicitud de restablecimiento genera el enlace (RN-166, RN-167).
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Autorización de rutas privadas (≈ 1 h)

- **Ficheros:**
  - modifica `tests/PlataformaReservas.PruebasIntegracion/PlataformaReservas.PruebasIntegracion.csproj` (referencia a `Web`);
  - crea `tests/PlataformaReservas.PruebasIntegracion/Autorizacion/PruebasAutorizacionRutas.cs` (duda 2);
  - modifica `PruebasInyeccionDependencias.cs` (matriz de políticas).
- **Implementa:** SPEC §16.2 ("autorización de rutas privadas"), RN-02, RN-05, `arquitectura.md` §8.3.
- **Contenido:**
  - **Matriz de políticas completa** con `IAuthorizationService`:
    - cliente con claim de empresa contra `EsPropietario`: no pasa sin el rol;
    - propietario contra `EsCliente`: pasa, porque también tiene el rol `Cliente` (Fase 15, duda 6);
    - anónimo contra las dos: no pasa.
  - **Reflexión sobre el ensamblado `Web`:**
    - cada componente con `[Route]` en el espacio de nombres del área de empresa lleva `[Authorize(Policy = "EsPropietario")]`;
    - los de `MiCuenta` llevan `EsCliente`;
    - el resto de rutas privadas conocidas (`/Account/Manage/*` de la plantilla) llevan `[Authorize]`.
  - **Lista de rutas públicas permitidas.** Las rutas públicas (portada, búsqueda, ficha, reserva, cuenta) están en una lista explícita. Cualquier ruta nueva que no esté protegida ni en esa lista hace fallar la prueba.
  - **Comprobación manual con `curl`** en una instancia propia en el puerto 5199: `/app/*` y `/my-account/*` responden 302 a `login` sin sesión.
- **Hecho cuando:** las pruebas de T2 de §6 están en verde y la comprobación manual está anotada.
- **Verificación:** `dotnet test` y `curl`.

### T3 — Concurrencia y RN-97 (≈ 0,75 h)

- **Ficheros:**
  - modifica `tests/PlataformaReservas.PruebasIntegracion/Reservas/PruebasConcurrenciaReservas.cs`;
  - modifica `CasosUso/Reservas/PruebasCancelarReservaEmpresa.cs` y `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs`.
- **Implementa:** RN-76, RN-77, RN-80, RN-97 (C), CU-03 (l.200), CU-10 (l.301), CU-22 (l.443), R-3, Control 3.
- **Contenido:**
  - **Concurrencia de RN-97:** se crea una reserva, se cancela y N tareas, cada una con su ámbito (`arquitectura.md` §12.3), intentan reservar el hueco liberado. Debe haber exactamente un éxito, N−1 `HuecoYaNoDisponible` y una sola reserva no cancelada. Se repite varias iteraciones, como §16.3.
  - **RN-97 con origen `Empresa`:** la empresa cancela y el mismo hueco se vuelve a reservar (auditoría 16, no verificable).
  - **CU-03 l.200:** dos consultas de disponibilidad simultáneas, con ámbitos distintos, muestran el mismo hueco libre a las dos.
  - **R-3:** se mantiene la prueba del catálogo `pg_constraint`; se anota como evidencia del Control 3.
- **Hecho cuando:** las pruebas de T3 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T4 — Regresión de los pendientes asignados (≈ 1,25 h)

- **Ficheros:**
  - modifica `src/PlataformaReservas.Aplicacion/CasosUso/Cuenta/EstablecerTelefono.cs` (duda 3) y `CasosUso/Reservas/ConsultarReservasEmpresa.cs` (duda 4);
  - pruebas en `CasosUso/Cuenta/PruebasEstablecerTelefono.cs`, `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs`, `Consultas/PruebasConsultasCatalogoPublico.cs`, `Identidad/PruebasRegistroCliente.cs`, `PruebasInyeccionDependencias.cs` y, en la suite unitaria, `Dtos/Empresas/PruebasDatosReservaDto.cs`.
- **Implementa:** los pendientes de §2. RN-140 y RN-154 (`CuentaRegistrada` después del commit), `arquitectura.md` §6 (precondición primero).
- **Contenido:**
  - **`EstablecerTelefono`** (revisión 14, importante 1): comprueba con `IServicioCuentas.ObtenerContactoAsync` si el usuario ya tiene teléfono y, si lo tiene, `Fallo(Conflicto)` sin llamar a la escritura. El `catch` se queda solo para la carrera entre dos pestañas, documentada (duda 3). Se elimina la comprobación muerta `if (telefono is null)`.
  - **`ConsultarReservasEmpresa`** (auditoría 16, caso 6): una fecha cuyo día siguiente no existe (`DateOnly.MaxValue`) da `Validacion` (duda 4).
  - **Prueba débil 2 de la auditoría 14:** `PruebasDatosReservaDto` ordena con un servicio que empieza por tilde ("Ácido" frente a "Corte").
  - **Caso 5:** `IConsultasReservaPublica`, `IConsultasReservasCliente`, `ConsultarDisponibilidad` y `EstablecerTelefono` en las comprobaciones de `Scoped`.
  - **Casos 3 y 4 de la auditoría 14:**
    - con dos empresas publicables, los datos para reservar de A no llevan nada de B;
    - `ObtenerDatosReservaAsync` no deja entidades en seguimiento.
  - **`CuentaRegistrada` después del commit** (`/revisar 16b`, menor 6): el publicador de la prueba comprueba, al publicar, que el usuario ya es legible desde otro ámbito.
  - **Prueba débil 4 de la auditoría 16:** se anota en `docs/pruebas.md` como guarda frente a cargar entidades. No se cambia.
- **Hecho cuando:** las pruebas de T4 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T5 — Datos de demostración (≈ 1 h)

- **Ficheros:**
  - modifica `src/PlataformaReservas.Infraestructura/Persistencia/DatosIniciales/SembradorDatos.cs`;
  - modifica `src/PlataformaReservas.Web/appsettings.json` y `appsettings.Development.json` (sección `DatosDemostracion`, sin contraseñas);
  - pruebas en `Persistencia/PruebasDatosIniciales.cs`.
- **Implementa:** SPEC l.873, `fase-13.md` duda 13, RN-16 (las dos empresas son publicables).
- **Contenido:**
  - **Solo con `DatosDemostracion:Habilitado = true`:**
    - siembra 2 empresas de demostración, cada una con servicios, profesionales asociados y horarios, publicables;
    - siembra un propietario por empresa y un cliente de prueba.
  - **Los usuarios se crean con `UserManager`**, con el cifrado activo (AGENTS §8 quater).
  - **La contraseña sale de `DatosDemostracion:Clave`, que pone el desarrollador en User Secrets.** Nunca está en el repositorio. Si falta, no se siembran usuarios y se registra un aviso.
  - **Idempotente**, como las categorías: sembrar dos veces no duplica nada.
  - `false` por defecto en `appsettings.json`.
- **Hecho cuando:** las pruebas de T5 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T6 — Cierre: sabotajes, documentación y auditoría (≈ 1 h)

- **Ficheros:** crea `docs/pruebas.md`; modifica `docs/TFM_PlataformaReservas_SPEC.md` §21, `docs/casos-uso.md`, `docs/arquitectura.md` §6, §12 y §13.6, `AGENTS.md` §11 bis y este fichero.
- **Implementa:** SPEC §18 (l.1080), AGENTS §11 bis (sabotajes y auditoría), `fase-16b-revision.md` importante 1 (duda 7).
- **Contenido:**
  - **`docs/pruebas.md`:**
    - estrategia: las dos suites, el contenedor compartido y las migraciones (A-10);
    - las pruebas transversales y qué demuestra cada una;
    - la trazabilidad de las RN ⚑ con su prueba (`reglas-negocio.md` §12);
    - los sabotajes de las fases 10 a 17 con la prueba que falló;
    - las limitaciones (sin bUnit ni pruebas HTTP, comprobaciones manuales);
    - cómo ejecutar las pruebas.
  - **SPEC §21:** marcar con evidencia lo ya cubierto (l.1214, l.1216, l.1220-1222) y anotar lo que sigue pendiente y de qué fase es.
  - **`arquitectura.md`:**
    - §12: cinco pruebas de arquitectura, no cuatro, y el estado real de la limpieza entre pruebas (sin `Respawn`);
    - §13.6: la frase sobre la descodificación del enlace (duda 7);
    - §6: la excepción de la carrera en `EstablecerTelefono` (duda 3).
  - **`AGENTS.md` §11 bis:** los dos transversales nuevos (dudas 1 y 2).
  - **Sabotajes de §6** y `/auditar-pruebas 17`.
  - **Comprobación manual del token de CU-24** (caduca y no se reutiliza, RN-159): la hace el desarrollador.
- **Hecho cuando:** los sabotajes han fallado su prueba, `docs/pruebas.md` existe y la auditoría está ejecutada.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 6 h de 6 h presupuestadas** (1 + 1 + 0,75 + 1,25 + 1 + 1).

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| SQL crudo: ninguna columna personal de `asp_net_users` contiene en claro el correo, el nombre ni el teléfono | I | `Persistencia/PruebasCifradoEnReposo.cs` | RN-162, R-10 | T1 |
| SQL crudo: `reservas` no contiene en claro el nombre ni el teléfono del cliente | I | `Persistencia/PruebasCifradoEnReposo.cs` | RN-163, R-11 | T1 |
| SQL crudo: `empresas` en claro y el buscador por ciudad la encuentra | I | `Persistencia/PruebasCifradoEnReposo.cs` | RN-164 | T1 |
| Con el cifrado activo: `FindByEmailAsync` encuentra y LINQ no; inicio de sesión; correo duplicado rechazado; restablecimiento | I | `Persistencia/PruebasCifradoEnReposo.cs` | RN-166, RN-167 | T1 |
| Matriz de políticas completa (anónimo, cliente, cliente con claim, propietario, propietario sin claim) contra `EsPropietario` y `EsCliente` | I | `PruebasInyeccionDependencias.cs` | RN-02, RN-05 | T2 |
| Toda ruta del área de empresa exige `EsPropietario`; toda ruta de `MiCuenta` exige `EsCliente` | I | `Autorizacion/PruebasAutorizacionRutas.cs` | SPEC §16.2, RN-02, RN-05 | T2 |
| Ninguna ruta nueva queda sin proteger fuera de la lista de públicas | I | `Autorizacion/PruebasAutorizacionRutas.cs` | SPEC §16.2 | T2 |
| Tras cancelar, N tareas concurrentes sobre el hueco liberado crean exactamente una reserva; varias iteraciones | C | `Reservas/PruebasConcurrenciaReservas.cs` | RN-97, RN-77, RN-80 | T3 |
| La empresa cancela y el mismo hueco se vuelve a reservar | I | `CasosUso/Reservas/PruebasCancelarReservaEmpresa.cs` | RN-97, CU-10 (l.301) | T3 |
| Dos consultas simultáneas de disponibilidad muestran el mismo hueco libre a las dos | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | CU-03 (l.200) | T3 |
| `EstablecerTelefono` con teléfono ya puesto: `Conflicto` sin llamar a la escritura; la carrera sigue dando `Conflicto` | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | `arquitectura.md` §6 | T4 |
| `ConsultarReservasEmpresa` con `DateOnly.MaxValue`: `Validacion` | I | `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` | AGENTS §11 bis (extremos) | T4 |
| Orden `es-ES` de los servicios para reservar con un nombre que empieza por tilde | U | `Dtos/Empresas/PruebasDatosReservaDto.cs` | Auditoría 14, débil 2 | T4 |
| `IConsultasReservaPublica`, `IConsultasReservasCliente`, `ConsultarDisponibilidad` y `EstablecerTelefono` son Scoped | I | `PruebasInyeccionDependencias.cs` | Auditorías 14 y 16, caso 5 | T4 |
| Dos empresas publicables: los datos para reservar de A no llevan nada de B; sin entidades en seguimiento | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-03, A-05 | T4 |
| `CuentaRegistrada` se publica cuando el usuario ya es legible desde otro ámbito | I | `Identidad/PruebasRegistroCliente.cs` | RN-140, RN-154 | T4 |
| Con `DatosDemostracion:Habilitado`, dos empresas publicables y sus usuarios; idempotente; sin clave no se crean usuarios; desactivado por defecto no siembra nada | I | `Persistencia/PruebasDatosIniciales.cs` | SPEC l.873, RN-16 | T5 |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| RN-169, RN-173 | Bloqueo y `429` | Fase 19 (endurecimiento) |
| RN-171 en la ficha pública | HTML escapado de la ficha | Fase 19, revisión E-3 (duda 6). El correo ya está cubierto (Fase 16b) |
| `PruebasCrearReserva`, empresa inexistente | Auditoría 14, caso 7 | Asignado a la Fase 19 por `PROJECT_PLAN.md` §6 |
| Páginas Razor | Prueba de render o HTTP | Sin bUnit ni `WebApplicationFactory` (duda 2). Se cubren con la reflexión y `curl` |
| `SembradorDatos` | Recurso de otra empresa; conflicto | No es un caso de uso; siembra datos fijos |
| RN-159 | Prueba automática del token | Lo gestiona Identity; se comprueba a mano (`PROJECT_PLAN.md` l.305) |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-163 | En `ConfiguracionReserva`, quitar el `ConversorCifrado` de `NombreCliente` | `SQL crudo: reservas no contiene en claro…` | Hecho el 2026-10-24: falló `Ni_asp_net_users_ni_reservas_guardan_en_claro_nombres_correos_ni_telefonos`. Restaurado |
| RN-02, RN-05 | Quitar `[Authorize(Policy = "EsCliente")]` de `MiCuenta/_Imports.razor` | `Toda ruta de MiCuenta exige EsCliente` | Hecho: fallaron `Toda_ruta_del_area_de_cliente_exige_la_politica_de_cliente` y `Ninguna_ruta_queda_sin_proteger_salvo_las_publicas_declaradas`. Restaurado |
| RN-02 | Añadir `[AllowAnonymous]` a `App/Configuracion.razor` | `Ninguna ruta fuera de las públicas admite anónimos` | Hecho el 2026-10-24 (auditoría 17): falló `Ninguna_ruta_fuera_de_las_publicas_declaradas_admite_anonimos`. Restaurado |
| RN-02 | Cambiar la política de `App/_Imports.razor` a `EsCliente` | `Toda página del espacio de nombres del área de empresa exige EsPropietario` | Hecho el 2026-10-24 (`/revisar 17`, menor 5): fallaron `Toda_pagina_del_espacio_de_nombres_del_area_de_empresa_exige_la_politica_de_propietario` y `Toda_ruta_del_area_de_empresa_exige_la_politica_de_propietario`. Restaurado |
| SPEC l.873 | Volver al `return` temprano cuando el propietario de demostración ya existe | `Los datos de demostración… idempotentes` | Hecho el 2026-10-24 (`/revisar 17`, menor 6): falló `Los_datos_de_demostracion_solo_se_siembran_habilitados_desde_el_arranque…`. Restaurado |
| RN-97 | En la restricción de la migración o en el filtro del motor, contar también las canceladas | `Tras cancelar, N tareas concurrentes…` | Hecho, quitando el filtro `Estado != Cancelada` de `ConsultasDisponibilidad.ObtenerReservasActivasAsync`: fallaron `Tras_cancelar_diez_peticiones_simultaneas_sobre_el_hueco_liberado_crean_una_sola_reserva` y `El_hueco_que_cancela_la_empresa_se_vuelve_a_ofrecer_y_se_puede_volver_a_reservar`. Restaurado |

## 7. Criterios de aceptación

- [x] **SPEC §21 (l.1224):** "Un `SELECT` directo sobre `asp_net_users` y sobre `reservas` no muestra ningún nombre, correo ni teléfono en claro." Comprobado otra vez con datos completos.
- [x] **SPEC §21 (l.1225):** "Iniciar sesión, registrar un correo duplicado y recuperar la contraseña siguen funcionando con el cifrado activo."
- [x] **SPEC §21 (l.1226):** "Los datos de la empresa **sí** están en claro, y el buscador por ciudad funciona."
- [x] **SPEC §21 (l.1214):** "**No es posible crear dos reservas solapadas para el mismo profesional**, y existe una prueba de concurrencia que lo demuestra contra PostgreSQL real."
- [x] **SPEC §21 (l.1215):** "**No existe acceso entre empresas**, y existen 5 pruebas que lo demuestran." Comprobado otra vez.
- [x] **SPEC §16.2:** las rutas privadas exigen su política y ninguna ruta nueva queda sin proteger. Evidencia:
  - **Reflexión:** `PruebasAutorizacionRutas` comprueba la política por prefijo y por espacio de nombres, y la ausencia de `[AllowAnonymous]`, con sabotajes.
  - **`curl` del 2026-10-24**, sin sesión, sobre una instancia propia en el puerto 5199 contra un PostgreSQL desechable, ya borrado: las 9 rutas privadas (`/app`, `/app/settings`, `/app/schedules`, `/app/staff`, `/app/bookings`, `/app/services`, `/my-account/bookings`, `/my-account/bookings/{id}` y `/my-account/profile`) responden **302** a `/login?ReturnUrl=…`; `/` y `/businesses` responden **200**.
- [x] **Concurrencia de RN-97:** demostrada con la prueba de concurrencia.
- [x] **`docs/pruebas.md` existe** (SPEC §18).
- [x] **SPEC §21 y `casos-uso.md` marcados con evidencia** donde la haya.
- [x] **Sin paquetes nuevos**, ningún secreto en el repositorio, y `DatosDemostracion:Habilitado` en `false` por defecto.
- [x] **Los sabotajes de §6** hacen fallar sus pruebas.
- [x] **`dotnet build`** sin avisos y **`dotnet test`** en verde.

**Control 3** (`PROJECT_PLAN.md` §3, l.175-181, literal; se vuelve a comprobar):

```text
Pruebas de integracion de reservas en verde
Prueba de concurrencia en verde contra PostgreSQL real (Testcontainers)
La restriccion ex_reservas_sin_solape EXISTE en la base de datos de pruebas
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-3 | La prueba de concurrencia de RN-97 pasa sin la restricción `EXCLUDE` | El esquema sale de las migraciones (A-10) y `PruebasEsquema` consulta `pg_constraint`. Hay sabotaje de RN-97 |
| R-10 | Una prueba busca por correo con LINQ y "funciona" porque no encuentra nada | T1 comprueba las dos vías: `FindByEmailAsync` encuentra y LINQ no |
| R-11 | Las copias de `reservas` en claro junto a un usuario cifrado | SQL crudo sobre `reservas` en T1, con sabotaje del conversor |
| R-1 | Las tareas concurrentes comparten `ContextoDatos` y la prueba mide otra cosa | Un ámbito de DI por tarea (`arquitectura.md` §12.3) |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `Dockerfile` y `docker compose up` desde cero | Fase 18 |
| Endurecimiento (bloqueo, `429`, cabeceras, CSP, dependencias vulnerables) y RN-171 en la ficha | Fase 19 (duda 6) |
| Auditoría 14, caso 7 (empresa inexistente en `CrearReserva`) | Fase 19 (`PROJECT_PLAN.md` §6) |
| Control 4b manual con Mailtrap, entregabilidad, SPF y DKIM | El desarrollador |
| Pantallas pendientes de recorrer en el navegador (`fase-14-revision.md` l.122, `fase-15-revision.md` l.84) | El desarrollador; se anotan en `docs/pruebas.md` |
| Actualizar la tabla de `PROJECT_PLAN.md` §7 | No asignado a ninguna fase |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-02, RN-03 | T2, T4 | `PruebasAislamientoEmpresas` (ya existe), `PruebasAutorizacionRutas`, `PruebasConsultasCatalogoPublico` (dos empresas) |
| RN-05 | T2 | `PruebasAutorizacionRutas`, matriz de políticas; sabotaje |
| RN-76, RN-77, RN-80 | T3 | `PruebasConcurrenciaReservas` (ya existe y se amplía) |
| RN-97 | T3 | `PruebasConcurrenciaReservas` (C), `PruebasCancelarReservaEmpresa`; sabotaje |
| RN-140, RN-154 | T4 | `PruebasRegistroCliente` (después del commit) |
| RN-159 | T6 | Manual |
| RN-162, RN-163, RN-164 | T1 | `PruebasCifradoEnReposo`; sabotaje de RN-163 |
| RN-166, RN-167 | T1 | `PruebasCifradoEnReposo` |
| RN-16 | T5 | `PruebasDatosIniciales` (empresas de demostración publicables) |
| CU-03 (l.200) | T3 | `PruebasConsultarDisponibilidad` |
| CU-10 (l.301), CU-22 (l.443) | T3 | `PruebasCancelarReservaEmpresa`; `PruebasCancelarReservaCliente` y `PruebasDarDeBajaCuenta` (ya existen) |
| CU-24 (l.544) | T6 | Manual |

## 11. Dudas

Resueltas a petición del desarrollador con la opción recomendada (2026-10-24).

### Decisiones de implementación

- **Hecho el 2026-10-24.** Todo en verde: 448 unitarias y 421 de integración al terminar las tareas. Tras las correcciones de los menores de 16b, de `/auditar-pruebas 17` y de `/revisar 17`: **448 unitarias y 439 de integración**.
- **T2:** la prueba de rutas encontró dos páginas de la plantilla que no estaban en el inventario, `/Error` y `/not-found`. Son públicas por diseño y se han declarado en la lista de públicas. No hay páginas `/Account/Manage/*`.
- **T2, `curl` hecho el 2026-10-24** con autorización del desarrollador: instancia propia en el puerto 5199 contra un PostgreSQL desechable (`docker run`, puerto 15433), con la cadena de conexión y claves de cifrado aleatorias por variables de entorno, sin escribir nada en ficheros. Las 9 rutas privadas dan 302 a `/login` y las públicas 200 (§7). Instancia parada y contenedor borrado; el PostgreSQL de desarrollo no se tocó.
- **T4, `EstablecerTelefono`:** se elimina la comprobación muerta `if (telefono is null)`. Hay prueba de la precondición (no llega a escribir) y de la carrera (otra escritura entre la lectura y la escritura da `Conflicto`).
- **T4, `ConsultarReservasEmpresa`:** rechaza con `Validacion` también `DateOnly.MinValue`, el otro extremo del calendario.
- **T5, datos de demostración:**
  - empresas `demo-barberia-centro` y `demo-estetica-norte`, de lunes a viernes de 9:00 a 14:00 y de 16:00 a 20:00;
  - propietarios `propietario.barberia@demo.example` y `propietaria.estetica@demo.example`, y el cliente `cliente@demo.example`, creados con `UserManager` y sin publicar `CuentaRegistrada`, para no generar correos de bienvenida;
  - **la clave la pone el desarrollador** en User Secrets (`DatosDemostracion:Clave`) y activa la siembra con `DatosDemostracion:Habilitado`;
  - en los dos `appsettings*.json` está desactivada.
- **`.claude/skills/revisar/comprobaciones.sh`:** la comprobación C18 tiene su propia lista de transversales y marcaba los dos nuevos como "varias clases en un fichero". Se añaden `CifradoEnReposo` y `AutorizacionRutas`, para que coincida con AGENTS §11 bis (dudas 1 y 2). Es una herramienta del proceso, no código del producto.
- **`Respawn`:** `arquitectura.md` §12.2 decía que limpiaba entre pruebas, pero no se usa. Se corrige el documento con el estado real y se deja el paquete referenciado: quitarlo sería tocar paquetes, fuera del radio.
- **Pendiente del desarrollador:** el token de CU-24 (caduca y no se reutiliza, RN-159), a mano. `/auditar-pruebas 17` hecho y sus hallazgos corregidos (ver abajo); `curl` hecho (T2).
- **Correcciones de `/revisar 17` (2026-10-24):**
  - **Bloqueantes 1 y 2:** el radio de §4 nombra los ficheros de las correcciones autorizadas, y §9 y la duda 7 ya no mandan los menores de 16b a la Fase 19.
  - **Importante 3:** `PI --> W` en `arquitectura.md` §2.1 y en el árbol de AGENTS §6.
  - **Importante 4:** el `curl` de arriba.
  - **Menor 5:** dos pruebas por espacio de nombres (`AreaEmpresa` y `MiCuenta`) en `PruebasAutorizacionRutas`, además de las de prefijo.
  - **Menor 6:** `SembrarPropietarioAsync` completa la pertenencia si el usuario ya existía sin ella; lo prueba `PruebasDatosIniciales` con un propietario creado de antemano.
  - **Menor 7:** quitados los comentarios que no marcan puntos críticos (`UrlBaseCorreo`, `RenderizadorPlantillas`, `SembradorDatos` y tres de pruebas).
  - **Menor 8:** cifras y cierre actualizados.
- **Correcciones de `/auditar-pruebas 17` (2026-10-24).** Todos los hallazgos, en el radio de `tests/` y `docs/`:
  - **Importante 1:** `PruebasAutorizacionRutas.Ninguna_ruta_fuera_de_las_publicas_declaradas_admite_anonimos`. Sabotaje: `[AllowAnonymous]` en `App/Configuracion.razor` la hace fallar.
  - **Menores 2 y 3:** `PruebasDatosIniciales` siembra con clave a través de `MigrarYSembrarAsync` (sabotaje: quitar la llamada del arranque la hace fallar) y comprueba que cada propietario es miembro solo de su empresa y que su clave funciona. Para ello `BaseDatosFixture.CrearServicios` admite configuración adicional con las mismas claves de cifrado.
  - **Menor 4:** `PruebasConsultarReservasEmpresa.La_fecha_contigua_al_extremo_del_calendario_se_consulta_sin_error`. Sabotaje: ampliar la guarda un día la hace fallar.
  - **Menor 5:** `PruebasAgregarIntervalo.Tras_guardar_el_intervalo_sus_huecos_aparecen_en_la_disponibilidad_de_ese_profesional` (CU-16).
  - **Menor 6:** el criterio de §16.2 queda desmarcado como parcial hasta el `curl`.
  - **Débil 1:** el duplicado comprueba el código `DuplicateEmail`. **Débil 2:** anotada en `docs/pruebas.md`. **Débil 3:** se funde la prueba antigua de teléfono con la de la precondición y se quita de la matriz la línea repetida del propietario.

### Duda 1 — Dónde va la prueba final de cifrado

- **Pregunta:** la prueba "con todo el sistema" (`fase-06.md` l.242) no prueba una clase, sino una propiedad del sistema, y la lista de transversales de AGENTS §11 bis es cerrada.
- **Opciones:**
  - (a) Un transversal nuevo, `Persistencia/PruebasCifradoEnReposo.cs`, añadido a la lista de AGENTS §11 bis.
  - (b) Repartirla entre `PruebasUsuario` y `PruebasConfiguracionReserva`.
- **Recomendación y motivo:** (a). Es una propiedad del sistema, como el aislamiento, y la trazabilidad de RN-162 a RN-164 (l.381-382) la describe como una sola prueba con SQL crudo sobre las dos tablas.
- **Bloquea:** T1.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 2 — Cómo probar la autorización de las rutas privadas

- **Pregunta:** SPEC §16.2 pide "autorización de rutas privadas". No hay infraestructura HTTP ni referencia a `Web` en las pruebas, y no se añaden paquetes sin preguntar (AGENTS §4).
- **Opciones:**
  - (a) Reflexión sobre el ensamblado `Web` (atributos `[Route]` y `[Authorize]`) más la matriz de políticas con `IAuthorizationService`, en un transversal nuevo, `Autorizacion/PruebasAutorizacionRutas.cs`. Exige añadir una referencia de proyecto de `PruebasIntegracion` a `Web`.
  - (b) Pruebas HTTP con `WebApplicationFactory`, que exigen el paquete nuevo `Microsoft.AspNetCore.Mvc.Testing`.
  - (c) Solo comprobación manual con `curl`.
- **Recomendación y motivo:** (a). No añade paquetes, falla en cuanto una página nueva se queda sin política y se completa con `curl` a mano. (b) es más fiel, pero amplía el stack. (c) no protege frente a regresiones.
- **Bloquea:** T2.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 3 — `EstablecerTelefono`: precondición o excepción

- **Pregunta:** el caso de uso resuelve "ya tiene teléfono" capturando la `DominioException` (revisión 14, importante 1), en contra de `arquitectura.md` §6 l.464. `fase-15.md` (duda 10) lo dejó para esta fase.
- **Opciones:**
  - (a) Comprobar antes con `IServicioCuentas.ObtenerContactoAsync` y devolver `Conflicto` sin llamar a la escritura. Se mantiene el `catch` solo para la carrera entre dos pestañas, documentada como excepción en `arquitectura.md` §6.
  - (b) Dejarlo como está.
- **Recomendación y motivo:** (a), como sugería la revisión. La precondición va primero y la carrera queda cubierta.
- **Bloquea:** T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 4 — `DateOnly.MaxValue` en `ConsultarReservasEmpresa`

- **Pregunta:** con `DateOnly.MaxValue`, `MotorDisponibilidad.DiaLocalEnUtc` llama a `AddDays(1)` y lanza `ArgumentOutOfRangeException`. La fecha la manda el cliente.
- **Opciones:**
  - (a) Devolver `Validacion` si no existe el día siguiente a la fecha, con su prueba.
  - (b) Dejar la excepción.
- **Recomendación y motivo:** (a). Una entrada del cliente es un desenlace previsto, que se resuelve con `Resultado` y no con una excepción (AGENTS §8 bis).
- **Bloquea:** T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 5 — Datos de demostración

- **Pregunta:** SPEC l.873 pide "2 empresas de demostración con sus servicios, profesionales y horarios, y usuarios de prueba", y `fase-13.md` (duda 13) lo dejó para esta fase. Los usuarios necesitan una contraseña, y las credenciales nunca van en el repositorio.
- **Opciones:**
  - (a) En `SembradorDatos`, solo con `DatosDemostracion:Habilitado = true` (en `false` por defecto) y con la contraseña en `DatosDemostracion:Clave`, que pone el desarrollador en User Secrets. Sin clave, no se crean usuarios.
  - (b) Llevarlo a la Fase 18.
- **Recomendación y motivo:** (a). Lo necesitan las comprobaciones manuales y el `docker compose` de la Fase 18, y cabe en el presupuesto. La configuración por defecto no siembra nada y el repositorio no lleva ninguna contraseña.
- **Bloquea:** T5.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 6 — RN-171 en la ficha pública

- **Pregunta:** la trazabilidad de RN-171 (l.385) pide comprobar el escapado "en la ficha pública y en el HTML del correo". El correo ya está cubierto (Fase 16b); la ficha no tiene prueba de render.
- **Opciones:**
  - (a) Dejarlo para la Fase 19, la revisión E-3 de las plantillas y la ficha (`arquitectura.md` l.1331; SPEC §21 l.1230).
  - (b) Hacerlo aquí, renderizando `FichaEstablecimiento` con `HtmlRenderer`.
- **Recomendación y motivo:** (a). E-3 y su criterio de §21 son de la Fase 19, y la ficha depende de servicios de la página que habría que simular.
- **Bloquea:** T6 (se anota como omisión).
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 7 — Los hallazgos abiertos de `/revisar 16b`

- **Pregunta:** la última revisión de la 16b dejó abiertos el importante 1 (`arquitectura.md` §13.6) y los menores 2-7, sin fase asignada.
- **Opciones:**
  - (a) El importante 1 (documental) y el menor 6 (prueba de `CuentaRegistrada` después del commit) aquí; los menores 2-5 y 7 a la Fase 19 (refactorización, `PROJECT_PLAN.md` l.276).
  - (b) Todos aquí.
- **Recomendación y motivo:** (a). Lo que es prueba o deja un documento contradiciendo al código va en esta fase; el resto es refactorización.
- **Bloquea:** T4, T6.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.
- **Actualización (2026-10-24):** a petición expresa del desarrollador, los menores 2-5 y 7 también se han resuelto en esta fase y ya no van a la Fase 19. Detalle en el seguimiento de `fase-16b-revision.md`; ficheros en §4.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde (448 unitarias, 439 de integración)
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [x] `/auditar-pruebas 17` (obligatoria en esta fase): hecha, y todos sus hallazgos corregidos (`fase-17-auditoria-pruebas.md`, seguimiento)
- [ ] Commit hecho (mensaje libre)
- [x] Punto de control: ninguno se cierra; Controles 1 y 3 comprobados otra vez (aislamiento, concurrencia y `pg_constraint` en verde)
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6: ninguna es de esta fase
