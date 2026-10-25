# Fase 05 — Entidades, contexto, Fluent API, índices, `EXCLUDE`, migraciones, datos iniciales, **conversores de cifrado**

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 7 h (SPEC §19; 6,75 h en la aprobación inicial, +0,25 h por D-26) |
| **Semana** | 1 (`PROJECT_PLAN.md` §2) |
| **Cierra** | ninguno |
| **Generada** | 2026-09-30 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar existen las entidades y los objetos de valor del dominio, `ContextoDatos` con una configuración Fluent API por entidad, y una migración inicial que crea el esquema completo con sus índices, sus `CHECK` y la restricción `ex_reservas_sin_solape`. Una prueba de integración contra PostgreSQL real demuestra que la restricción existe en la base de datos (`PROJECT_PLAN.md` §2, objetivo de la semana 1).

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 (fila 5) | Contenido y presupuesto, 6,75 h, de las que 0,75 h son los conversores de `Reserva` (recorrido del presupuesto) |
| SPEC §9.1 a §9.11 | Datos de cada entidad, tipos, longitudes y `EstadoReserva` |
| SPEC §4.1 | `PoliticasReserva` y la precedencia `Servicio.X ?? Empresa.X` |
| SPEC §12.4 | `FranjaHoraria` y `SeSolapaCon`, semiabierto |
| SPEC §13 | SQL literal de `btree_gist` y `ex_reservas_sin_solape`, y su `Down()` |
| SPEC §14 | `snake_case`, índices, únicos, `CHECK`, `DeleteBehavior.Restrict`, borrado lógico, datos iniciales solo en desarrollo |
| SPEC §15.1, D-22 | Cifrado de `Reserva.NombreCliente` y `Reserva.TelefonoCliente` con conversores de EF Core |
| SPEC §18 | `docs/base-datos.md` es entregable de la Fase 5 |
| D-13, D-14, D-15, D-16, D-19 | `snake_case`, modelo rico, siete agregados, `Guid` v7, fábrica + scoped con `NoTracking` |
| M-01 a M-05 | Modelo rico, agregados, `Guid` v7 sin tipar, cuatro objetos de valor |
| A-04, A-05 | Registro del contexto y seguimiento explícito (`arquitectura.md` §9.2, §9.3) |
| A-10 | Contenedor compartido de pruebas (`arquitectura.md` §12.2) |
| `modelo-dominio.md` §2, §4, §5, §7, §8 | Agregados, fichas de entidad, objetos de valor, enumeraciones y mapa a tablas |
| `arquitectura.md` §8.5 | `Usuario : IdentityUser<Guid>` con `[ProtectedPersonalData]` y `ConversorCifrado` |
| `arquitectura.md` §9.1, §9.5, §11.3 | `ContextoDatos`, restricción a mano, cadena `ConnectionStrings__PlataformaReservas` |
| D-26, `arquitectura.md` §11.4 | Base de datos local: `postgres` en Docker Compose, `.env` sin plantilla, `dotnet-ef` como herramienta local, credenciales a cargo del desarrollador |
| `PROJECT_PLAN.md` §6 | Verificar la monotonía de `Guid.CreateVersion7()` tal como la almacena Npgsql |
| RN-01, RN-06, RN-10, RN-14, RN-20, RN-21, RN-32, RN-41, RN-45, RN-50, RN-51, RN-77, RN-79, RN-82, RN-90, RN-97, RN-100, RN-101, RN-120, RN-121, RN-122, RN-123, RN-163 | Ver §10 |
| CU | No aplica: ningún caso de uso se implementa en esta fase |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| Solución, seis proyectos, referencias y paquetes (Fase 4) | ✔ comprobado |
| `EntidadBase` en `Dominio/Compartido` | ✔ comprobado |
| Las 4 pruebas de arquitectura | ✔ comprobado (existen) |
| `Microsoft.EntityFrameworkCore.Design` en `Web` | ✔ comprobado |
| Docker disponible para Testcontainers | ✔ comprobado en la Fase 4 (28.5.1) |
| Herramienta `dotnet ef` | ✔ comprobado el 2026-10-03: `.config/dotnet-tools.json` con `dotnet-ef` 10.0.12 |
| Base de datos local | ✔ comprobado el 2026-10-03: `docker-compose.yml` y `.env` presentes, `UserSecretsId` en `Web.csproj`. Antes: ✘ ausente → T0 (D-26). Solo la necesitan `dotnet ef database update` y la aplicación en desarrollo; las pruebas de integración usan Testcontainers |
| `dotnet build` en verde con el `.editorconfig` actual | ✘ sin comprobar → se comprueba al empezar T1 (duda 1, resuelta) |
| `IPersonalDataProtector` implementado | ✘ ausente: es de la Fase 6 → se usa la interfaz y un doble de pruebas (duda 4, resuelta) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Compartido/DominioException`, `Compartido/Validacion` (*añadido tras la revisión*), `Entidades/`, `ValueObjects/`, `Enumeraciones/` |
| `src/PlataformaReservas.Aplicacion` | no | Nada: sin casos de uso ni interfaces en esta fase |
| `src/PlataformaReservas.Infraestructura` | sí | `Persistencia/` (contexto, configuraciones, migraciones, datos iniciales), `Identidad/Usuario` (duda 3), `InyeccionDependencias.cs` |
| Raíz | sí | `docker-compose.yml` (solo `postgres`, lo mantiene el desarrollador), `.config/dotnet-tools.json` y la línea `.env` en `.gitignore` (T0). `.editorconfig`: solo la sección `generated_code` de `Migraciones/` (T9, A-18) (*añadido tras la revisión*) |
| `src/PlataformaReservas.Web` | sí | `Program.cs` (llamada a `AgregarInfraestructura` y `MigrarYSembrarAsync` en desarrollo) y el `UserSecretsId` que añade `dotnet user-secrets init` en `Web.csproj` (T0) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | Pruebas de los objetos de valor y de las fábricas de las entidades (`Entidades/PruebasEntidades`, *añadido tras la revisión*). Las de arquitectura **no se tocan** |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `BaseDatosFixture`, `ColeccionBaseDatos`, la prueba de esquema, `ProtectorFalso` (el doble de T8) y `PruebasDatosIniciales` (T11) |
| `docs/` | sí | `docs/base-datos.md` y este fichero. *Añadidos tras la revisión, por decisiones del desarrollador:* `AGENTS.md`, SPEC y `arquitectura.md` (`DominioException`, CA1710); `AGENTS.md`, `reglas-negocio.md` y SPEC (sufijo `Utc`); `arquitectura.md` §3.3 (A-18); `arquitectura.md`, SPEC y `base-datos.md` (`postgres:latest`, sin `.env.example`) |

**Todo lo que no figure como "sí" no se toca en esta fase.**

## 5. Tareas

### T0 — Base de datos local y `dotnet-ef` (≈ 0,25 h)

Se hace **antes de T6**, que es la primera tarea que necesita una cadena de conexión. Tiene dos partes con responsable distinto.

**Parte de la IA:**

- **Ficheros:** crea `.config/dotnet-tools.json` · modifica `.gitignore`. **`docker-compose.yml` ya está entregado** (generado fuera de Claude Code el 2026-09-30); la IA solo comprueba que coincide con `arquitectura.md` §11.4. *`.env.example` se retiró tras la revisión: ya no se usa (§11).*
- **Implementa:** D-26, SPEC §17, `arquitectura.md` §11.4
- **Hecho cuando:**
  - `docker-compose.yml` en la raíz coincide con `arquitectura.md` §11.4: solo el servicio `postgres`, sin `web` ni `Dockerfile` (Fase 18). **No se modifica.**
  - `.gitignore` ignora `.env`.
  - `dotnet-ef` está en `.config/dotnet-tools.json` con la versión 10 fijada (`dotnet new tool-manifest` y `dotnet tool install dotnet-ef --version 10.*`).
- **Verificación:** `dotnet tool restore` y `dotnet ef --version`.

**Parte del desarrollador** (la IA no la hace: son credenciales):

1. Crear `.env` con `POSTGRES_USER`, `POSTGRES_PASSWORD` y, si hace falta, `POSTGRES_PUERTO` (`arquitectura.md` §11.4, tabla "Secretos").
2. `dotnet user-secrets init --project src/PlataformaReservas.Web` y `dotnet user-secrets set "ConnectionStrings:PlataformaReservas" "<cadena>" --project src/PlataformaReservas.Web`, con la misma contraseña.
3. `docker compose up -d postgres` y comprobar con `docker compose ps` que el servicio está `healthy`.

La contraseña **no se escribe en ningún prompt**: quedaría en el historial de la conversación.

### T1 — `DominioException` y enumeraciones (≈ 0,25 h)

- **Ficheros:** crea `Dominio/Compartido/DominioException.cs`, `Dominio/Enumeraciones/EstadoReserva.cs`, `Dominio/Enumeraciones/RolMiembro.cs`
- **Implementa:** RN-90, `modelo-dominio.md` §7, `arquitectura.md` §2.3
- **Hecho cuando:**
  - Antes de nada, `dotnet build` con el `.editorconfig` actual. Si falla por reglas de estilo, se corrige el código existente para cumplirlas (duda 1). En `PruebasArquitectura.cs` solo se permiten cambios de estilo (p. ej. `var` → tipo explícito), **nunca** de lógica.
  - `EstadoReserva { Confirmada = 0, Cancelada = 1, Completada = 2 }` y `RolMiembro { Propietario = 0 }` con los valores literales. `OrigenCancelacion` **no** se crea (Fase 11b).
- **Verificación:** `dotnet build` y `dotnet test tests/PlataformaReservas.PruebasUnitarias` (4 en verde).

### T2 — Los cuatro objetos de valor (≈ 0,5 h)

- **Ficheros:** crea `Dominio/ValueObjects/FranjaHoraria.cs`, `IntervaloHorario.cs`, `Slug.cs`, `PoliticasReserva.cs` · crea `PruebasUnitarias/ValueObjects/PruebasObjetosValor.cs`
- **Implementa:** RN-41, RN-50, RN-51, RN-100, M-05, SPEC §4.1, §12.4, `modelo-dominio.md` §5
- **Hecho cuando:** `FranjaHoraria` exige `Kind = Utc` y `FinUtc > InicioUtc`; `SeSolapaCon` es la expresión literal de SPEC §12.4; `IntervaloHorario` exige `HoraInicio < HoraFin`. `PoliticasReserva` se crea **sin** comprobar rangos (llegan en la Fase 10, duda 2). Solo cuatro objetos de valor (M-05).
- **Verificación:** pruebas unitarias de los constructores y de `SeSolapaCon` en los límites exactos (`[inicio, fin)`).

### T3 — `Categoria`, `Empresa`, `MiembroEmpresa` y `Servicio` (≈ 0,75 h)

- **Ficheros:** crea `Dominio/Entidades/Categoria.cs`, `Empresa.cs`, `MiembroEmpresa.cs`, `Servicio.cs`
- **Implementa:** RN-01, RN-14, RN-20, RN-21, SPEC §9.2 a §9.5, `modelo-dominio.md` §4.2 a §4.5
- **Hecho cuando:** entidades `sealed`, heredan de `EntidadBase`, constructor privado sin parámetros, sin `set`/`init` públicos, `EmpresaId` sin método que lo cambie (RN-01). `Empresa.Politicas` es `PoliticasReserva`. Fábricas `Crear(...)` de `modelo-dominio.md` §4 con sus invariantes de creación, y nada más (duda 2). `Empresa.Crear` recibe los datos de contacto como parámetros sueltos, sin `DatosContacto` (duda 8).
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias` — las 4 de arquitectura siguen en verde.

