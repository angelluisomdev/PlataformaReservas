# Fase 16 — Panel y gestión de reservas de la empresa

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4 h (SPEC §19) |
| **Semana** | 5 (`PROJECT_PLAN.md` §2, l.113-125), junto a las fases 15 y 16b |
| **Cierra** | Control 4: "Los tres flujos completos funcionan de extremo a extremo" (`PROJECT_PLAN.md` §3, l.185-192) |
| **Generada** | 2026-10-18 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar existen las dos vistas del área de empresa que faltan, en `InteractiveServer`, con la política `EsPropietario` y siempre filtradas por `EmpresaActualId` en la propia consulta:

- **`/app`, el panel de CU-18.** Sustituye a la página mínima de la Fase 8 (duda 9). Muestra:
  - las reservas de hoy y las próximas;
  - el aviso de "aún no visible" con los requisitos que faltan (RN-19);
  - con la empresa dada de baja, el aviso de baja (RN-134).
- **`/app/bookings`, el listado de CU-19.** Tiene selector de fecha y filtros por profesional y estado. Desde él se cancela (CU-20) y se marca como completada (CU-21), con los casos de uso que ya existen desde la Fase 11.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §5.3 (l.213-214) | "Panel de resumen: reservas de hoy y próximas." "Listado y gestión de reservas: cancelar y marcar como completada." |
| SPEC §9.11 (l.532-535) | "La **empresa** puede cancelar en cualquier momento." "Marcar como `Completada` es **manual**, desde `/app/bookings`, y solo si `FinUtc < ahora`." Los estados terminales y el hueco liberado |
| SPEC §10 (l.566, 584, 598, 604) | Aislamiento entre empresas, contexto resuelto desde el claim, defensa en profundidad y "no encontrado" en lugar de "prohibido" |
| SPEC §11 (l.648-659) | `/app`: "resumen: reservas de hoy y próximas". `/app/bookings`: "listado con selector de fecha; cancelar y completar". "**Total: 16 vistas.** … no debe crecer" |
| SPEC §14 (l.850) | Índice `reservas (empresa_id, inicio_utc)`, que sirve al listado por fecha |
| SPEC §21 (l.1213) | "La empresa puede ver, cancelar y completar sus reservas." |
| D-01 | `InteractiveServer` en el área de empresa |
| D-11, D-18 | Repositorios y consultas filtrados en origen por el claim `empresa_id`; sin filtros globales |
| D-17 | Una clase por caso de uso, que devuelve `Resultado` con DTOs |
| D-22 | Las copias del cliente en `Reserva` están cifradas; solo se descifra lo que se muestra |
| A-09 (`arquitectura.md` §7, l.481-484) | Duplicación consciente de las transiciones entre caso de uso y dominio, y nada más |
| `arquitectura.md` §4.3 (l.375) | "**nunca dos casos de uso en paralelo dentro de un componente**" |
| RN-02 ⚑, RN-03 ⚑ | Solo los recursos de la propia empresa. Uno ajeno por identificador es "no encontrado" |
| RN-16 ⚑, RN-18, RN-19 | Condición de publicable, calculada en cada consulta, y el aviso del panel con los requisitos que faltan |
| RN-82 | `NombreCliente` y `TelefonoCliente` son "la única vía de contacto de la empresa con el cliente" |
| RN-91, RN-92 | Transiciones legales y estados terminales |
| RN-94 | "La empresa puede cancelar cualquiera de sus reservas en estado `Confirmada`, sin restricción temporal." |
| RN-96 | Completar es manual y "solo es posible si `FinUtc < ahora`" |
| RN-97 ⚑, RN-98 | El hueco se libera y se registra `FechaCancelacion` |
| RN-134 | Una empresa inactiva conserva el acceso a `/app` "para consultar el histórico y reactivarla" |
| RN-140 ⚑, RN-149 | `ReservaCancelada` (origen `Empresa`) se encola después del commit. El correo es de la Fase 16b |
| RN-163 ⚑ | Las copias del cliente se descifran con los conversores de EF Core; nunca están en claro en la tabla |
| RN-171 ⚑ | Nada de `MarkupString` |
| CU-18 a CU-21 (`casos-uso.md` l.585-588) | Panel, listado con filtros, cancelar y completar. "La acción no se ofrece si `FinUtc >= ahora`" |
| CU-11 paso 7 (l.321) | El panel "muestra los pasos que faltan para publicarlo: crear un servicio, crear un profesional, asociarlos y definir su horario" |
| CU-23 pasos 8 y 9, criterio l.492 | Con la empresa inactiva, el panel muestra el aviso de baja, y el propietario "ve el histórico" (parcial: "el histórico llega en la Fase 16") |
| `casos-uso.md` §5.3 (l.630-638) | Guion de la prueba manual del Control 4 |
| `fase-09.md` l.300-301 | Contar las reservas del día de una excepción "si se decide entonces" (duda 9), y la cancelación manual de reservas afectadas, que llega con CU-20 |
| `fase-11b.md` l.268 (duda 10) | Corregir el intercambio CU-22/CU-23 en `reglas-negocio.md` (RN-130, RN-131, RN-136 y RN-137), asignado a "Fases 15 y 16"; la 15 no lo hizo |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `CancelarReservaEmpresa` y `CompletarReserva`, con sus comandos y pruebas (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`) | ✔ comprobado (Fase 11) |
| `ReservaCancelada` con origen `Empresa` publicado después del commit | ✔ comprobado (Fase 11b) |
| `IRepositorioReservas` y `RepositorioReservas`, filtrados por `EmpresaActualId` | ✔ comprobado |
| `IContextoEmpresa` (`EmpresaActualId`, `TieneEmpresa`) y la política `EsPropietario` (rol más claim `empresa_id`) | ✔ comprobado |
| `Components/App/_Imports.razor` con `[Authorize(Policy = "EsPropietario")]` e `Inicio.razor` (`/app`, página mínima) | ✔ comprobado |
| `ListarProfesionales` (para el filtro por profesional) | ✔ comprobado |
| `ReactivarEmpresa` y su botón en `/app/settings` (CU-23) | ✔ comprobado (Fase 8) |
| Conversores de cifrado de `NombreCliente` y `TelefonoCliente` (`ConfiguracionReserva`) | ✔ comprobado (Fase 5) |
| `MotorDisponibilidad.ALocal` (público) y `ALocalUtc` (privado), el único punto de conversión | ✔ comprobado |
| Condición de RN-16 en `ConsultasCatalogoPublico.Visibles()` | ✔ comprobado |
| `Reserva.EsCancelablePorCliente` como precedente de regla en un único punto | ✔ comprobado (Fase 15) |
| `EscenarioReserva`, `EscenarioEmpresa` y `ContextoEmpresaFijo` en las pruebas de integración | ✔ comprobado |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Entidades/Reserva.cs`: `EsCompletable`, que también usa `Completar` (duda 6) |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IConsultasReservasEmpresa.cs` (nuevo, duda 1). `Dtos/Reservas/ReservaEmpresaDto.cs` y `PanelEmpresaDto.cs` (nuevos, duda 2). `CasosUso/Reservas/ConsultarReservasEmpresa.cs`, su comando, y `ConsultarPanelEmpresa.cs` (nuevos). `Disponibilidad/MotorDisponibilidad.cs` (rango UTC de un día local, duda 4). `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Consultas/ConsultasReservasEmpresa.cs` (nuevo). `Consultas/CondicionPublicable.cs` (nuevo, duda 5) y `Consultas/ConsultasCatalogoPublico.cs`, cuyo `Visibles()` pasa a usarlo. `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Web` | sí | `Components/App/Inicio.razor` (pasa a ser el panel). `Components/App/Reservas.razor` (nuevo, `/app/bookings`). Sus `.razor.css`. `Components/App/_Imports.razor` (usings de reservas) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Entidades/PruebasReserva.cs`. `Disponibilidad/PruebasMotorDisponibilidad.cs`. `Dtos/Reservas/PruebasReservaEmpresaDto.cs` y `PruebasPanelEmpresaDto.cs` (nuevos) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Consultas/PruebasConsultasReservasEmpresa.cs` (nuevo). `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` y `PruebasConsultarPanelEmpresa.cs` (nuevos). `CasosUso/Reservas/PruebasCompletarReserva.cs` (extremo `FinUtc == ahora`). `Consultas/PruebasConsultasCatalogoPublico.cs`, solo si la extracción de RN-16 lo exige |
| `docs/` | sí | `casos-uso.md` (criterios de CU-18 a CU-21 y el de CU-23 l.492). `reglas-negocio.md` (origen de RN-130, RN-131, RN-136 y RN-137, pendiente de `fase-11b.md` l.268). `arquitectura.md` §2.3 (los nombres nuevos) y la tabla de familias de acceso (duda 1). `modelo-dominio.md` §4.10 (`EsCompletable`). `AGENTS.md` §8 (duda 1). Este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- `Components/Layout/MainLayout.razor`, porque al área se entra por "Mi establecimiento" y el panel enlaza al listado;
- `Web/Cuenta/`, `Program.cs` ni `Components/MiCuenta/`;
- los manejadores de correo (Fase 16b);
- `CancelarReservaEmpresa` ni su comando;
- las demás páginas de `App/`, en particular `/app/schedules` (duda 9);
- ninguna migración y ningún paquete.

## 5. Tareas

### T1 — Consulta de reservas de la empresa (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IConsultasReservasEmpresa.cs`;
  - crea `Aplicacion/Dtos/Reservas/ReservaEmpresaDto.cs` y `PanelEmpresaDto.cs`;
  - crea `Infraestructura/Consultas/ConsultasReservasEmpresa.cs` y `Infraestructura/Consultas/CondicionPublicable.cs`;
  - modifica `Infraestructura/Consultas/ConsultasCatalogoPublico.cs` (`Visibles()` usa `CondicionPublicable`);
  - modifica `Dominio/Entidades/Reserva.cs` (`EsCompletable`, duda 6);
  - modifica `Infraestructura/InyeccionDependencias.cs`;
  - pruebas: `Consultas/PruebasConsultasReservasEmpresa.cs`, `Dtos/Reservas/PruebasReservaEmpresaDto.cs`, `PruebasPanelEmpresaDto.cs` y `Entidades/PruebasReserva.cs`.
- **Implementa:** CU-18, CU-19, RN-02, RN-03, RN-16, RN-18, RN-19, RN-82, RN-96, RN-163, D-11, D-22.
- **Contenido:**
  - **El puerto.** `IConsultasReservasEmpresa` no nombra `IContextoEmpresa`; su implementación lo recibe y filtra por `EmpresaActualId` **en la consulta**, sin seguimiento (duda 1). Tiene dos operaciones:
    - las reservas con `InicioUtc` en un rango `[desdeUtc, hastaUtc)`, opcionalmente de un profesional y de un estado, en orden ascendente;
    - el panel: estado de la empresa, los cuatro requisitos de publicación y las reservas de dos rangos (hoy y próximas).
  - **`ReservaEmpresaDto`.** Lleva:
    - `Id`, inicio y fin UTC;
    - servicio y profesional (nombres proyectados en SQL);
    - nombre y teléfono del cliente (duda 7), descifrados por los conversores al materializar, nunca filtrados ni ordenados en SQL;
    - observaciones y estado;
    - `Cancelable`, que es `Estado == Confirmada` (RN-94);
    - `Completable`, que es `Reserva.EsCompletable(estado, franja, ahora)` (duda 6).

    Se construye con `ConvertirADto` y la sobrecarga de valores proyectados (AGENTS §8 bis).
  - **`PanelEmpresaDto`.** Lleva:
    - `Activa`;
    - `TieneServicioActivo`, `TieneProfesionalActivo`, `TieneAsociacion` y `EsPublicable` (duda 5);
    - `Hoy` y `Proximas`, listas de `ReservaEmpresaDto`.
  - **`CondicionPublicable`.** Es la combinación de RN-16, escrita una sola vez. La usan `Visibles()` (más `Activa`, RN-109) y el panel. RN-18 se cumple porque se calcula en cada consulta.
  - **`Reserva.EsCompletable(estado, franja, ahora)`** devuelve `Confirmada && FinUtc < ahora`. `Completar` la usa en lugar de sus dos comprobaciones, y sigue lanzando `DominioException`.
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Casos de uso del panel y del listado (≈ 0,75 h)

- **Ficheros:**
  - crea `Aplicacion/CasosUso/Reservas/ConsultarReservasEmpresa.cs`, `ConsultarReservasEmpresaComando.cs` y `ConsultarPanelEmpresa.cs`;
  - modifica `Aplicacion/Disponibilidad/MotorDisponibilidad.cs` (rango UTC de un día local, duda 4);
  - modifica `Aplicacion/InyeccionDependencias.cs`;
  - pruebas: `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs`, `PruebasConsultarPanelEmpresa.cs` y `Disponibilidad/PruebasMotorDisponibilidad.cs`.
- **Implementa:** CU-18, CU-19, RN-19, RN-134, AGENTS §9 (conversión en un único punto).
- **Contenido:**
  - **`MotorDisponibilidad`.** Gana un método público estático que da el `[desdeUtc, hastaUtc)` de un `DateOnly` local (Europe/Madrid), apoyado en `ALocalUtc`. Ningún otro sitio convierte.
  - **`ConsultarReservasEmpresa.EjecutarAsync(comando, ct)`.** El comando lleva `Fecha` (`DateOnly`), `ProfesionalId?` y `Estado?`.
    - Un `Estado` fuera de la enumeración da `Validacion`.
    - Convierte el día con el motor y devuelve la lista con `ahora` del reloj.
  - **`ConsultarPanelEmpresa.EjecutarAsync(ct)`**, sin comando: la empresa sale del contexto.
    - Hoy es el día local de `ALocal(reloj.AhoraUtc)`.
    - Las próximas son las confirmadas de los 7 días siguientes a hoy (duda 3).
    - Devuelve `PanelEmpresaDto`, también con la empresa inactiva (RN-134).
  - Sin comprobación de `TieneEmpresa`, igual que el resto de casos de uso de empresa: la política `EsPropietario` exige el claim.
- **Hecho cuando:** las pruebas de T2 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T3 — Página `/app/bookings` (≈ 1 h)

- **Ficheros:** crea `Web/Components/App/Reservas.razor` y su `.razor.css`; modifica `Web/Components/App/_Imports.razor`.
- **Implementa:** CU-19, CU-20, CU-21, RN-94, RN-96, RN-171, D-01, SPEC §9.11 (l.533).
- **Contenido:**
  - `@page "/app/bookings"`, `InteractiveServer`, protegida por `App/_Imports.razor`.
  - **Filtros:**
    - selector de fecha, hoy por defecto;
    - profesional, cargado con `ListarProfesionales`;
    - estado.

    Se aplican con llamadas **secuenciales** (R-1).
  - **Cada fila** muestra la hora local (`MotorDisponibilidad.ALocal`), el servicio, el profesional, el nombre y teléfono del cliente, las observaciones y el estado.
  - **"Cancelar"** solo aparece si `Cancelable`, con una confirmación explícita, y llama a `CancelarReservaEmpresa`.
  - **"Marcar como completada"** solo aparece si `Completable` y llama a `CompletarReserva`.
  - Un `Fallo` se muestra con su mensaje y la lista se recarga.
  - Botones deshabilitados durante la operación. Ningún `MarkupString`.
- **Hecho cuando:** la verificación manual de §6 está marcada para el listado.
- **Verificación:** manual (§6).

### T4 — Panel `/app` (≈ 0,75 h)

- **Ficheros:** modifica `Web/Components/App/Inicio.razor`; crea su `.razor.css`.
- **Implementa:** CU-18, CU-11 paso 7, CU-23 pasos 8 y 9, RN-19, RN-134, RN-171.
- **Contenido:**
  - **Empresa inactiva.** Primero, el aviso de baja con enlace a `/app/settings`, donde está la reactivación (duda 8).
  - **Empresa no publicable.** Primero, el aviso "aún no visible" y, de los cuatro pasos de CU-11 paso 7, los que faltan, cada uno con su enlace (`/app/services`, `/app/staff`, `/app/schedules`).
  - **Las dos listas.** Hoy y próximas, con la hora local y un enlace a `/app/bookings`.
  - Se conservan los enlaces a la configuración de la página mínima.
  - Sin gráficos ni métricas (CU-18).
- **Hecho cuando:** la verificación manual de §6 está marcada para el panel.
- **Verificación:** manual (§6).

### T5 — Sabotajes, verificación manual, Control 4 y documentación (≈ 0,5 h)

- **Ficheros:** `docs/casos-uso.md`, `docs/reglas-negocio.md` (l.236-237 y l.247-248), `docs/arquitectura.md` §2.3 y la tabla de familias, `docs/modelo-dominio.md` §4.10, `AGENTS.md` §8 (duda 1) y este fichero.
- **Implementa:** AGENTS §11 bis (sabotajes), `PROJECT_PLAN.md` §3 (Control 4), `fase-11b.md` duda 10.
- **Hecho cuando:**
  - los tres sabotajes de §6 han fallado su prueba y el código está restaurado;
  - el guion de `casos-uso.md` §5.3 se ha recorrido a mano;
  - los documentos están corregidos;
  - `/auditar-pruebas 16` está ejecutado.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 4 h de 4 h presupuestadas.**

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Solo devuelve reservas de la empresa del contexto: las de otra empresa en el mismo rango no aparecen (`ContextoEmpresaFijo`) | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | RN-02, RN-03, SPEC §10.4 | T1 |
| Filtra por rango `[desde, hasta)`: una reserva que empieza en `hasta` no entra y una que empieza en `desde` sí | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | CU-19 | T1 |
| Filtra por profesional y por estado, y ordena por inicio | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | CU-19 | T1 |
| El nombre y el teléfono del cliente llegan descifrados, y en la tabla siguen cifrados | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | RN-82, RN-163 | T1 |
| Las consultas no dejan entidades en seguimiento | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | A-05 | T1 |
| Requisitos del panel uno por uno: sin servicio activo; sin profesional activo; sin asociación; asociados sin horario; publicable | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | RN-16, RN-18, RN-19 | T1 |
| El panel de una empresa inactiva sigue devolviendo sus reservas | I | `Consultas/PruebasConsultasReservasEmpresa.cs` | RN-134 | T1 |
| Las búsquedas y fichas del catálogo siguen igual tras extraer `CondicionPublicable` (las pruebas existentes de RN-16 y RN-109) | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-16, RN-109 | T1 |
| `EsCompletable`: solo `Confirmada` con `FinUtc < ahora`; extremos a un tick (`FinUtc == ahora` no), los tres estados y el cambio de día en UTC | U | `Entidades/PruebasReserva.cs` | RN-91, RN-96, A-09 | T1 |
| `Completar` sobre `FinUtc == ahora` lanza | U | `Entidades/PruebasReserva.cs` | RN-96 | T1 |
| `ReservaEmpresaDto.ConvertirADto`: copia los datos; `Cancelable` solo si `Confirmada`; `Completable` sigue a `EsCompletable` | U | `Dtos/Reservas/PruebasReservaEmpresaDto.cs` | RN-94, RN-96 | T1 |
| `PanelEmpresaDto.ConvertirADto`: copia los indicadores y las dos listas | U | `Dtos/Reservas/PruebasPanelEmpresaDto.cs` | CU-18 | T1 |
| Rango UTC de un día local: invierno (+1), verano (+2), y los días del cambio de hora, de 23 h y de 25 h; el resultado tiene `Kind == Utc` | U | `Disponibilidad/PruebasMotorDisponibilidad.cs` | AGENTS §9, R-2 | T2 |
| Caso normal: las reservas del día local pedido, con el `ahora` del reloj en `Completable` | I | `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` | CU-19 | T2 |
| Una reserva a las 23:30 locales cae en su día local y no en el siguiente, aunque en UTC sea otro día | I | `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` | CU-19, AGENTS §9 | T2 |
| Un estado fuera de la enumeración da `Validacion` | I | `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` | AGENTS §11 bis | T2 |
| Un profesional de otra empresa como filtro devuelve una lista vacía, sin revelar nada | I | `CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` | RN-03 | T2 |
| Panel: hoy son las del día local; próximas, las confirmadas de los 7 días siguientes; las del día 8 no entran | I | `CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` | CU-18 | T2 |
| Panel de una empresa no publicable: `EsPublicable = false` y los indicadores que faltan | I | `CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` | RN-19 | T2 |
| Panel de otra empresa: no aparece ninguna de sus reservas | I | `CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` | RN-02 | T2 |
| `CompletarReserva` con `FinUtc == ahora` da `EstadoNoValido` | I | `CasosUso/Reservas/PruebasCompletarReserva.cs` | RN-96 | T1 |
| Cancelar y completar desde la empresa, publicar el evento tras el commit y liberar el hueco | I | `PruebasCancelarReservaEmpresa.cs`, `PruebasCompletarReserva.cs`, `PruebasDarDeBajaCuenta.cs` (hueco) | RN-91, RN-94, RN-97, RN-98, RN-140, RN-149 | ya existen (Fase 11 y 11b) |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `ConsultarPanelEmpresa` | Validación | No tiene comando: la empresa sale del claim |
| `ConsultarReservasEmpresa`, `ConsultarPanelEmpresa` | Recurso inexistente; conflicto; fallo a mitad; eventos | Son de solo lectura y devuelven listas: una fecha sin reservas o un filtro sin coincidencias da una lista vacía, sin transacción ni eventos |
| `ConsultarReservasEmpresa`, `ConsultarPanelEmpresa` | Sin sesión o sin empresa (`NoAutorizado`) | La política `EsPropietario` exige el rol y el claim `empresa_id`; los demás casos de uso de empresa tampoco lo comprueban (`ListarProfesionales`, `ObtenerConfiguracionEmpresa`) |
| `ConsultasReservasEmpresa` | Escritura y defensa en profundidad | Solo lectura; cancelar y completar usan `IRepositorioReservas`, ya probado |
| DTO nuevos | Casos de objeto de valor | Solo `ConvertirADto` |
| Páginas de `App/` | Pruebas automáticas | No hay bUnit en el stack (AGENTS §4). Se cubren con la verificación manual |

**Verificación manual.** Se hace en una instancia propia en el puerto 5199, con datos desechables. Los pasos con sesión de propietario los hace el desarrollador.

Comprobado con `curl` el 2026-10-18 en una instancia propia, ya detenida. Lo que exige sesión lo ha recorrido el desarrollador en el navegador, con resultado correcto (2026-10-18).

- [x] Sin sesión, `/app` y `/app/bookings` responden 302 a `login?ReturnUrl=…` (`curl`: `login?ReturnUrl=%2Fapp` y `login?ReturnUrl=%2Fapp%2Fbookings`). Un cliente sin empresa no entra porque `EsPropietario` exige el rol y el claim, sin cambios en esta fase.
- [x] `/app/bookings` muestra las reservas del día elegido en hora local, filtra por profesional y estado, y no muestra ninguna de otra empresa.
- [x] Cancelar pide confirmación y deja la reserva cancelada; el hueco vuelve a ofrecerse en `/book/{slug}`.
- [x] "Marcar como completada" solo aparece en reservas confirmadas ya terminadas y las deja completadas.
- [x] El panel muestra las reservas de hoy y de los 7 días siguientes.
- [x] Una empresa nueva ve "aún no visible" con los pasos que faltan, y cada paso desaparece al cumplirlo.
- [x] Con la empresa dada de baja, el panel muestra el aviso de baja y el enlace a `/app/settings`, y `/app/bookings` muestra el histórico.
- [x] `grep -rn MarkupString src/PlataformaReservas.Web/Components/App` no da resultados (0 coincidencias).
- [x] Guion del Control 4 (`casos-uso.md` §5.3) recorrido de principio a fin.

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-02, RN-03 | En `ConsultasReservasEmpresa`, quitar el filtro por `EmpresaActualId` | `Solo devuelve reservas de la empresa del contexto…` y `Panel de otra empresa…` | Fallaron 12 de 18, entre ellas `Solo_devuelve_reservas_de_la_empresa_del_contexto`, `El_panel_separa_hoy_de_las_proximas_confirmadas_y_no_mezcla_otra_empresa`, `No_aparece_ninguna_reserva_de_otra_empresa` y `Un_profesional_de_otra_empresa_como_filtro_devuelve_una_lista_vacia`. Restaurado |
| RN-16 | En `CondicionPublicable`, quitar la exigencia de horario (`p.Horarios.Any()`) | `Requisitos del panel uno por uno` (asociados sin horario) y las pruebas de RN-16 de `PruebasConsultasCatalogoPublico` | Fallaron `El_panel_indica_uno_por_uno_los_requisitos_que_faltan_para_publicar`, `Una_empresa_no_publicable_lo_indica_con_los_requisitos_que_faltan` y `Sin_un_camino_completo_servicio_profesional_horario_no_es_publicable` (catálogo): la extracción protege a los dos consumidores. Restaurado |
| RN-96 (no es ⚑; refuerza A-09) | En `Reserva.EsCompletable`, `<=` en lugar de `<` | `EsCompletable: … extremos a un tick` y `CompletarReserva con FinUtc == ahora…` | Fallaron 4 unitarias: `EsCompletable_solo_una_confirmada_que_termino_antes_de_ahora(Confirmada, 0, False)`, `EsCompletable_cruza_el_cambio_de_dia_en_utc`, `Completable_sigue_la_regla_del_dominio(Confirmada, 0, False)` y `Completar_rechaza_una_reserva_que_no_ha_terminado(-45)`. `PruebasCompletarReserva` sigue en verde, como se espera: el caso de uso conserva su propia comprobación (A-09). Restaurado |

## 7. Criterios de aceptación

- [x] CU-18: el panel muestra las reservas de hoy y las próximas de la empresa, y si no es publicable, primero el aviso con los requisitos que faltan.
- [x] CU-19: el listado tiene selector de fecha y filtros por profesional y estado, y una reserva de otra empresa nunca aparece.
- [x] CU-20: la empresa cancela una reserva `Confirmada` sin restricción temporal, el hueco se libera y el evento se encola después del commit.
- [x] CU-21: "completar" solo se ofrece y solo funciona si `FinUtc < ahora`.
- [x] CU-23 (l.492): "El propietario sigue entrando en /app y ve el histórico."
- [x] SPEC §21: "La empresa puede ver, cancelar y completar sus reservas."
- [x] Las páginas son `InteractiveServer`, están protegidas por `EsPropietario` y no tienen `MarkupString`.
- [x] Ninguna migración ni paquete nuevo. Siguen siendo 16 vistas (SPEC §11).
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

**Control 4** (`PROJECT_PLAN.md` §3, literal):

```text
Los tres flujos completos funcionan de extremo a extremo:
  la empresa se registra, se configura y se publica sola
  el visitante busca, encuentra y reserva
  cliente y empresa gestionan la reserva
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | El panel o el listado lanzan dos consultas a la vez (por ejemplo, profesionales y reservas) y EF Core falla con "a second operation was started on this context" | Un solo caso de uso para el panel; en el listado, las llamadas se encadenan con `await`. Nada de `Task.WhenAll` ni `async void` |
| R-2 | El rango del día se construye con un `DateTime` sin `Kind == Utc` y Npgsql lo rechaza al filtrar | El rango sale solo del motor, que devuelve UTC; lo prueba `PruebasMotorDisponibilidad` |
| R-11 | Se intenta filtrar u ordenar por el nombre del cliente en SQL, o se proyecta la columna sin el conversor | El filtro y el orden son por inicio, profesional y estado; la prueba de RN-163 comprueba el descifrado y que la tabla sigue cifrada |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Correo al cliente cuando la empresa cancela (RN-149) | Fase 16b |
| Crear reservas desde la empresa | Fuera del MVP (`casos-uso.md` §6.1, l.654) |
| Gráficos o métricas en el panel | Fuera de alcance (CU-18) |
| Botón de reactivar en el panel | No se duplica: está en `/app/settings` (duda 8) |
| Cifra de reservas afectadas al crear una excepción | No se hace (duda 9) |
| Actualizar la tabla de estado de `PROJECT_PLAN.md` §7 | No está asignado a ninguna fase |
| Pendientes de la Fase 14 | Fase 17 (`fase-15.md`, duda 10) |
| Menor de `/revisar 15`: el motivo de "no cancelable" resuelto en el DTO de cliente | Fase 19 (`PROJECT_PLAN.md` l.276) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-02, RN-03 | T1, T2 | `PruebasConsultasReservasEmpresa` (aislamiento), `PruebasConsultarReservasEmpresa` (profesional ajeno), `PruebasConsultarPanelEmpresa`; sabotaje |
| RN-16, RN-18 | T1 | `PruebasConsultasReservasEmpresa` (requisitos), `PruebasConsultasCatalogoPublico`; sabotaje |
| RN-19 | T1, T2, T4 | `PruebasConsultasReservasEmpresa`, `PruebasConsultarPanelEmpresa`; manual |
| RN-82, RN-163 | T1, T3 | `PruebasConsultasReservasEmpresa` (descifrado) |
| RN-91, RN-92 | T1 | `PruebasReserva` (`EsCompletable`, estados) |
| RN-94 | T1, T3 | `PruebasReservaEmpresaDto` (`Cancelable`), `PruebasCancelarReservaEmpresa`; manual |
| RN-96 | T1, T3 | `PruebasReserva`, `PruebasReservaEmpresaDto`, `PruebasCompletarReserva`; sabotaje |
| RN-97, RN-98, RN-140, RN-149 | — (ya implementadas) | `PruebasCancelarReservaEmpresa` (Fases 11 y 11b); manual (hueco liberado) |
| RN-134 | T2, T4 | `PruebasConsultasReservasEmpresa` (empresa inactiva); manual |
| RN-171 | T3, T4 | grep manual |
| CU-18 | T1, T2, T4 | `PruebasConsultarPanelEmpresa`, `PruebasPanelEmpresaDto`; manual |
| CU-19 | T1, T2, T3 | `PruebasConsultasReservasEmpresa`, `PruebasConsultarReservasEmpresa`, `PruebasMotorDisponibilidad`; manual |
| CU-20 | T3 | `PruebasCancelarReservaEmpresa`; manual |
| CU-21 | T1, T3 | `PruebasCompletarReserva`, `PruebasReserva`; manual |
| CU-23 (l.470, l.492) | T4 | manual |

## 11. Dudas

Resueltas a petición del desarrollador con la opción recomendada (2026-10-18).

### Duda 1 — Puerto de lectura de las reservas de la empresa

- **Pregunta:** el listado y el panel necesitan nombres de servicio y profesional y datos del cliente proyectados. Ningún contrato existente lo da. ¿Dónde vive la lectura?
- **Opciones:**
  - (a) Un puerto nuevo, `IConsultasReservasEmpresa`, en `Aplicacion/Abstracciones`. Devuelve DTOs y su implementación recibe `IContextoEmpresa` y filtra por `EmpresaActualId` en origen. Se añade como lectura de la familia "repositorios de empresa" en AGENTS §8 y en `arquitectura.md`.
  - (b) Métodos en `IRepositorioReservas` que devuelven entidades, y el caso de uso busca los nombres con otros repositorios.
  - (c) Ampliar `IConsultasDisponibilidad`, que recibe `empresaId` explícito y no usa el contexto.
- **Recomendación y motivo:** (a). Es el precedente de `IConsultasReservasCliente` (Fase 15). Proyecta sin cargar entidades ni descifrar de más (D-22). Y mantiene el filtro en origen por el claim (D-11, D-18). (b) carga entidades enteras y multiplica las consultas; (c) mezcla un puerto sin contexto con datos privados.
- **Bloquea:** T1, T2.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 2 — Nombres que no están en los documentos

- **Pregunta:** los documentos no nombran las clases nuevas.
- **Opciones:**
  - (a) Casos de uso `ConsultarPanelEmpresa` y `ConsultarReservasEmpresa` (con `ConsultarReservasEmpresaComando`), en `CasosUso/Reservas/`. DTOs `ReservaEmpresaDto` y `PanelEmpresaDto`, en `Dtos/Reservas/`. Consulta `ConsultasReservasEmpresa` y `CondicionPublicable`, en `Infraestructura/Consultas/`. Página `Components/App/Reservas.razor`.
  - (b) Otros nombres que elija el desarrollador.
- **Recomendación y motivo:** (a). Siguen la forma verbo más sustantivo (`arquitectura.md` l.409) y el paralelo con `ConsultarMisReservas` y `ReservaClienteDto`. El panel va en `Reservas/` porque su contenido son reservas (una carpeta por agregado, l.199).
- **Bloquea:** T1, T2, T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 3 — Qué son "las próximas" del panel

- **Pregunta:** SPEC §11 dice "reservas de hoy y próximas" sin acotar las próximas.
- **Opciones:**
  - (a) Las confirmadas de los 7 días locales siguientes a hoy.
  - (b) Todas las futuras confirmadas, con un tope de filas.
  - (c) Solo las de mañana.
- **Recomendación y motivo:** (a). Es una semana de trabajo, que es lo que un negocio planifica, y queda acotado por fecha, que es lo que sirve el índice `(empresa_id, inicio_utc)`. (b) depende de un tope arbitrario y (c) se queda corto un viernes.
- **Bloquea:** T2, T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 4 — Conversión de un día local a un rango UTC

- **Pregunta:** el selector de fecha es local y la consulta filtra en UTC. AGENTS §9 exige que la conversión ocurra en un único punto, el motor, cuyo `ALocalUtc` es privado.
- **Opciones:**
  - (a) Un método público estático en `MotorDisponibilidad` que devuelva el `[desdeUtc, hastaUtc)` de un `DateOnly`, apoyado en `ALocalUtc`.
  - (b) Convertir con `TimeZoneInfo` en el caso de uso.
- **Recomendación y motivo:** (a). Mantiene el punto único, igual que `FechasReservables` y `ALocal`. Además, los días de 23 h y 25 h se resuelven bien en un solo sitio con prueba. (b) es una segunda conversión.
- **Bloquea:** T2.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 5 — Requisitos que faltan para publicar sin duplicar RN-16

- **Pregunta:** RN-19 exige mostrar qué requisitos faltan. RN-16 solo existe dentro de `ConsultasCatalogoPublico.Visibles()`, y copiarla abre la puerta a que el panel y el catálogo discrepen.
- **Opciones:**
  - (a) Extraer la combinación de RN-16 a `Infraestructura/Consultas/CondicionPublicable.cs`, que usen `Visibles()` y el panel. El panel añade tres indicadores parciales que siguen los pasos de CU-11 paso 7 (servicio activo, profesional activo, asociación entre activos). El cuarto, horario, es la propia condición completa.
  - (b) Calcularlo en memoria en el caso de uso, cargando servicios, profesionales y horarios.
- **Recomendación y motivo:** (a). RN-16 sigue escrita una sola vez y se calcula en cada consulta (RN-18). Las pruebas existentes del catálogo protegen la extracción. (b) duplica la regla y carga entidades.
- **Bloquea:** T1, T2, T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 6 — "Completable" en un único punto

- **Pregunta:** el listado tiene que saber si ofrece "completar". La condición está hoy en `Reserva.Completar` y en `CompletarReserva`, y ponerla en el DTO sería una tercera copia, que es lo que `/revisar 15` señaló con "cancelable".
- **Opciones:**
  - (a) `Reserva.EsCompletable(EstadoReserva estado, FranjaHoraria franja, DateTime ahoraUtc)`, estático, igual que `EsCancelablePorCliente`. Lo usan `Completar` y `ReservaEmpresaDto`, y `CompletarReserva` conserva su comprobación con mensaje propio (A-09). "Cancelable por la empresa" es `Estado == Confirmada` (RN-94), sin regla temporal.
  - (b) Calcularlo en el DTO.
- **Recomendación y motivo:** (a), por la misma razón que en la Fase 15: una regla, un sitio. Además, lo pueden usar las proyecciones sin entidad (D-22).
- **Bloquea:** T1, T2.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 7 — Datos del cliente en el listado

- **Pregunta:** ¿qué datos personales del cliente muestra `/app/bookings`?
- **Opciones:**
  - (a) Nombre y teléfono, descifrados.
  - (b) Solo el nombre.
- **Recomendación y motivo:** (a). RN-82 y SPEC l.501 dicen que son "la única vía de contacto de la empresa con el cliente". D-22 limita el descifrado a lo que se muestra, y ambos se muestran. No hay vista de detalle de empresa (SPEC §11, 16 vistas), así que van en la fila.
- **Bloquea:** T1, T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 8 — Panel con la empresa inactiva

- **Pregunta:** CU-23 paso 8 dice "el panel muestra el aviso de baja y la acción de reactivar". La Fase 8 ya puso el botón de reactivar en `/app/settings`.
- **Opciones:**
  - (a) Aviso de baja en el panel con un enlace a `/app/settings`, donde está la acción.
  - (b) Un segundo botón de reactivar en el panel, que llame a `ReactivarEmpresa`.
- **Recomendación y motivo:** (a). No duplica una acción con confirmación que ya está probada, y el panel la deja a un clic. `/app/settings` es parte del área de empresa que CU-23 llama "panel".
- **Bloquea:** T4.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Duda 9 — Contar las reservas del día de una excepción

- **Pregunta:** `fase-09.md` l.300 deja para la Fase 16 contar las reservas del día de una excepción "si se decide entonces". La duda 4 de la Fase 9 se resolvió con un aviso estático (RN-46).
- **Opciones:**
  - (a) No hacerlo. Se mantiene el aviso estático y la cancelación manual se hace desde `/app/bookings`, con el selector en ese día.
  - (b) Mostrar la cifra en `/app/schedules`, lo que obliga a tocar `Horarios.razor` y añadir una consulta.
- **Recomendación y motivo:** (a). Ningún documento lo exige, y `/app/bookings` ya enseña esas reservas con un clic. (b) amplía el radio sin regla que lo pida.
- **Bloquea:** ninguna.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-18.

### Decisiones de implementación

- **`IConsultasReservasEmpresa` tiene dos operaciones.** `ObtenerReservasAsync(desdeUtc, hastaUtc, profesionalId?, estado?, ahoraUtc)` y `ObtenerPanelAsync(hoyDesdeUtc, hoyHastaUtc, proximasHastaUtc, ahoraUtc)`. Las próximas van de `hoyHastaUtc` a `proximasHastaUtc` y solo son `Confirmada`; las de hoy, de cualquier estado.
- **`ConsultasReservasEmpresa` filtra antes de proyectar** (empresa, rango semiabierto por inicio, profesional y estado), sin seguimiento y ordenado por inicio. Proyecta `NombreCliente` y `TelefonoCliente` como objetos de valor, así que los conversores los descifran al materializar (RN-163); nunca se filtra ni ordena por ellos.
- **`CondicionPublicable.Aplica(db)`** devuelve la expresión de RN-16. `Visibles()` es `Activa` más esa expresión, y el panel la evalúa sobre la empresa del contexto. Los tres indicadores parciales (servicio activo, profesional activo, asociación entre activos) son consultas `Any` en la misma clase de consulta.
- **`MotorDisponibilidad.DiaLocalEnUtc(fecha)`** apoya en `ALocalUtc` con la medianoche local, que siempre existe en Europe/Madrid (el cambio de hora es a las 2:00 y a las 3:00).
- **`ConsultarPanelEmpresa.DiasProximas = 7`** es la constante de la duda 3; la página la usa para el título.
- **`Reserva.Completar`** usa `EsCompletable` y lanza un único `DominioException`; `CompletarReserva` mantiene sus dos mensajes (A-09).
- **Sin componente nuevo para las listas del panel:** `Inicio.razor` usa un `RenderFragment` privado para no añadir ficheros fuera del radio.
- **`PruebasCompletarReserva` no cambia:** su caso `-45` (fin igual a ahora, con 45 minutos de servicio) ya cubría el extremo que §4 pedía añadir.
- **`/auditar-pruebas 16`** lo lanza el desarrollador, como `/revisar`.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde (424 unitarias, 347 de integración)
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas 16` (obligatoria en esta fase)
- [ ] Commit hecho (mensaje libre)
- [x] Punto de control superado: Control 4, con el guion de `casos-uso.md` §5.3 recorrido a mano (desarrollador, en el navegador, 2026-10-18)
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas: ninguna es de esta fase
