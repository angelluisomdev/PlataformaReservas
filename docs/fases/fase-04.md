# Fase 04 — Solución, proyectos, referencias, nullable, analizadores, pruebas, `AGENTS.md` y las 4 pruebas de arquitectura

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4,5 h (SPEC §19) |
| **Semana** | 1 (`PROJECT_PLAN.md` §2) |
| **Cierra** | ninguno |
| **Generada** | 2026-09-24 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar existen la solución y sus seis proyectos, con las referencias y los paquetes de `arquitectura.md` §2, la compilación endurecida de §3 y las cuatro pruebas de arquitectura en verde. `dotnet build` compila sin avisos y `dotnet test` pasa.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 (fila 4) | Contenido y presupuesto: solución, proyectos, referencias, nullable, analizadores, pruebas, `AGENTS.md` y las 4 pruebas de arquitectura, 4,5 h |
| SPEC §7 | Stack: .NET 10, xUnit + FluentAssertions, Testcontainers obligatorio |
| SPEC §8 | Seis proyectos y sus dependencias sin ciclos |
| SPEC §18 | `AGENTS.md` canónico y `CLAUDE.md` que lo importa |
| SPEC §20, reglas 7 y 8 | Instrucciones permanentes en `AGENTS.md`; las cuatro pruebas de arquitectura de la Fase 4 |
| D-12 | Nomenclatura en castellano (nombres de proyectos y namespaces) |
| D-14 | Modelo rico, que protegen las pruebas de arquitectura |
| A-07 | Central Package Management (`Directory.Packages.props`) y `Directory.Build.props` |
| A-08 | `TreatWarningsAsErrors` desde el primer día |
| A-17, D-25 | Solución `.slnx`, FluentAssertions 7.x, xUnit 2.x con `xunit.runner.visualstudio`, `EFCore.Design` en `Web` |
| A-18 | Supresión de analizadores solo en `.editorconfig`, justificada y registrada; CA1707 aprobada para `tests/` |
| `arquitectura.md` §3.3 | Contenido del `.editorconfig` |
| `arquitectura.md` §2, §2.1, §2.2 | Mapa de la solución, referencias entre proyectos y paquetes exactos por proyecto |
| `arquitectura.md` §3.1, §3.2 | Contenido de `Directory.Packages.props` y `Directory.Build.props` |
| `arquitectura.md` §11.3 | `.gitignore` con `appsettings.*.local.json` |
| `arquitectura.md` §17 | Lista de lo que queda listo para la Fase 4 |
| `modelo-dominio.md` §9.1, capa 1 | Las cuatro pruebas de arquitectura, con su código |
| `modelo-dominio.md` §9.1, capa 2 | `EntidadBase` con `Id` protegido |
| RN | **Ninguna directa.** La fase es estructural. Las pruebas de arquitectura protegen indirectamente RN-42 (`modelo-dominio.md` §9.1) |
| CU | No aplica: ningún caso de uso se implementa aquí |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| SDK de .NET 10 instalado | ✔ comprobado (10.0.204 y 10.0.303) |
| Docker instalado (lo usarán después las pruebas de integración) | ✔ comprobado (28.5.1) |
| `AGENTS.md` en la raíz | ✔ comprobado |
| `CLAUDE.md` que importa `AGENTS.md` | ✔ comprobado |
| Documentación de las fases 0 a 3 (`docs/*.md`) | ✔ comprobado |
| `src/` y `tests/` sin contenido previo | ✔ comprobado (no existen) |
| Repositorio git inicializado (necesario para el commit de cierre) | ✘ ausente → lo inicializa el desarrollador antes de T1 (duda 7, resuelta) |
| `.gitignore` con `bin/`, `obj/` y `appsettings.*.local.json` | ✘ ausente → se completa en T1 (duda 7, resuelta) |
| `.gitattributes` y `.claude/` instalados por el desarrollador | Herramientas del proceso, no de la fase (`arquitectura.md` §2). Se comprueban, no se crean |
| Skill `/revisar` y subagentes `revisor-arquitectura` y `revisor-seguridad` | Instalados por el desarrollador en `.claude/` (duda 8) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| Raíz | sí | `PlataformaReservas.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore` |
| `src/PlataformaReservas.Dominio` | sí | `.csproj` y `Compartido/EntidadBase.cs` (duda 2) |
| `src/PlataformaReservas.Aplicacion` | sí | Solo `.csproj`, con su referencia y sus paquetes |
| `src/PlataformaReservas.Infraestructura` | sí | Solo `.csproj`, con sus referencias y sus paquetes |
| `src/PlataformaReservas.Web` | sí | Proyecto de plantilla Blazor Web App, con su referencia y sus paquetes. Sin cambios funcionales |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `.csproj` y las cuatro pruebas de arquitectura |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | Solo `.csproj`, con sus referencias y sus paquetes. Sin pruebas |
| `docs/` | sí | Este fichero, y el que genera `/revisar` al cerrar: `docs/fases/fase-04-revision.md` |
| `AGENTS.md`, `CLAUDE.md`, `.claude/`, `.gitattributes` | no | Solo se comprueban (T7). Si hace falta cambiarlos, se para y se pregunta |

