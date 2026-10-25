# Fase 14 — Flujo de reserva público, incluida la petición de teléfono

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 5,5 h (SPEC §19) |
| **Semana** | 4 (`PROJECT_PLAN.md` §2), junto a las fases 11b, 12 y 13 |
| **Cierra** | Ninguno. Contribuye al Control 4: "el visitante busca, encuentra y reserva" (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-17 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

`/book/{slug}` existe como un único componente `InteractiveServer` por pasos. El recorrido es:

1. servicio, con su duración y su precio;
2. profesional o "cualquier profesional";
3. fecha, dentro de la ventana efectiva;
4. huecos del motor;
5. resumen con observaciones;
6. confirmación.

Un visitante puede consultar la disponibilidad. Para confirmar hay que iniciar sesión, y el flujo se retoma en el mismo punto. Si el perfil no tiene teléfono, se pide y se guarda en el perfil.

La reserva la crea el `CrearReserva` ya existente. La página solo la orquesta.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §11 (l.632-638) | "`/book/{slug}` — flujo completo por pasos en un único componente". "Es el **único** punto de entrada al flujo. El botón de la ficha pública navega aquí" |
| SPEC §12 (l.663-666), §12.1 | "lo usan exactamente igual el flujo público y el área de empresa. No se duplica lógica". Contrato `ObtenerHuecosAsync(empresaId, servicioId, profesionalId?, fecha)` |
| SPEC §13, capa 1 | "Botón de confirmación deshabilitado al primer clic y testigo de idempotencia en el estado del componente" |
| SPEC §13, manejo del error | "La interfaz recarga los huecos del día y pide al usuario que elija otro" |
| SPEC §9.1, §9.10 | `Usuario.Telefono` opcional en el registro; `Reserva.TelefonoCliente` obligatorio |
| D-01 | `InteractiveServer` en la reserva |
| D-02 | No hay reserva como invitado |
| `arquitectura.md` §4.2, §4.3 | Secuencia de `CrearReserva` desde el circuito. El ámbito es el circuito: nunca dos casos de uso en paralelo |
| `arquitectura.md` §2.3, §5.1 | `CasosUso/Cuenta/`, `Components/Reserva/`; correspondencia uno a uno caso de uso ↔ CU |
| `modelo-dominio.md` l.269 | `void EstablecerTelefono(string telefono); // usado al reservar si falta (RN-81)` |
| RN-04, RN-16 ⚑, RN-109 | El flujo público no usa contexto de empresa, y solo admite empresas activas y publicables (precondición de CU-03) |
| RN-23 ⚑ | Política efectiva `Servicio.X ?? Empresa.X`, resuelta una sola vez en el dominio |
| RN-24, RN-33, RN-56 | Servicios y profesionales inactivos, o no asociados, no se ofrecen |
| RN-50 a RN-60 | Motor de disponibilidad (Fase 10). Aquí se consume; RN-60 ⚑: una sola implementación |
| RN-70 a RN-80, RN-83 | Creación de la reserva (Fase 11). Aquí se invoca `CrearReserva` y se tratan sus resultados |
| RN-81, RN-82 | Teléfono opcional al registrarse, obligatorio para reservar; si falta, se pide y se guarda en el perfil |
| RN-100 a RN-102 | UTC en la persistencia; la conversión local ↔ UTC, en un único punto, dentro del motor |
| RN-140 a RN-142 | El evento se encola después del commit (ya lo hace `CrearReserva`); la pantalla no espera al envío |
| RN-162, RN-167 | El teléfono del usuario va cifrado; se escribe por `UserManager`, nunca por LINQ |
| RN-171 ⚑ | Nada de `MarkupString` en las páginas públicas |
| RN-178 | Formato del teléfono |
| CU-03 | Flujo completo, alternativos 1a, 3a, 4a, 6a y 7a |
| CU-07 | Pasos 1 a 12; alternativos 1a, 2a, 2b, 4a, 6a, 8a y 10a |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `CrearReserva` y `CrearReservaComando` (sin `FinUtc`, la copia del contacto sale del perfil, `HuecoYaNoDisponible`) | ✔ comprobado (`Aplicacion/CasosUso/Reservas/`) |
| `IMotorDisponibilidad`, `MotorDisponibilidad` y `HuecoDisponible` | ✔ comprobado (`Aplicacion/Disponibilidad/`) |
| `PoliticasReserva.Resolver` (RN-23) | ✔ comprobado (`Dominio/ValueObjects/PoliticasReserva.cs`) |
| `IConsultasCatalogoPublico` con el filtro `Visibles()` (activa y publicable) | ✔ comprobado (Fase 13) |
| `IServicioCuentas.ObtenerContactoAsync` e `IContextoUsuario` | ✔ comprobado |
| Objeto de valor `Telefono` (RN-178) | ✔ comprobado (`Dominio/ValueObjects/Telefono.cs`) |
| `Login.razor` y `Register.razor` con `ReturnUrl`, y `RedirectToLogin` | ✔ comprobado (`Web/Cuenta/`) |
| La ficha enlaza a `book/{slug}` | ✔ comprobado (`FichaEstablecimiento.razor`) |
| `Usuario.EstablecerTelefono` y el método de cuentas para escribir el teléfono | ✘ ausente. **Se crean en T1**; no bloquean la fase |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | `Telefono` y `PoliticasReserva` ya existen |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IServicioCuentas.cs` (`EstablecerTelefonoAsync`, duda 4). `Abstracciones/IConsultasReservaPublica.cs` (`ObtenerDatosReservaAsync`, duda 1, modificada en implementación). `Dtos/Empresas/DatosReservaDto.cs`, `Dtos/Servicios/ServicioReservaDto.cs` y `Dtos/Profesionales/ProfesionalReservaDto.cs` (duda 1). `CasosUso/Reservas/ConsultarDisponibilidad.cs` y su comando (duda 2). `CasosUso/Cuenta/EstablecerTelefono.cs` y su comando (duda 4). `Disponibilidad/MotorDisponibilidad.cs`, solo el ayudante de fechas (duda 3). `InyeccionDependencias.cs`. Por el hallazgo 7 de `/revisar 14`, ordenado por el desarrollador: `Compartido/OrdenEspanol.cs` (nuevo) y `Dtos/Empresas/FichaEmpresaDto.cs` (Fase 13), que pasa a usarlo |
| `src/PlataformaReservas.Infraestructura` | sí | `Identidad/Usuario.cs` (`EstablecerTelefono`). `Identidad/ServicioCuentas.cs`. `Consultas/ConsultasCatalogoPublico.cs`, que implementa también el puerto nuevo. `InyeccionDependencias.cs` (registro del puerto nuevo: excepción derivada de la duda 1 modificada). **Sin migración**: el esquema no cambia |
| `src/PlataformaReservas.Web` | sí | `Components/Reserva/FlujoReserva.razor` y su `.razor.css` (duda 2) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Dtos/Empresas/PruebasDatosReservaDto.cs`, `Dtos/Servicios/PruebasServicioReservaDto.cs`, `Dtos/Profesionales/PruebasProfesionalReservaDto.cs`, `Disponibilidad/PruebasMotorDisponibilidad.cs` (ayudante de fechas) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Identidad/PruebasUsuario.cs`, `Identidad/PruebasServicioCuentas.cs`, `Consultas/PruebasConsultasCatalogoPublico.cs`, `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs`, `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` |
| `docs/` | sí | `casos-uso.md` (marcar los criterios de CU-03 y CU-07); `arquitectura.md` §2.3 (`ConsultarDisponibilidad`, `EstablecerTelefono` e `IConsultasReservaPublica`), §4.1 y §4.2 (el puerto y la página); `modelo-dominio.md` l.269 (T6); este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:
- `CrearReserva` ni `PruebasCrearReserva.cs` (duda 4);
- `Web/Cuenta/` ni `Program.cs`;
- el algoritmo del motor (solo se extrae el cálculo de fechas, duda 3);
- `/my-account/*` (Fase 15);
- los correos (Fase 16b);
- ningún paquete.

## 5. Tareas

### T1 — Teléfono en el perfil (≈ 1 h)

- **Ficheros:**
  - modifica `Infraestructura/Identidad/Usuario.cs`;
  - modifica `Aplicacion/Abstracciones/IServicioCuentas.cs` e `Infraestructura/Identidad/ServicioCuentas.cs`;
  - crea `Aplicacion/CasosUso/Cuenta/EstablecerTelefono.cs` y `EstablecerTelefonoComando.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs`;
  - pruebas en `PruebasUsuario.cs`, `PruebasServicioCuentas.cs` y `CasosUso/Cuenta/PruebasEstablecerTelefono.cs`.
- **Implementa:** RN-81, RN-82, RN-162, RN-167, RN-178 y CU-07 2a y 2b.
- **Contenido:**
  - `Usuario.EstablecerTelefono(Telefono)` recibe el objeto de valor (AGENTS §7), no `string` como en `modelo-dominio.md` l.269. Esa línea se corrige en T6.
  - `IServicioCuentas.EstablecerTelefonoAsync(Guid usuarioId, Telefono telefono, CancellationToken ct)` escribe por `UserManager.UpdateAsync`, que mantiene el cifrado (RN-162). Nunca por LINQ (RN-167).
  - `EstablecerTelefono.EjecutarAsync(EstablecerTelefonoComando(string Telefono), ct)` hace esto:
    - sin sesión: `NoAutorizado`;
    - teléfono inválido: `Validacion`;
    - si no, lo guarda para el usuario de `IContextoUsuario`.
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Datos para reservar por slug (≈ 1 h)

- **Ficheros:**
  - modifica `IConsultasCatalogoPublico.cs` y `ConsultasCatalogoPublico.cs`;
  - crea `DatosReservaDto`, `ServicioReservaDto` y `ProfesionalReservaDto`;
  - pruebas en `PruebasConsultasCatalogoPublico.cs` y las pruebas unitarias de los tres DTO.
- **Implementa:** RN-04, RN-16 ⚑, RN-23 ⚑, RN-24, RN-33, RN-56, RN-109; CU-03 pasos 1 y 3, alternativos 1a y 3a.
- **Contenido:** `ObtenerDatosReservaAsync(string slug, CancellationToken ct)` devuelve `DatosReservaDto?`.
  - Usa el mismo `Visibles()` y el mismo slug exacto que la ficha. Si la empresa no es visible, `null`.
  - Contiene el `Id` y el nombre de la empresa, y los servicios activos ordenados por nombre (`Id`, nombre, duración, precio y `AntelacionMaximaDias` efectiva con `PoliticasReserva.Resolver`, RN-23).
  - Cada servicio lleva los profesionales activos de la empresa que lo prestan (`Id` y nombre).
  - Sin contexto de empresa (RN-04).
- **Hecho cuando:** las pruebas de T2 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T3 — Consultar disponibilidad (≈ 0,75 h)

- **Ficheros:**
  - crea `Aplicacion/CasosUso/Reservas/ConsultarDisponibilidad.cs` y `ConsultarDisponibilidadComando.cs`;
  - modifica `Aplicacion/Disponibilidad/MotorDisponibilidad.cs` (ayudante de fechas);
  - modifica `InyeccionDependencias.cs`;
  - pruebas en `PruebasMotorDisponibilidad.cs` y `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs`.
- **Implementa:** CU-03 pasos 5 a 7, alternativos 4a, 6a y 7a; RN-55, RN-59, RN-60 ⚑, RN-102.
- **Contenido:**
  - `MotorDisponibilidad.FechasReservables(DateTime ahoraUtc, int antelacionMaximaDias)` es público y estático. Devuelve la primera fecha local (hoy en `Europe/Madrid`) y la última (hoy más la ventana).
  - `SuperaLaVentana` pasa a usarlo, para que la conversión siga en un único punto (RN-102).
  - `ConsultarDisponibilidad.EjecutarAsync(ConsultarDisponibilidadComando(Guid EmpresaId, Guid ServicioId, Guid? ProfesionalId, DateOnly Fecha), ct)` devuelve `Resultado<IReadOnlyList<HuecoDisponible>>`. Delega en `IMotorDisponibilidad`, la misma implementación que usa `CrearReserva` (RN-60).
- **Hecho cuando:** las pruebas de T3 de §6 están en verde y las 13 pruebas del motor siguen en verde.
- **Verificación:** `dotnet test`.

### T4 — Página `/book/{slug}`: selección (≈ 1,25 h)

- **Ficheros:** crea `Web/Components/Reserva/FlujoReserva.razor` y `FlujoReserva.razor.css`.
- **Implementa:** CU-03 completo; D-01; RN-171 ⚑.
- **Contenido:**
  - `@page "/book/{Slug}"`, `@rendermode InteractiveServer` y sin `[Authorize]`: un visitante consulta la disponibilidad.
  - Si `ObtenerDatosReservaAsync` devuelve `null`, `NavigationManager.NotFound()` (duda 6).
  - Pasos:
    - servicio, con duración y precio;
    - profesional, con "cualquier profesional" (RN-56);
    - fecha, con `min` y `max` de `FechasReservables` (RN-55);
    - huecos devueltos por `ConsultarDisponibilidad`, ordenados (RN-59), cada uno con su profesional (4a).
  - 1a: sin servicios, "el establecimiento no admite reservas en este momento".
  - 3a: sin profesionales, "el servicio no está disponible".
  - 7a: sin huecos, se indica sin dar el motivo y el selector de fecha sigue activo.
  - Las llamadas son secuenciales: ni `Task.WhenAll` ni `async void` (AGENTS §13, R-1).
  - Ningún `MarkupString`. CSS aislado, sin estilos en línea.
- **Hecho cuando:** la verificación manual de §6 está marcada para la selección.
- **Verificación:** manual (§6).

### T5 — Página `/book/{slug}`: confirmación (≈ 1 h)

- **Ficheros:** modifica `FlujoReserva.razor` y `FlujoReserva.razor.css`.
- **Implementa:** RN-70, RN-80, RN-81, RN-83; CU-07 pasos 1 a 4 y 12, alternativos 1a, 2a, 2b, 4a, 6a, 8a y 10a; SPEC §13 capa 1.
- **Contenido:**
  - **Parámetros de consulta** `service`, `staff`, `date` y `start` (duda 5). Si llegan, la página restaura la selección. Un hueco que ya no se ofrece se descarta sin error y el usuario vuelve a la lista de huecos.
  - **Sin sesión:** al pulsar "Continuar", navega a `login?returnUrl=` con la URL de `/book/{slug}` y la selección en la consulta (CU-07 1 y 1a). El registro conserva el `ReturnUrl` que ya pasa `Login.razor`.
  - **Con sesión (duda 8):** resumen con establecimiento, servicio, profesional, fecha, hora local, duración, precio, observaciones, y nombre y teléfono del perfil (CU-07 paso 2).
  - **Teléfono:** si el perfil no lo tiene, un campo obligatorio. Al confirmar, primero `EstablecerTelefono` y después `CrearReserva`, en secuencia (duda 4). Un teléfono inválido no deja confirmar (2b).
  - **Doble envío:** botón deshabilitado al primer clic, con un testigo de idempotencia en el estado del componente (paso 4, 4a).
  - **`HuecoYaNoDisponible`:** "Esa hora ya no está disponible", y se recargan los huecos del día (8a, 10a, RN-80). Otro `Fallo` muestra su mensaje (6a).
  - **Éxito:** pantalla de reserva creada con un enlace a `/my-account/bookings/{id}` (paso 12, duda 7). No espera al correo (RN-142).
- **Hecho cuando:** la verificación manual de §6 está marcada para la confirmación.
- **Verificación:** manual (§6).

### T6 — Sabotajes, verificación manual y documentación (≈ 0,5 h)

- **Ficheros:** `docs/casos-uso.md`, `docs/arquitectura.md` §2.3 y §4.2, `docs/modelo-dominio.md` l.269 (la firma recibe `Telefono`) y este fichero.
- **Implementa:** AGENTS §11 bis (sabotajes).
- **Hecho cuando:**
  - los tres sabotajes de §6 han fallado su prueba y el código está restaurado;
  - la verificación manual está marcada;
  - los documentos están corregidos.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 5,5 h de 5,5 h presupuestadas.**

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `EstablecerTelefono` guarda el teléfono y queda en el perfil al releer | I | `Identidad/PruebasUsuario.cs` | RN-81 | T1 |
| Establecer un teléfono a quien ya lo tiene: comportamiento fijado (lanza `DominioException`) | I | `Identidad/PruebasUsuario.cs` | RN-81 | T1 |
| `EstablecerTelefonoAsync` persiste y el teléfono **no** está en claro en `asp_net_users` | I | `Identidad/PruebasServicioCuentas.cs` | RN-81, RN-162 | T1 |
| Caso normal: guarda el teléfono del usuario de la sesión y `ObtenerContactoAsync` lo devuelve | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | RN-81 | T1 |
| Sin sesión: `NoAutorizado` | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | RN-70 | T1 |
| Teléfono vacío, con letras, con 5 o con 16 dígitos: `Validacion`, y el perfil queda como estaba | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | RN-178, RN-82 | T1 |
| Después de establecerlo, `CrearReserva` copia ese teléfono en la reserva | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | RN-81, RN-78 | T1 |
| Si el usuario ya tiene teléfono: `Conflicto`, y el teléfono no cambia | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | RN-81 | T1 |
| Si el usuario de la sesión no existe: lanza `InvalidOperationException` (estado imposible) | I | `CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | AGENTS §8 bis | T1 |
| Datos para reservar de una empresa publicable: servicios activos con profesionales activos y asociados | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-24, RN-33, RN-56 | T2 |
| Una empresa inactiva, no publicable o con un slug inexistente o mal formado da `null` | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-16, RN-109 | T2 |
| `AntelacionMaximaDias` del servicio sobrescribe la de la empresa; sin la del servicio, hereda | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-23 | T2 |
| `ConvertirADto` de cada DTO nuevo | U | `Dtos/Empresas/PruebasDatosReservaDto.cs`, `Dtos/Servicios/PruebasServicioReservaDto.cs`, `Dtos/Profesionales/PruebasProfesionalReservaDto.cs` | — | T2 |
| `FechasReservables`: hoy local y hoy más la ventana; a las 23:30 UTC ya es el día siguiente en Madrid; ventana de 1 y de 365 días | U | `Disponibilidad/PruebasMotorDisponibilidad.cs` | RN-55, RN-102 | T3 |
| `ConsultarDisponibilidad` devuelve los mismos huecos que el motor | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-60 | T3 |
| Fecha fuera de la ventana: lista vacía | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-55 (6a) | T3 |
| "Cualquier profesional" agrega los huecos de todos, y cada hueco conserva su profesional | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-59 (4a) | T3 |
| Un servicio de otra empresa o de una empresa inactiva: lista vacía | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-71 | T3 |
| Un servicio o una empresa inexistentes (también `Guid.Empty`): lista vacía | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-71 | T3 |
| `ALocal` rechaza un instante con `Kind` `Local` o `Unspecified` | U | `Disponibilidad/PruebasMotorDisponibilidad.cs` | RN-100, RN-102 | T3 |
| Sin contexto de empresa y sin sesión: funciona | I | `CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` | RN-04 | T3 |
| RN-70, RN-74, RN-76, RN-77, RN-78, RN-79, RN-80, RN-83 | — | `CasosUso/Reservas/PruebasCrearReserva.cs`, `Reservas/PruebasConcurrenciaReservas.cs` (Fase 11) | Ya cubiertas; no se duplican | — |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `ConsultarDisponibilidad` | Recurso de otra empresa como `NoEncontrado`; fallo a mitad; eventos | Es de solo lectura y pública (RN-04): no hay "otra empresa" respecto a una sesión, ni transacción, ni eventos. Un servicio ajeno da lista vacía, como el motor (SPEC §12.2 paso 1) |
| `ConsultarDisponibilidad` | Validación (`Validacion`) | El comando solo lleva identificadores y una fecha, sin texto que validar. Un identificador inexistente no es un error: el motor devuelve lista vacía (CU-03 7a, sin revelar el motivo), y así lo fija `Un_servicio_o_una_empresa_inexistentes_dan_lista_vacia` |
| `EstablecerTelefono` | Recurso de otra empresa; eventos | Actúa sobre el usuario de la sesión, sin empresa, y no registra eventos |
| `EstablecerTelefono` | Fallo a mitad sin dejar nada | Una sola escritura por `UserManager`, sin transacción propia |
| `DatosReservaDto` y sus hijos | Casos de objeto de valor | Son DTO: solo `ConvertirADto` |
| `FlujoReserva.razor` | Pruebas automáticas | No hay bUnit en el stack (AGENTS §4). Se cubre con la verificación manual |

**Verificación manual** (instancia propia en el puerto 5199, con datos desechables en la base de datos de desarrollo, que se borran al terminar):

Comprobado con `curl` sobre el HTML prerenderizado (2026-10-17), con la empresa desechable "Barberia Pruebafase14", que se borró después. **Lo que exige interactuar con el circuito queda pendiente de recorrerlo en un navegador:**

- [ ] Como visitante: servicio → profesional → fecha → huecos.
  - Comprobado en el HTML: el servicio con su duración y su precio (`18,50 €`), el selector con `min` igual a hoy y `max` igual a hoy más 30 (la ventana de la empresa), y los marcadores de `InteractiveServer`.
  - Pendiente: los clics en el navegador.
- [ ] "Cualquier profesional" muestra los huecos con el nombre de cada profesional. Lo cubre `PruebasConsultarDisponibilidad`; falta verlo en el navegador.
- [x] Un día sin huecos lo indica sin dar el motivo. Con un `date` fuera de la ventana sale "No hay horas libres ese día. Prueba con otra fecha."
- [x] `/book/<slug-inexistente>` y un slug mal formado dan un 404 real. El caso de una empresa no publicable lo cubre `Los_datos_para_reservar_de_una_empresa_inactiva_o_no_publicable_son_nulos`.
- [ ] Al continuar sin sesión, se llega al inicio de sesión y, al volver, la selección está intacta.
  - Comprobado:
    - `/book/{slug}?service=…&staff=…&date=…&start=…` restaura el servicio, el profesional, la fecha y el hueco, y muestra el resumen con "Continuar";
    - un `start` manipulado (07:10Z, que no es un hueco) se descarta;
    - `/login?returnUrl=book/…` responde 200.
  - Pendiente: el recorrido completo iniciando sesión de verdad.
- [ ] Lo mismo pasando por el registro. Pendiente en el navegador.
- [ ] Un cliente sin teléfono tiene que indicarlo, y uno inválido no deja confirmar; en la siguiente reserva ya no se le pide. Lo cubre `PruebasEstablecerTelefono`; falta verlo en el navegador.
- [ ] El doble clic en "Confirmar" crea una sola reserva. Pendiente en el navegador.
- [ ] Hueco tomado en otra pestaña entre la selección y la confirmación: sale "Esa hora ya no está disponible" y se recargan los huecos. Pendiente en el navegador.
- [ ] La pantalla de reserva creada enlaza a `/my-account/bookings/{id}`. Pendiente en el navegador.
- [x] `grep -rn MarkupString src/PlataformaReservas.Web/Components/Reserva` no da resultados.

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-16 | En `ObtenerDatosReservaAsync`, `db.Empresas` en lugar de `Visibles()` | `Una empresa inactiva, no publicable… da null` | Falló `Los_datos_para_reservar_de_una_empresa_inactiva_o_no_publicable_son_nulos` (1 de 25 en `PruebasConsultasCatalogoPublico`). Restaurado |
| RN-23 | En `DatosReservaDto.ConvertirADto`, donde vive la resolución: `empresa.Politicas` en lugar de `PoliticasReserva.Resolver(empresa, s)` | `AntelacionMaximaDias del servicio sobrescribe…` | Fallaron `PruebasDatosReservaDto.ConvertirADto_ordena_los_servicios_y_resuelve_la_ventana_efectiva_de_cada_uno` (unitaria) y `La_ventana_del_servicio_sobrescribe_la_de_la_empresa_y_sin_ella_se_hereda` (integración). Restaurado |
| RN-102 | En `FechasReservables`, `DateOnly.FromDateTime(ahoraUtc)` sin convertir a `Europe/Madrid` | `FechasReservables… a las 23:30 UTC ya es el día siguiente` | Fallaron los casos 23:30Z y 31-dic 23:00Z de `FechasReservables_va_de_hoy_en_madrid_a_hoy_mas_la_ventana`, y `Caso11_hoy_es_la_fecha_local_de_madrid_y_no_la_fecha_utc` (el motor usa el ayudante). Restaurado |

## 7. Criterios de aceptación

- [x] CU-03: los huecos que ve el visitante son los del motor (horario, duración, excepciones y reservas), con "cualquier profesional" y cada hueco con su profesional.
- [x] CU-03: el selector de fecha no permite salir de la ventana efectiva, y una fecha manipulada da lista vacía.
- [x] CU-03: una empresa inactiva o no publicable da 404 en `/book/{slug}`.
- [ ] CU-07: no se puede reservar sin autenticación, y al volver de iniciar sesión o de registrarse la selección sigue intacta. El servidor lo cubre `PruebasCrearReserva`, y la restauración por la consulta se comprobó con `curl`. Pendiente en el navegador.
- [ ] CU-07: un usuario sin teléfono debe indicarlo antes de confirmar, y queda guardado en su perfil. El servidor lo cubre `PruebasEstablecerTelefono`. Pendiente en el navegador.
- [ ] CU-07: reservar un hueco ya ocupado devuelve un error controlado y recarga los huecos. El servidor lo cubren `PruebasCrearReserva` y `PruebasConcurrenciaReservas`. Pendiente en el navegador.
- [ ] CU-07: el doble clic no crea dos reservas. Pendiente en el navegador.
- [x] RN-60: el flujo usa el mismo `IMotorDisponibilidad` que `CrearReserva`, sin lógica de disponibilidad en la página.
- [x] RN-171: no hay `MarkupString` en `Components/Reserva`.
- [x] `InteractiveServer` solo en `/book/{slug}`. Sin estilos en línea ni recursos externos.
- [x] Sin paquetes nuevos ni migraciones.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

Esta fase no cierra ningún punto de control.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | `A second operation was started on this context` al encadenar `EstablecerTelefono`, `CrearReserva` y la recarga de huecos en el mismo circuito | Llamadas siempre secuenciales con `await`, sin `Task.WhenAll` ni `async void`. El botón deshabilitado evita una segunda operación mientras dura la primera |
| R-2 | Un `InicioUtc` con `Kind` distinto de `Utc` llega a `CrearReserva` desde la página | El inicio se toma del `HuecoDisponible` devuelto por el motor. Si viene de la consulta (`start`), solo se acepta si coincide con un hueco recargado. La página nunca construye instantes |
| R-4 | Bucles de redirección o pérdida del `ReturnUrl` al pasar por el inicio de sesión o el registro | Se usa el `ReturnUrl` que ya existe en `Login.razor` y `Register.razor`; no se reimplementan las páginas de cuenta |
| R-10 | Escribir el teléfono con LINQ sobre `Users` | Se escribe por `UserManager.UpdateAsync`. La prueba comprueba que no está en claro |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `/my-account/bookings` y `/my-account/bookings/{id}` (destino del enlace de reserva creada) | Fase 15 |
| Edición del perfil (`ActualizarPerfil`) | Fase 15 |
| Correos de confirmación al cliente y de aviso al negocio (RN-146, RN-147) | Fase 16b |
| Limitación de frecuencia, cabeceras y CSP (RN-173, RN-175) | Fase 19 |
| Concurrencia y restricción `EXCLUDE` (RN-77) | Fase 11, ya hecha |
| Reserva como invitado | No existe (D-02) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-04 | T2, T3 | Datos para reservar sin contexto; `ConsultarDisponibilidad` sin contexto ni sesión |
| RN-16, RN-109 | T2, T4 | Inactiva o no publicable da `null`; sabotaje RN-16; 404 manual |
| RN-23 | T2 | Ventana efectiva del servicio frente a la de la empresa; sabotaje RN-23 |
| RN-24, RN-33, RN-56 | T2, T4 | Datos para reservar de una empresa publicable |
| RN-50 a RN-54, RN-57, RN-58 | T3 | Motor de la Fase 10 (13 pruebas), consumido sin cambios |
| RN-55 | T3, T4 | `FechasReservables`; fecha fuera de la ventana |
| RN-59 | T3, T4 | "Cualquier profesional" |
| RN-60 | T3 | `ConsultarDisponibilidad` devuelve los mismos huecos que el motor |
| RN-70 | T1, T5 | `EstablecerTelefono` sin sesión; `PruebasCrearReserva` (Fase 11); retorno tras iniciar sesión (manual) |
| RN-71 a RN-80, RN-83 | T5 | `PruebasCrearReserva` y `PruebasConcurrenciaReservas` (Fase 11); hueco tomado y doble clic (manual) |
| RN-81 | T1, T5 | `PruebasUsuario`, `PruebasServicioCuentas`, `PruebasEstablecerTelefono`; manual |
| RN-82, RN-178 | T1 | Teléfono inválido da `Validacion` |
| RN-100 a RN-102 | T3 | `FechasReservables` en hora de Madrid; sabotaje RN-102 |
| RN-140 a RN-142 | T5 | `PruebasCrearReserva` (Fases 11 y 11b) |
| RN-162, RN-167 | T1 | Teléfono no en claro en `asp_net_users` |
| RN-171 | T4, T5 | `grep MarkupString` |
| CU-03 | T2, T3, T4 | Todas las de T2 y T3; manual |
| CU-07 | T1, T5 | Las de T1; `PruebasCrearReserva`; manual |

## 11. Dudas

### Duda 1 — Nombre y forma de los datos para reservar por slug

- **Pregunta:** la página necesita el `Id` de la empresa, de los servicios y de los profesionales para llamar al motor y a `CrearReserva`, y `FichaEmpresaDto` no los lleva. ¿De dónde salen?
- **Opciones:**
  - (a) un método nuevo, `ObtenerDatosReservaAsync(slug)`, en `IConsultasCatalogoPublico`, que devuelve `DatosReservaDto`, con `ServicioReservaDto` y `ProfesionalReservaDto` dentro;
  - (b) ampliar `FichaEmpresaDto` con identificadores;
  - (c) reutilizar `IConsultasDisponibilidad` resolviendo antes el `empresaId` por el slug.
- **Recomendación y motivo:** (a). La ficha SSR no expone identificadores internos, y el filtro de activa y publicable (RN-109) sigue en el puerto del catálogo público, sin contexto (RN-04).
- **Bloquea:** T2, T4, T5.
- **Resolución:** opción (a), la recomendada.
- **Modificada en la implementación** (decisión del desarrollador, 2026-10-17):
  - En lugar de ampliar `IConsultasCatalogoPublico`, se crea un puerto separado, `IConsultasReservaPublica`, con el mismo método, para no tocar el doble `CatalogoPublicoFalso.cs` de la Fase 13.
  - Lo implementa la misma clase `ConsultasCatalogoPublico`, así que `Visibles()` y `SlugExacto` siguen existiendo una sola vez (A-09).
  - Obliga a registrarlo en `Infraestructura/InyeccionDependencias.cs`.

### Duda 2 — ¿CU-03 tiene caso de uso propio?

- **Pregunta:** ¿la página llama al motor directamente o a través de un caso de uso? ¿Cómo se llaman la página y el caso de uso?
- **Opciones:**
  - (a) un caso de uso `ConsultarDisponibilidad` con `ConsultarDisponibilidadComando`, que devuelve `Resultado<IReadOnlyList<HuecoDisponible>>`; la página es `Components/Reserva/FlujoReserva.razor`;
  - (b) la página inyecta `IMotorDisponibilidad` y lo llama directamente.
- **Recomendación y motivo:** (a). Mantiene la correspondencia uno a uno entre caso de uso y CU (`arquitectura.md` §5.1) y la forma de `Resultado`, y deja la página sin lógica (AGENTS §6).
- **Bloquea:** T3, T4.
- **Resolución:** opción (a), la recomendada.

### Duda 3 — Rango de fechas del selector

- **Pregunta:** CU-03 paso 5 pide un selector limitado a la ventana efectiva (RN-55). "Hoy" es una fecha local de Madrid, y RN-102 obliga a que la conversión ocurra en un único punto, dentro del motor. ¿Dónde se calcula el rango?
- **Opciones:**
  - (a) un ayudante público y estático, `MotorDisponibilidad.FechasReservables(ahoraUtc, antelacionMaximaDias)`, que el motor reutiliza en `SuperaLaVentana`;
  - (b) un método nuevo en `IMotorDisponibilidad`, que cambia el contrato de SPEC §12.1;
  - (c) un selector sin límite superior, confiando en que el motor devuelva una lista vacía, lo que incumple el paso 5.
- **Recomendación y motivo:** (a). No toca el contrato y la conversión sigue en un solo sitio.
- **Bloquea:** T3, T4.
- **Resolución:** opción (a), la recomendada.

### Duda 4 — Dónde se guarda el teléfono

- **Pregunta:** RN-81 y CU-07 2a piden que el teléfono que falta se guarde en el perfil al confirmar. ¿Lo guarda `CrearReserva` o una operación aparte?
- **Opciones:**
  - (a) un caso de uso propio, `EstablecerTelefono` (`CasosUso/Cuenta/`, nombre de `modelo-dominio.md` l.269), que la página llama justo antes de `CrearReserva`, sin cambiar `CrearReserva`;
  - (b) `CrearReservaComando` recibe un `Telefono` opcional y `CrearReserva` lo guarda en la misma transacción.
- **Recomendación y motivo:** (a). No reabre `CrearReserva`, que ya está cerrado y probado en la Fase 11, y cuya copia sale del perfil (Fase 11, duda sobre RN-78). Además, el teléfono se queda guardado aunque el hueco ya no esté libre, que es lo que pide "no vuelva a pedírselo".
- **Bloquea:** T1, T5.
- **Resolución:** opción (a), la recomendada.

### Duda 5 — Cómo se retoma el flujo tras iniciar sesión

- **Pregunta:** RN-70 y CU-07 1a exigen volver "en el mismo punto". El inicio de sesión es SSR y recarga la página, así que el estado del circuito se pierde. ¿Dónde se guarda la selección?
- **Opciones:**
  - (a) en la consulta del `returnUrl`: `/book/{slug}?service=…&staff=…&date=…&start=…` (rutas en inglés, SPEC §3.3);
  - (b) en `ProtectedSessionStorage`;
  - (c) volver al principio del flujo, lo que incumple 1a.
- **Recomendación y motivo:** (a). No guarda estado en el navegador, funciona también al pasar por el registro y lo resuelve el `ReturnUrl` que ya existe. El servidor no se fía del `start`: solo lo acepta si coincide con un hueco recargado.
- **Bloquea:** T5.
- **Resolución:** opción (a), la recomendada. Los parámetros son `service`, `staff`, `date` y `start`.

### Duda 6 — Empresa no visible en `/book/{slug}`

- **Pregunta:** ¿qué ve el usuario si la empresa no existe, está inactiva o no es publicable?
- **Opciones:**
  - (a) un 404 real con `NavigationManager.NotFound()`, como la ficha (RN-116);
  - (b) un mensaje dentro de la página.
- **Recomendación y motivo:** (a). Es coherente con la ficha y no revela si la empresa existe.
- **Bloquea:** T4.
- **Resolución:** opción (a), la recomendada.

### Duda 7 — Pantalla de reserva creada

- **Pregunta:** CU-07 paso 12 pide un enlace al detalle, pero `/my-account/bookings/{id}` es de la Fase 15.
- **Opciones:**
  - (a) enlazar ya a `/my-account/bookings/{id}`, aunque dé 404 hasta la Fase 15;
  - (b) mostrar el resumen sin enlace, y añadirlo en la Fase 15.
- **Recomendación y motivo:** (a). Cumple el paso 12 tal como está escrito, y la Fase 15 no tendrá que tocar `/book`.
- **Bloquea:** T5.
- **Resolución:** opción (a), la recomendada.

### Duda 8 — Quién puede confirmar

- **Pregunta:** RN-70 dice "usuario autenticado" y CU-07 nombra al actor Cliente. `casos-uso.md` l.20 dice que los roles son acumulativos, pero un propietario dado de alta por `/for-business/register` no tiene el rol `Cliente`. ¿Hace falta el rol?
- **Opciones:**
  - (a) basta con estar autenticado, como hace hoy `CrearReserva`;
  - (b) se exige `PoliticaCliente`, y un propietario sin ese rol no podría reservar.
- **Recomendación y motivo:** (a). Respeta RN-70 literalmente y "el propietario de una peluquería puede reservar en un fisioterapeuta con la misma cuenta" (`casos-uso.md` l.20). Cómo verá ese propietario sus reservas en `/my-account` se decide en la Fase 15.
- **Bloquea:** T5.
- **Resolución:** opción (a), la recomendada.

### Decisiones de implementación

- **Puerto separado `IConsultasReservaPublica`** (la duda 1, modificada; ver arriba). La tabla de familias de acceso a datos de `AGENTS.md` §8 sigue nombrando solo `IConsultasCatalogoPublico` e `IConsultasDisponibilidad`. No se ha tocado porque `AGENTS.md` no está en el radio de cambio.
- **`MotorDisponibilidad.ALocal(instanteUtc)`** se añade junto a `FechasReservables` para que la página muestre la hora local de los huecos sin hacer otra conversión de zona horaria (RN-102).
  - `SuperaLaVentana` reutiliza `FechasReservables`.
  - El resultado es el mismo que antes, porque la medianoche de `Europe/Madrid` siempre existe.
  - Lo vigilan las 13 pruebas del motor y el sabotaje de RN-102.
- **`Usuario.EstablecerTelefono` lanza un error si el usuario ya tiene teléfono.** Solo sirve para completar un perfil que no lo tiene (RN-81); cambiarlo es cosa de `ActualizarPerfil`, en la Fase 15. La página solo llama a `EstablecerTelefono` cuando el perfil no tiene teléfono.
- **La página lee el nombre y el teléfono con `IServicioCuentas.ObtenerContactoAsync`**, un puerto de lectura, igual que la ficha lee `IConsultasCatalogoPublico`. La página no tiene reglas de negocio: solo decide si muestra el campo del teléfono.
- **`staff` lleva el profesional del hueco elegido, no el filtro.** Con "cualquier profesional" puede haber dos huecos a la misma hora, y el profesional es lo que los distingue. Al volver, el selector muestra ese profesional.
- **Correcciones de `/revisar 14`** (2026-10-17, todas las importantes y menores, por orden del desarrollador):
  - **Hallazgo 1.** `EstablecerTelefono` convierte la `DominioException` de "ya tiene teléfono" en `Fallo(Conflicto)`, porque es un desenlace previsto: dos pestañas sobre un perfil sin teléfono. La página, ante `Conflicto`, sigue adelante con el teléfono que ya tiene el perfil. Un usuario de la sesión inexistente sigue lanzando `InvalidOperationException`, porque es un estado imposible, y queda fijado con una prueba. Comprobado con un sabotaje: si se quita la traducción, falla `Si_el_usuario_ya_tiene_telefono_da_conflicto_y_no_lo_cambia`.
  - **Hallazgo 2.** Hay prueba de servicio y empresa inexistentes, y la omisión de la validación queda anotada en §6.
  - **Hallazgo 4.** La excepción de `EstablecerTelefonoAsync` lleva solo los `Code` de Identity, no sus `Description`, que pueden contener el correo (RN-172).
  - **Hallazgo 5.** Después de guardar el teléfono, la página vuelve a leer el contacto con `ObtenerContactoAsync` en vez de normalizarlo ella misma.
  - **Hallazgo 6.** `ALocal` rechaza con `ArgumentException` un instante que no está en UTC (AGENTS §9).
  - **Hallazgo 7.** El comparador `es-ES` vive una sola vez, en `Aplicacion/Compartido/OrdenEspanol.cs`, y lo usan `FichaEmpresaDto`, `DatosReservaDto` y `ServicioReservaDto`. Tocar `FichaEmpresaDto`, que es de la Fase 13, se anota en §4.
  - **Hallazgo 3** (criterios de CU-07 sin evidencia de pantalla): no se puede corregir desde aquí. Necesita que el desarrollador recorra el flujo en un navegador.
- **`start` es el instante UTC en formato ISO (`o`).** Solo se acepta si coincide con un hueco recargado del motor; si no, se descarta sin dar error (R-2).

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo en las fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado: no aplica, esta fase no cierra ninguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas: ninguna es de esta fase
