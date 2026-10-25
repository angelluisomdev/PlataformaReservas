# Fase 09 — Horarios y excepciones (dominio + pantalla)

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4 h (SPEC §19) + 0,5 h declaradas por las dudas 5 y 6. El exceso sale del margen de la semana 5 (`PROJECT_PLAN.md` §2) |
| **Semana** | 3 (`PROJECT_PLAN.md` §2), la semana crítica |
| **Cierra** | Ninguno. El Control 3 cierra al final de la Fase 11 |
| **Generada** | 2026-10-10 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

El propietario define el horario semanal de cada profesional en `/app/schedules`, con varios intervalos por día y sin solapes. También marca días completos no disponibles (excepciones). El agregado `Profesional` protege RN-41 a RN-45 y deja preparados los datos que el motor de la Fase 10 necesita.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §9.8 | `Horario`: `DiaSemana`, `HoraInicio`, `HoraFin`, varias filas por profesional y día; `HoraInicio < HoraFin` y sin solapes en el mismo día |
| SPEC §9.9 | `ExcepcionHorario`: `Fecha`, `Motivo string(200)` opcional, solo día completo, índice único `(ProfesionalId, Fecha)` |
| SPEC §11 | Ruta `/app/schedules` ("horario semanal + excepciones"), área de empresa en InteractiveServer |
| D-01 | `InteractiveServer` en el área de empresa |
| D-04 | Horario siempre por profesional (RN-40) |
| M-02 | `Horario` y `ExcepcionHorario` son hijos del agregado `Profesional` (`modelo-dominio.md` §2.1) |
| M-05 | Objetos de valor: se reabre para `MotivoExcepcion` (duda 2) |
| `modelo-dominio.md` §4.6, §4.7, §4.8, §5.2 | Firmas de `AgregarIntervalo`, `EliminarIntervalo`, `MarcarNoDisponible` y `QuitarExcepcion`; `IntervaloHorario.SeSolapaCon` semiabierto |
| `arquitectura.md` §2.3 | `CasosUso/Horarios/`: `AgregarIntervalo`, `EliminarIntervalo`, `MarcarNoDisponible`, `QuitarExcepcion` |
| `arquitectura.md` §9.3 | El agregado se carga con `Include(Horarios)` e `Include(Excepciones)` para modificarlo; la lectura va sin seguimiento por defecto |
| D-16 | Los `Guid` versión 7 se generan en `Aplicacion`: también los de `Horario` y `ExcepcionHorario` (duda 5) |
| RN-03 | Un recurso de otra empresa responde "no encontrado" |
| RN-40 | El horario se define siempre por profesional |
| RN-41 | `HoraInicio` estrictamente menor que `HoraFin` |
| RN-42 | Dos intervalos del mismo profesional y día no se solapan |
| RN-43 | Varios intervalos el mismo día (jornada partida) |
| RN-44 | Una excepción inhabilita el día completo |
| RN-45 | Una sola excepción por profesional y fecha |
| RN-46 | Crear una excepción no cancela las reservas de ese día |
| RN-47 | Modificar o eliminar un horario no afecta a las reservas ya creadas |
| RN-121 | `Horario` y `ExcepcionHorario` se borran físicamente |
| CU-16 | Configurar horario semanal (`casos-uso.md` §3.6), flujos 5a, 5b, 4a y 1a |
| CU-17 | Gestionar excepciones: alta y eliminación, motivo opcional, una por profesional y fecha, no cancela reservas, "la interfaz debe avisar si ya hay reservas ese día" |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `Horario` (`EmpresaId`, `ProfesionalId`, `DiaSemana`, `Intervalo`), sin fábrica | ✔ comprobado |
| `ExcepcionHorario` (`EmpresaId`, `ProfesionalId`, `Fecha`, `Motivo` como `string?`), sin fábrica | ✔ comprobado |
| `Profesional` con `_horarios` y `_excepciones` expuestos como `IReadOnlyCollection` | ✔ comprobado |
| `IntervaloHorario` valida RN-41 en el constructor | ✔ comprobado. **No tiene `SeSolapaCon`** (T1) |
| `ConfiguracionHorario`, `ConfiguracionExcepcionHorario` (`Motivo` de 200, índice único) y relaciones en cascada en `ConfiguracionProfesional` | ✔ comprobado |
| `RepositorioProfesionales.ObtenerParaModificarAsync` incluye `Horarios` y `Excepciones` | ✔ comprobado |
| `Resultado`, `CodigoError`, `IUnidadTrabajo`, `IRelojSistema`, `ListarProfesionales`, `ProfesionalDto`, `Components/App/` con `EsPropietario`, `EscenarioEmpresa` y `AmbitoDeEmpresa` (Fase 8) | ✔ comprobado. Commits `547f191`, `bd71111` y `cdbac62`, con la revisión en APTO |
| Id de `Horario` y `ExcepcionHorario` mapeado como `ValueGeneratedOnAdd` (`ContextoDatosModelSnapshot`) | ✘ Hay que cambiarlo a `ValueGeneratedNever` (duda 5, T2b). Si no, EF toma por existente un hijo nuevo con Id ya relleno y lanza un `UPDATE` que falla |
| Método de lectura sin seguimiento del profesional con sus hijos | ✘ No existe: `ObtenerPorIdAsync` no incluye los hijos y `ObtenerParaModificarAsync` lleva seguimiento (duda 6, T6) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Entidades/Profesional.cs`, `Horario.cs` y `ExcepcionHorario.cs`. `ValueObjects/IntervaloHorario.cs` y el nuevo `ValueObjects/MotivoExcepcion.cs`. `Repositorios/IRepositorioProfesionales.cs` (método `ObtenerConHorarioAsync`) |
| `src/PlataformaReservas.Aplicacion` | sí | `CasosUso/Horarios/` (cinco casos de uso y sus comandos), `Dtos/` (`HorarioProfesionalDto` y sus partes) e `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Persistencia/Conversores/ConversorMotivoExcepcion.cs`; `Persistencia/Configuraciones/ConfiguracionHorario.cs` y `ConfiguracionExcepcionHorario.cs`; `Repositorios/RepositorioProfesionales.cs` (método `ObtenerConHorarioAsync`); `Persistencia/Migraciones/` (una migración solo de snapshot, `IdsHijosProfesionalSinGenerar`, con `Up` y `Down` vacíos) |
| `src/PlataformaReservas.Web` | sí | `Components/App/Horarios.razor` (`/app/schedules`), `Components/App/Inicio.razor` (enlace) y `Components/App/_Imports.razor` (`using`) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `ValueObjects/PruebasIntervaloHorario.cs` (existe), `ValueObjects/PruebasMotivoExcepcion.cs` (nuevo), `Entidades/PruebasProfesional.cs` (existe) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `CasosUso/Horarios/` (un fichero por caso de uso), `Persistencia/Configuraciones/PruebasConfiguracionExcepcionHorario.cs` (nuevo), `Repositorios/PruebasRepositorioProfesionales.cs` (existe) |
| `docs/` | sí | `modelo-dominio.md` (§4.6, M-05 y §5), `arquitectura.md` §2.3 y §9.3, `AGENTS.md` §3.7 y §7 (dudas 1 y 2) y este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se toca ningún otro repositorio, ni el esquema (la migración de T2b no cambia tablas ni columnas), ni el motor de disponibilidad, ni `/app/bookings`, ni los eventos.

## 5. Tareas

### T1 — `IntervaloHorario.SeSolapaCon` (≈ 0,25 h)

- **Ficheros:** modifica `Dominio/ValueObjects/IntervaloHorario.cs` y `PruebasUnitarias/ValueObjects/PruebasIntervaloHorario.cs`.
- **Implementa:** RN-42, con la firma de `modelo-dominio.md` §5.2: `public bool SeSolapaCon(IntervaloHorario otro);`. Es semiabierto: dos intervalos contiguos (09:00–14:00 y 14:00–18:00) no se solapan. Duda 1.
- **Hecho cuando:** solapes parciales, contención e igualdad dan `true`; los contiguos y los disjuntos dan `false`. El resultado es simétrico.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias --filter PruebasIntervaloHorario`.