**Todo lo que no figure como "sí" no se toca en esta fase.**

## 5. Tareas

### T1 — Solución y ficheros comunes de compilación (≈ 0,5 h)

- **Ficheros:** crea `PlataformaReservas.slnx` (duda 1), `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` (duda 6) · modifica `.gitignore`
- **Implementa:** A-07, A-08, A-17, A-18, `arquitectura.md` §3.1, §3.2, §3.3 y §11.3
- **Hecho cuando:**
  - `Directory.Build.props` contiene **literalmente** las siete propiedades de `arquitectura.md` §3.2: `net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild` e `InvariantGlobalization false`.
  - `Directory.Packages.props` tiene `ManagePackageVersionsCentrally` y un `PackageVersion` para cada paquete de §2.2, y solo para esos. FluentAssertions en 7.x y xUnit en 2.x (A-17).
  - `.editorconfig` es **literalmente** el de `arquitectura.md` §3.3: `root = true` y CA1707 a `none` solo en `tests/**.cs`, con su comentario. Nada más.
  - `.gitignore` ignora `bin/`, `obj/` y `appsettings.*.local.json`, y conserva las dos líneas de Claude Code que ya instaló el desarrollador.
- **Verificación:** revisión del diff frente a §2.2, §3.1, §3.2 y §3.3.

### T2 — Proyectos de `src/` con referencias y paquetes (≈ 0,75 h)

- **Ficheros:** crea `src/PlataformaReservas.Dominio/PlataformaReservas.Dominio.csproj`, `src/PlataformaReservas.Aplicacion/PlataformaReservas.Aplicacion.csproj`, `src/PlataformaReservas.Infraestructura/PlataformaReservas.Infraestructura.csproj` y `src/PlataformaReservas.Web/` con **`dotnet new blazor --interactivity Server --empty`**, sin autenticación (duda 5) · modifica `PlataformaReservas.slnx`
- **Implementa:** SPEC §8, `arquitectura.md` §2.1, §2.2 y §17, D-01 (render de la plantilla Web), D-25
- **Hecho cuando:**
  - Referencias exactas: `Aplicacion → Dominio`, `Infraestructura → Aplicacion`, `Web → Infraestructura`. No hay otras.
  - `Dominio` **no tiene ningún `PackageReference`**.
  - Cada proyecto tiene los paquetes de §2.2 sin atributo `Version`, que se toma de CPM.
  - **`Microsoft.EntityFrameworkCore.Design` está en `Web` y no en `Infraestructura`** (duda 9).
  - `Web` no contiene las páginas de ejemplo `Counter` ni `Weather`: `--empty` no las genera.
  - Se borra el `Class1.cs` que generan las plantillas de biblioteca.