### T4 — `Profesional` con `Horario`, `ExcepcionHorario` y `ServiciosQuePresta` (≈ 0,75 h)

- **Ficheros:** crea `Dominio/Entidades/Profesional.cs`, `Horario.cs`, `ExcepcionHorario.cs`, `ProfesionalServicio.cs` (duda 5)
- **Implementa:** RN-01, RN-32, RN-40, RN-45, SPEC §9.6 a §9.9, `modelo-dominio.md` §2.1, §2.3, §4.6 a §4.9
- **Hecho cuando:** colecciones como campo privado expuesto como `IReadOnlyCollection<Horario>`, `IReadOnlyCollection<ExcepcionHorario>` e `IReadOnlySet<Guid>`. `Horario` y `ExcepcionHorario` llevan `EmpresaId` desnormalizado. `ProfesionalServicio` es un tipo pequeño sin `EntidadBase` ni identidad propia, respaldo interno de `ServiciosQuePresta` (duda 5). Sin `AgregarIntervalo` ni el resto del comportamiento de horarios (Fase 9, duda 2).
- **Verificación:** pruebas de arquitectura en verde.

### T5 — `Reserva` (≈ 0,5 h)

- **Ficheros:** crea `Dominio/Entidades/Reserva.cs`
- **Implementa:** RN-79, RN-82, RN-100, SPEC §9.10, `modelo-dominio.md` §4.10 y §9.1 capa 3
- **Hecho cuando:** `Franja` es `FranjaHoraria`; `NombreCliente` y `TelefonoCliente` no admiten nulo; `Estado` nace `Confirmada`. `Crear` recibe el `Servicio` completo y calcula `FinUtc` (`modelo-dominio.md` §4.10). Sin las tres transiciones (Fase 11, duda 2). La genera la IA como el resto (duda 7).
- **Verificación:** pruebas de arquitectura en verde.

