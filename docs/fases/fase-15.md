# Fase 15 — Área de cliente, perfil y baja de cuenta

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4,5 h (SPEC §19) |
| **Semana** | 5 (`PROJECT_PLAN.md` §2), junto a las fases 16 y 16b |
| **Cierra** | Ninguno. Contribuye al Control 4: "cliente y empresa gestionan la reserva" (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-18 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Existen las tres vistas del área de cliente, en `InteractiveServer` y filtradas siempre por el `UsuarioId` de la sesión, en la propia consulta:

- `/my-account/bookings`: las reservas propias, en dos pestañas (próximas e históricas).
- `/my-account/bookings/{id}`: el detalle de una reserva, con su cancelación.
- `/my-account/profile`: editar el nombre y el teléfono, y darse de baja. La baja cancela en cascada las reservas futuras.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §11 (l.640-646) | "Área de cliente — InteractiveServer, requiere rol `Cliente`": `/my-account/bookings` (próximas e históricas, en pestañas), `/my-account/bookings/{id}` (detalle y cancelación) y `/my-account/profile` (perfil y baja de cuenta) |
| SPEC §10.3 (l.600) | "`/my-account/*` no filtra por empresa: filtra por `UsuarioId`, que es un eje distinto. Un cliente ve sus reservas en todas las empresas" |
| SPEC §6.1 (l.242) | La pantalla de perfil vuelve al alcance porque en ella vive la baja de cuenta (CU-22) |
| SPEC §9.1, §9.10, §9.11 | `Usuario.Activo`, las copias de la reserva y sus estados |
| D-01 | `InteractiveServer` en el área de cliente |
| A-06 (`arquitectura.md` §8.4) | "`DarDeBajaCuenta` pone `Activo = false` **y llama a `UserManager.UpdateSecurityStampAsync`**"; el revalidador expulsa el circuito |
| `arquitectura.md` §2.3, §9.4 (l.763) | Los nombres `ActualizarPerfil` y `DarDeBajaCuenta` en `CasosUso/Cuenta/` y la carpeta `Components/MiCuenta/`. `DarDeBajaCuenta` es una de las tres transacciones que abarcan varios agregados |
| AGENTS §8 (tercera familia) | El eje cliente filtra por `UsuarioId` en origen, y su implementación recibe `IContextoUsuario` |
| `modelo-dominio.md` §4.1 (l.268-270) | `ActualizarPerfil(nombreCompleto, telefono?)`, `DarDeBaja()` |
| `modelo-dominio.md` §10bis (l.846) | `CuentaDadaDeBaja` "se registrará en la fase 15, porque `Usuario` no hereda de `EntidadBase`" |
| RN-03, RN-05, RN-95 | Una reserva ajena es "no encontrado"; el cliente solo ve y cancela las suyas |
| RN-78 | Las copias de la reserva no cambian al editar el perfil |
| RN-81 | Teléfono opcional en el perfil y obligatorio al reservar |
| RN-91 a RN-93, RN-97, RN-98 | Transiciones de estado, cancelación del cliente mientras `InicioUtc > ahora`, hueco liberado y `FechaCancelacion` |
| RN-135 ⚑ | Un usuario inactivo no puede iniciar sesión, y la sesión que tenía abierta deja de valer |
| RN-136 | La baja pone `Activo = false`; no borra nada |
| RN-137 ⚑ | La baja cancela en la misma transacción todas las reservas confirmadas con `InicioUtc > ahora` |
| RN-138 | Un propietario con empresa activa no puede darse de baja; se le enlaza a `/app/settings` |
| RN-139 | La baja es una desactivación; reactivar no está en el MVP |
| RN-140 a RN-142 | Los eventos se encolan después del commit |
| RN-151, RN-155 | Los eventos de la baja (los correos son de la Fase 16b) |
| RN-162, RN-167 | Los datos del usuario van cifrados; se leen y escriben por `UserManager` |
| RN-171 ⚑ | Nada de `MarkupString` |
| CU-08, CU-09 | Listado y detalle (`casos-uso.md` l.566-567) |
| CU-10 | Cancelar reserva desde `/my-account/bookings/{id}` (§3.4) |
| CU-22 | Gestionar perfil y darse de baja (§3.7) |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `CancelarReservaCliente` (CU-10, servidor) | ✔ comprobado (`Aplicacion/CasosUso/Reservas/`) |
| `IRepositorioReservasCliente` y su implementación, que filtra por `UsuarioId` en la consulta | ✔ comprobado |
| `Reserva.CancelarPorCliente(ahora)` y `CancelarPorEmpresa(ahora, origen)`, que rechaza `BajaCuenta` | ✔ comprobado |
| `OrigenCancelacion.BajaCuenta` y el evento `CuentaDadaDeBaja` | ✔ comprobado (Fase 11b, duda 7) |
| `ConfirmacionUsuarioActivo` (RN-135 al iniciar sesión) y `RevalidadorIdentidad` (5 minutos) | ✔ comprobado (Fase 6) |
| `SecurityStampValidatorOptions` configurado | ✔ comprobado (`Infraestructura/InyeccionDependencias.cs`) |
| Endpoint POST `Account/Logout`, que usa `MainLayout` | ✔ comprobado |
| `IServicioCuentas` (`ObtenerContactoAsync`, `EstablecerTelefonoAsync`, `RenovarSelloMiembrosAsync`) | ✔ comprobado |
| `IRepositorioMiembros.ObtenerPorUsuarioAsync` e `IConsultasDisponibilidad.ObtenerEmpresaAsync` (que devuelve `Activa`) | ✔ comprobado |
| `PoliticaCliente` (rol `Cliente`) | ✔ comprobado. Los propietarios no tienen ese rol (duda 6) |
| `Usuario.ActualizarPerfil`, `Usuario.DarDeBaja`, los casos de uso `ActualizarPerfil` y `DarDeBajaCuenta`, la consulta del cliente y las páginas `/my-account/*` | ✘ ausente. **Se crean en esta fase** |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Entidades/Reserva.cs`: `CancelarPorCliente` recibe `OrigenCancelacion` (duda 3). Tras `/revisar 15` (hallazgos 4 y 5): `EsCancelablePorCliente`, la regla única de "cancelable por el cliente" |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IConsultasReservasCliente.cs` (nuevo, duda 1). `Abstracciones/IServicioCuentas.cs` (`ActualizarPerfilAsync`, `DarDeBajaAsync`, duda 4; `EstaActivaAsync`, hallazgo 3 de `/revisar 15`). `Dtos/Reservas/ReservaClienteDto.cs`, `DetalleReservaClienteDto.cs` y `MisReservasDto.cs` (dudas 1 y 2). `CasosUso/Reservas/ConsultarMisReservas.cs`, `ConsultarDetalleReserva.cs` y su comando. `CasosUso/Reservas/CancelarReservaCliente.cs` (pasa el origen `Cliente`, duda 3). `CasosUso/Cuenta/ActualizarPerfil.cs`, `DarDeBajaCuenta.cs` y sus comandos. `InyeccionDependencias.cs`. Tras `/revisar 15` (hallazgo 6): `CasosUso/Cuenta/ConsultarPerfil.cs` y `Dtos/Cuenta/PerfilDto.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Identidad/Usuario.cs` (`ActualizarPerfil`, `DarDeBaja`). `Identidad/ServicioCuentas.cs` (los dos métodos nuevos, y el rol `Cliente` en `CrearPropietarioAsync`, duda 6). `Repositorios/RepositorioReservasCliente.cs` (las reservas futuras confirmadas del usuario, para modificarlas). `Consultas/ConsultasReservasCliente.cs` (nuevo). `InyeccionDependencias.cs`. `Persistencia/Migraciones/`: **una migración solo de datos**, que da el rol `Cliente` a los propietarios existentes (duda 6) |
| `src/PlataformaReservas.Web` | sí | `Components/MiCuenta/` (`arquitectura.md` §2.3): `_Imports.razor` con `PoliticaCliente`, `MisReservas.razor`, `DetalleReserva.razor`, `Perfil.razor` y sus `.razor.css`. **Excepción al radio, autorizada por el desarrollador el 2026-10-18:** `Components/Layout/MainLayout.razor`, para los enlaces "Mis reservas" y "Mi perfil" del menú (visibles con `EsCliente`). **Excepción al radio, autorizada por el desarrollador el 2026-10-18 al corregir `/revisar 15` (hallazgo 6):** `Components/Reserva/FlujoReserva.razor` lee el perfil con `ConsultarPerfil` en lugar de `IServicioCuentas` |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Entidades/PruebasReserva.cs`. `Dtos/Reservas/` (una prueba por DTO nuevo). `Dtos/Cuenta/PruebasPerfilDto.cs` (hallazgo 6) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Consultas/PruebasConsultasReservasCliente.cs` (nuevo). `CasosUso/Reservas/PruebasConsultarMisReservas.cs`, `PruebasConsultarDetalleReserva.cs` (nuevos) y `PruebasCancelarReservaCliente.cs`. `CasosUso/Cuenta/PruebasActualizarPerfil.cs` y `PruebasDarDeBajaCuenta.cs` (nuevos). `Identidad/PruebasUsuario.cs`, `PruebasServicioCuentas.cs`. `Repositorios/PruebasRepositorioReservasCliente.cs`. `PruebasInyeccionDependencias.cs`, solo si la resolución de los servicios nuevos lo exige. `CasosUso/Cuenta/PruebasConsultarPerfil.cs` (hallazgo 6) |
| `.claude/skills/revisar/comprobaciones.sh` | sí, excepción | **Excepción al radio, autorizada por el desarrollador el 2026-10-18 al corregir `/revisar 15` (hallazgo 1):** C13 busca LINQ sobre `Users` solo en `src/`. La única coincidencia en `tests/` era la prueba de RN-167, que hace esa consulta a propósito |
| `docs/` | sí | `casos-uso.md` (marcar los criterios de CU-08, CU-09, CU-10 y CU-22). `arquitectura.md` §2.3 y §8.3 (rol `Cliente` de los propietarios). `modelo-dominio.md` §4.1 y §4.10 (firmas con objetos de valor y el origen de `CancelarPorCliente`). `fase-09.md` l.300-301 (duda 9). Este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- el inicio de sesión, `ConfirmacionUsuarioActivo` ni `RevalidadorIdentidad`, que ya cubren RN-135;
- `Web/Cuenta/` ni `Program.cs`;
- los manejadores de correo (Fase 16b);
- `/app/*` (Fase 16);
- los pendientes de la Fase 14 (duda 10);
- ningún paquete.

## 5. Tareas

### T1 — Consulta de mis reservas (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IConsultasReservasCliente.cs`;
  - crea `Aplicacion/Dtos/Reservas/ReservaClienteDto.cs`, `DetalleReservaClienteDto.cs` y `MisReservasDto.cs`;
  - crea `Aplicacion/CasosUso/Reservas/ConsultarMisReservas.cs`, `ConsultarDetalleReserva.cs` y `ConsultarDetalleReservaComando.cs`;
  - crea `Infraestructura/Consultas/ConsultasReservasCliente.cs`;
  - modifica los dos `InyeccionDependencias.cs`;
  - pruebas: `Consultas/PruebasConsultasReservasCliente.cs`, `CasosUso/Reservas/PruebasConsultarMisReservas.cs`, `PruebasConsultarDetalleReserva.cs` y las unitarias de los DTO.
- **Implementa:** CU-08, CU-09, RN-03, RN-05, RN-95, AGENTS §8 (tercera familia).
- **Contenido:**
  - **El puerto.** `IConsultasReservasCliente` no nombra `IContextoUsuario`. Su implementación lo recibe y filtra por `UsuarioId` **en la consulta**, sin seguimiento. Tiene dos operaciones:
    - `ObtenerMisReservasAsync` devuelve un `MisReservasDto` con las dos listas (duda 2): próximas (`InicioUtc >= ahora`, en orden ascendente) e históricas (el resto, en orden descendente);
    - `ObtenerDetalleAsync(id)` devuelve un `DetalleReservaClienteDto`, o `null` si la reserva es ajena o no existe.
  - **Los datos.** Los DTO llevan establecimiento, servicio, profesional, fecha y hora, estado, `PrecioAplicado` y `DuracionAplicada`, y el detalle añade además las observaciones. Los nombres se proyectan en SQL. Si hay que proyectar sin cargar la entidad entera, se usa la sobrecarga de `ConvertirADto` con los valores proyectados (AGENTS §8 bis), sin descifrar más de lo que se muestra (D-22).
  - **El instante actual.** `ahora` sale de `IRelojSistema`, en el caso de uso.
  - **Los casos de uso.** `ConsultarMisReservas` y `ConsultarDetalleReserva`: sin sesión, `NoAutorizado`. Una reserva ajena o inexistente da `NoEncontrado` (RN-03).
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Páginas de mis reservas y rol `Cliente` de los propietarios (≈ 1 h)

- **Ficheros:**
  - crea `Web/Components/MiCuenta/_Imports.razor`, `MisReservas.razor`, `DetalleReserva.razor` y sus `.razor.css`;
  - modifica `Infraestructura/Identidad/ServicioCuentas.cs` (`CrearPropietarioAsync` da también el rol `Cliente`);
  - crea la migración de datos en `Infraestructura/Persistencia/Migraciones/`;
  - pruebas en `Identidad/PruebasServicioCuentas.cs`.
- **Implementa:** CU-08, CU-09, CU-10 (pantalla, alternativos 1a, 2a y 3a), RN-93, D-01, SPEC §11 l.640, `casos-uso.md` l.20.
- **Contenido:**
  - `@page "/my-account/bookings"` y `@page "/my-account/bookings/{Id:guid}"`, con `InteractiveServer`, protegidas en `_Imports.razor` con `PoliticaCliente`, como `App/_Imports.razor` hace con `EsPropietario`.
  - **Listado:** dos pestañas, con establecimiento, servicio, profesional, fecha y hora (local, con `MotorDisponibilidad.ALocal`) y estado. Cada fila enlaza a su detalle.
  - **Detalle:**
    - muestra todos los datos y `PrecioAplicado`;
    - si `NoEncontrado`, `NavigationManager.NotFound()`;
    - "Cancelar reserva" solo aparece si la reserva está `Confirmada` y empieza en el futuro, con una confirmación explícita (CU-10 paso 4), y llama a `CancelarReservaCliente`;
    - 2a y 3a: explica por qué no se puede cancelar y, en 3a, que hay que contactar con el establecimiento.
  - Llamadas secuenciales y botón deshabilitado durante la operación (R-1). Ningún `MarkupString`.
  - **Rol `Cliente` de los propietarios** (duda 6): `CrearPropietarioAsync` añade el rol `Cliente` además de `Propietario`. Una migración de solo datos (SQL escrito a mano, idempotente) da el rol `Cliente` a los usuarios con fila en `miembros_empresa` que todavía no lo tienen. Su `Down` no hace nada, porque no se puede saber qué roles había antes.
- **Hecho cuando:** la verificación manual de §6 está marcada para las dos páginas y la prueba del rol está en verde.
- **Verificación:** manual (§6) y `dotnet test`.

### T3 — Editar el perfil (≈ 0,5 h)

- **Ficheros:**
  - modifica `Infraestructura/Identidad/Usuario.cs`, `Aplicacion/Abstracciones/IServicioCuentas.cs` e `Infraestructura/Identidad/ServicioCuentas.cs`;
  - crea `Aplicacion/CasosUso/Cuenta/ActualizarPerfil.cs` y `ActualizarPerfilComando.cs`;
  - pruebas en `PruebasUsuario.cs`, `PruebasServicioCuentas.cs` y `CasosUso/Cuenta/PruebasActualizarPerfil.cs`.
- **Implementa:** CU-22 pasos 1 a 3, RN-78, RN-81, RN-162, RN-167.
- **Contenido:**
  - **Entidad.** `Usuario.ActualizarPerfil(NombrePersona, Telefono?)` recibe objetos de valor (AGENTS §7). Un teléfono nulo lo quita (duda 8).
  - **Puerto.** `IServicioCuentas.ActualizarPerfilAsync(Guid usuarioId, NombrePersona nombre, Telefono? telefono, CancellationToken ct)` escribe por `UserManager.UpdateAsync`.
  - **Caso de uso.** `ActualizarPerfil` devuelve:
    - `NoAutorizado` sin sesión;
    - `Validacion` si un objeto de valor lanza;
    - éxito en el resto de casos.
  - **Datos de sesión.** No hace falta renovar el sello: los claims de la sesión no llevan el nombre (`FabricaClaimsUsuario`).
- **Hecho cuando:** las pruebas de T3 de §6 están en verde, incluida la de RN-78.
- **Verificación:** `dotnet test`.

### T4 — Dar de baja la cuenta (≈ 1 h)

- **Ficheros:**
  - modifica `Dominio/Entidades/Reserva.cs` (duda 3) y `Aplicacion/CasosUso/Reservas/CancelarReservaCliente.cs` (pasa `OrigenCancelacion.Cliente`);
  - modifica `Infraestructura/Identidad/Usuario.cs` (`DarDeBaja`) y `IServicioCuentas`/`ServicioCuentas` (`DarDeBajaAsync`);
  - modifica `Dominio/Repositorios/IRepositorioReservasCliente.cs` e `Infraestructura/Repositorios/RepositorioReservasCliente.cs`, con un método nuevo que devuelve las reservas futuras confirmadas del usuario, con seguimiento;
  - crea `Aplicacion/CasosUso/Cuenta/DarDeBajaCuenta.cs`;
  - pruebas en `PruebasReserva.cs`, `PruebasUsuario.cs`, `PruebasServicioCuentas.cs`, `PruebasRepositorioReservasCliente.cs`, `PruebasCancelarReservaCliente.cs` y `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs`.
- **Implementa:** CU-22 pasos 4 a 9 y alternativos 5a, 8a y 9a; RN-135 ⚑, RN-136, RN-137 ⚑, RN-138, RN-139, RN-140, RN-151 y RN-155 (solo el evento); A-06.
- **Contenido:**
  - **Origen de la cancelación.** `Reserva.CancelarPorCliente(DateTime ahoraUtc, OrigenCancelacion origen)` solo admite `Cliente` o `BajaCuenta`, con las mismas comprobaciones de estado y hora (RN-91, RN-93), y registra `ReservaCancelada` con ese origen (duda 3).
  - **`DarDeBajaCuenta.EjecutarAsync(ct)`**, sin comando: el usuario sale del contexto (AGENTS §8 bis). Devuelve `Resultado<int>` con el número de reservas canceladas, como `DarDeBajaEmpresa`. Hace esto, en orden:
    1. sin sesión, `NoAutorizado`;
    2. RN-138 (duda 5): si el usuario es miembro de una empresa activa (`IRepositorioMiembros.ObtenerPorUsuarioAsync` más `IConsultasDisponibilidad.ObtenerEmpresaAsync`), `Fallo(Conflicto)` con el mensaje que remite a `/app/settings`;
    3. abre la transacción;
    4. `IServicioCuentas.DarDeBajaAsync(usuarioId)` pone `Activo = false` y renueva el sello (A-06). Si el usuario ya estaba de baja, `EstadoNoValido`;
    5. cancela con origen `BajaCuenta` todas las reservas confirmadas del usuario con `InicioUtc > ahora` (RN-137);
    6. guarda y confirma la transacción. Si algo falla antes, no queda nada aplicado (8a);
    7. publica, después del commit, `CuentaDadaDeBaja`, que crea el propio caso de uso (duda 4), y los `ReservaCancelada`, y limpia los eventos.
  - **Recuento previo.** El número de reservas que se cancelarán (paso 6 de CU-22) lo da `ConsultarMisReservas`, contando las próximas confirmadas. No hace falta otra operación.
- **Hecho cuando:** las pruebas de T4 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T5 — Página `/my-account/profile` (≈ 0,75 h)

- **Ficheros:** crea `Web/Components/MiCuenta/Perfil.razor` y su `.razor.css`.
- **Implementa:** CU-22 completo (pasos 1 a 10, alternativos 5a y 9a), RN-138, RN-139, D-01.
- **Contenido:**
  - **Perfil:** muestra el nombre, el correo (solo lectura) y el teléfono, y permite editar el nombre y el teléfono con `ActualizarPerfil`.
  - **Baja:** el botón "Dar de baja mi cuenta" muestra cuántas reservas futuras confirmadas se cancelarán, avisa de que no se puede revertir desde la aplicación (RN-139) y pide una confirmación explícita.
  - **Resultado de la baja:**
    - 5a (`Conflicto`): el mensaje con un enlace a `/app/settings`;
    - éxito: la confirmación, con el número de reservas canceladas y un formulario POST a `Account/Logout`, el mismo de `MainLayout` (duda 7). Además, el sello renovado expulsa el circuito como mucho a los 5 minutos (A-06).
  - Llamadas secuenciales, botones deshabilitados durante la operación y ningún `MarkupString`.
- **Hecho cuando:** la verificación manual de §6 está marcada para el perfil.
- **Verificación:** manual (§6).

### T6 — Sabotajes, verificación manual y documentación (≈ 0,25 h)

- **Ficheros:** `docs/casos-uso.md`, `docs/arquitectura.md` §2.3 y §8.3, `docs/modelo-dominio.md` §4.1 y §4.10, `docs/fases/fase-09.md` l.300-301 (duda 9) y este fichero.
- **Implementa:** AGENTS §11 bis (sabotajes).
- **Hecho cuando:**
  - los tres sabotajes de §6 han fallado su prueba y el código está restaurado;
  - la verificación manual está marcada;
  - los documentos están corregidos.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 4,5 h de 4,5 h presupuestadas.**

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Solo devuelve las reservas del usuario de la sesión, de todas sus empresas | I | `Consultas/PruebasConsultasReservasCliente.cs` | RN-05, SPEC §10.3 | T1 |
| Próximas en orden ascendente; históricas (pasadas, canceladas y completadas) en orden descendente; el límite `InicioUtc == ahora` cae en próximas | I | `Consultas/PruebasConsultasReservasCliente.cs` | CU-08 | T1 |
| El detalle de una reserva ajena o inexistente es `null` | I | `Consultas/PruebasConsultasReservasCliente.cs` | RN-03, RN-95 | T1 |
| El detalle lleva `PrecioAplicado`, aunque el servicio haya cambiado de precio | I | `Consultas/PruebasConsultasReservasCliente.cs` | RN-78, CU-09 | T1 |
| Las consultas no dejan entidades en seguimiento | I | `Consultas/PruebasConsultasReservasCliente.cs` | A-05 | T1 |
| Sin sesión: `NoAutorizado` | I | `CasosUso/Reservas/PruebasConsultarMisReservas.cs`, `PruebasConsultarDetalleReserva.cs` | RN-05 | T1 |
| Reserva ajena: `NoEncontrado` | I | `CasosUso/Reservas/PruebasConsultarDetalleReserva.cs` | RN-03 | T1 |
| `ConvertirADto` de cada DTO nuevo | U | `Dtos/Reservas/Pruebas<Dto>.cs` (uno por DTO) | — | T1 |
| Un propietario nuevo tiene los roles `Propietario` y `Cliente` | I | `Identidad/PruebasServicioCuentas.cs` | SPEC §11 l.640, `casos-uso.md` l.20 | T2 |
| `ActualizarPerfil` cambia el nombre y el teléfono; un teléfono nulo lo quita | I | `Identidad/PruebasUsuario.cs` | RN-81 | T3 |
| `ActualizarPerfilAsync` persiste, y el nombre y el teléfono no quedan en claro en `asp_net_users` | I | `Identidad/PruebasServicioCuentas.cs` | RN-162 | T3 |
| Caso normal; nombre vacío o teléfono inválido: `Validacion`, y el perfil no cambia; sin sesión: `NoAutorizado` | I | `CasosUso/Cuenta/PruebasActualizarPerfil.cs` | RN-178, CU-22 | T3 |
| Editar el perfil no altera el nombre ni el teléfono copiados en una reserva ya creada | I | `CasosUso/Cuenta/PruebasActualizarPerfil.cs` | RN-78 | T3 |
| `CancelarPorCliente` con `BajaCuenta` registra ese origen; con `Empresa` o `BajaEmpresa` lanza; con una reserva ya iniciada lanza | U | `Entidades/PruebasReserva.cs` | RN-91, RN-93, duda 3 | T4 |
| `CancelarReservaCliente` sigue registrando el origen `Cliente` | I | `CasosUso/Reservas/PruebasCancelarReservaCliente.cs` | RN-148 | T4 |
| `DarDeBaja` desactiva; repetirlo lanza | I | `Identidad/PruebasUsuario.cs` | RN-136 | T4 |
| `DarDeBajaAsync` desactiva y renueva el sello; con la cuenta ya inactiva lanza `DominioException` y no cambia el sello; con un usuario inexistente lanza. `EstaActivaAsync` distingue activa de inactiva y lanza con un usuario inexistente | I | `Identidad/PruebasServicioCuentas.cs` | A-06, RN-135, `arquitectura.md` §6 | T4 |
| Las reservas futuras confirmadas se devuelven solo para el usuario de la sesión y con seguimiento | I | `Repositorios/PruebasRepositorioReservasCliente.cs` | RN-05 | T4 |
| La baja cancela solo las futuras confirmadas; las pasadas, las completadas y las ya canceladas no cambian | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-137 | T4 |
| Tras la baja: `Activo = false`, el sello cambió y no puede iniciar sesión (`UsuariosPrueba.ComprobarClaveAsync`) | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-135, RN-136 | T4 |
| Un propietario con empresa activa: `Conflicto` y nada cambia; con la empresa ya dada de baja, puede darse de baja | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-138 | T4 |
| Después del commit se publican `CuentaDadaDeBaja` y un `ReservaCancelada(BajaCuenta)` por cada reserva cancelada; las entidades quedan sin eventos | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-140, RN-151, RN-155 | T4 |
| Un fallo a mitad no deja nada aplicado: el usuario sigue activo y las reservas siguen confirmadas | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | CU-22 8a | T4 |
| Sin sesión: `NoAutorizado`. Dar de baja dos veces: `EstadoNoValido` | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | CU-22 | T4 |
| Una cuenta ya inactiva da `EstadoNoValido` antes de abrir la transacción: ninguna reserva cambia, ni en la base de datos ni en el rastreador, el sello no cambia y no se publica nada | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | `arquitectura.md` §6 (hallazgo 3) | T4 |
| Si el usuario de la sesión no existe, lanza: es un estado imposible | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | AGENTS §11 bis (hallazgo 2) | T4 |
| `EsCancelablePorCliente`: solo `Confirmada` con `InicioUtc > ahora`; extremos a un tick, los tres estados y el cambio de día en UTC | U | `Entidades/PruebasReserva.cs` | RN-93, RN-137, A-09 (hallazgos 4 y 5) | T4 |
| `ReservaClienteDto.Cancelable` sigue la regla del dominio; `MisReservasDto.Cancelables` no cuenta una confirmada que empieza justo ahora, aunque esté en próximas | U | `Dtos/Reservas/PruebasReservaClienteDto.cs`, `PruebasMisReservasDto.cs` | RN-137 (hallazgo 4) | T1 |
| `Cancelables` cuenta las confirmadas futuras, igual que la baja; un usuario de la sesión sin reservas o inexistente ve las dos listas vacías | I | `CasosUso/Reservas/PruebasConsultarMisReservas.cs` | RN-137, AGENTS §11 bis (hallazgos 2 y 4) | T1 |
| Un identificador vacío da `NoEncontrado` | I | `CasosUso/Reservas/PruebasConsultarDetalleReserva.cs` | AGENTS §11 bis (hallazgo 2) | T1 |
| `ConsultarPerfil`: nombre y teléfono descifrados; sin teléfono, nulo; sin sesión, `NoAutorizado`; usuario inexistente, `NoEncontrado`. `PerfilDto.ConvertirADto` | I, U | `CasosUso/Cuenta/PruebasConsultarPerfil.cs`, `Dtos/Cuenta/PruebasPerfilDto.cs` | A-01 (hallazgo 6) | T5 |
| La reserva de otro cliente en la misma franja no se toca | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-05 | T4 |
| El hueco cancelado vuelve a ofrecerse | I | `CasosUso/Cuenta/PruebasDarDeBajaCuenta.cs` | RN-97 | T4 |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `ConsultarMisReservas`, `ConsultarDetalleReserva` | Recurso de otra **empresa**; conflicto; fallo a mitad; eventos | Son del eje cliente y de solo lectura: el aislamiento es por usuario, y se prueba como "reserva ajena". Sin transacción ni eventos |
| `ConsultarMisReservas` | Validación | No tiene comando: el usuario sale del claim. El recurso inexistente (usuario sin reservas o que no existe) sí se prueba: devuelve las dos listas vacías |
| `ConsultarDetalleReserva` | Validación de formato | El comando solo lleva un `Guid`, que no tiene formato que validar. `Guid.Empty` se trata como inexistente (`NoEncontrado`), y se prueba; ningún caso de uso del proyecto valida `Guid.Empty` aparte |
| `ActualizarPerfil` | Recurso inexistente o de otra empresa; eventos; fallo a mitad | Actúa sobre el usuario de la sesión, sin empresa. Una sola escritura por `UserManager`. Ningún documento define un evento para editar el perfil |
| `DarDeBajaCuenta` | Recurso de otra empresa | No tiene empresa: el aislamiento es por usuario y lo cubre "la reserva de otro cliente no se toca" |
| `DarDeBajaCuenta` | Validación | No tiene comando: el usuario sale del claim. El usuario inexistente sí se prueba (lanza) |
| `ConsultarPerfil` | Validación; recurso de otra empresa; conflicto; fallo a mitad; eventos | No tiene comando, actúa sobre el usuario de la sesión sin empresa, y es de solo lectura, sin transacción ni eventos |
| DTO nuevos | Casos de objeto de valor | Solo `ConvertirADto` |
| Páginas de `MiCuenta/` | Pruebas automáticas | No hay bUnit en el stack (AGENTS §4). Se cubren con la verificación manual |

**Verificación manual** (instancia propia en el puerto 5199, con datos desechables que se borran al terminar; los pasos que exijan iniciar sesión con un usuario los hace el desarrollador):

Comprobado con `curl` el 2026-10-18 en una instancia propia, ya detenida. Lo que exige iniciar sesión lo ha recorrido el desarrollador en el navegador, con resultado correcto (2026-10-18).

- [x] Sin sesión, `/my-account/bookings`, `/my-account/profile` y `/my-account/bookings/{id}` responden 302 a `login?ReturnUrl=…`.
- [x] Las pestañas muestran las reservas del cliente en el orden de CU-08, y no aparece ninguna de otro cliente. Lo cubren además `PruebasConsultasReservasCliente` y `PruebasMisReservasDto`.
- [x] El detalle de una reserva ajena o inexistente da 404. Lo cubre además `PruebasConsultarDetalleReserva`.
- [x] Cancelar una reserva futura pide confirmación y la deja cancelada. Una ya iniciada o cancelada no ofrece el botón y explica el motivo.
- [x] El perfil se edita, y una reserva ya creada conserva sus copias.
- [x] Un propietario con empresa activa ve el mensaje y el enlace a `/app/settings`.
- [x] La baja avisa de cuántas reservas se cancelarán, las cancela, permite cerrar la sesión y, después, ya no se puede iniciar sesión.
- [x] `grep -rn MarkupString src/PlataformaReservas.Web/Components/MiCuenta` no da resultados.
- [ ] Con sesión de cliente (o de propietario, que también tiene el rol `Cliente`), el menú muestra "Mis reservas" y "Mi perfil"; sin sesión no aparecen. *Pendiente de comprobar en el navegador por el desarrollador.*
- [x] La migración `RolClienteParaPropietarios` está aplicada en la base de datos de desarrollo: ningún miembro de `miembros_empresa` se queda sin el rol `Cliente`.

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-137 | En `RepositorioReservasCliente.ObtenerFuturasConfirmadasParaModificarAsync`, `InicioUtc > ahora.AddDays(-3)` en lugar de `> ahora` | `La baja cancela solo las futuras confirmadas…` | Fallaron `Cancela_solo_las_futuras_confirmadas_y_deja_intactas_las_demas` y `Las_futuras_confirmadas_son_solo_las_del_usuario_y_con_seguimiento`. Restaurado |
| RN-135 | En `DarDeBajaAsync`, `UpdateAsync` en lugar de `UpdateSecurityStampAsync`. El cambio de texto afectó también a `RenovarSelloMiembrosAsync` | `DarDeBajaAsync renueva el sello` y `Tras la baja: … el sello cambió` | Fallaron `Desactiva_la_cuenta_renueva_el_sello_y_ya_no_puede_iniciar_sesion`, `Dar_de_baja_renueva_el_sello_y_la_segunda_vez_devuelve_falso` (hoy `Dar_de_baja_desactiva_y_renueva_el_sello`) y `Renovar_el_sello_de_los_miembros_lo_cambia`. Restaurado |
| RN-93 y RN-137 (tras `/revisar 15`, hallazgos 4 y 5) | En `Reserva.EsCancelablePorCliente`, `>=` en lugar de `>` | `EsCancelablePorCliente: … extremos a un tick` | Fallaron 8 unitarias, entre ellas `EsCancelablePorCliente_solo_una_confirmada_que_empieza_despues_de_ahora(Confirmada, 0, False)`, `CancelarPorCliente_rechaza_una_reserva_que_ya_ha_empezado(0)`, `Cancelables_cuenta_solo_las_confirmadas_que_aun_no_han_empezado…` y `Una_reserva_que_empieza_justo_ahora_no_es_cancelable`. Restaurado |
| Precondición de la baja (tras `/revisar 15`, hallazgo 3) | En `DarDeBajaCuenta`, anular la comprobación de `EstaActivaAsync` | `Una cuenta ya inactiva da EstadoNoValido antes de abrir la transacción…` | Fallaron `Una_cuenta_ya_inactiva_da_estado_no_valido_antes_de_tocar_ninguna_reserva` y `Darse_de_baja_dos_veces_da_estado_no_valido`. Restaurado |
| RN-05 (en el sitio de RN-03 ⚑ para el eje cliente) | En `ConsultasReservasCliente`, quitar el filtro por `UsuarioId` | `El detalle de una reserva ajena o inexistente es null` | Fallaron `El_detalle_de_una_reserva_ajena_o_inexistente_es_nulo`, `Solo_devuelve_las_reservas_del_usuario_de_todas_sus_empresas`, `Las_proximas_empiezan_en_ahora…` y `Una_reserva_de_otro_usuario_o_inexistente_da_no_encontrado`. Restaurado |

## 7. Criterios de aceptación

- [x] CU-08: las dos pestañas, con el orden de `casos-uso.md` l.566 y solo las reservas del usuario.
- [x] CU-09: el detalle muestra `PrecioAplicado`. Una reserva ajena da "no encontrado".
- [x] CU-10: un cliente no puede cancelar la reserva de otro.
- [x] CU-10: no se puede cancelar una reserva ya iniciada.
- [x] CU-10: no se puede cancelar dos veces.
- [x] CU-22: editar el perfil no altera las reservas ya creadas.
- [x] CU-22: un propietario con empresa activa no puede darse de baja.
- [x] CU-22: la baja cancela solo las reservas futuras confirmadas, nunca las pasadas ni las completadas.
- [x] CU-22: tras la baja, el inicio de sesión se rechaza.
- [x] Las tres páginas son `InteractiveServer`, están protegidas con `PoliticaCliente` y no tienen `MarkupString`.
- [x] Sin paquetes nuevos. La única migración es la de datos del rol.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

Esta fase no cierra ningún punto de control.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | `A second operation was started on this context` al cancelar o dar de baja mientras se recarga la lista | Llamadas secuenciales con `await`, sin `Task.WhenAll` ni `async void`. Botones deshabilitados durante la operación |
| R-2 | Un `DateTime` no UTC al comparar con `ahora` | `ahora` sale de `IRelojSistema`, y la hora local solo se pinta con `MotorDisponibilidad.ALocal` |
| R-10 | Leer o escribir el nombre o el teléfono del usuario con LINQ | Solo `UserManager` (`FindByIdAsync` y `UpdateAsync`). Las pruebas comprueban que no queda nada en claro |
| R-11 | Editar el perfil y suponer que cambia la copia de la reserva | La prueba de RN-78 lo fija: la reserva conserva su copia cifrada |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Los correos de la baja (RN-151 al negocio, RN-155 al usuario) y el de la cancelación (RN-148) | Fase 16b |
| Reactivar una cuenta dada de baja | No está en el MVP (RN-139) |
| Supresión real de datos personales (RGPD) | Trabajo futuro (aviso de RN-139) |
| `/app/bookings` y la cancelación por la empresa | Fase 16 |
| Cambiar el correo o la contraseña desde el perfil | No figura en CU-22 |
| Los pendientes de la Fase 14 (importante 1 de la segunda revisión; caso 2 y prueba débil 1 de la auditoría) | Fase 17 (duda 10) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-03, RN-95 | T1, T2 | Detalle ajeno `null` / `NoEncontrado`; sabotaje RN-05; 404 manual |
| RN-05 | T1, T4 | Solo las del usuario; repositorio por usuario; "la reserva de otro cliente no se toca" |
| RN-78 | T1, T3 | `PrecioAplicado` en el detalle; editar el perfil no altera la reserva |
| RN-81 | T3 | `ActualizarPerfil` con teléfono nulo |
| RN-91, RN-93 | T4 | `CancelarPorCliente` con origen |
| RN-97 | T4 | El hueco cancelado vuelve a ofrecerse |
| RN-98 | T4 | `FechaCancelacion` en las reservas canceladas por la baja |
| RN-135 | T4 | Sello renovado; no puede iniciar sesión; sabotaje RN-135 |
| RN-136 | T4 | `Activo = false` |
| RN-137 | T4 | La cascada; sabotaje RN-137 |
| RN-138 | T4, T5 | `Conflicto` con empresa activa; manual |
| RN-139 | T5 | Aviso en la página (manual) |
| RN-140, RN-151, RN-155 | T4 | Eventos después del commit |
| RN-162, RN-167 | T3 | Nada en claro en `asp_net_users` |
| RN-171 | T2, T5 | `grep MarkupString` |
| CU-08, CU-09 | T1, T2 | Las de T1; manual |
| CU-10 | T2, T4 | `PruebasCancelarReservaCliente` (Fase 11 y T4); manual |
| CU-22 | T3, T4, T5 | Las de T3 y T4; manual |

## 11. Dudas

### Duda 1 — La consulta de mis reservas (CU-08 y CU-09)

- **Pregunta:** ningún documento nombra cómo se leen la lista y el detalle de las reservas del cliente. Necesitan nombres de cuatro agregados (empresa, servicio, profesional y reserva).
- **Opciones:**
  - (a) un puerto de lectura `IConsultasReservasCliente`, implementado por `ConsultasReservasCliente` en `Infraestructura/Consultas/`, con `ObtenerMisReservasAsync` y `ObtenerDetalleAsync`, que devuelven `ReservaClienteDto` y `DetalleReservaClienteDto`. Su implementación recibe `IContextoUsuario`, como el repositorio del eje cliente. Se usa a través de dos casos de uso, `ConsultarMisReservas` y `ConsultarDetalleReserva`;
  - (b) ampliar `IRepositorioReservasCliente` para que devuelva entidades, y montar los DTO en el caso de uso cruzando con los otros tres agregados;
  - (c) como (a), pero la página llama directamente al puerto, sin casos de uso.
- **Recomendación y motivo:** (a). Proyecta en SQL, sigue el patrón de los puertos de consulta (AGENTS §8 bis) y mantiene la correspondencia caso de uso ↔ CU (`arquitectura.md` §5.1).
- **Bloquea:** T1, T2.
- **Resolución:** opción (a), la recomendada.

### Duda 2 — Las pestañas, en una llamada o en dos

- **Pregunta:** ¿la consulta devuelve las dos pestañas a la vez o una cada vez?
- **Opciones:**
  - (a) una sola llamada que devuelve un `MisReservasDto` con `Proximas` e `Historicas`;
  - (b) un parámetro de pestaña, y una llamada por pestaña.
- **Recomendación y motivo:** (a). La regla de qué es "próxima" vive en un solo sitio, la consulta, y la página no necesita ninguna lógica para cambiar de pestaña.
- **Bloquea:** T1.
- **Resolución:** opción (a), la recomendada.

### Duda 3 — La cancelación por baja de cuenta en `Reserva`

- **Pregunta:** `CancelarPorCliente` registra siempre el origen `Cliente`, y `CancelarPorEmpresa` rechaza `BajaCuenta`. RN-151 y CU-22 exigen el origen `BajaCuenta`.
- **Opciones:**
  - (a) un parámetro `OrigenCancelacion` en `CancelarPorCliente`, que solo admite `Cliente` o `BajaCuenta`, como hizo la Fase 11b con `CancelarPorEmpresa` (su duda 6);
  - (b) un método nuevo, `CancelarPorBajaCuenta`.
- **Recomendación y motivo:** (a). La transición es la misma (solo reservas confirmadas y futuras, RN-93); solo cambia el motivo. Es coherente con la decisión de la Fase 11b.
- **Bloquea:** T4.
- **Resolución:** opción (a), la recomendada.

### Duda 4 — Quién registra y publica `CuentaDadaDeBaja`

- **Pregunta:** `Usuario` es de Identity y no hereda de `EntidadBase`, así que no puede registrar eventos.
- **Opciones:**
  - (a) lo crea el caso de uso `DarDeBajaCuenta` y lo publica después del commit, junto con los `ReservaCancelada`. El método nuevo `IServicioCuentas.DarDeBajaAsync(usuarioId, ct)` pone `Activo = false` y renueva el sello;
  - (b) `Usuario` gana su propia lista de eventos.
- **Recomendación y motivo:** (a). No cambia la jerarquía de `Usuario` y respeta RN-140: el evento sale después de confirmar.
- **Bloquea:** T4.
- **Resolución:** opción (a), la recomendada.

### Duda 5 — Cómo saber si el usuario es propietario de una empresa activa (RN-138)

- **Pregunta:** el caso de uso necesita saber si el usuario de la sesión pertenece a una empresa activa.
- **Opciones:**
  - (a) `IRepositorioMiembros.ObtenerPorUsuarioAsync` más `IConsultasDisponibilidad.ObtenerEmpresaAsync(...).Activa`, que ya existen;
  - (b) un método nuevo en `IServicioCuentas`;
  - (c) leer el claim `empresa_id`.
- **Recomendación y motivo:** (a). No crea nada nuevo y no depende de un claim que puede haber quedado desfasado.
- **Bloquea:** T4.
- **Resolución:** opción (a), la recomendada.

### Duda 6 — El rol para entrar en `/my-account`

- **Pregunta:** SPEC §11 l.640 exige el rol `Cliente`. Un propietario dado de alta por `/for-business/register` solo tiene `Propietario`. Aun así puede reservar (Fase 14, duda 8), y RN-138 da por hecho que llega a su perfil.
- **Opciones:**
  - (a) dar también el rol `Cliente` al propietario: en `ServicioCuentas.CrearPropietarioAsync` para los nuevos, y con una migración de solo datos para los que ya existen;
  - (b) que `/my-account` exija solo estar autenticado, lo que se aparta de la SPEC;
  - (c) dejar `PoliticaCliente` como está, y el propietario no ve sus reservas.
- **Recomendación y motivo:** (a). Cumple a la vez la SPEC y `casos-uso.md` l.20 ("roles acumulativos: el propietario de una peluquería puede reservar en un fisioterapeuta con la misma cuenta").
- **Bloquea:** T2, T5.
- **Resolución:** opción (a), la recomendada. Amplía §4 con `ServicioCuentas.CrearPropietarioAsync` y con una migración de solo datos.

### Duda 7 — Cerrar la sesión después de la baja (CU-22 paso 10)

- **Pregunta:** `Account/Logout` es un POST con protección antifalsificación, y una página `InteractiveServer` no puede hacerlo desde el circuito.
- **Opciones:**
  - (a) mostrar la confirmación con un formulario POST a `Account/Logout`, el mismo de `MainLayout`. Además, el sello renovado expulsa el circuito como mucho a los 5 minutos (A-06);
  - (b) un endpoint GET de cierre de sesión, que obliga a tocar `Web/Cuenta/` y abre una vía CSRF;
  - (c) navegar a la portada con `forceLoad` y confiar en el sello.
- **Recomendación y motivo:** (a). Reutiliza lo que ya existe, sin abrir una vía nueva, y deja la sesión cerrada al momento si el usuario pulsa.
- **Bloquea:** T5.
- **Resolución:** opción (a), la recomendada.

### Duda 8 — ¿`ActualizarPerfil` permite quitar el teléfono?

- **Pregunta:** `modelo-dominio.md` §4.1 admite `telefono` nulo, y RN-81 lo volvería a pedir en la siguiente reserva.
- **Opciones:**
  - (a) sí: un teléfono nulo lo quita;
  - (b) no: una vez puesto, solo se puede cambiar.
- **Recomendación y motivo:** (a). Es literal al modelo ("`Telefono` puede ser nulo, pero no cadena vacía"), y RN-81 ya cubre el caso al reservar.
- **Bloquea:** T3.
- **Resolución:** opción (a), la recomendada.

### Duda 9 — Incoherencia en `fase-09.md` l.300-301

- **Pregunta:** dos puntos fuera de alcance de la Fase 9 se asignan a la "Fase 15 (`/app/bookings`)", pero `/app/bookings` es de la Fase 16 (SPEC §19).
- **Opciones:**
  - (a) corregirlos a "Fase 16" en T6;
  - (b) dejarlos para la Fase 20.
- **Recomendación y motivo:** (a). Es una línea, y evita que la Fase 16 los pase por alto.
- **Bloquea:** ninguna (T6 aplica la corrección).
- **Resolución:** opción (a), la recomendada.

### Duda 10 — Los pendientes de la Fase 14

- **Pregunta:** la Fase 14 está en commit con tres puntos abiertos:
  - el importante 1 de la segunda revisión: `EstablecerTelefono` atrapa la `DominioException` en lugar de comprobar antes la precondición;
  - el caso 2 de la auditoría: el orden `es-ES` de la ficha no está fijado por ninguna prueba;
  - la prueba débil 1 de la auditoría: quitar `p.Activo` no hace fallar nada.
- **Opciones:**
  - (a) dejarlos para la Fase 17, la de pruebas finales;
  - (b) meterlos en esta fase, lo que amplía el radio y supera el presupuesto (≈ 0,75 h).
- **Recomendación y motivo:** (a). Ninguno toca el área de cliente, y la Fase 17 está pensada para las pruebas que faltan.
- **Bloquea:** ninguna.
- **Resolución:** opción (a), la recomendada.

### Decisiones de implementación

- **Las reglas de las pestañas y de "cancelable" viven en los DTO, una sola vez.**
  - `MisReservasDto.ConvertirADto(reservas, ahora)` separa próximas (`InicioUtc >= ahora`) e históricas, y las ordena. No es genérico, así que CA1000 no aplica.
  - "Cancelable por el cliente" (`Confirmada` e `InicioUtc > ahora`) vive una sola vez, en `Reserva.EsCancelablePorCliente(estado, franja, ahora)` (hallazgos 4 y 5 de `/revisar 15`, A-09). Es estático para que sirva a las proyecciones sin entidad (D-22). Lo usan `CancelarPorCliente`, `ReservaClienteDto.Cancelable` y `DetalleReservaClienteDto.Cancelable`. `CancelarReservaCliente` conserva sus dos comprobaciones con mensajes distintos (2a y 3a), que es la duplicación entre caso de uso y dominio que A-09 permite.
  - `MisReservasDto.Cancelables` cuenta las reservas cancelables. Es el número que muestra la baja, con la misma condición que la cascada de RN-137 (`InicioUtc > ahora`): una reserva que empieza justo ahora está en próximas pero no se cuenta.
  - La página solo pinta: no tiene ninguna regla de negocio.
  - El puerto recibe `ahora`: `ObtenerMisReservasAsync(ahoraUtc)` y `ObtenerDetalleAsync(id, ahoraUtc)`.
- **`ConsultasReservasCliente` proyecta en SQL.**
  - Une reservas, empresas, servicios y profesionales, y filtra por `UsuarioId` y por el `Id` *antes* de proyectar (un filtro sobre el record proyectado no se traduce a SQL).
  - No carga `NombreCliente` ni `TelefonoCliente` (D-22).
  - Los DTO tienen sobrecargas de `ConvertirADto` con los valores proyectados, y la versión con la entidad delega en ellas (AGENTS §8 bis).
- **La precondición "cuenta activa" va antes que nada (`arquitectura.md` §6; hallazgo 3 de `/revisar 15`).**
  - `IServicioCuentas.EstaActivaAsync` la consulta, y `DarDeBajaAsync` no devuelve nada.
  - Una cuenta ya inactiva da `EstadoNoValido` antes de abrir la transacción. Así ninguna reserva queda cancelada en el rastreador del circuito tras un rollback.
  - `Usuario.DarDeBaja` sigue lanzando si la cuenta ya estaba inactiva: llegar ahí sería un fallo de programación. Lo mismo vale para un usuario de la sesión que no existe.
  - La baja y el sello nuevo se guardan juntos con `UserManager.UpdateSecurityStampAsync`.
- **Orden de `DarDeBajaCuenta`.**
  - Primero, fuera de la transacción: la sesión, RN-138 y la cuenta activa.
  - Después, dentro de la transacción y en este orden: las cancelaciones, `GuardarCambiosAsync`, `DarDeBajaAsync` y el commit.
  - `DarDeBajaAsync` va después de las cancelaciones para que la prueba 8a (`CuentasQueFallanAlDarDeBaja`, que lanza en ese paso) demuestre que las cancelaciones ya guardadas en la transacción se deshacen.
  - Identity guarda con el mismo `ContextoDatos` del ámbito, así que entra en la transacción.
- **El perfil se lee con el caso de uso `ConsultarPerfil`** (A-01; hallazgo 6 de `/revisar 15`). Devuelve un `PerfilDto` (`Dtos/Cuenta/`), y lo usan `Perfil.razor` y `FlujoReserva.razor`, que ya no inyectan `IServicioCuentas`.
- **C13 de `comprobaciones.sh` mira solo `src/`** (hallazgo 1 de `/revisar 15`). RN-167 prohíbe la consulta LINQ en el código; la prueba que la demuestra la hace a propósito.
- **El correo del perfil sale de `AuthenticationState`** (`User.Identity.Name`, que es el correo). `ObtenerContactoAsync` no lo devuelve, y no hacía falta ampliar el puerto para mostrarlo.
- **Rol `Cliente` para los propietarios.**
  - `CrearPropietarioAsync` usa `AddToRolesAsync` con `Propietario` y `Cliente`.
  - La migración `RolClienteParaPropietarios` solo cambia datos: el modelo no cambia y `has-pending-model-changes` responde "No changes".
  - Su `Up` es idempotente (`ON CONFLICT DO NOTHING`) y su `Down` está vacío, con un comentario que explica por qué.
- **Enlaces del menú al área de cliente.** Tras las pruebas en el navegador, el desarrollador pidió el acceso desde el menú y autorizó tocar `Components/Layout/MainLayout.razor`, que no estaba en §4 (anotado allí como excepción). Hay un `AuthorizeView` con la política `EsCliente`, de modo que los enlaces solo aparecen a quien puede entrar.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo en las fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado: no aplica, esta fase no cierra ninguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas: ninguna es de esta fase