- **Verificación:** `dotnet list <proyecto> reference` y `dotnet list <proyecto> package` coinciden con §2.1 y §2.2.

### T3 — Proyectos de pruebas (≈ 0,5 h)

- **Ficheros:** crea `tests/PlataformaReservas.PruebasUnitarias/PlataformaReservas.PruebasUnitarias.csproj` y `tests/PlataformaReservas.PruebasIntegracion/PlataformaReservas.PruebasIntegracion.csproj` · modifica `PlataformaReservas.slnx`
- **Implementa:** SPEC §8, `arquitectura.md` §2.1, §2.2 y §12.1
- **Hecho cuando:**
  - `PruebasUnitarias → Dominio, Aplicacion` con `xunit`, `FluentAssertions`, `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`.
  - `PruebasIntegracion → Aplicacion, Infraestructura` con lo anterior más `Testcontainers.PostgreSql` y `Respawn`.
  - Se borra la prueba de ejemplo (`UnitTest1.cs`) que genera la plantilla.
- **Verificación:** `dotnet list ... reference` y `dotnet list ... package`.

### T4 — Compilación limpia con los analizadores activos (≈ 0,5 h)

- **Ficheros:** los que generan las plantillas de T2 y T3, solo si algún aviso obliga a tocarlos
- **Implementa:** A-08, `arquitectura.md` §3.2
- **Hecho cuando:** `dotnet build` compila la solución con **0 avisos y 0 errores**, sin `#pragma`, sin `NoWarn` y sin tocar `Directory.Build.props` (AGENTS.md §13, A-18).
- **Verificación:** `dotnet build`. Si salta un analizador distinto de CA1707 en `tests/` y la única salida es suprimirlo, **se para y se pregunta** (duda 6). Si se aprueba, se añade al `.editorconfig` con su comentario **y** a la tabla de `arquitectura.md` §3.3.

### T5 — `EntidadBase` mínima en `Dominio` (≈ 0,25 h)

- **Ficheros:** crea `src/PlataformaReservas.Dominio/Compartido/EntidadBase.cs`
- **Implementa:** `modelo-dominio.md` §9.1, capa 2 ("`EntidadBase` con `Id` protegido"), y `arquitectura.md` §2.3
- **Hecho cuando:** existe `EntidadBase` abstracta con `Id` (`Guid`) de lectura pública y escritura no pública. **No incluye** el registro de eventos de `modelo-dominio.md` §10 bis, que necesita `IEventoDominio` y llega en la Fase 11b.
- **Verificación:** `dotnet build`.

### T6 — Las cuatro pruebas de arquitectura (≈ 0,75 h)

