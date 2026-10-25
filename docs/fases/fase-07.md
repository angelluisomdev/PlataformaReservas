# Fase 07 — Multi-tenancy: `IContextoEmpresa`, repositorios, pruebas de aislamiento

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 5 h (SPEC §19) |
| **Semana** | 2 (`PROJECT_PLAN.md` §2) |
| **Cierra** | Control 1 (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-04 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar existen tres piezas:

- `IContextoEmpresa`, que lee la empresa del claim `empresa_id`;
- los cinco repositorios de empresa, que filtran por `EmpresaActualId` en la propia consulta;
- las 5 pruebas de aislamiento, que demuestran que un usuario de la empresa A recibe "no encontrado" al pedir por identificador un recurso de la empresa B.

Con esto se cierra el Control 1.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 | Contenido y 5 h |
| SPEC §10, §10.1 | Base de datos compartida, con `EmpresaId` como discriminador. Las entidades que lo llevan son `Servicio`, `Profesional`, `Horario`, `ExcepcionHorario`, `Reserva` y `MiembroEmpresa`. Regla crítica: una empresa nunca lee, modifica, elimina ni enumera lo de otra |
| SPEC §10.2 | Sin `HasQueryFilter` ni `IgnoreQueryFilters()` (AGENTS §3.8) |
| SPEC §10.3 | La firma de `IContextoEmpresa`, copiada literal: `Guid EmpresaActualId { get; }   // lanza si no hay empresa resuelta` y `bool TieneEmpresa { get; }`. Las dos familias de acceso a datos. Defensa en profundidad. "No encontrado" en lugar de "prohibido" |
| SPEC §10.4 | Una prueba por tipo de recurso: `Servicio`, `Profesional`, `Horario`, `ExcepcionHorario` y `Reserva` |
| SPEC §16.2 | "Aislamiento entre empresas (5 pruebas, §10.4)" |
| D-09 | Interfaces de repositorio en `Aplicacion` e implementaciones con EF Core en `Infraestructura` |
| D-11 | `IContextoEmpresa`, repositorios filtrados en origen y validación explícita de `EmpresaId`. Sin filtros globales |
| D-18 · A-03 | La empresa sale del claim `empresa_id`, sin consultar la base en cada operación |
| A-04 · A-05 | `ContextoDatos` con ámbito *scoped*. `NoTracking` por defecto, con `AsTracking()` solo en `ObtenerParaModificarAsync` |
| `modelo-dominio.md` §8 | Un repositorio por raíz que se escribe: `IRepositorioEmpresas`, `IRepositorioServicios`, `IRepositorioProfesionales`, `IRepositorioReservas` e `IRepositorioMiembros`. **No hay `IRepositorioHorarios`** |
| `modelo-dominio.md` §6 | El aislamiento lo garantizan los repositorios, que filtran en origen, más la validación explícita |
| `arquitectura.md` §2.3 | Las abstracciones van en `Aplicacion/Abstracciones/`, los repositorios en `Infraestructura/Repositorios/` y `ContextoEmpresa` en `Infraestructura/Identidad/` |
| `arquitectura.md` §8.2 | `ContextoEmpresa` lee el claim. `EmpresaActualId` lanza en lugar de devolver `Guid.Empty`. `FabricaClaimsUsuario` usa `_miembros.ObtenerPorUsuarioAsync` |
| `arquitectura.md` §9.3 | `ObtenerParaModificarAsync` con `AsTracking()` y filtro por `EmpresaActualId`. `Profesional` se carga con `Include(Horarios)` e `Include(Excepciones)` |
| `arquitectura.md` §10 | Repositorios e `IContextoEmpresa` con ámbito *scoped*. "Ningún servicio con estado es singleton" |
| `fase-06.md` §11, duda 9 | `FabricaClaimsUsuario` pasa de la consulta directa a `IRepositorioMiembros` |
| RN-01 | Todo recurso de negocio pertenece a exactamente una empresa |
| RN-02 ⚑ | Solo se accede a los recursos de la empresa a la que se pertenece |
| RN-03 ⚑ | Un recurso ajeno pedido por identificador da "no encontrado" |
| RN-04 | El catálogo público no recibe `IContextoEmpresa`. Aquí solo se comprueba que ningún repositorio de empresa lo sustituye |
| RN-06 | Como máximo una pertenencia por usuario. El índice único ya existe desde la Fase 5 |
| CU | Ninguno directo. Los CU-11 a CU-21, que usarán estos repositorios, son de las fases 8 a 16 |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `Aplicacion` referencia a `Dominio`, e `Infraestructura` referencia a `Aplicacion` | ✔ comprobado |
| `ContextoDatos` con los `DbSet` de las seis entidades con `EmpresaId`, y migraciones aplicadas | ✔ comprobado |
| `FabricaClaimsUsuario.ClaimEmpresa` (`"empresa_id"`) se emite al iniciar sesión | ✔ comprobado (`PruebasFabricaClaimsUsuario`) |
| `RevalidadorIdentidad` registrado como `AuthenticationStateProvider` en `Web/Program.cs` | ✔ comprobado |
| Política `EsPropietario` con rol y claim | ✔ comprobado (`PruebasInyeccionDependencias`) |
| `BaseDatosFixture`, `DominioPrueba` y `UsuariosPrueba` | ✔ comprobado |
| `Multiempresa/PruebasAislamientoEmpresas.cs` en la lista cerrada de pruebas transversales (AGENTS §11 bis) | ✔ comprobado |
| Fábricas `Crear` de `Empresa`, `Servicio`, `Profesional`, `Reserva` y `MiembroEmpresa` | ✔ comprobado |
| Fábrica o método de dominio para crear `Horario` y `ExcepcionHorario` | ✘ ausente, como se esperaba: son de la Fase 9. Ver duda 5 |
| `Aplicacion/Abstracciones/` | ✘ ausente: `Aplicacion` está vacío. La carpeta la crea T2 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | Nada |
| `src/PlataformaReservas.Aplicacion` | sí | Solo `Abstracciones/`: `IContextoEmpresa` y las cinco interfaces `IRepositorio*` |
| `src/PlataformaReservas.Infraestructura` | sí | `Identidad/ContextoEmpresa.cs` (nuevo), `Identidad/FabricaClaimsUsuario.cs`, `Repositorios/` (cinco clases nuevas) e `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Web` | no | `AuthenticationStateProvider` ya está registrado. No se crean pantallas |
| `tests/PlataformaReservas.PruebasUnitarias` | no | Las cinco pruebas de arquitectura siguen como están y tienen que pasar |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Identidad/PruebasContextoEmpresa.cs` (nuevo), `Multiempresa/PruebasAislamientoEmpresas.cs` (nuevo), `Infraestructura/ContextoEmpresaFijo.cs` (doble nuevo) y `PruebasInyeccionDependencias.cs` |
| `docs/` | sí | `arquitectura.md` §8.2 (la excepción de `ObtenerPorUsuarioAsync`, duda 2) y este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.**

## 5. Tareas

### T1 — `IContextoEmpresa` y `ContextoEmpresa` (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/Abstracciones/IContextoEmpresa.cs`, `Infraestructura/Identidad/ContextoEmpresa.cs` y `PruebasIntegracion/Identidad/PruebasContextoEmpresa.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs`.
- **Implementa:** SPEC §10.3, D-11, A-03, `arquitectura.md` §8.2 y §10, RN-02.
- **Hecho cuando:**
  - La interfaz es la de SPEC §10.3, literal.
  - `ContextoEmpresa` lee el claim `FabricaClaimsUsuario.ClaimEmpresa` del principal, de la fuente que decida la duda 6.
  - `EmpresaActualId` lanza `InvalidOperationException` si no hay claim o el claim no es un `Guid` válido. Nunca devuelve `Guid.Empty`.
  - `ContextoEmpresa` se registra con ámbito *scoped*.
- **Verificación:** `PruebasContextoEmpresa`:
  - con el claim, devuelve el `Guid` y `TieneEmpresa` vale `true`;
  - sin el claim, `TieneEmpresa` vale `false` y `EmpresaActualId` lanza;
  - con un claim mal formado, lanza.

### T2 — Interfaces de repositorio (≈ 0,5 h)

- **Ficheros:** crea en `Aplicacion/Abstracciones/` los ficheros `IRepositorioEmpresas.cs`, `IRepositorioServicios.cs`, `IRepositorioProfesionales.cs`, `IRepositorioReservas.cs` e `IRepositorioMiembros.cs`.
- **Implementa:** D-09, SPEC §10.3, `modelo-dominio.md` §8, `arquitectura.md` §9.3.
- **Hecho cuando:**
  - Existen los métodos que fije la duda 1, y ninguno más.
  - Devuelven entidades de dominio, o `null` cuando no se encuentra (SPEC §10.3).
  - Ninguna firma expone `IQueryable` ni tipos de EF Core (`arquitectura.md` §16).
  - No hay `IRepositorioHorarios`.
- **Verificación:** `dotnet build` sin avisos. La prueba de arquitectura que comprueba que `Dominio` no referencia EF Core sigue en verde.

### T3 — `RepositorioEmpresas`, `RepositorioMiembros` y `FabricaClaimsUsuario` (≈ 0,75 h)

- **Ficheros:** crea `Infraestructura/Repositorios/RepositorioEmpresas.cs` y `RepositorioMiembros.cs`. Modifica `Infraestructura/Identidad/FabricaClaimsUsuario.cs`.
- **Implementa:** RN-02, RN-03, RN-06, A-03, `arquitectura.md` §8.2, `fase-06.md` duda 9.
- **Hecho cuando:**
  - `RepositorioEmpresas` filtra por `Id == EmpresaActualId` en la consulta.
  - `RepositorioMiembros.ObtenerPorUsuarioAsync` filtra por `UsuarioId`, como decida la duda 2.
  - `FabricaClaimsUsuario` recibe `IRepositorioMiembros` en lugar de `ContextoDatos`.
  - Ninguno llama a `SaveChanges` (AGENTS §8 bis).
- **Verificación:** `PruebasFabricaClaimsUsuario`, sin cambios, sigue en verde.

### T4 — `RepositorioServicios`, `RepositorioProfesionales` y `RepositorioReservas` (≈ 1 h)

- **Ficheros:** crea `Infraestructura/Repositorios/RepositorioServicios.cs`, `RepositorioProfesionales.cs` y `RepositorioReservas.cs`.
- **Implementa:** RN-01, RN-02, RN-03 ⚑, D-11, A-05, `arquitectura.md` §9.3.
- **Hecho cuando:**
  - Toda consulta lleva `EmpresaId == contexto.EmpresaActualId` dentro del `Where`, nunca después en memoria (AGENTS §8).
  - `ObtenerParaModificarAsync` usa `AsTracking()`.
  - `RepositorioProfesionales.ObtenerParaModificarAsync` incluye `Horarios` y `Excepciones`.
  - Ninguna consulta usa `NombreCliente` ni `TelefonoCliente` de `Reserva`, que van cifrados (R-10).
  - Ninguno llama a `SaveChanges`.
  - La defensa en profundidad sigue lo que decida la duda 3.
- **Verificación:** las pruebas de T6.

### T5 — Registro de los repositorios (≈ 0,25 h)

- **Ficheros:** modifica `Infraestructura/InyeccionDependencias.cs` y `PruebasIntegracion/PruebasInyeccionDependencias.cs`.
- **Implementa:** `arquitectura.md` §10.
- **Hecho cuando:** los cinco repositorios e `IContextoEmpresa` están registrados con ámbito *scoped*.
- **Verificación:** una prueba nueva en `PruebasInyeccionDependencias` comprueba que ninguno de los seis tiene ámbito singleton.

### T6 — Las 5 pruebas de aislamiento (≈ 1 h)

- **Ficheros:** crea `PruebasIntegracion/Infraestructura/ContextoEmpresaFijo.cs`, un doble de `IContextoEmpresa` con un `Guid` fijo, y `PruebasIntegracion/Multiempresa/PruebasAislamientoEmpresas.cs`.
- **Implementa:** SPEC §10.4, RN-02 ⚑, RN-03 ⚑, Control 1.
- **Hecho cuando:**
  - Hay una prueba por recurso: `Servicio`, `Profesional`, `Horario`, `ExcepcionHorario` y `Reserva`.
  - En cada una se crean las empresas A y B. Con el contexto de A, el recurso de B no aparece ni al leer ni al cargarlo para modificar o eliminar, que es desactivar (AGENTS §13). El alcance de "modificar y eliminar" lo fija la duda 4.
  - Con el contexto de B, el recurso se lee y no ha cambiado. Esto es además el control positivo: demuestra que el repositorio no devuelve siempre `null`.
  - `Horario` y `ExcepcionHorario` se comprueban a través del `Profesional` que los contiene, porque no tienen repositorio. Sus datos se crean como decida la duda 5.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion` con las cinco pruebas en verde.

### T7 — Control 1 y documentación (≈ 0,5 h)

- **Ficheros:** modifica `docs/arquitectura.md` §8.2 y este fichero (§7 y §12).
- **Implementa:** Control 1 (`PROJECT_PLAN.md` §3) y la duda 2.
- **Hecho cuando:**
  - Cada línea del Control 1 tiene su evidencia anotada en §7.
  - `arquitectura.md` §8.2 recoge la decisión de la duda 2.
- **Verificación:** `dotnet build` y `dotnet test` completos. La línea de la clave respaldada la confirma el desarrollador.

**Suma: 5 h de 5 h presupuestadas.** No queda margen. El riesgo de desviación está en T6, si la duda 5 se resuelve con algo más que SQL.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Con claim, `EmpresaActualId` lo devuelve y `TieneEmpresa` es `true` | I | `Identidad/PruebasContextoEmpresa.cs` | A-03 | T1 |
| Sin claim, o con uno mal formado, `EmpresaActualId` lanza y `TieneEmpresa` es `false` | I | `Identidad/PruebasContextoEmpresa.cs` | `arquitectura.md` §8.2 | T1 |
| El principal lleva `empresa_id` solo si hay pertenencia, ya a través de `IRepositorioMiembros` | I | `Identidad/PruebasFabricaClaimsUsuario.cs` (existente) | A-03 | T3 |
| Los repositorios e `IContextoEmpresa` no son singleton | I | `PruebasInyeccionDependencias.cs` | `arquitectura.md` §10 | T5 |
| Aislamiento de `Servicio` | I | `Multiempresa/PruebasAislamientoEmpresas.cs` | RN-02 ⚑, RN-03 ⚑ | T6 |
| Aislamiento de `Profesional` | I | `Multiempresa/PruebasAislamientoEmpresas.cs` | RN-02 ⚑, RN-03 ⚑ | T6 |
| Aislamiento de `Horario` | I | `Multiempresa/PruebasAislamientoEmpresas.cs` | RN-02 ⚑, RN-03 ⚑ | T6 |
| Aislamiento de `ExcepcionHorario` | I | `Multiempresa/PruebasAislamientoEmpresas.cs` | RN-02 ⚑, RN-03 ⚑ | T6 |
| Aislamiento de `Reserva` | I | `Multiempresa/PruebasAislamientoEmpresas.cs` | RN-02 ⚑, RN-03 ⚑ | T6 |
| Las 5 pruebas de arquitectura siguen en verde | A | `Arquitectura/PruebasArquitectura.cs` | D-14 | todas |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

`PruebasAislamientoEmpresas` es la prueba transversal que prevé AGENTS §11 bis: comprueba una propiedad del sistema, no una clase. Por eso los repositorios no tienen fichero propio en esta fase. Sus pruebas por clase llegarán con los métodos que añadan las fases 8 a 16.

## 7. Criterios de aceptación

- [x] `IContextoEmpresa` existe con la firma literal de SPEC §10.3, y `EmpresaActualId` lanza si no hay empresa. Evidencia: `PruebasContextoEmpresa`, con 5 casos.
- [x] Los repositorios filtran por `EmpresaActualId` dentro del `Where`. `RepositorioMiembros` es la excepción documentada de la duda 2: filtra por `UsuarioId`. No hay `HasQueryFilter` ni `IgnoreQueryFilters()` en ningún sitio. Evidencia: `grep` sin resultados.
- [x] Ningún repositorio llama a `SaveChanges`. Evidencia: `grep` sin resultados en `Infraestructura/Repositorios/`.
- [x] No existe `IRepositorioHorarios`.
- [x] `FabricaClaimsUsuario` usa `IRepositorioMiembros`. Evidencia: `PruebasFabricaClaimsUsuario` sigue en verde sin cambios.
- [x] Ningún servicio con estado tiene ámbito singleton. Evidencia: `PruebasInyeccionDependencias.El_contexto_de_empresa_y_los_repositorios_son_scoped`.
- [x] `dotnet build` sin avisos y `dotnet test` en verde, el 2026-10-04: 168 pruebas unitarias y 60 de integración.

**Control 1**, copiado literal de `PROJECT_PLAN.md` §3:

```text
dotnet build           OK
dotnet test            OK
Un usuario de la empresa A no puede leer ni modificar recursos de la empresa B
(5 pruebas de aislamiento en verde)

Un SELECT directo sobre asp_net_users no muestra correos ni telefonos en claro
Iniciar sesion, registrar correo duplicado y recuperar contrasena funcionan
La clave de cifrado esta respaldada FUERA de la maquina
```

Las tres últimas ya tienen evidencia desde la Fase 6:

- `PruebasCifradoIdentidad`, hoy repartida en `PruebasUsuario`, `PruebasRegistroCliente` y `PruebasSolicitudRestablecimientoClave`;
- `fase-06.md` §3, para la clave respaldada fuera de la máquina.

En T7 se vuelven a ejecutar, y el desarrollador confirma de nuevo que la clave está respaldada.

**Evidencias del Control 1 (2026-10-04):**

- [x] `dotnet build` OK, con 0 avisos.
- [x] `dotnet test` OK: 168 pruebas unitarias y 60 de integración.
- [x] Las 5 pruebas de aislamiento están en verde (`Multiempresa/PruebasAislamientoEmpresas.cs`). Un sabotaje lo confirma: al quitar el filtro `EmpresaId` de `RepositorioServicios`, la prueba de `Servicio` falla. Después se restauró.
- [x] El `SELECT` directo sobre `asp_net_users` no devuelve nada en claro: `Identidad/PruebasUsuario.cs`, en verde.
- [x] Iniciar sesión, registrar un correo duplicado y recuperar la contraseña funcionan: `PruebasUsuario`, `PruebasRegistroCliente` y `PruebasSolicitudRestablecimientoClave`, en verde.
- [x] La clave de cifrado está respaldada fuera de la máquina. Lo confirmó el desarrollador el 2026-10-04 (R-9).

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | Un repositorio con ámbito singleton, o dos operaciones a la vez sobre el mismo `ContextoDatos` | Repositorios *scoped* (T5, con prueba). Cada prueba de T6 abre su propio ámbito o contexto |
| R-10 | Un repositorio que filtra `Reserva` por `NombreCliente` o `TelefonoCliente` devuelve vacío sin error | En T4 no hay ninguna consulta por campos cifrados. Las búsquedas por cliente son por `UsuarioId` (RN-05, Fase 15) |
| Aislamiento (`arquitectura.md` §10, sin número R-xx) | Un singleton que guarda `EmpresaActualId` mezclaría empresas para todos los usuarios | `ContextoEmpresa` *scoped*, sin estado propio: lee el principal en cada acceso. Prueba en T5 |

Los demás riesgos (R-2 a R-9 y R-11 a R-14) no aplican: tratan de fechas, `EXCLUDE`, Identity con Blazor, `Guid` v7, eventos, correo, claves, copias de `Reserva`, CSP, bloqueo y limitación de frecuencia.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `Resultado`, `CodigoError` e `IRelojSistema` (ver duda 7) | Fase 8, con el primer caso de uso |
| Métodos de repositorio para listar, agregar o buscar por slug | En la fase del caso de uso que los necesite (8 a 16) |
| `IConsultasCatalogoPublico` (RN-04, RN-109) | Fase 13 |
| Reservas del cliente por `UsuarioId` (RN-05) | Fase 15 |
| Carpeta `App/` con `[Authorize(Policy = "EsPropietario")]` | Fase 8, con la primera pantalla de empresa |
| Comportamiento de `Horario` y `ExcepcionHorario` (`AgregarIntervalo`, `MarcarNoDisponible`) | Fase 9 |
| Pruebas de autorización de rutas privadas | Fase 17 |
| Aislamiento comprobado a nivel de caso de uso (`Resultado` `NoEncontrado`) | En la fase de cada caso de uso, y repaso en la Fase 17 |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-01 | T4 | Las 5 de aislamiento: cada recurso se crea con un `EmpresaId` y solo lo ve su empresa |
| RN-02 ⚑ | T1, T3, T4, T6 | Las 5 de aislamiento |
| RN-03 ⚑ | T3, T4, T6 | Las 5 de aislamiento: `null`, que el caso de uso traducirá a "no encontrado" |
| RN-04 | T2 | Sin prueba propia en esta fase: ningún repositorio de empresa es el catálogo. La prueba llega con `IConsultasCatalogoPublico` (Fase 13) |
| RN-06 | T3 | El índice único ya está probado en `PruebasEsquema` (Fase 5) |

## 11. Dudas

### Duda 1 — Qué métodos tiene cada repositorio

- **Pregunta:** los documentos solo nombran `ObtenerParaModificarAsync` (`arquitectura.md` §9.3), `ObtenerPorUsuarioAsync` (§8.2) y `Agregar` (diagrama de §4.2). No hay firmas para el resto.
- **Opciones:**
  - (a) **Mínimo para esta fase:**
    - en los repositorios de empresa, servicio, profesional y reserva: `Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken ct)`, sin seguimiento, y `Task<T?> ObtenerParaModificarAsync(Guid id, CancellationToken ct)`, con seguimiento;
    - en `IRepositorioMiembros`: `Task<MiembroEmpresa?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct)`;
    - `Agregar` y los listados entran con el caso de uso que los necesite.
  - (b) Todos los métodos que vayan a usar los CU-11 a CU-21, diseñados ahora.
- **Recomendación y motivo:** (a). Es lo que piden las pruebas de aislamiento y la fábrica de claims. Diseñar ahora firmas para fases futuras sería ampliar el alcance sin un caso de uso que las valide. Las clases se llamarían como la interfaz sin la `I`: `RepositorioEmpresas`, `RepositorioServicios`, etc.
- **Bloquea:** T2, T3, T4, T6.
- **Resolución** Opción a.

### Duda 2 — `ObtenerPorUsuarioAsync` no puede filtrar por empresa

- **Pregunta:** SPEC §10.3 dice que los repositorios de empresa filtran siempre por `EmpresaActualId`. Pero `IRepositorioMiembros.ObtenerPorUsuarioAsync` es justo lo que **resuelve** la empresa al iniciar sesión, cuando aún no hay claim (`arquitectura.md` §8.2).
- **Opciones:**
  - (a) Excepción documentada: ese método filtra por `UsuarioId`, que es otro eje, igual que el área de cliente (RN-05), y no usa `IContextoEmpresa`. Se anota en `arquitectura.md` §8.2.
  - (b) `FabricaClaimsUsuario` sigue con la consulta directa a `ContextoDatos` y no usa el repositorio. Contradice la duda 9 de la Fase 6.
- **Recomendación y motivo:** (a). La consulta sale de la identidad del propio usuario autenticado y no puede devolver la pertenencia de otro. Dejarla escrita evita que una revisión la marque como fuga.
- **Bloquea:** T3, T7.
- **Resolución** Opción a.

### Duda 3 — Dónde vive la defensa en profundidad

- **Pregunta:** SPEC §10.3 exige que "toda modificación valida explícitamente `entidad.EmpresaId == _contexto.EmpresaActualId` antes de persistir", pero no dice dónde.
- **Opciones:**
  - (a) En el repositorio, en los métodos por los que entra una escritura (`Agregar`), que lanzan si el `EmpresaId` no coincide. En esta fase no hay `Agregar` (duda 1), así que la comprobación se escribe junto con el primer `Agregar`, en la Fase 8.
  - (b) En cada caso de uso, antes de `SaveChanges`.
  - (c) En una redefinición de `ContextoDatos.SaveChanges` que revise todas las entidades seguidas. Obliga a inyectar `IContextoEmpresa` en el contexto, y choca con el alta de empresa y con las cascadas.
- **Recomendación y motivo:** (a). Está en un solo sitio por agregado, no depende de que cada caso de uso se acuerde, y no mete el inquilino en `ContextoDatos`. Las cargas para modificar ya filtran en origen, así que la entidad seguida siempre es propia.
- **Bloquea:** T4.
- **Resolución** Opción a, el 2026-10-04. Como en esta fase no hay `Agregar`, no se escribe código: la comprobación llega con el primer `Agregar`, en la Fase 8.

### Duda 4 — Qué significa "modificar y eliminar" sin casos de uso

- **Pregunta:** SPEC §10.4 pide que A intente leer, modificar y eliminar un recurso de B. Pero los casos de uso que modifican son de las fases 8 a 11, y nada se borra físicamente (AGENTS §13).
- **Opciones:**
  - (a) A nivel de repositorio:
    - leer es `ObtenerPorIdAsync`;
    - modificar o desactivar exige antes `ObtenerParaModificarAsync`;
    - los dos devuelven `null` con el contexto de A;
    - con el contexto de B, el recurso sigue igual.

    Cuando lleguen los casos de uso, cada fase comprobará su `NoEncontrado`, y la Fase 17 lo repasa.
  - (b) Aplazar las 5 pruebas hasta que existan los casos de uso. Deja sin cerrar el Control 1.
- **Recomendación y motivo:** (a). Ningún caso de uso puede modificar ni desactivar sin cargar antes con `ObtenerParaModificarAsync`. Si la carga devuelve `null`, no hay nada que modificar. Es la garantía en origen que pide D-11.
- **Bloquea:** T6.
- **Resolución** Opción a.

### Duda 5 — Cómo se crean `Horario` y `ExcepcionHorario` en las pruebas

- **Pregunta:** las dos entidades no tienen todavía fábrica ni método en `Profesional`. Eso es de la Fase 9.
- **Opciones:**
  - (a) Insertarlas con SQL en la propia prueba, igual que la Fase 6 puso `Activo = false` con SQL (`fase-06.md` T8).
  - (b) Adelantar `AgregarIntervalo` y `MarcarNoDisponible`. Amplía el alcance hacia la Fase 9.
  - (c) Dejar tres pruebas y completar las otras dos en la Fase 9. Incumple el Control 1, que pide 5.
- **Recomendación y motivo:** (a). No toca `Dominio` y cumple el Control 1. En la Fase 9 se podrán cambiar las inserciones por las fábricas.
- **Bloquea:** T6.
- **Resolución** Opción a.

### Duda 6 — De dónde lee `ContextoEmpresa` el principal

- **Pregunta:** `arquitectura.md` §8.2 dice "del `IHttpContextAccessor` o del `AuthenticationStateProvider`". `EmpresaActualId` es síncrono, pero `GetAuthenticationStateAsync` es asíncrono. Y en un circuito interactivo `HttpContext` es nulo.
- **Opciones:**
  - (a) `AuthenticationStateProvider`, leyendo la tarea de estado ya resuelta, que en el servidor siempre lo está. Si no estuviera resuelta, se trata como "sin empresa": `TieneEmpresa` vale `false` y `EmpresaActualId` lanza.
  - (b) `IHttpContextAccessor`. No funciona en `/app`, que es `InteractiveServer` (AGENTS §4).
  - (c) Las dos: `HttpContext` cuando exista y, si no, el proveedor.
- **Recomendación y motivo:** (a). Es el constructor que muestra §8.2. Vale para SSR y para el circuito. Nunca bloquea de verdad y nunca inventa una empresa.
- **Bloquea:** T1.
- **Resolución** Opcion a.

### Duda 7 — `IRelojSistema` y `Resultado`

- **Pregunta:** `fase-05.md` §9 asignó a la Fase 7 `IRelojSistema`, y "casos de uso y `Resultado`" a las fases 7 en adelante. SPEC §19 no los incluye en la Fase 7, y ninguna pieza de esta fase los usa.
- **Opciones:**
  - (a) Llevarlos a la Fase 8, con el primer caso de uso.
  - (b) Crearlos ahora sin uso (≈ 0,25 h, por encima del presupuesto).
- **Recomendación y motivo:** (a). SPEC §19 tiene más autoridad que una especificación de fase (AGENTS §2), y crear código sin uso amplía el alcance.
- **Bloquea:** ninguna tarea. Solo afecta a §9.
- **Resolución** Opción a.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [x] `/revisar` sin hallazgos bloqueantes *(2026-10-04 18:38: `APTO` en la segunda ronda, con 168 unitarias y 61 de integración. Quedan 3 menores abiertos; ver el seguimiento de 2026-10-12 en `fase-07-revision.md`)*
- [x] Commit hecho *(`4f7e539`; antes de la fase, `5bb8767` y `0ec9dd0`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Control 1 superado *(2026-10-04: las seis evidencias de §7 marcadas, y confirmado en la revisión `APTO`)*
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna) *(no aplica)*