### T2 — Objeto de valor `MotivoExcepcion` (≈ 0,5 h)

- **Ficheros:**
  - crea `Dominio/ValueObjects/MotivoExcepcion.cs` e `Infraestructura/Persistencia/Conversores/ConversorMotivoExcepcion.cs`;
  - modifica `Dominio/Entidades/ExcepcionHorario.cs` (`Motivo` pasa a `MotivoExcepcion?`) e `Infraestructura/Persistencia/Configuraciones/ConfiguracionExcepcionHorario.cs`;
  - crea las pruebas `PruebasUnitarias/ValueObjects/PruebasMotivoExcepcion.cs` y `PruebasIntegracion/Persistencia/Configuraciones/PruebasConfiguracionExcepcionHorario.cs`.
- **Implementa:** SPEC §9.9 (`Motivo string(200)` opcional) y AGENTS §7. Igual que `DescripcionServicio`: recorta espacios, no admite vacío y como máximo tiene 200 caracteres. Reabre M-05: duda 2.
- **Hecho cuando:**
  - el objeto rechaza el texto vacío y el de 201 caracteres;
  - una excepción con motivo y otra sin motivo se guardan y se leen;
  - `dotnet ef migrations has-pending-model-changes` no detecta cambios, porque la columna es la misma.
- **Verificación:** las dos pruebas más el comando `has-pending-model-changes`.

### T2b — Ids de los hijos sin generación en EF (≈ 0,25 h)

- **Ficheros:**
  - modifica `Infraestructura/Persistencia/Configuraciones/ConfiguracionHorario.cs` y `ConfiguracionExcepcionHorario.cs`: `Property(x => x.Id).ValueGeneratedNever()`;
  - crea la migración `IdsHijosProfesionalSinGenerar` en `Persistencia/Migraciones/` con `dotnet ef migrations add` (AGENTS §11).
- **Implementa:** D-16 (el Id llega desde `Aplicacion`) y la duda 5. Un hijo añadido a la colección de un agregado con seguimiento se guarda como alta.
- **Hecho cuando:**
  - la migración solo cambia el snapshot: `Up` y `Down` vacíos, sin cambios de esquema;
  - `dotnet ef migrations has-pending-model-changes` no detecta cambios después de crearla.
- **Verificación:** el comando anterior y las pruebas de integración de T4 y T5, que guardan hijos nuevos.