- **Ficheros:** crea `tests/PlataformaReservas.PruebasUnitarias/Arquitectura/PruebasArquitectura.cs` (duda 2)
- **Implementa:** SPEC §20 regla 8, D-14, `modelo-dominio.md` §9.1 capa 1
- **Hecho cuando:**
  - Existen las cuatro pruebas con los nombres literales de `modelo-dominio.md` §9.1: `Ninguna_entidad_expone_setters_publicos`, `Ninguna_entidad_expone_colecciones_mutables`, `Toda_entidad_tiene_constructor_privado_sin_parametros` y `El_dominio_no_depende_de_infraestructura`.
  - El cuerpo es **literalmente** el de `modelo-dominio.md` §9.1, que ya incluye el ancla `typeof(EntidadBase).Assembly` (duda 2) y el patrón `Where(...).Should().BeEmpty()` en la tercera prueba (duda 3). Sin ningún ajuste adicional.
  - Las cuatro están en verde.
  - **Comprobación negativa**, sin commit: con una entidad anémica temporal (setter público, `List<>` pública y sin constructor privado), las tres primeras fallan. La entidad se borra después. Sin esto, una prueba en verde con cero entidades no demuestra nada.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias`, primero con la entidad temporal (rojo) y después sin ella (verde).

### T7 — Comprobación de `AGENTS.md` y `CLAUDE.md` frente a la solución creada (≈ 0,25 h)

- **Ficheros:** ninguno se modifica
- **Implementa:** SPEC §18, SPEC §20 regla 7, fila 4 de SPEC §19 (`AGENTS.md`)
- **Hecho cuando:** los nombres de proyecto, las rutas y los comandos de `AGENTS.md` §6 y §11 coinciden con lo creado, y `CLAUDE.md` importa `AGENTS.md`. Si algo no coincide, se anota y se pregunta. **No se edita.**
- **Verificación:** lectura cruzada, y los comandos `dotnet test tests/PlataformaReservas.PruebasUnitarias` y `dotnet test tests/PlataformaReservas.PruebasIntegracion` de `AGENTS.md` §11 se ejecutan sin error.

### T8 — Cierre: compilación, pruebas y revisión (≈ 0,5 h)

- **Ficheros:** ninguno de código. `/revisar 4` escribe `docs/fases/fase-04-revision.md` (duda 8).
- **Implementa:** `PROJECT_PLAN.md` §5 (fin de sesión)
- **Hecho cuando:** los puntos de §12 están marcados.
- **Verificación:** `dotnet build` y `dotnet test` desde la raíz.

**Suma: 4,0 h de 4,5 h presupuestadas.** Queda 0,5 h de margen para T4, que es la tarea con más incertidumbre: no se sabe cuántos avisos darán las plantillas con `latest-recommended`. La incertidumbre es menor que en la primera versión de esta especificación, porque `--empty` elimina las páginas de ejemplo y CA1707 ya está resuelta.

## 6. Pruebas

| Prueba | Tipo | Regla que verifica | Tarea |
|---|---|---|---|
| `Ninguna_entidad_expone_setters_publicos` | A | D-14, `modelo-dominio.md` §9.1 (incluido `init`) | T6 |
| `Ninguna_entidad_expone_colecciones_mutables` | A | D-14, RN-42 de forma indirecta (§9.1) | T6 |
| `Toda_entidad_tiene_constructor_privado_sin_parametros` | A | D-14, `modelo-dominio.md` §9.1 capa 2 | T6 |
| `El_dominio_no_depende_de_infraestructura` | A | SPEC §8.1, `arquitectura.md` §2.2 | T6 |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

`PruebasIntegracion` no lleva pruebas en esta fase: el contenedor compartido (§12.2) llega con la persistencia.

## 7. Criterios de aceptación

- [x] La solución tiene exactamente los seis proyectos de SPEC §8, con los nombres literales. *(comprobado el 2026-10-03: `PlataformaReservas.slnx`)*
- [x] Las referencias entre proyectos coinciden con `arquitectura.md` §2.1, sin ninguna más. *(comprobado el 2026-10-03: `ProjectReference` de los seis `.csproj`)*
- [x] Los paquetes de cada proyecto coinciden con `arquitectura.md` §2.2. `Dominio` no tiene ninguno y `Microsoft.EntityFrameworkCore.Design` está en `Web`. *(comprobado el 2026-10-03: cumplido al cerrar la fase.)*
- [x] `.editorconfig` coincide con `arquitectura.md` §3.3 y no contiene ninguna supresión que no figure en su tabla. *(comprobado el 2026-10-03: CA1707 en `tests/` y código generado de `Migraciones/`, ambas en la tabla)*
- [x] Ningún `.csproj` declara versiones: todas están en `Directory.Packages.props` (A-07). *(comprobado el 2026-10-03: ningún `Version=` en los `.csproj`)*
- [x] `Directory.Build.props` coincide con `arquitectura.md` §3.2 (A-08). *(comprobado el 2026-10-03: las siete propiedades, literales)*
- [x] `dotnet build` da 0 avisos, sin `#pragma` ni `NoWarn`. *(comprobado el 2026-10-03: sin `NoWarn`; los únicos `#pragma` son los que genera EF en la migración, documentados en §3.3. Compilación sin avisos según §12)*
- [ ] Las cuatro pruebas de arquitectura están en verde, **y se ha comprobado que fallan** ante una entidad anémica. *(pendiente, 2026-10-03: en verde según §12; no queda constancia de la comprobación negativa. Sigue pendiente el 2026-10-12: las pruebas están en verde en todas las revisiones de las fases 6 a 9, pero no consta que se las haya visto fallar ante una entidad anémica)*
- [x] `dotnet test` en verde desde la raíz. *(comprobado el 2026-10-03: según §12)*