### T6 — `Usuario`, `ContextoDatos` y registro (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Identidad/Usuario.cs`, `Infraestructura/Persistencia/ContextoDatos.cs`, `Infraestructura/InyeccionDependencias.cs` · modifica `Web/Program.cs`
- **Implementa:** D-13, D-19, A-04, A-05, SPEC §9.1, §14, `arquitectura.md` §8.5, §9.1, §9.2, §11.3
- **Hecho cuando:** `Usuario` en `Infraestructura/Identidad`, sin `EntidadBase`, con setters privados y `[ProtectedPersonalData]` en `NombreCompleto` y `Telefono` (duda 3). `ContextoDatos : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>`, `base.OnModelCreating` y `ApplyConfigurationsFromAssembly`. `AgregarInfraestructura` registra la fábrica con `UseNpgsql`, `UseSnakeCaseNamingConvention` y `NoTracking`, y el puente scoped de §9.2. La cadena se llama `PlataformaReservas` y **no** está en `appsettings.json`. Sin `HasQueryFilter` (AGENTS.md §3.8).
- **Verificación:** `dotnet build`.

### T7 — Configuraciones Fluent API (≈ 0,75 h)

- **Ficheros:** crea una clase por entidad en `Infraestructura/Persistencia/Configuraciones/` (`ConfiguracionCategoria`, `ConfiguracionEmpresa`, `ConfiguracionMiembroEmpresa`, `ConfiguracionServicio`, `ConfiguracionProfesional`, `ConfiguracionReserva`, y las de `Usuario` si hace falta) (duda 8)
- **Implementa:** SPEC §9, §14 (índices, únicos, `CHECK`, `Restrict`), RN-06, RN-10, RN-45, RN-101, `modelo-dominio.md` §7 y §8
- **Hecho cuando:**
  - Longitudes de SPEC §9, `decimal(10,2)`, enumeraciones como entero.
  - Todos los índices, únicos y `CHECK` de la lista de SPEC §14, **ni uno más**: en particular, **sin** índice B-tree sobre `(profesional_id, inicio_utc)` (SPEC §13).
  - `Franja` aplanada en columnas llamadas exactamente `inicio_utc` y `fin_utc`, y `Politicas` en `intervalo_huecos_minutos`, `antelacion_minima_minutos`, `antelacion_maxima_dias` (la convención generaría `franja_inicio_utc`, que rompería el SQL de §13).
  - Colecciones con `PropertyAccessMode.Field`. `DeleteBehavior.Restrict` en toda FK hacia `reservas`.
  - `ServiciosQuePresta` mapeado con `OwnsMany` a `profesional_servicio`, con PK compuesta `(profesional_id, servicio_id)` (duda 5).
- **Verificación:** revisión del SQL de la migración de T9.

### T8 — Conversor de cifrado para las copias de `Reserva` (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Persistencia/ConversorCifrado.cs` · modifica `ConfiguracionReserva`
- **Implementa:** RN-163, D-22, SPEC §15.1, `arquitectura.md` §8.5 ("Lo que Identity NO cubre")
- **Hecho cuando:**
  - `NombreCliente` y `TelefonoCliente` usan `HasConversion(new ConversorCifrado(protector))`, con el `IPersonalDataProtector` de Identity obtenido del contenedor al construir el modelo (duda 4).
  - La implementación real es de la Fase 6. Hasta entonces, un doble en `PruebasIntegracion`.
  - `Observaciones` y todos los datos de `Empresa` **no** se cifran (RN-164).
  - **Si el tiempo de diseño de `dotnet ef` obliga a crear una pieza no documentada, se para y se pregunta**; la salida prevista es trasladar T8 a la Fase 6 (duda 4, opción b).