### T3 — Comportamiento del agregado `Profesional` (≈ 0,75 h)

- **Ficheros:**
  - modifica `Dominio/Entidades/Profesional.cs`;
  - modifica `Horario.cs` y `ExcepcionHorario.cs`, con una fábrica `internal static Crear(...)` cada una: solo las crea su profesional (§4.7: "nace y muere con las llamadas a su profesional");
  - modifica `PruebasUnitarias/Entidades/PruebasProfesional.cs`.
- **Implementa:** las firmas de `modelo-dominio.md` §4.6, con el Id que llega desde `Aplicacion` (D-16, duda 5) y el motivo como objeto de valor (duda 2):
  ```csharp
  void AgregarIntervalo(Guid horarioId, DayOfWeek dia, IntervaloHorario intervalo);           // valida RN-41 y RN-42
  void EliminarIntervalo(Guid horarioId);                                                     // RN-121
  void MarcarNoDisponible(Guid excepcionId, DateOnly fecha, MotivoExcepcion? motivo);         // valida RN-45
  void QuitarExcepcion(DateOnly fecha);
  ```
  - **Reglas que cubre:** RN-40, RN-41 (por el objeto de valor), RN-42 (con `IntervaloHorario.SeSolapaCon` contra los intervalos de ese día), RN-43, RN-44, RN-45 y RN-121.
  - **Errores:** `EliminarIntervalo` y `QuitarExcepcion` lanzan `DominioException` si no existe lo que se quiere quitar.
  - **Datos de los hijos:** heredan `EmpresaId` y `ProfesionalId` del profesional. Sus fábricas `internal` validan el Id con `Validacion.IdentificadorObligatorio`.
- **Hecho cuando:**
  - dos intervalos el mismo día sin solaparse se aceptan (RN-43);
  - uno solapado lanza;
  - uno igual en otro día se acepta;
  - una segunda excepción en la misma fecha lanza;
  - eliminar y quitar retiran el hijo;
  - quitar lo que no existe lanza;
  - un Id vacío lanza;
  - las cinco pruebas de arquitectura siguen en verde.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias`.

### T4 — Casos de uso `AgregarIntervalo` y `EliminarIntervalo` (≈ 0,5 h)

- **Ficheros:**
  - crea en `Aplicacion/CasosUso/Horarios/`: `AgregarIntervalo.cs`, `AgregarIntervaloComando.cs`, `EliminarIntervalo.cs` y `EliminarIntervaloComando.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs`;
  - crea `PruebasIntegracion/CasosUso/Horarios/PruebasAgregarIntervalo.cs` y `PruebasEliminarIntervalo.cs`.
- **Implementa:** CU-16, RN-41, RN-42, RN-47 y RN-121.
  - **Forma (AGENTS §8 bis):** el comando lleva primitivos (`ProfesionalId`, `DiaSemana`, `HoraInicio`, `HoraFin` / `ProfesionalId`, `HorarioId`). Se carga con `ObtenerParaModificarAsync`. `AgregarIntervalo` genera el Id con `Guid.CreateVersion7()` (D-16).
  - **Desenlaces:**
    - un profesional ajeno o inexistente da `NoEncontrado`;
    - una `DominioException` (RN-41, RN-42) da `Validacion` con el mensaje del dominio;
    - un horario inexistente en `EliminarIntervalo` da `NoEncontrado`.
- **Hecho cuando:**
  - el intervalo queda guardado como alta, sin `DbUpdateConcurrencyException`;
  - el solapado da `Validacion` y no guarda;
  - un profesional de otra empresa da `NoEncontrado`;
  - al eliminar, la fila desaparece de `horarios` (RN-121);
  - una reserva confirmada de ese profesional sigue igual tras eliminar el intervalo (RN-47).
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "FullyQualifiedName~CasosUso.Horarios"`.

### T5 — Casos de uso `MarcarNoDisponible` y `QuitarExcepcion` (≈ 0,5 h)

- **Ficheros:**
  - crea en `Aplicacion/CasosUso/Horarios/`: `MarcarNoDisponible.cs`, `MarcarNoDisponibleComando.cs`, `QuitarExcepcion.cs` y `QuitarExcepcionComando.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs`;
  - crea `PruebasIntegracion/CasosUso/Horarios/PruebasMarcarNoDisponible.cs` y `PruebasQuitarExcepcion.cs`.
- **Implementa:** CU-17, RN-44, RN-45, RN-46 y RN-121.
  - **Comando:** `ProfesionalId`, `Fecha` y `Motivo` (string anulable, que el caso de uso traduce a `MotivoExcepcion`). `MarcarNoDisponible` genera el Id con `Guid.CreateVersion7()` (D-16).
  - **Desenlaces:**
    - RN-45 y un motivo inválido dan `Validacion`;
    - un profesional ajeno da `NoEncontrado`;
    - quitar una excepción inexistente da `NoEncontrado`.