Esta fase no cierra ningún punto de control (`PROJECT_PLAN.md` §3).

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-4 | Crear `Web` con la plantilla de Identity traería su propio `DbContext`, sus migraciones y un proveedor que no es PostgreSQL, en conflicto con `Infraestructura` | Plantilla Blazor Web App **sin** autenticación. La integración de Identity desde la plantilla oficial se deja a la Fase 6 (duda 5) |
| R-3 | No se da aún: no hay migraciones ni contenedor | No aplica en esta fase. Se deja anotado que `PruebasIntegracion` nunca usará `EnsureCreated` (`arquitectura.md` §12.2) |

Los demás riesgos (R-1, R-2 y de R-5 a R-14) no aplican: tratan de persistencia, eventos, correo, cifrado o endurecimiento, que no existen en esta fase.

Riesgo propio de la fase, **sin código R-xx**: que las pruebas de arquitectura pasen en verde sin probar nada porque todavía no hay entidades. Se mitiga con la comprobación negativa de T6.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Entidades, objetos de valor, enumeraciones y `DominioException` | Fase 5 |
| Registro de eventos en `EntidadBase` e `IEventoDominio` | Fase 11b |
| `ContextoDatos`, configuraciones de Fluent API, migraciones y `EXCLUDE` | Fase 5 |
| `BaseDatosFixture`, `ColeccionBaseDatos` y Testcontainers en marcha | Fase 5 o 7, con la primera prueba de integración |
| `Program.cs` según el esqueleto de `arquitectura.md` §10.1 (Serilog, Identity, políticas, `AgregarInfraestructura`, `AgregarAplicacion`) | Fases 5 a 7 |
| `InyeccionDependencias.cs` de cada capa | Con el primer servicio que registrar |
| Identity y sus páginas | Fase 6 |
| Sección `Correo` de `appsettings.json` | Fase 16b |
| Carpetas vacías de `arquitectura.md` §2.3 | Cada una con su primer fichero. Git no guarda carpetas vacías |
| `README.md` | Fase 20 (y R-9 en la Fase 6) |
| `Dockerfile` y `docker-compose.yml` | Fase 18 |
| `global.json` | No está en los documentos. No se crea |
| `.gitattributes` y `.claude/` | Herramientas del proceso: los instala el desarrollador, no esta fase (`arquitectura.md` §2) |
| Cambios en `AGENTS.md` o `CLAUDE.md` | Solo si T7 detecta una discrepancia y el desarrollador lo aprueba |

## 10. Trazabilidad

| RN · CU · decisión | Tarea | Prueba |
|---|---|---|
| SPEC §8 (proyectos y dependencias) | T2, T3 | `El_dominio_no_depende_de_infraestructura` (en parte) y comprobación con `dotnet list` |
| A-07 | T1, T2, T3 | Criterio de aceptación: ningún `.csproj` con versión |
| A-08 | T1, T4 | `dotnet build` con 0 avisos |
| D-14 | T5, T6 | Las cuatro pruebas de arquitectura |
| RN-42 (indirecta) | T6 | `Ninguna_entidad_expone_colecciones_mutables` |
| SPEC §18 y §20 regla 7 (`AGENTS.md`) | T7 | Lectura cruzada y comandos de §11 |

No hay reglas RN ni casos de uso CU directos en esta fase.

## 11. Dudas

Todas resueltas por el desarrollador el 2026-09-24. Se mantienen aquí para dejar rastro de cada decisión.