- **Verificación:** prueba de integración con el doble: guardar y leer una `Reserva` devuelve el nombre y el teléfono originales, y el doble ha sido invocado.

### T9 — Migración inicial con `EXCLUDE` (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Persistencia/Migraciones/<fecha>_Inicial.cs` y su designer y snapshot (`dotnet ef migrations add Inicial`, comando de `AGENTS.md` §11) (duda 8)
- **Implementa:** RN-77, RN-97, D-07, SPEC §13, `arquitectura.md` §9.5
- **Hecho cuando:** `Up()` termina con los dos `migrationBuilder.Sql(...)` **literales** de SPEC §13; `Down()` empieza con `ALTER TABLE reservas DROP CONSTRAINT ex_reservas_sin_solape;`. Tablas con los nombres de `modelo-dominio.md` §8.
- **Verificación:** lectura del SQL con `dotnet ef migrations script`.

### T10 — Contenedor compartido y prueba de esquema (≈ 0,75 h)

- **Ficheros:** crea `PruebasIntegracion/Infraestructura/BaseDatosFixture.cs` y `ColeccionBaseDatos.cs`, y `PruebasIntegracion/Persistencia/PruebasEsquema.cs` (duda 8)
- **Implementa:** A-10, R-3, R-5, RN-77, `arquitectura.md` §12.2, `PROJECT_PLAN.md` §6
- **Hecho cuando:**
  - `BaseDatosFixture` con `postgres:latest` (era `16-alpine`; cambiado tras la revisión, §11), **aplica las migraciones** (nunca `EnsureCreated`, con el comentario que pide §12.2).
  - Una prueba consulta `pg_constraint` y comprueba que `ex_reservas_sin_solape` existe.
  - Una prueba inserta varias filas con `Guid.CreateVersion7()` y comprueba si el orden de `uuid` en PostgreSQL coincide con el de inserción. **El resultado, sea cual sea, se documenta** en T12 (R-5).
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion` con Docker levantado.

### T11 — Datos iniciales solo en desarrollo (≈ 0,5 h)

- **Ficheros:** crea `Infraestructura/Persistencia/DatosIniciales/SembradorDatos.cs` y `MigrarYSembrarAsync` · modifica `Web/Program.cs`
- **Implementa:** RN-122, RN-123, SPEC §9.2, §14 (datos iniciales), `arquitectura.md` §10.1
- **Hecho cuando:** en `Development` se migra y se siembran **solo las 8 categorías** de SPEC §9.2, de forma idempotente (duda 6). Empresas y usuarios de demostración, no.
- **Verificación:** prueba de integración: sembrar dos veces deja exactamente 8 categorías.

### T12 — `docs/base-datos.md` y cierre (≈ 0,5 h)

- **Ficheros:** crea `docs/base-datos.md`
- **Implementa:** SPEC §18, R-5
- **Hecho cuando:** documenta tablas, índices, `CHECK`, `EXCLUDE` con su justificación (SPEC §13), qué columnas van cifradas, y el resultado real de la comprobación de `Guid` v7. `dotnet build` y `dotnet test` en verde.
- **Verificación:** `dotnet build`, `dotnet test`.

**Suma: 7 h de 7 h presupuestadas.** No queda margen. El mayor riesgo de desviación está en T1 (si el `.editorconfig` obliga a retocar código existente) y en T8 (tiempo de diseño de `dotnet ef`).

## 6. Pruebas

| Prueba | Tipo | Regla que verifica | Tarea |
|---|---|---|---|
| `FranjaHoraria` rechaza `Kind` distinto de `Utc` | U | RN-100 | T2 |
| `FranjaHoraria` rechaza `FinUtc <= InicioUtc` | U | RN-50 | T2 |
| `SeSolapaCon`: consecutivas no se solapan, parciales sí | U | RN-50, RN-51 | T2 |
| `IntervaloHorario` rechaza `HoraInicio >= HoraFin` | U | RN-41 | T2 |
| Las 4 pruebas de arquitectura siguen en verde **con entidades reales** | A | D-14 | T3, T4, T5 |
| `ex_reservas_sin_solape` existe en `pg_constraint` | I | RN-77, R-3 | T10 |
| Orden de `Guid` v7 en PostgreSQL | I | `PROJECT_PLAN.md` §6, R-5 | T10 |
| Guardar y leer una `Reserva` pasa por el conversor | I | RN-163 | T8 |
| Sembrar dos veces deja 8 categorías | I | RN-122, RN-123 | T11 |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

Los rangos de `PoliticasReserva` (RN-26 a RN-29) se prueban en la Fase 10 (SPEC §19, duda 2). El cifrado real en la tabla (RN-163 con SQL crudo) se prueba en la Fase 17.

## 7. Criterios de aceptación

- [x] Existen las entidades de SPEC §9 con los nombres literales, y solo los cuatro objetos de valor de M-05. *(comprobado el 2026-10-03: nueve en `Dominio/Entidades` (con `ProfesionalServicio`, duda 5), `Usuario` en `Infraestructura/Identidad` (duda 3) y los cuatro objetos de valor)*
- [x] Las 4 pruebas de arquitectura en verde **con entidades reales**, sin haberlas modificado. *(comprobado el 2026-10-12: `PruebasArquitectura.cs` no cambia entre `8f49ad1` y `31c073d` (git), y pasa en la primera revisión de la Fase 6, sobre la base `31c073d`. La quinta prueba llegó después, en `0ec9dd0`)*
- [x] `Dominio` sigue sin referencias a EF Core, Npgsql ni ASP.NET. *(comprobado el 2026-10-03: el `.csproj` no tiene referencias ni paquetes)*
- [x] Una configuración `IEntityTypeConfiguration<T>` por entidad; `OnModelCreating` sin configuración propia. *(comprobado el 2026-10-03: una clase por entidad; `OnModelCreating` solo aplica configuraciones)*
- [x] Sin `HasQueryFilter` ni `IgnoreQueryFilters()`. *(comprobado el 2026-10-03: ninguna aparición)*
- [x] La migración crea todos los índices, únicos y `CHECK` de SPEC §14, y la restricción de §13 con su `Down()`. *(comprobado el 2026-10-03: los dos índices compuestos de `reservas` y la restricción van en SQL manual, con su `Down()`)*
- [x] La prueba de integración confirma `ex_reservas_sin_solape` en el catálogo de PostgreSQL. *(comprobado el 2026-10-12: `PruebasEsquema` en verde en todas las revisiones desde la Fase 6; la primera, con 24 pruebas de integración)*
- [x] `NombreCliente` y `TelefonoCliente` configurados con conversor. *(comprobado el 2026-10-03: `ConfiguracionReserva`; el conversor solo se aplica si hay protector registrado)*
- [x] Ninguna cadena de conexión ni clave en el repositorio. *(comprobado el 2026-10-03: `appsettings*.json` limpios, `.env` ignorado y no versionado, secretos en User Secrets)*
- [x] `docs/base-datos.md` existe e incluye el resultado de la comprobación de `Guid` v7. *(comprobado el 2026-10-03: §8: la monotonía se conserva)*
- [x] `dotnet build` sin avisos y `dotnet test` en verde. *(comprobado el 2026-10-12 con evidencia posterior: la primera revisión de la Fase 6 (2026-10-03 19:57, commit base `31c073d`) ejecutó 46 pruebas unitarias y 24 de integración, todas en verde, y 0 avisos)*

Esta fase no cierra ningún punto de control. Adelanta la tercera línea del Control 3 (`PROJECT_PLAN.md` §3), que se volverá a comprobar en la Fase 11.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-3 | La prueba pasa y la restricción no existe en la base de pruebas | T10 aplica migraciones, nunca `EnsureCreated`, y consulta `pg_constraint` |
| R-2 | `DateTime` no UTC rechazado por Npgsql al guardar | `FranjaHoraria` valida `Kind` en el constructor (T2); `FechaAlta` y demás se fijan en UTC |
| R-5 | Se afirma una ventaja de `Guid` v7 que no existe | T10 mide el orden real y T12 documenta el resultado, sea el que sea |
| R-11 | Copias de `Reserva` en claro al lado de un usuario cifrado | T8 configura el conversor en esta fase; la prueba con SQL crudo es de la Fase 17 |
| R-1 | `AddDbContextFactory` a secas rompe Identity en la Fase 6 | T6 registra fábrica **y** puente scoped (§9.2) |
| Propio, sin código R-xx | `ComplexProperty` genera `franja_inicio_utc` y el SQL de §13 falla al migrar | T7 fija `HasColumnName` explícito; T10 lo detecta si falla |

Los demás (R-4, R-6 a R-10, R-12 a R-14) no aplican: tratan de Identity, eventos, correo, CSP o autenticación.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Implementación de `IPersonalDataProtector`, `ILookupProtector` y el anillo de claves; `ProtectPersonalData = true` | Fase 6 |
| `AddIdentityCore`, roles, `FabricaClaimsUsuario`, páginas de cuenta | Fase 6 |
| Repositorios, `IContextoEmpresa`, `IRelojSistema` | Fase 7 |
| Casos de uso y `Resultado` | Fases 7 en adelante |
| `AgregarIntervalo`, `MarcarNoDisponible` y resto del comportamiento de horarios | Fase 9 |
| Invariantes de rango de `PoliticasReserva` y sus pruebas | Fase 10 |
| `CancelarPorCliente`, `CancelarPorEmpresa`, `Completar` | Fase 11 |
| Empresas y usuarios de demostración | Fase 9, propuesta de la duda 6 — **pendiente de reflejar en SPEC §14/§19** |
| `TraductorErroresPostgres` (`23P01`) y prueba de concurrencia | Fase 11 |
| Eventos de dominio en `EntidadBase`, `OrigenCancelacion` | Fase 11b |
| `Dockerfile` y servicio `web` en `docker-compose.yml` | Fase 18 |
| Prueba de cifrado con SQL crudo | Fase 17 |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-01 | T3, T4, T5 | Ausencia de método que cambie `EmpresaId`; arquitectura (sin setters) |
| RN-06, RN-10, RN-45 | T7, T9 | Índices únicos en la migración |
| RN-14 | T3, T7 | Columnas no nulas de `empresas` |
| RN-20, RN-21 | T7, T9 | `CHECK` de la migración |
| RN-32 | T4, T7 | PK compuesta de `profesional_servicio` |
| RN-41 | T2 | `IntervaloHorario` rechaza inicio ≥ fin |
| RN-50, RN-51 | T2 | Pruebas de `SeSolapaCon` |
| RN-77, RN-97 | T9, T10 | `ex_reservas_sin_solape` en `pg_constraint` (con `WHERE estado <> 1`) |
| RN-79, RN-82 | T5, T7 | Columnas no nulas; `Reserva.Crear` deja el estado en `Confirmada` |
| RN-90 | T1 | Valores literales de la enumeración |
| RN-100 | T2 | `FranjaHoraria` rechaza `Kind` no UTC |
| RN-101 | T7 | `time` y `date` sin conversión |
| RN-120, RN-121 | T3, T4, T5 | Sin métodos de borrado en las raíces; revisión |
| RN-122, RN-123 | T11 | Sembrar dos veces deja 8 categorías |
| RN-163 | T8 | Ida y vuelta por el conversor con el doble; prueba definitiva con SQL crudo en la Fase 17 |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-09-30. Se conservan para dejar rastro de cada decisión.

### Duda 1 — El nuevo `.editorconfig` y el código de los documentos

- **Pregunta:** `.editorconfig` incluye ahora reglas de estilo con severidad `warning`/`error` (`var` prohibido, prefijo `_`, llaves obligatorias). Con `EnforceCodeStyleInBuild` y `TreatWarningsAsErrors` son errores de compilación. Los ejemplos de los documentos usan `var` (incluido `PruebasArquitectura.cs`, que según AGENTS.md no se modifica). ¿Compila hoy la solución? ¿Se sigue el `.editorconfig` o los ejemplos?
- **Opciones:** (a) el código nuevo sigue el `.editorconfig` (tipos explícitos) y, si `PruebasArquitectura.cs` no compila, se decide aparte si se adapta; (b) bajar esas reglas a `suggestion`; (c) retirar las reglas añadidas.
- **Recomendación y motivo:** (a), pero **comprobando primero** `dotnet build`: si las pruebas de arquitectura ya no compilan, es un cambio que solo puede autorizar el desarrollador (CLAUDE.md, "si alguna falla, el código está mal, no la prueba"). Además, estas reglas no están en `arquitectura.md` §3.3.
- **Bloquea:** todas.
- **Resolución:** **predomina siempre el `.editorconfig`**. Todo el código nuevo lo cumple aunque los ejemplos de los documentos digan otra cosa. Si el código existente no compila, T1 lo corrige; en `PruebasArquitectura.cs`, solo cambios de estilo, nunca de lógica.

### Duda 2 — Cuánto comportamiento llevan las entidades en esta fase

- **Pregunta:** SPEC §19 reparte el comportamiento: horarios en la 9, `PoliticasReserva` en la 10, transiciones de reserva en la 11. ¿Qué entra ahora?
- **Opciones:** (a) **forma completa sin comportamiento de fases futuras**: propiedades, constructores privados, colecciones, las fábricas `Crear(...)` de `modelo-dominio.md` §4 con sus invariantes de creación, y los objetos de valor con su validación de constructor; sin `AgregarIntervalo`, sin transiciones y sin los rangos de `PoliticasReserva`; (b) entidades completas con todo el comportamiento de `modelo-dominio.md` §4; (c) solo propiedades, sin fábricas.
- **Recomendación y motivo:** (a). Respeta el reparto de SPEC §19 y cabe en el presupuesto. (b) adelanta horas de las fases 9 a 11 y no cabe en 6,75 h. (c) deja entidades que no se pueden construir. Con (a), `PoliticasReserva` se crea sin comprobar rangos hasta la Fase 10: es una ventana en la que existen instancias inválidas, aceptable porque nada las usa aún.
- **Bloquea:** T2, T3, T4, T5, T11.
- **Resolución:** opción (a). Forma completa y fábricas `Crear(...)`; sin comportamiento de las fases 9, 10 y 11.

### Duda 3 — Dónde vive `Usuario`

- **Pregunta:** `Usuario : IdentityUser<Guid>` (SPEC §9.1) necesita el paquete de Identity, y `Dominio` no puede tener paquetes (`arquitectura.md` §2.2). `arquitectura.md` §2.3 no lo sitúa en ninguna carpeta, y el ejemplo de §8.5 le da `{ get; set; }` públicos.
- **Opciones:** (a) en `Infraestructura/Identidad/Usuario.cs`, sin heredar de `EntidadBase` (las pruebas de arquitectura no lo ven), con setters privados y los métodos de `modelo-dominio.md` §4.1 cuando toque (Fase 6/15); (b) en `Dominio`, añadiéndole el paquete de Identity (rompe §2.2 y probablemente la cuarta prueba de arquitectura).
- **Recomendación y motivo:** (a). Es la única compatible con §2.2 y con la cuarta prueba. Conviene anotarlo en `arquitectura.md` §2.3.
- **Bloquea:** T6.
- **Resolución:** opción (a). `Infraestructura/Identidad/Usuario.cs`, sin `EntidadBase` y con setters privados.

### Duda 4 — El conversor necesita un protector que no existe hasta la Fase 6

- **Pregunta:** `ConversorCifrado(protector)` necesita un `IPersonalDataProtector`, cuya implementación AES-256-GCM es de la Fase 6. Además, `dotnet ef migrations add` construye el modelo y necesitaría un protector también en tiempo de diseño.
- **Opciones:** (a) en esta fase el conversor recibe la interfaz `IPersonalDataProtector` de Identity y `ContextoDatos` la obtiene del contenedor al construir el modelo (como hace el propio Identity); en la Fase 6 se registra la implementación real, y hasta entonces una implementación de pruebas en `PruebasIntegracion`; (b) trasladar T8 entera a la Fase 6 y sumar allí sus 0,75 h; (c) conversor con una clave de desarrollo fija en esta fase.
- **Recomendación y motivo:** (a), si el tiempo de diseño se resuelve sin crear piezas nuevas; si no, (b). (c) mete material criptográfico provisional, justo lo que R-9 y RN-168 quieren evitar. Con (b), las columnas se crean igual (`text`) y no hace falta migración nueva.
- **Bloquea:** T8.
- **Resolución:** opción (a). Interfaz de Identity y doble de pruebas. Si el tiempo de diseño exige una pieza no documentada, se para y se pregunta, con (b) como salida.

### Duda 5 — Cómo se mapea `ServiciosQuePresta` a `profesional_servicio`

- **Pregunta:** `modelo-dominio.md` §2.3 y §4.9 dicen que en el dominio es una colección de `Guid` y en la base una tabla de unión con PK compuesta. EF Core no mapea una colección de `Guid` a una tabla aparte: la convierte en una columna (`uuid[]` en Npgsql).
- **Opciones:** (a) un tipo propio pequeño en `Dominio` (p. ej. `ProfesionalServicio`, sin `EntidadBase`, sin identidad propia) mapeado con `OwnsMany` a `profesional_servicio`, expuesto hacia fuera como `IReadOnlySet<Guid>`; (b) aceptar la columna `uuid[]` y cambiar el esquema documentado; (c) `ProfesionalServicio` como entidad hija con `Id`.
- **Recomendación y motivo:** (a). Conserva la tabla, la PK compuesta y la consulta `EXISTS` de RN-16, y la interfaz pública de `Profesional` no cambia. Contradice en parte "no existe como entidad de dominio" (§4.9), por eso hay que decidirlo.
- **Bloquea:** T4, T7.
- **Resolución:** opción (a). Tipo `ProfesionalServicio` en `Dominio/Entidades`, mapeado con `OwnsMany` a `profesional_servicio`; `Profesional` expone `IReadOnlySet<Guid>`.

### Duda 6 — Qué datos iniciales entran en esta fase

- **Pregunta:** SPEC §14 pide categorías, 2 empresas de demostración **completamente configuradas** y usuarios de prueba. Los usuarios necesitan Identity y cifrado (Fase 6: "cifrar desde la Fase 6 no es casualidad", `PROJECT_PLAN.md` §2), y los horarios necesitan `AgregarIntervalo` (Fase 9).
- **Opciones:** (a) en esta fase solo las 8 categorías de SPEC §9.2 y la infraestructura `MigrarYSembrarAsync`; empresas y usuarios de demostración cuando existan Identity con cifrado y los horarios; (b) todo ahora, con usuarios sin cifrar que habría que migrar después; (c) nada ahora.
- **Recomendación y motivo:** (a). (b) contradice D-22 y el motivo de `PROJECT_PLAN.md` §2. Hay que decidir **en qué fase** se siembra el resto (propuesta: la Fase 9, cuando ya hay horarios), porque ninguna lo tiene asignado.
- **Bloquea:** T11.
- **Resolución:** opción (a). Solo las 8 categorías en esta fase. **Queda pendiente del desarrollador** reflejar en SPEC §14/§19 en qué fase se siembran empresas y usuarios de demostración (propuesta: Fase 9).

### Duda 7 — ¿Escribe `Reserva` el desarrollador a mano?

- **Pregunta:** `modelo-dominio.md` §9.1 capa 3 dice que "la palanca más fuerte" es escribir `Reserva.cs` a mano y la primera, para que la IA imite el patrón.
- **Opciones:** (a) T5 la hace el desarrollador **antes que T3 y T4**, y la IA la revisa; (b) la genera la IA como el resto.
- **Recomendación y motivo:** (a). Es lo que dice el documento. Si se elige, T5 pasa a ser la primera tarea de entidades.
- **Bloquea:** T3, T4, T5 (orden).
- **Resolución:** opción (b). `Reserva` la genera la IA, en el orden de las tareas. Se aparta de `modelo-dominio.md` §9.1 capa 3.

### Duda 8 — Nombres que no están en los documentos

- **Pregunta:** faltan nombres para: las clases de configuración (`arquitectura.md` §2.3 solo dice "una clase por entidad"), la migración inicial, los ficheros de prueba de integración, la clase de siembra y `DatosContacto` (aparece en la firma de `Empresa.Crear`, `modelo-dominio.md` §4.3, sin definir; y M-05 prohíbe un quinto objeto de valor).
- **Opciones:** (a) aceptar los propuestos en las tareas (`Configuracion<Entidad>`, `Inicial`, `PruebasEsquema`, `SembradorDatos`) y que `Empresa.Crear` reciba los datos de contacto como parámetros sueltos; (b) otros que indique el desarrollador.
- **Recomendación y motivo:** (a), por coherencia con la nomenclatura de §5 y para no crear un quinto objeto de valor.
- **Bloquea:** T3, T7, T9, T10, T11.
- **Resolución:** opción (a). `Configuracion<Entidad>`, migración `Inicial`, `PruebasEsquema`, `SembradorDatos`, y `Empresa.Crear` sin `DatosContacto`.

### Duda 9 — Base de datos local (añadida tras la aprobación)

*No la detectó `/fase`: salió al preguntar el desarrollador cuándo había que tener la base de datos levantada.*

- **Pregunta:** SPEC §17 asignaba `docker-compose.yml` a la Fase 18, pero `AGENTS.md` §11 lo usa desde el principio y esta fase necesita una base local para `dotnet ef database update` y para arrancar la aplicación en desarrollo (T11).
- **Resolución:** aprobada por el desarrollador el 2026-09-30. Nueva decisión **D-26**: la composición con `postgres` nace en esta fase (T0) y la Fase 18 le añade `web` y el `Dockerfile`. `dotnet-ef` pasa a herramienta local. Se trasladan 0,25 h de la Fase 18 a esta, sin cambiar el total. **Documentos actualizados:** SPEC §17, §19 y D-26, `arquitectura.md` §2, §2.2 y §11.4, `PROJECT_PLAN.md` §1 y §2.
- **Efecto sobre la aprobación:** la especificación sigue `APROBADA`; esta duda deja constancia del cambio posterior.

### Decisiones tomadas durante la implementación (2026-09-30)

Todas decididas por el desarrollador. La especificación sigue `APROBADA`.

- **Firmas de `Crear` (T1-T3).** Reciben `Guid id` como primer parámetro y `DateTime ahoraUtc` como último, para cumplir D-16 y la regla del reloj inyectado. Se apartan de las firmas literales de `modelo-dominio.md` §4.
- **`DominioException` (T1).** Sustituye a `ExcepcionDominio`, porque CA1710 exige el sufijo `Exception`. Se añadió la "excepción de sufijo" a la nomenclatura de `AGENTS.md` §5 y SPEC §3.
- **Decisión 1 de T4-T12: conversor condicional (T8).** Si no hay ningún `IPersonalDataProtector` registrado (en el diseño de `dotnet ef` y en desarrollo hasta la Fase 6), el modelo se construye sin conversor. El esquema no cambia. Ver `base-datos.md` §7 y R-11.
- **Índices de clave ajena (T7).** Se aceptan los que añade EF por convención. Dos de ellos, `reservas(empresa_id)` y `reservas(usuario_id)`, quedan redundantes con los compuestos, que van **a mano** en la migración porque EF no indexa columnas de un tipo complejo. Ver `base-datos.md` §3.
- **Sufijo `Utc`.** Todo instante se sigue guardando en UTC (D-08 intacta). Lo que desaparece es la **regla de nombre**: `FechaAlta`, `FechaModificacion` y `FechaCancelacion` se quedan así. Se actualizaron `AGENTS.md` §3.5 y §9, RN-100 y SPEC §20.
- **`IntervaloHorario.SeSolapaCon` se retira (T2 y T4)** para no reimplementar la regla de solapamiento (AGENTS.md §3.7). **Queda pendiente para antes de la Fase 9** resolver la contradicción con `modelo-dominio.md` §5.2, que declara ese método para RN-42.
- **Tablas de Identity en `snake_case` (T7).** `IdentityDbContext` fija `AspNetUsers` y la convención no lo reescribe. Se fijan como `asp_net_*`, que es lo que esperan `modelo-dominio.md` §8, el Control 1 y RN-162.
- **Migraciones como código generado (T9).** En `.editorconfig`, `generated_code = true` para `Infraestructura/Persistencia/Migraciones/`, porque CA1861 saltaba sobre el código que genera EF (A-18). Anotado en la tabla de supresiones de `arquitectura.md` §3.3.

### Decisiones tomadas tras la primera revisión (2026-10-03)

Corrigen los hallazgos de `fase-05-revision.md` (revisión del 2026-09-30). Todas decididas por el desarrollador.

- **Radio de cambio ampliado (#1, #2, #7, #9).** §4 recoge ahora lo que se tocó por decisiones aprobadas sobre la marcha: `.editorconfig`, los documentos de las decisiones anteriores, `Compartido/Validacion` y los ficheros de prueba.
- **`Validacion` (#7).** Es una clase `internal static` en `Dominio/Compartido` con las comprobaciones comunes de las fábricas `Crear`: identificador, texto obligatorio, texto opcional e instante UTC. Anotada en `arquitectura.md` §2.3.
- **Nombres no previstos en la duda 8 (#9).**
  - `ProtectorFalso`: el doble de T8.
  - `PruebasDatosIniciales`: la prueba de T11.
  - `ConfiguracionesIdentidad.cs`: seis clases pequeñas en un fichero, una por tabla de Identity, que solo fijan su nombre `asp_net_*`.
- **Identificadores de las categorías en `Infraestructura` (#14).** Es una excepción consciente a D-16/M-03, solo para los datos iniciales: el sembrador no pasa por ningún caso de uso.
- **`postgres:latest` en local y en pruebas, sin `.env.example` (#3, #4).**
  - Las dos usan la misma imagen, y siempre la última versión.
  - El volumen se monta en `/var/lib/postgresql`, como pide PostgreSQL 18 o posterior.
  - El desarrollador crea `.env` sin plantilla: las variables están descritas en `arquitectura.md` §11.4.
  - `docker-compose.yml` lo actualiza el desarrollador.
- **Conversor que falla en cerrado (#5): aplazado a la Fase 6.** Con el protector real registrado, `ObtenerProtector()` lanzará si falta, salvo en el tiempo de diseño de `dotnet ef`, y habrá una prueba con `AgregarInfraestructura` real. Anotado en `base-datos.md` §7.
- **Validación de `Empresa.Crear` (#11).**
  - Los datos de contacto son obligatorios y tienen la longitud de SPEC §9.3. La descripción es opcional, de 1000 como máximo.
  - El formato del correo se valida en `Aplicacion`.
  - `Categoria.Crear` acota el slug a 80 caracteres (#12).
- **Aplazados.**
  - #13, el `HashSet` de `ServiciosQuePresta`: Fase 9, con `Presta`.
  - #15, el fixture con `AgregarInfraestructura`: Fase 7, cuando haya repositorios.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos *(comprobado el 2026-10-12 con evidencia posterior: la primera revisión de la Fase 6 (2026-10-03 19:57, commit base `31c073d`) ejecutó 46 pruebas unitarias y 24 de integración, todas en verde, y 0 avisos)*
- [x] `dotnet test` en verde *(ver la línea anterior)*
- [ ] `/revisar 5` con veredicto `APTO` *(no consta, 2026-10-12: no existe `fase-05-revision.md`. El código de la Fase 5 quedó dentro de la primera revisión de la Fase 6)*
- [x] Commit hecho *(`be4e1bb` y `31c073d`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Punto de control superado, si esta fase cierra alguno (no cierra ninguno) *(comprobado el 2026-10-03: no aplica)*
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas: **`Guid` v7** (T10 y T12) *(comprobado el 2026-10-03: `docs/base-datos.md` §8)*