- **Hecho cuando:**
  - la excepción queda guardada con su motivo, como alta, sin `DbUpdateConcurrencyException`;
  - una segunda en la misma fecha da `Validacion`;
  - una reserva confirmada de ese profesional en esa fecha sigue confirmada (RN-46);
  - al quitar, la fila desaparece de `excepciones_horario` (RN-121).
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "FullyQualifiedName~CasosUso.Horarios"`.

### T6 — Caso de uso `ObtenerHorarioProfesional` (≈ 0,75 h)

- **Ficheros:**
  - crea `Aplicacion/CasosUso/Horarios/ObtenerHorarioProfesional.cs` y `ObtenerHorarioProfesionalComando.cs`;
  - crea `Aplicacion/Dtos/HorarioProfesionalDto.cs`, `IntervaloHorarioDto.cs` y `ExcepcionHorarioDto.cs`;
  - modifica `Aplicacion/InyeccionDependencias.cs`;
  - modifica `Dominio/Repositorios/IRepositorioProfesionales.cs` e `Infraestructura/Repositorios/RepositorioProfesionales.cs`: `Task<Profesional?> ObtenerConHorarioAsync(Guid id, CancellationToken ct)`, sin seguimiento, con `Include` de `Horarios` y `Excepciones` y filtrado por `EmpresaActualId` en la consulta (AGENTS §8, duda 6);
  - crea `PruebasIntegracion/CasosUso/Horarios/PruebasObtenerHorarioProfesional.cs` y modifica `PruebasIntegracion/Repositorios/PruebasRepositorioProfesionales.cs`.
- **Implementa:** CU-16 paso 3 ("el sistema muestra los intervalos existentes de cada día") y CU-17. Duda 3.
  - **Qué devuelve:** los intervalos ordenados por día y hora de inicio, y las excepciones ordenadas por fecha. Solo DTOs (AGENTS §8 bis).
  - **Profesional ajeno:** `NoEncontrado`.
- **Hecho cuando:**
  - devuelve los intervalos y las excepciones del profesional en orden;
  - un profesional de otra empresa da `NoEncontrado`;
  - `ObtenerConHorarioAsync` trae los hijos y devuelve `null` para un profesional de otra empresa.
- **Verificación:** las dos pruebas de integración.

### T7 — Pantalla `/app/schedules` (≈ 0,75 h)

- **Ficheros:**
  - crea `Web/Components/App/Horarios.razor`;
  - modifica `Web/Components/App/Inicio.razor` (enlace) y `Web/Components/App/_Imports.razor` (`using` de `CasosUso.Horarios`).
- **Implementa:** CU-16 y CU-17, con `@page "/app/schedules"` y `@rendermode InteractiveServer` (D-01).
  - **Profesional:** un selector con los de `ListarProfesionales`.
  - **Semana:** la semana completa, día a día, con sus intervalos y un botón para eliminar cada uno.
  - **Formulario de intervalo:** día, hora de inicio y hora de fin.
  - **Excepciones:** la lista, con un botón para quitar cada una, y un formulario con fecha y motivo opcional.
  - **Aviso estático (duda 4):** "Marcar un día como no disponible no cancela las reservas que ya existan ese día; si procede, cancélalas a mano" (RN-46).
  - **Sin profesionales:** un enlace a `/app/staff` (CU-16 1a).
  - **Mensajes de error:** los del `Resultado`, que incluyen el del solape (CU-16 5b).
  - **Sin lógica de negocio en la página (AGENTS §6):** un caso de uso por acción, nunca dos en paralelo (R-1).
- **Hecho cuando:** con la aplicación arrancada, el propietario consigue todo esto:
  - crea dos intervalos el mismo día;
  - ve rechazado uno solapado y uno con inicio posterior al fin;
  - elimina un intervalo;
  - marca y quita una excepción;
  - ve rechazada una segunda excepción el mismo día.
- **Verificación:** prueba manual en el navegador. Un `GET /app/schedules` anónimo redirige a `/login`.

### T8 — Documentación (≈ 0,25 h)

- **Ficheros:** modifica `docs/modelo-dominio.md`, `docs/arquitectura.md` y `AGENTS.md`.
- **Implementa:** registrar las dudas resueltas.
  - **`modelo-dominio.md`:**
    - §4.6, las firmas de `AgregarIntervalo` y `MarcarNoDisponible` con el Id y `MotivoExcepcion?`;
    - §4.8, `Motivo` como objeto de valor;
    - M-05 y §5, 22 objetos de valor.
  - **`arquitectura.md` §2.3:** `ObtenerHorarioProfesional` y los DTO nuevos.
  - **`arquitectura.md` §9.3:** `ObtenerConHorarioAsync` como lectura sin seguimiento con hijos, y `ValueGeneratedNever` en los Id de los hijos.
  - **`AGENTS.md`:**
    - §3.7, que nombre `IntervaloHorario.SeSolapaCon` junto a `FranjaHoraria.SeSolapaCon`;
    - §7, 22 objetos de valor con `MotivoExcepcion`.
- **Hecho cuando:** ningún documento contradice el código.
- **Verificación:** `grep` de `MarcarNoDisponible`, `Veintiún`/`Veintidós` y `SeSolapaCon` en `docs/` y `AGENTS.md`.

**Suma: 4,5 h de 4 h presupuestadas, más 0,5 h declaradas por las dudas 5 y 6** (T2b y la parte de repositorio de T6). No queda margen: si T7 se alarga, se para y se avisa. No se recorta de la Fase 10 (`PROJECT_PLAN.md` §2: la semana 3 no se recorta). El exceso sale del margen de la semana 5.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Solape semiabierto: parcial, contención, igualdad, contiguos y disjuntos | U | `ValueObjects/PruebasIntervaloHorario.cs` | RN-42 | T1 |
| Motivo vacío o de 201 caracteres se rechaza; se recortan espacios | U | `ValueObjects/PruebasMotivoExcepcion.cs` | SPEC §9.9 | T2 |
| Excepción con y sin motivo ida y vuelta por la base de datos | I | `Persistencia/Configuraciones/PruebasConfiguracionExcepcionHorario.cs` | SPEC §9.9 | T2 |
| Un Id vacío en `AgregarIntervalo` o `MarcarNoDisponible` lanza | U | `Entidades/PruebasProfesional.cs` | D-16 | T3 |
| Dos intervalos el mismo día sin solapar se aceptan | U | `Entidades/PruebasProfesional.cs` | RN-43 | T3 |
| Intervalo solapado el mismo día lanza; el mismo en otro día se acepta | U | `Entidades/PruebasProfesional.cs` | RN-42 | T3 |
| Segunda excepción en la misma fecha lanza | U | `Entidades/PruebasProfesional.cs` | RN-45 | T3 |
| Eliminar o quitar lo que no existe lanza; lo que existe se retira | U | `Entidades/PruebasProfesional.cs` | RN-121 | T3 |
| Los hijos heredan `EmpresaId` y `ProfesionalId` | U | `Entidades/PruebasProfesional.cs` | RN-40 | T3 |
| Intervalo guardado como alta (sin error de concurrencia); solapado o invertido da `Validacion`; profesional ajeno da `NoEncontrado` | I | `CasosUso/Horarios/PruebasAgregarIntervalo.cs` | RN-41, RN-42, RN-03 | T4 |
| Eliminar borra la fila; una reserva confirmada sigue igual | I | `CasosUso/Horarios/PruebasEliminarIntervalo.cs` | RN-121, RN-47 | T4 |
| Excepción guardada; segunda en la misma fecha da `Validacion`; una reserva confirmada ese día sigue confirmada | I | `CasosUso/Horarios/PruebasMarcarNoDisponible.cs` | RN-44, RN-45, RN-46 | T5 |
| Quitar borra la fila; inexistente da `NoEncontrado` | I | `CasosUso/Horarios/PruebasQuitarExcepcion.cs` | RN-121 | T5 |
| Devuelve intervalos y excepciones ordenados; profesional ajeno da `NoEncontrado` | I | `CasosUso/Horarios/PruebasObtenerHorarioProfesional.cs` | CU-16, RN-03 | T6 |
| `ObtenerConHorarioAsync` trae los hijos y devuelve `null` para otra empresa | I | `Repositorios/PruebasRepositorioProfesionales.cs` | RN-02, RN-03 | T6 |
| Un minuto (00:00–00:01) y el día completo (00:00–23:59) son válidos | U | `ValueObjects/PruebasIntervaloHorario.cs` | RN-41 | T1 |
| La longitud máxima del motivo se mide tras recortar | U | `ValueObjects/PruebasMotivoExcepcion.cs` | SPEC §9.9 | T2 |
| Contiguos y el que encaja en el hueco se aceptan; el solape se busca contra todos los intervalos del día | U | `Entidades/PruebasProfesional.cs` | RN-42, RN-43 | T3 |
| Eliminar retira solo ese intervalo y libera el hueco; quitar retira solo esa fecha y permite volver a marcarla; fechas distintas admiten su excepción | U | `Entidades/PruebasProfesional.cs` | RN-45, RN-121 | T3 |
| Horario ida y vuelta: Id asignado por `Aplicacion`, día y horas aplanadas | I | `Persistencia/Configuraciones/PruebasConfiguracionHorario.cs` | SPEC §9.8, D-16 | T2b |
| Profesional inexistente da `NoEncontrado` (agregar, marcar, obtener) | I | `CasosUso/Horarios/PruebasAgregarIntervalo.cs`, `PruebasMarcarNoDisponible.cs`, `PruebasObtenerHorarioProfesional.cs` | RN-03 | T4, T5, T6 |
| Con dos intervalos borra solo el indicado; un horario de otro profesional de la misma empresa da `NoEncontrado` y queda intacto | I | `CasosUso/Horarios/PruebasEliminarIntervalo.cs` | RN-121 | T4 |
| Un motivo en blanco se guarda como sin motivo | I | `CasosUso/Horarios/PruebasMarcarNoDisponible.cs` | SPEC §9.9 | T5 |
| Con dos excepciones quita solo la de esa fecha | I | `CasosUso/Horarios/PruebasQuitarExcepcion.cs` | RN-121 | T5 |
| Un profesional sin horario devuelve las listas vacías | I | `CasosUso/Horarios/PruebasObtenerHorarioProfesional.cs` | CU-16 | T6 |
| `ObtenerConHorarioAsync` de un Id inexistente devuelve `null` | I | `Repositorios/PruebasRepositorioProfesionales.cs` | RN-03 | T6 |
| Las cinco pruebas de forma siguen en verde | A | `Arquitectura/PruebasArquitectura.cs` (sin cambios) | AGENTS §7 | T3 |

## 7. Criterios de aceptación

- [x] Se pueden crear dos intervalos el mismo día sin solaparse (CU-16, RN-43). Evidencia: `PruebasProfesional` y `PruebasAgregarIntervalo.Guarda_dos_intervalos_el_mismo_dia_como_altas`.
- [x] Un intervalo solapado se rechaza (CU-16, RN-42) en el dominio, no solo en la pantalla. Evidencia: `PruebasIntervaloHorario`, `PruebasProfesional` y `PruebasAgregarIntervalo`.
- [x] Un intervalo con inicio posterior o igual al fin se rechaza (CU-16, RN-41). Evidencia: `PruebasIntervaloHorario` y `PruebasAgregarIntervalo` (invertido da `Validacion`).
- [x] Eliminar un horario no cancela reservas existentes (CU-16, RN-47) y lo borra físicamente (RN-121). Evidencia: `PruebasEliminarIntervalo`.
- [x] Solo hay una excepción por profesional y fecha (RN-45), y crearla no cancela las reservas de ese día (RN-46). Evidencia: `PruebasProfesional` y `PruebasMarcarNoDisponible`.
- [x] Ningún método público de `Profesional` recibe `string`: el motivo entra como `MotivoExcepcion` (AGENTS §7, quinta prueba de arquitectura). Evidencia: `PruebasArquitectura` en verde.
- [x] La regla de solape de horarios existe una sola vez, en `IntervaloHorario.SeSolapaCon`, y `Profesional` la reutiliza. Evidencia: `Profesional.AgregarIntervalo`.
- [x] Un recurso de otra empresa devuelve `NoEncontrado` en los cinco casos de uso (RN-03). Evidencia: una prueba de profesional ajeno en cada `Pruebas<CasoDeUso>` de `CasosUso/Horarios/`.
- [x] `/app/schedules` funciona en InteractiveServer para el propietario y redirige a `/login` sin sesión. Evidencia: smoke del 2026-10-10 (200 con el marcador interactivo de Blazor; anónimo → `/login?ReturnUrl=%2Fapp%2Fschedules`). Prueba manual de T7 en el navegador, hecha por el desarrollador el 2026-10-10: OK (añadir, rechazar y eliminar intervalos; marcar y quitar excepciones).
- [x] Una sola migración, solo de snapshot (`IdsHijosProfesionalSinGenerar`, sin cambios de esquema), y después `has-pending-model-changes` no detecta cambios. Evidencia: `Up` y `Down` vacíos; el comando no detecta cambios.
- [x] Los hijos nuevos se guardan como alta con el Id generado en `Aplicacion` (D-16), sin `DbUpdateConcurrencyException`. Evidencia: `PruebasAgregarIntervalo` y `PruebasMarcarNoDisponible`. Sabotaje: sin `ValueGeneratedNever` en `ConfiguracionHorario`, `PruebasAgregarIntervalo` falla con esa excepción.
- [x] La lectura del horario va sin seguimiento y filtra por empresa en la consulta (`arquitectura.md` §9.3, AGENTS §8). Evidencia: `PruebasRepositorioProfesionales.ObtenerConHorario_trae_los_hijos_y_no_encuentra_uno_de_otra_empresa` (sin entradas en el `ChangeTracker`).
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: 2026-10-10, 234 pruebas unitarias y 139 de integración (tras completar los casos borde). Sabotajes: comparar solo con el primer intervalo del día hace fallar `AgregarIntervalo_compara_con_todos_los_intervalos_del_dia`; `<=` en `SeSolapaCon` hace fallar las pruebas de contiguos.

**Esta fase no cierra ningún punto de control.** El criterio de CU-16 "al guardar un horario, los huecos aparecen en CU-03" depende del motor y del flujo público, y se comprueba en las fases 10 y 13.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | `"A second operation was started on this context"` al cambiar de profesional mientras se guarda un intervalo | `Horarios.razor` ejecuta un caso de uso por acción, con `await` y sin `Task.WhenAll` ni `async void` |
| R-2 | No aplica: horarios y excepciones son `TimeOnly`/`DateOnly`, sin `timestamptz` | — |

**Riesgos propios de la fase, sin código R:**

- `public ICollection<Horario> Horarios { get; set; }` anula RN-42 (`modelo-dominio.md` §9). Lo vigilan las pruebas de arquitectura y la revisión.
- Un hijo nuevo con el Id ya relleno, añadido a la colección de un agregado con seguimiento, se toma por existente si su clave está como `ValueGeneratedOnAdd`: EF lanza un `UPDATE` y falla con `DbUpdateConcurrencyException`. Lo mitiga T2b (`ValueGeneratedNever`), y lo prueban T4 y T5.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Generación de huecos a partir del horario y las excepciones; conversión local ↔ UTC | Fase 10 (motor) |
| Criterio "al guardar un horario, los huecos aparecen en CU-03" | Fases 10 y 13 |
| Publicabilidad por existir horario (RN-16 a RN-19) y aviso del panel | Fases 13 y 16 |
| Horario resumido en la ficha pública (RN-115) | Fase 13 |
| Contar las reservas del día de una excepción para avisar con cifra (duda 4) | Fase 16 (`/app/bookings`), si se decide entonces. *Corregido en la Fase 15, duda 9* |
| Cancelación manual de reservas afectadas por una excepción | Fase 16 (CU-20) |
| Correos | Fase 16b |
| Horario personalizado por día (excepciones con horas) | Trabajo futuro (SPEC §9.9) |
| Prueba de `CancelarPorEmpresa` sobre una reserva completada (aplazada desde la revisión de la Fase 8) | Fase 11, con `Reserva.Completar` |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-40 | T3 | `PruebasProfesional` (los hijos heredan empresa y profesional) |
| RN-41 | T3, T4 | `PruebasIntervaloHorario` (existente), `PruebasAgregarIntervalo` |
| RN-42 | T1, T3, T4 | `PruebasIntervaloHorario`, `PruebasProfesional`, `PruebasAgregarIntervalo` |
| RN-43 | T3 | `PruebasProfesional` |
| RN-44 | T3, T5 | `PruebasMarcarNoDisponible` |
| RN-45 | T3, T5 | `PruebasProfesional`, `PruebasMarcarNoDisponible` |
| RN-46 | T5, T7 | `PruebasMarcarNoDisponible`; aviso estático en la pantalla |
| RN-47 | T4 | `PruebasEliminarIntervalo` |
| RN-121 | T3, T4, T5 | `PruebasProfesional`, `PruebasEliminarIntervalo`, `PruebasQuitarExcepcion` |
| RN-03 | T4, T5, T6 | `PruebasAgregarIntervalo`, `PruebasObtenerHorarioProfesional`, `PruebasRepositorioProfesionales` |
| D-16 | T2b, T3, T4, T5 | `PruebasProfesional` (Id vacío), `PruebasAgregarIntervalo`, `PruebasMarcarNoDisponible` (alta sin error de concurrencia) |
| CU-16 | T4, T6, T7 | `PruebasAgregarIntervalo`, `PruebasEliminarIntervalo`, `PruebasObtenerHorarioProfesional`; prueba manual |
| CU-17 | T5, T6, T7 | `PruebasMarcarNoDisponible`, `PruebasQuitarExcepcion`; prueba manual |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-10-10 con la opción recomendada. Las dudas 5 y 6 salieron al validar el documento contra el repositorio, después de cerrar la Fase 8.

### Duda 1 — ¿Dónde vive la regla de solape de `IntervaloHorario`? *(resuelta: a)*

- **Pregunta:** `modelo-dominio.md` §5.2 y §9 prevén `IntervaloHorario.SeSolapaCon` (RN-42), pero AGENTS §3.7 dice que la regla de solapamiento existe "una vez, en `FranjaHoraria.SeSolapaCon`".
- **Opciones:**
  - (a) Añadir `IntervaloHorario.SeSolapaCon`, para horas locales del horario, y ajustar AGENTS §3.7 para que nombre las dos: cada una en su tipo, sin duplicar ninguna.
  - (b) Convertir los intervalos en `FranjaHoraria` para compararlos, inventando una fecha y un UTC.
- **Recomendación y motivo:** (a). Es la firma documentada, y (b) mezclaría la hora local con UTC fuera del motor (AGENTS §9).
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T1, T3.

### Duda 2 — ¿Objeto de valor para `Motivo`? *(resuelta: a)*

- **Pregunta:** la firma `MarcarNoDisponible(DateOnly fecha, string? motivo)` de `modelo-dominio.md` §4.6 incumple la quinta prueba de arquitectura y AGENTS §7, porque `Motivo` tiene una regla de validación (200 caracteres, SPEC §9.9).
- **Opciones:**
  - (a) Reabrir M-05 con `MotivoExcepcion` (22 objetos de valor), con conversor y sin migración.
  - (b) Quitar el motivo.
- **Recomendación y motivo:** (a). Es la misma solución que se aplicó en la Fase 8 a `DescripcionServicio`, y CU-17 pide el motivo.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T2, T3, T5.

### Duda 3 — ¿Cómo lee la pantalla el horario? *(resuelta: a)*

- **Pregunta:** `arquitectura.md` §2.3 solo nombra los cuatro casos de uso de escritura de `Horarios/`. CU-16 paso 3 necesita mostrar los intervalos existentes.
- **Opciones:**
  - (a) Un caso de uso nuevo, `ObtenerHorarioProfesional`, que devuelva `HorarioProfesionalDto` (intervalos y excepciones).
  - (b) Ampliar `ProfesionalDto` y `ListarProfesionales` con los horarios.
- **Recomendación y motivo:** (a). No carga los hijos en todos los listados de profesionales, y sigue el patrón de `ObtenerConfiguracionEmpresa`.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T6, T7.

### Duda 4 — Aviso de reservas existentes al crear una excepción (CU-17) *(resuelta: c)*

- **Pregunta:** CU-17 dice que "la interfaz debe avisar si ya hay reservas ese día". Contarlas exige convertir un día local a un rango UTC, y AGENTS §9 solo permite esa conversión dentro del motor (Fase 10).
- **Opciones:**
  - (a) Aplazar el aviso hasta la Fase 15.
  - (b) Convertir en un caso de uso de esta fase, lo que rompe AGENTS §9.
  - (c) Un aviso estático en la pantalla, sin contar nada: crear la excepción no cancela las reservas de ese día y la empresa debe cancelarlas a mano si procede.
- **Recomendación y motivo:** (c). Cumple el propósito de RN-46 sin romper §9 ni anticipar el motor.
- **Resolución:** (c). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T7.

### Duda 5 — Id de `Horario` y `ExcepcionHorario` *(resuelta: a)*

- **Pregunta:** D-16 dice que los `Guid` v7 se generan en `Aplicacion`, pero la firma de §4.6 `AgregarIntervalo(DayOfWeek, IntervaloHorario)` no recibe ningún Id. Además, el Id de los hijos está mapeado como `ValueGeneratedOnAdd`, así que un hijo nuevo con Id ya relleno, añadido a la colección de un agregado con seguimiento, EF lo toma por existente y el guardado falla.
- **Opciones:**
  - (a) El caso de uso pasa `Guid.CreateVersion7()` a `AgregarIntervalo(Guid horarioId, …)` y a `MarcarNoDisponible(Guid excepcionId, …)`, con `ValueGeneratedNever()` en las dos configuraciones y una migración solo de snapshot.
  - (b) `Profesional` genera el Id. Se aparta de D-16 y necesita igualmente `ValueGeneratedNever`.
- **Recomendación y motivo:** (a). Cumple D-16 y deja el Id bajo control de `Aplicacion`, como en las raíces.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T2b, T3, T4, T5.

### Duda 6 — Lectura del profesional con sus hijos *(resuelta: a)*

- **Pregunta:** `ObtenerHorarioProfesional` necesita el profesional con sus horarios y excepciones. `ObtenerPorIdAsync` no incluye los hijos y `ObtenerParaModificarAsync` lleva seguimiento, mientras que `arquitectura.md` §9.3 pide que la lectura vaya sin seguimiento. El radio original no tocaba repositorios.
- **Opciones:**
  - (a) Un método nuevo, `IRepositorioProfesionales.ObtenerConHorarioAsync(Guid id, CancellationToken ct)`: sin seguimiento, con `Include` de los hijos y filtrado por empresa en la consulta.
  - (b) Reutilizar `ObtenerParaModificarAsync`, con seguimiento.
- **Recomendación y motivo:** (a). Respeta §9.3 y no deja entidades rastreadas en el circuito.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada, junto con el presupuesto declarado de 4,5 h.
- **Bloqueaba:** T6.

### Decisiones tomadas durante la implementación (2026-10-10)

- **`PruebasAislamientoEmpresas.cs` (transversal, fuera del radio de §4):** se cambia una comparación, `x.Motivo == "Vacaciones"` por `x.Motivo == new MotivoExcepcion("Vacaciones")`. Es necesario porque T2 cambia el tipo de `Motivo` y, sin ello, no compila. No cambia la lógica de la prueba.
- **Mensaje del solape (CU-16 5b):** `Profesional.AgregarIntervalo` dice con qué intervalo choca ("se solapa con el de 09:00 a 14:00 del mismo día").
- **Valor devuelto:** `AgregarIntervalo` devuelve `Resultado<Guid>` con el Id del horario creado; los demás casos de uso devuelven `Resultado`.
- **`EliminarIntervalo` y `QuitarExcepcion`:** comprueban antes de llamar al dominio que el hijo existe, y si no, devuelven `NoEncontrado`, el mismo patrón que `DesasociarServicio` en la Fase 8.
- **Limitación conocida, sin prueba:** dos `AgregarIntervalo` simultáneos que se solapan podrían guardarse los dos, porque RN-42 la garantiza el agregado y no hay restricción en base de datos sobre `horarios`. Los documentos no piden protegerlo (la restricción `EXCLUDE` es solo de `reservas`).
- **Cobertura completada (2026-10-10):** 17 pruebas de casos borde, recogidas en §6.
- **Correcciones tras la revisión del 2026-10-10 21:25** (`fase-09-revision.md`):
  - El formulario de día no disponible ya no propone la fecha de hoy calculada desde UTC (AGENTS §9): la fecha empieza vacía y es obligatoria.
  - La migración `IdsHijosProfesionalSinGenerar` absorbe, además de `ValueGeneratedNever`, un desfase del snapshot heredado del commit `0ec9dd0`: los renombrados `DuracionMinutos` → `Duracion` (`Servicio`) y `DuracionAplicadaMinutos` → `DuracionAplicada` (`Reserva`), que se hicieron sin migración. Las columnas no cambian (`duracion_minutos` y `duracion_aplicada_minutos` siguen igual) y `Up` y `Down` están vacíos.
  - `ObtenerHorarioProfesional` ordena de lunes a domingo, como la pantalla. La prueba incluye el domingo.
  - Si `ListarProfesionales` falla, la pantalla muestra el error.
  - `arquitectura.md` §9.3 recoge `ValueGeneratedNever`.
  - `PruebasAislamientoEmpresas.cs` se queda como está, por decisión del desarrollador.
- **La pantalla muestra todos los profesionales**, con "(desactivado)" en los inactivos, como `/app/staff`. Los documentos no dicen nada sobre el horario de un profesional desactivado.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [x] `/revisar` sin hallazgos bloqueantes *(2026-10-10 21:34: `APTO` en la segunda ronda, con 234 unitarias y 139 de integración. Su único menor, en `modelo-dominio.md` §12, se corrigió el 2026-10-12; ver el seguimiento en `fase-09-revision.md`)*
- [x] Commit hecho *(`6b1ac13`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Punto de control superado, si esta fase cierra alguno (no cierra ninguno) *(no aplica)*
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna) *(no aplica)*