Tras la revisión, **las resoluciones están incorporadas a los documentos** (SPEC, `arquitectura.md` y `modelo-dominio.md`), de modo que el código de esta fase ya no se aparta de ellos en ningún punto. Queda pendiente solo `AGENTS.md`, que está fuera de `docs/` y lo actualiza el desarrollador.

### Duda 1 — Formato de la solución

- **Pregunta:** ¿`PlataformaReservas.sln` o `.slnx`? Los documentos (SPEC §8, `arquitectura.md` §2) dicen `.sln`, pero el SDK de .NET 10 crea `.slnx` por defecto. El hook `compilar.sh` acepta los dos.
- **Opciones:** (a) `.sln`, con `dotnet new sln --format sln`; (b) `.slnx`, y corregir los documentos.
- **Recomendación y motivo:** (a). Es lo que dicen los documentos, que mandan, y no obliga a tocarlos.
- **Bloquea:** T1.
- **Resolución:** **`.slnx`**. La solución será `PlataformaReservas.slnx`. **Documentos actualizados**: SPEC §8 y D-25, `arquitectura.md` §2 y A-17. `AGENTS.md` §6 lo actualiza el desarrollador.

### Duda 2 — `EntidadBase` y el ancla de las pruebas de arquitectura sin entidades

- **Pregunta:** el código de `modelo-dominio.md` §9.1 usa `typeof(Reserva).Assembly` y `EntidadBase`, pero `Reserva` no existe hasta la Fase 5 (y §9.1 capa 3 pide escribirla la primera y a mano). ¿Qué se crea ahora?
- **Opciones:** (a) crear en esta fase una `EntidadBase` mínima (abstracta, con `Id` protegido y sin eventos) y anclar las pruebas con `typeof(EntidadBase).Assembly`; (b) crear ya `Reserva`; (c) aplazar las pruebas de arquitectura a la Fase 5.
- **Recomendación y motivo:** (a). (b) adelanta trabajo de la Fase 5 y va contra §9.1 capa 3. (c) incumple la fila 4 de SPEC §19. Además hay que fijar el nombre de la clase de pruebas, que no está en los documentos: se propone `PruebasArquitectura`, en `tests/PlataformaReservas.PruebasUnitarias/Arquitectura/PruebasArquitectura.cs`.
- **Bloquea:** T5, T6.
- **Resolución:** opción (a). `EntidadBase` mínima; el ancla de las pruebas es `typeof(EntidadBase).Assembly`, y la clase es `PruebasArquitectura`, en `Arquitectura/PruebasArquitectura.cs`. **Documento actualizado**: `modelo-dominio.md` §9.1 recoge ya el ancla, la ubicación y la comprobación negativa.

### Duda 3 — `OnlyContain` con cero entidades

- **Pregunta:** `Toda_entidad_tiene_constructor_privado_sin_parametros` usa `Should().OnlyContain(...)`. Hasta donde sé, FluentAssertions falla con una colección vacía ("but the collection is empty"). Si es así, la suite queda en rojo hasta la Fase 5 aunque el código esté bien. Se confirmará al ejecutarla.
- **Opciones:** (a) reescribirla con el mismo patrón que las otras tres: filtrar las entidades **sin** constructor privado y afirmar `Should().BeEmpty()`, que es equivalente en cuanto haya entidades; (b) dejarla literal y aceptar el rojo hasta la Fase 5; (c) añadir una entidad de relleno en `Dominio`.
- **Recomendación y motivo:** (a). La semántica es la misma, pasa con cero entidades y la comprobación negativa de T6 demuestra que detecta el fallo. (b) viola "`dotnet test` en verde". (c) amplía el alcance.
- **Bloquea:** T6.
- **Resolución:** opción (a). La tercera prueba se escribe con el patrón `Where(...).Should().BeEmpty()`. **Documento actualizado**: el código de `modelo-dominio.md` §9.1 ya usa ese patrón, así que T6 lo copia literalmente sin ningún ajuste.

### Duda 4 — Versiones de FluentAssertions y xUnit

- **Pregunta:** el stack nombra los paquetes, pero no sus versiones mayores, y hay dos que importan:
  - **FluentAssertions 8** tiene licencia comercial de Xceed (gratuita para uso no comercial). La 7.x es Apache 2.0.
  - **xUnit v3** cambia el paquete (`xunit.v3`) y la firma de `IAsyncLifetime`, que pasa a `ValueTask`. El `BaseDatosFixture` de `arquitectura.md` §12.2 está escrito con la firma de v2 (`Task InitializeAsync()`).
- **Opciones:** (a) FluentAssertions 7.x y xUnit 2.x, con `xunit.runner.visualstudio`; (b) las últimas versiones de ambos y ajustar después el código de §12.2.
- **Recomendación y motivo:** (a). Coincide con el código ya documentado y evita la duda de licencia en un trabajo que se publica. Si se elige (a), el paquete `xunit.runner.visualstudio` es necesario para que `dotnet test` descubra las pruebas y no figura en §2.2: hay que autorizarlo expresamente (AGENTS.md §4, "sin paquetes nuevos sin preguntar").
- **Bloquea:** T1, T3.
- **Resolución:** opción (a). FluentAssertions 7.x y xUnit 2.x. Queda **autorizado** `xunit.runner.visualstudio`, que se añade a CPM y a los dos proyectos de pruebas. **Documentos actualizados**: SPEC §7 y D-25, `arquitectura.md` §2.2 y A-17. Los paquetes permitidos de `AGENTS.md` §4 los actualiza el desarrollador.

### Duda 5 — Plantilla del proyecto `Web`

- **Pregunta:** ¿con qué plantilla se crea `Web`? R-4 dice "partir de la plantilla oficial", pero la variante con autenticación individual trae su propio `DbContext`, sus migraciones y un proveedor que no es PostgreSQL.
- **Opciones:** (a) `blazor` con `--interactivity Server`, interactividad por página y **sin autenticación**, e incorporar las páginas de Identity de la plantilla oficial en la Fase 6; (b) `blazor --auth Individual` ya ahora, retirando después lo que sobre.
- **Recomendación y motivo:** (a). Respeta D-01 (SSR estático por defecto e `InteractiveServer` por página), no introduce paquetes fuera de §2.2 y deja Identity a su fase.
- **Bloquea:** T2.
- **Resolución:** opción (a), **con `--empty`**: `dotnet new blazor --interactivity Server --empty`. Interactividad por página, sin autenticación y sin las páginas de ejemplo, que habría que borrar después y sobre las que podrían saltar analizadores en T4. **Documento actualizado**: `arquitectura.md` §17.

### Duda 6 — Analizadores sobre los nombres de las pruebas (condicional)

- **Pregunta:** si con `AnalysisLevel latest-recommended` y `TreatWarningsAsErrors` salta CA1707 por los guiones bajos de los nombres de prueba de §9.1, o salta cualquier otra regla sobre el código de plantilla, ¿cómo se resuelve sin `#pragma`?
- **Opciones:** (a) un `.editorconfig` en `tests/` que baje de severidad solo esa regla, con comentario del motivo; (b) cambiar los nombres de las pruebas; (c) parar y decidir según el caso.
- **Recomendación y motivo:** (c) si llega a pasar. Ninguna opción está en los documentos: (a) añade un fichero no previsto y (b) contradice los nombres documentados. Puede que no ocurra.
- **Bloquea:** T4 y T6, solo si se da.
- **Resolución:** **se cierra con una política** (A-18, `arquitectura.md` §3.3) en lugar de esperar a que ocurra:
  - Las supresiones van **solo** en un `.editorconfig` en la raíz, con alcance mínimo, comentario del motivo en cada regla y registro en la tabla de §3.3. Nunca `#pragma` ni `NoWarn`.
  - **CA1707 queda aprobada de antemano, solo en `tests/**.cs`**: es casi seguro que salte con los nombres de §9.1, y los guiones bajos son la convención de xUnit. El `.editorconfig` se crea en T1 con ella.
  - Cualquier otra regla sigue la opción (c): se para y se pregunta caso por caso.

### Duda 7 — Repositorio git y `.gitignore`

- **Pregunta:** el directorio no es un repositorio git, y la fase termina con un commit (§12). El hook menciona un `.gitattributes` que no existe.
- **Opciones:** (a) el desarrollador ejecuta `git init` antes de T1, y en T1 se añaden a `.gitignore` `bin/`, `obj/` y `appsettings.*.local.json` (este último lo exige `arquitectura.md` §11.3); (b) que lo haga el agente en T1.
- **Recomendación y motivo:** (a). La creación del repositorio y su configuración son decisión del desarrollador. `.gitattributes` queda fuera porque no está en los documentos, salvo que se indique lo contrario.
- **Bloquea:** el commit de §12, y T1 en lo que toca a `.gitignore`.
- **Resolución:** opción (a). El desarrollador ejecuta `git init` antes de T1; en T1 se completa `.gitignore`. `.gitattributes` **no lo crea esta fase**: forma parte de las herramientas de Claude Code y lo instala el desarrollador junto con `.claude/` (`arquitectura.md` §2).

### Duda 8 — Herramientas de cierre inexistentes

- **Pregunta:** §12 pide `/revisar` y `/metricas`, pero esas skills no existen, y tampoco `docs/desarrollo-asistido-ia.md`.
- **Opciones:** (a) revisión manual del diff y registro manual, con el desarrollador creando `docs/desarrollo-asistido-ia.md`; (b) crear las skills y el fichero dentro de esta fase.
- **Recomendación y motivo:** (a). (b) queda fuera del alcance de la fila 4 de SPEC §19 y del radio de cambio.
- **Bloquea:** los puntos 3 y 4 de §12.
- **Resolución inicial:** opción (a). Revisión manual del diff y registro manual de métricas.
- **Resolución final:** **las skills ya existen.** El desarrollador instala en `.claude/`, antes de T1, las skills `revisar` y `metricas` y los subagentes `revisor-arquitectura` y `revisor-seguridad`. Son herramientas del proceso, no entregables de la fase, así que no amplían su alcance. El cierre (T8 y §12) se hace con `/revisar 4` y `/metricas 4`, y `/metricas` crea `docs/desarrollo-asistido-ia.md` la primera vez que se ejecuta.
- **Nota (2026-10-03):** la skill `/metricas` y el registro `docs/desarrollo-asistido-ia.md` se retiran del proyecto. El cierre de fase queda en `/revisar`.

### Duda 9 — Ubicación de `Microsoft.EntityFrameworkCore.Design`

*Detectada en la revisión de esta especificación, no por `/fase`.*

- **Pregunta:** `arquitectura.md` §2.2 situaba el paquete en `Infraestructura`. ¿Funciona ahí?
- **Respuesta:** no. `dotnet ef` exige el paquete en el **proyecto de arranque**, que es `Web` (`-s src/PlataformaReservas.Web` en todos los comandos de `AGENTS.md` §11). Además es una dependencia solo de desarrollo y no se propaga a otros proyectos. El primer `dotnet ef migrations add` de la Fase 5 habría fallado.
- **Bloquea:** T2.
- **Resolución:** el paquete va en **`Web`**. **Documentos actualizados**: `arquitectura.md` §2.2 y SPEC D-25. Las migraciones siguen en `Infraestructura/Persistencia/Migraciones/`.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar 4` con veredicto `APTO` *(no consta, 2026-10-12: no existe `fase-04-revision.md`)*
- [x] Commit hecho (duda 7) *(`8f49ad1`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Punto de control superado, si esta fase cierra alguno (no cierra ninguno)
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna es de la Fase 4)
