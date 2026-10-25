# AGENTS.md — PlataformaReservas

Instrucciones para cualquier agente de IA que trabaje en este repositorio.
Fichero **canónico y agnóstico de herramienta**: Claude Code, Codex, Copilot y cualquier otro parten de aquí.

> Si eres Claude Code, `CLAUDE.md` importa este fichero y añade lo específico de tu flujo.

---

## 1. Qué es este proyecto

Plataforma web multiempresa (SaaS multi-tenant) de reserva de servicios. TFM.
Monolito modular con Clean Architecture sobre .NET 10, Blazor y PostgreSQL.

**El producto no lleva IA.** La IA es solo la herramienta de desarrollo.

## 2. Fuentes de verdad

Por orden de autoridad. Si hay contradicción, gana el de más arriba.

| Documento | Qué define |
|---|---|
| `TFM_PlataformaReservas_SPEC.md` | Alcance, decisiones cerradas (D-xx), entidades, rutas, motor, concurrencia |
| `docs/reglas-negocio.md` | Las 140 reglas de negocio (RN-xx), 29 críticas |
| `docs/modelo-dominio.md` | Agregados, invariantes, objetos de valor, comportamiento |
| `docs/arquitectura.md` | Proyectos, paquetes, casos de uso, `Resultado`, DI, persistencia, autenticación |
| `docs/casos-uso.md` | Los 24 casos de uso (CU-xx) y sus flujos |
| `PROJECT_PLAN.md` | Calendario y puntos de control |

**Regla de oro:** si algo no está en estos documentos, **no está en el alcance**. No lo inventes, no lo añadas "por si acaso", no lo sugieras dentro del código. Si detectas un hueco, **para y pregunta**.

---

## 3. Reglas absolutas

Estas nueve no se negocian. Romper cualquiera invalida el trabajo.

1. **No amplíes el alcance.** Ni entidades, ni campos, ni endpoints, ni pantallas, ni paquetes que no estén en los documentos.
2. **No salgas del radio de cambio** que se te haya indicado. Si te dicen "no toques Infraestructura", no la toques.
3. **Nomenclatura en castellano** sin tildes ni eñes en identificadores. Rutas web en inglés (§5).
4. **Modelo rico.** Sin `set` ni `init` públicos en entidades (§7).
5. **Todo instante absoluto es UTC** (§9).
6. **Toda consulta privada filtra por empresa en origen** (§8).
7. **Nunca reimplementes la regla de solapamiento.** Existe una vez por tipo: `FranjaHoraria.SeSolapaCon` para reservas (UTC) e `IntervaloHorario.SeSolapaCon` para el horario semanal (hora local). *Fase 9, duda 1.* Única excepción: una consulta puede acotar en SQL por rango con la misma condición semiabierta (`Inicio < hasta && Fin > desde`), solo para traer menos filas; la decisión de dominio (descartar un hueco, rechazar una reserva) la toma siempre `SeSolapaCon`. *Fase 10, duda 5.*
8. **Sin filtros globales de consulta de EF Core** (`HasQueryFilter`) ni `IgnoreQueryFilters()`.
9. **Si una decisión no está tomada, pregunta.** No elijas tú y sigas.
10. **No comentar el código**. El código no se comenta, es un code smell, si está bien escrito y bien definido se auto-explica. Únicamente comentar en puntos críticos.

---

## 4. Stack — fijo, no negociable

```
.NET 10 · C# · ASP.NET Core · Blazor Web App
Entity Framework Core + Npgsql · PostgreSQL 16+
ASP.NET Core Identity · FluentValidation · Serilog
xUnit · FluentAssertions · Testcontainers
Docker · Docker Compose
```

**No propongas alternativas fuera de este stack**, ni siquiera como comentario.

**No añadas paquetes** sin pedirlo antes. Especialmente: nada de MediatR, AutoMapper, buses de eventos ni bibliotecas de CQRS. El proyecto los descarta a propósito.

Paquetes permitidos y ya decididos: `EFCore.NamingConventions` (snake_case), `Testcontainers.PostgreSql`, `Respawn`, `MailKit`.

**Para los eventos no hace falta ningún paquete:** `System.Threading.Channels` viene en la biblioteca base. Nada de RabbitMQ, Kafka ni corredores de mensajes.

**Render de Blazor:** SSR estático en páginas públicas; `InteractiveServer` en reserva, área de cliente y área de empresa. Nunca WebAssembly.

---

## 5. Nomenclatura

| Elemento | Idioma | Ejemplo |
|---|---|---|
| Clases, interfaces, métodos, propiedades | Castellano | `Reserva`, `IMotorDisponibilidad`, `DuracionMinutos` |
| Namespaces y proyectos | Castellano | `PlataformaReservas.Dominio` |
| Tablas y columnas | Castellano `snake_case` | `reservas`, `profesional_id` |
| Comentarios y mensajes de error | Castellano | |
| **Rutas web** | **Inglés** | `/businesses/{slug}`, `/my-account/bookings` |
| Tipos del BCL y de terceros | Su nombre original | `DayOfWeek`, `TimeOnly`, `IdentityUser` |
| **Excepción de carpeta**: `Dominio/ValueObjects/` | Inglés | Término técnico consolidado. **Su contenido sí va en castellano**: `PoliticasReserva`, `FranjaHoraria`, `IntervaloHorario`, `Slug` |
| **Excepción de sufijo**: clases de excepción | Raíz en castellano, sufijo `Exception` | `DominioException`. Lo exige el analizador CA1710, que con `TreatWarningsAsErrors` es un error de compilación |
| **Excepción de plantilla**: páginas de `Web/Cuenta/` | Inglés | Siguen la convención de la plantilla de Identity (`Input`, `InputModel`, `Password`, `ConfirmPassword`), también `AltaEmpresa.razor`. *Autorizado por el desarrollador en la Fase 8* |

> Son las **tres únicas** excepciones: la carpeta `ValueObjects`, el sufijo `Exception` de las clases de excepción y los modelos de formulario de las páginas de `Web/Cuenta/`. `Entidades`, `Eventos`, `Enumeraciones`, `Compartido`, `Abstracciones`, `CasosUso`, `Repositorios` y todas las demás van en castellano. No las tomes como permiso para nombrar otras cosas en inglés.

**Ningún identificador lleva tilde, diéresis ni eñe**, aunque C# lo permita:

```csharp
// MAL
public string Descripción { get; }
public void AñadirIntervalo(...)

// BIEN
public string Descripcion { get; }
public void AgregarIntervalo(...)
```

Vocabulario del dominio, un concepto un nombre: `Empresa` (en la interfaz pública se llama "establecimiento"), `Profesional`, `Usuario`, `HuecoDisponible`. No alternes sinónimos.

---

## 6. Arquitectura

```
src/
├── PlataformaReservas.Dominio          entidades, objetos de valor, invariantes, contratos de repositorio
├── PlataformaReservas.Aplicacion       casos de uso, DTOs, puertos de aplicacion, motor
├── PlataformaReservas.Infraestructura  EF Core, Identity, repositorios
└── PlataformaReservas.Web              Blazor

tests/
├── PlataformaReservas.PruebasUnitarias      -> Dominio, Aplicacion
└── PlataformaReservas.PruebasIntegracion    -> Aplicacion, Infraestructura, Web (reflexion de rutas, Fase 17 duda 2; y clases de Web que no son paginas, Fase 19)
```

Dependencias permitidas, **en un solo sentido**:

```
Dominio  <-  Aplicacion  <-  Infraestructura  <-  Web
```

- `Dominio` no referencia **nada**: ni EF Core, ni Npgsql, ni ASP.NET. Hay una prueba que lo comprueba.
- `Dominio` define los contratos de repositorio (`Dominio/Repositorios`, `IRepositorio*`): el repositorio es un concepto del modelo, una colección de agregados.
- `Aplicacion` define los puertos de aplicación (`Aplicacion/Abstracciones`): contexto de empresa, unidad de trabajo, reloj, cuentas, consultas que devuelven DTOs, correo y eventos.
- `Infraestructura` implementa ambos.
- `Web` **no contiene reglas de negocio**. Si escribes un `if` con una regla en un componente Blazor, está en el sitio equivocado.

**SOLID aplicado aquí, sin ceremonia:** una interfaz por necesidad real, no una por clase. No crees `IServicioX` para una clase que solo tiene una implementación y nunca se sustituye en pruebas.

---

## 7. Modelo de dominio — la desviación más frecuente

Modelo **rico**. Las entidades protegen sus invariantes. Esta es la regla que más veces se rompe por inercia.

```csharp
// MAL — modelo anemico
public class Reserva
{
    public Guid Id { get; set; }
    public EstadoReserva Estado { get; set; }
    public DateTime InicioUtc { get; set; }
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
```

Con eso, una pantalla puede marcar como completada una cita que aún no ha empezado, y cualquiera puede hacer `.Add()` saltándose RN-42.

```csharp
// BIEN
public sealed class Reserva : EntidadBase
{
    private Reserva() { }                          // solo para EF Core

    public EstadoReserva Estado { get; private set; }
    public FranjaHoraria Franja { get; private set; } = null!;

    public static Reserva Crear(/* ... */) { /* valida y construye */ }

    public void Completar(DateTime ahoraUtc)
    {
        if (Estado != EstadoReserva.Confirmada)
            throw new DominioException("Solo se completan reservas confirmadas.");
        if (Franja.FinUtc >= ahoraUtc)
            throw new DominioException("La reserva aun no ha terminado.");
        Estado = EstadoReserva.Completada;
    }
}
```

Reglas de forma, todas comprobadas por pruebas de arquitectura:

- Entidades `sealed`, heredando de `EntidadBase`.
- Constructor privado sin parámetros para EF Core. **Sin constructor público**: se crea con `Crear(...)`.
- Ninguna propiedad con `set` o `init` público.
- Colecciones: campo privado `_horarios`, propiedad `IReadOnlyCollection<Horario>`.
- Los métodos que no pueden cumplir su contrato **lanzan `DominioException`**, no devuelven `bool`.

**EF Core no necesita setters públicos.** Escribe en setters privados y en campos de respaldo. No hay excusa técnica.

Veintidós objetos de valor: `PoliticasReserva`, `IntervaloHorario`, `FranjaHoraria`, `Slug`, `Email`, `Telefono`, `CodigoPostal`, `Precio`, `Duracion`, `NombreCategoria`, `NombreEmpresa`, `DescripcionEmpresa`, `Direccion`, `Ciudad`, `NombreServicio`, `DescripcionServicio`, `NombrePersona`, `Observaciones`, `IntervaloHuecos`, `AntelacionMinimaReserva`, `AntelacionMaximaReserva` y `MotivoExcepcion`. Todo campo de una entidad con una regla de validación es un objeto de valor, también los textos opcionales (la propiedad es anulable, el objeto siempre tiene contenido). Solo los identificadores (`Guid`) y los instantes UTC (`DateTime`) siguen validándose con `Validacion`. No crees más sin reabrir M-05.

**Las fábricas `Crear` y los métodos de dominio reciben objetos de valor, nunca el primitivo que validan.** Construirlos es trabajo de quien llama: el caso de uso traduce el comando, que lleva primitivos (§8 bis).

```csharp
// MAL — obsesion por los primitivos: la entidad orquesta la validacion
public static Servicio Crear(Guid id, Guid empresaId, string nombre, int duracionMinutos, decimal precio, DateTime ahoraUtc)
{
    Duracion = new Duracion(duracionMinutos);   // ...
}

// BIEN — la entidad recibe valores ya validos
public static Servicio Crear(Guid id, Guid empresaId, NombreServicio nombre, Duracion duracion, Precio precio, DateTime ahoraUtc)
```

---

## 8. Multi-tenancy — el error más caro

Base de datos compartida, discriminador `EmpresaId`. Un usuario de la empresa A **jamás** ve datos de la B.

Tres familias de acceso a datos que **no se mezclan**:

| | Repositorios de empresa | Consultas públicas | Eje cliente |
|---|---|---|---|
| **Contratos** | `IRepositorioEmpresas`, `IRepositorioServicios`, `IRepositorioProfesionales`, `IRepositorioReservas`, `IRepositorioMiembros`; y para leer con DTOs, `IConsultasReservasEmpresa` (*Fase 16, duda 1*) | `IConsultasCatalogoPublico`, `IConsultasDisponibilidad` | `IRepositorioReservasCliente` |
| **Contexto** | Su implementación recibe `IContextoEmpresa` | **No reciben** contexto | Su implementación recibe `IContextoUsuario` (el contrato no lo nombra) |
| **Filtro** | Por `EmpresaActualId` **en origen**, en cada consulta | Solo empresas activas y publicables | Por `UsuarioId` **en origen** |
| **Acceso** | Leen y escriben | Solo lectura, devuelven DTOs | Lee y escribe; al agregar comprueba que la reserva es del usuario actual |

**Tercera familia, el eje cliente** (SPEC §10.3, RN-05; *Fase 11, duda 2*): `IRepositorioReservasCliente` filtra por el `UsuarioId` de la sesión **en origen**, y al agregar comprueba que la reserva es del usuario actual. El usuario sale de un claim, como la empresa: `IContextoUsuario` lee el identificador del `ClaimsPrincipal`. **El contrato de `Dominio` no nombra `IContextoUsuario`**: solo lo recibe la implementación de `Infraestructura`, igual que `IRepositorioReservas` no nombra `IContextoEmpresa`. Una reserva de otro usuario es "no encontrada".

**Excepción de segundo plano: `IConsultasNotificacion`** (`docs/arquitectura.md` §13.5; *Fase 16b, revisión*). Los manejadores de eventos se ejecutan en el `ProcesadorEventos`, sin usuario ni sesión, así que no hay contexto de empresa ni de usuario. Lee datos privados filtrando **por el identificador que lleva el evento** (reserva, empresa o usuario), nunca por listados, y es de solo lectura con DTOs. No se usa fuera de `Aplicacion/Notificaciones`.

```csharp
// MAL — filtrar despues, o no filtrar
var servicios = await _db.Servicios.ToListAsync(ct);
return servicios.Where(s => s.EmpresaId == _contexto.EmpresaActualId);

// MAL — filtro global de EF Core
modelBuilder.Entity<Servicio>().HasQueryFilter(s => s.EmpresaId == _contexto.EmpresaActualId);

// BIEN — filtrado en origen
return await _db.Servicios
    .Where(s => s.EmpresaId == _contexto.EmpresaActualId)
    .ToListAsync(ct);
```

**Defensa en profundidad:** toda modificación valida además `entidad.EmpresaId == _contexto.EmpresaActualId` antes de persistir.

**Un recurso ajeno devuelve "no encontrado", nunca "prohibido".** No se revela que existe.

**No hay repositorio de horarios.** `Horario` y `ExcepcionHorario` se guardan con su `Profesional`.

**El contexto de empresa sale de un claim**, no de una consulta: `IContextoEmpresa` lee `empresa_id` del `ClaimsPrincipal`. No añadas consultas de pertenencia por operación.

---

## 8 bis. Casos de uso — forma obligatoria

**Una clase por caso de uso, un solo método `EjecutarAsync`.** Nombre igual al caso de uso que implementa: `CrearReserva`, `CancelarReservaCliente`, `DarDeBajaEmpresa`.

No agrupes operaciones en un `ServicioReservas` con quince métodos. No uses MediatR ni ningún bus.

**Entrada:** un `record` de comando con tipos primitivos. Nunca entidades, nunca tipos de ASP.NET. Un caso de uso sin datos de entrada (la empresa sale del contexto) no lleva comando: `EjecutarAsync(CancellationToken ct)`.
**Salida:** `Resultado` o `Resultado<T>`. **Nunca entidades de dominio**: las pantallas reciben DTOs.
**DTOs:** viven en `Aplicacion/Dtos/<Agregado>/` y cada uno se construye con su método estático `ConvertirADto(entidad, ...)`. Ni el caso de uso ni `Infraestructura` montan un DTO campo a campo. Los puertos de consulta (`IConsultas*`) también devuelven DTOs, nunca entidades. *Decidido en la Fase 10.* Única excepción a "recibe la entidad": cuando una consulta de EF debe proyectar en SQL para no cargar la entidad entera (por ejemplo, para no descifrar datos personales, D-22), el DTO admite una sobrecarga `ConvertirADto` que recibe solo los valores proyectados, y la versión con la entidad delega en ella (`ReservaOcupadaDto.ConvertirADto(Guid, FranjaHoraria)`).

### `Resultado` frente a excepción

| Situación | Qué usar |
|---|---|
| Desenlace de negocio previsto que la interfaz debe tratar bien | `Resultado.Fallo(codigo, mensaje)` |
| Estado imposible, invariante que nunca debió intentarse romper | `throw new DominioException(...)` |
| Fallo técnico | Excepción, sube al manejador global |

```csharp
// MAL — excepcion para un desenlace normal y esperado
if (!huecoLibre) throw new Exception("Hueco ocupado");

// BIEN
if (!huecoLibre)
    return Resultado<Guid>.Fallo(CodigoError.HuecoYaNoDisponible, "Esa hora ya no esta disponible.");
```

El hueco ocupado y el "no encontrado" ocurren **en condiciones normales**: son el sistema funcionando, no una catástrofe.

Los métodos del dominio **siempre lanzan**: no conocen `Resultado`, que vive en `Aplicacion`.

### Transacciones

**Un caso de uso, una transacción.** La abre y la cierra el caso de uso. **Los repositorios nunca llaman a `SaveChanges`.**

### Hora del sistema

Usa siempre `IRelojSistema.AhoraUtc` inyectado. Nunca `DateTime.UtcNow` directamente dentro de un caso de uso o del motor: las pruebas necesitan fijar el reloj.

## 8 ter. Eventos y notificaciones

Las entidades **registran** eventos; el caso de uso los **publica después del commit**. Nunca al revés.

```csharp
// MAL — despachar dentro de SaveChanges o antes de confirmar.
// Un manejador que falla tumba la operacion: es justo lo que se queria evitar.

// BIEN
await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);                              // la reserva ya existe
await _publicador.PublicarAsync(reserva.Eventos, ct);  // encolar, no enviar
reserva.LimpiarEventos();
```

Reglas:

- **Ningún envío de correo dentro de una transacción.** Se encola después de confirmar.
- **Un fallo de correo nunca deshace la operación.** Se registra y se sigue.
- `PublicarAsync` **encola**; el envío ocurre en el `ProcesadorEventos`, en otro hilo y en otro ámbito.
- Los eventos llevan **identificadores, no entidades**: se procesan más tarde, cuando el contexto que cargó la entidad ya no existe. El manejador vuelve a leer.
- El procesador crea **un ámbito de DI nuevo por evento** con `IServiceScopeFactory`. Sin eso, la primera inyección falla.
- El bucle del procesador va **envuelto en `try/catch`**. Una excepción que escape mata el `BackgroundService` en silencio y no vuelve a salir ningún correo.
- `Correo:Habilitado` es **`false` por defecto**. Olvidarlo significa no enviar, nunca enviar de verdad por descuido.
- En pruebas, el manejador se invoca **directamente**; no se levanta el servicio alojado.

## 8 quater. Cifrado de datos personales

Los datos personales van **cifrados en reposo** (D-22). Identity lo hace solo con `ProtectPersonalData`; las copias de `Reserva` van con conversores de EF Core.

```csharp
// MAL — devuelve vacio SIN error, porque la columna esta cifrada
var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == correo, ct);

// BIEN — atraviesa el ILookupProtector
var u = await _userManager.FindByEmailAsync(correo);
```

Es el fallo más traicionero de esta parte: **no lanza excepción, simplemente no encuentra nada.**

Reglas:

- **Nunca consultes por LINQ un campo marcado `[ProtectedPersonalData]`.** Usa los métodos de `UserManager`.
- **Nunca cifres datos de la empresa**: son públicos y `Ciudad` tiene que poder buscarse.
- Si añades un campo personal nuevo a `Usuario`, márcalo `[ProtectedPersonalData]`. Si lo añades a `Reserva`, ponle su conversor.
- Las contraseñas llevan **hash**, no cifrado. No se tocan.
- Las claves salen de configuración, nunca del repositorio, y son **dos distintas**: una para cifrar y otra para el valor de búsqueda.

## 8 quinquies. Endurecimiento

Seis medidas, todas obligatorias (D-23). Detalle en `docs/arquitectura.md` §14.

**`MarkupString` está prohibido** en las plantillas de correo y en la ficha pública. Es el único vector de inyección real del diseño: la descripción de la empresa la escribe el propietario y acaba en HTML dentro de un correo.

```csharp
// MAL — ejecuta lo que el propietario haya escrito
@((MarkupString)datos.Descripcion)
var html = $"<p>{datos.Descripcion}</p>";

// BIEN — Blazor codifica por defecto
@datos.Descripcion
```

Al tocar el inicio de sesión:

```csharp
// MAL — el contador no sube NUNCA y el bloqueo no existe, aunque este configurado
await _signInManager.PasswordSignInAsync(usuario, clave, recordar, lockoutOnFailure: false);

// BIEN
await _signInManager.PasswordSignInAsync(usuario, clave, recordar, lockoutOnFailure: true);
```

Resto de reglas:

- Una cuenta bloqueada da **el mismo mensaje** que una contraseña incorrecta.
- Los intentos fallidos se registran con el **correo enmascarado**, nunca entero.
- La limitación de frecuencia usa `AddRateLimiter` del marco: **sin paquetes**. `UseRateLimiter` va **antes** de `UseAuthentication`.
- No toques la CSP sin probar a mano las tres áreas: una CSP mal puesta **deja la aplicación inerte sin error visible**.

## 9. Tiempo

- Zona horaria única en el MVP: `Europe/Madrid`.
- Todo instante absoluto se persiste en UTC (`timestamptz`). **El nombre de la propiedad no lleva sufijo obligatorio** (`FechaAlta`, `InicioUtc`): lo que garantiza el UTC es el tipo `timestamptz` y la comprobación de `Kind == Utc` en el dominio. *Decidido en la Fase 5.*
- `TimeOnly` y `DateOnly` son hora local, sin conversión.
- La conversión local ↔ UTC ocurre **en un único punto**: dentro del motor de disponibilidad.

```csharp
// MAL
var ahora = DateTime.Now;

// BIEN
var ahora = DateTime.UtcNow;     // o el reloj inyectado, si existe
```

Npgsql **rechaza en tiempo de ejecución** cualquier `DateTime` cuyo `Kind` no sea `Utc` destinado a un `timestamptz`. `FranjaHoraria` lo comprueba en su constructor.

---

## 10. Motor de disponibilidad y reservas

Es el núcleo del TFM. Aquí se va despacio.

- **Una sola implementación** de `IMotorDisponibilidad`, compartida por el flujo público y el área de empresa. No la dupliques.
- El algoritmo está en SPEC §12.2. Síguelo paso a paso, no improvises.
- `FinUtc` se calcula **siempre en servidor**. Nunca se acepta del cliente.
- La disponibilidad se recomprueba **dentro de la transacción**, justo antes de insertar.
- El no solapamiento lo garantiza una restricción `EXCLUDE` de PostgreSQL, **no el código**. Va escrita a mano en la migración con `migrationBuilder.Sql(...)`.
- El error `23P01` (`exclusion_violation`) se traduce a `HuecoYaNoDisponible` en `Infraestructura`.
- **En las fases 10 y 11, las pruebas van primero.**

Rangos válidos de las políticas, como invariantes de `PoliticasReserva`:
`IntervaloHuecosMinutos` 1-480 · `AntelacionMinimaMinutos` 0-43200 · `AntelacionMaximaDias` 1-365.
Un intervalo de cero **cuelga el motor** con un bucle infinito.

---

## 11. Comandos

```bash
# Base de datos
docker compose up -d postgres

# Compilar y probar
dotnet build
dotnet test
dotnet test tests/PlataformaReservas.PruebasUnitarias      # rapidas
dotnet test tests/PlataformaReservas.PruebasIntegracion    # requieren Docker

# Migraciones
dotnet ef migrations add <Nombre> \
  -p src/PlataformaReservas.Infraestructura \
  -s src/PlataformaReservas.Web
dotnet ef database update \
  -p src/PlataformaReservas.Infraestructura \
  -s src/PlataformaReservas.Web

# Todo en pie
docker compose up --build
```

Antes de dar una tarea por terminada: `dotnet build` sin avisos nuevos y `dotnet test` en verde.

---

## 11 bis. Organización de las pruebas

**Un fichero de pruebas por clase probada.** Nunca un fichero que agrupe las pruebas de varias clases, por pequeñas que sean.

- **Nombre:** `Pruebas<Clase>.cs`, con una única clase `Pruebas<Clase>`. Ejemplos: `PruebasFranjaHoraria`, `PruebasSlug`, `PruebasReserva`, `PruebasMotorDisponibilidad`, `PruebasCrearReserva`.
- **Ubicación:** la misma carpeta relativa que la clase probada tiene dentro de su proyecto de `src/`.

| Clase probada | Fichero de pruebas |
|---|---|
| `src/PlataformaReservas.Dominio/ValueObjects/FranjaHoraria.cs` | `tests/PlataformaReservas.PruebasUnitarias/ValueObjects/PruebasFranjaHoraria.cs` |
| `src/PlataformaReservas.Dominio/Entidades/Reserva.cs` | `tests/PlataformaReservas.PruebasUnitarias/Entidades/PruebasReserva.cs` |
| `src/PlataformaReservas.Infraestructura/Identidad/<Clase>.cs` | `tests/PlataformaReservas.PruebasIntegracion/Identidad/Pruebas<Clase>.cs` |

- **Unitarias e integración por separado.** Si una clase tiene pruebas en las dos suites, tiene un fichero en cada una, con el mismo nombre y la misma carpeta relativa.
- **Casos de uso:** el caso de uso es la clase probada. Sus pruebas de integración van en `Pruebas<CasoDeUso>.cs`, no repartidas por entidad.
- **Pruebas transversales:** lo que no prueba una clase sino una propiedad del sistema va en un fichero con el nombre de esa propiedad. La lista es cerrada:

| Fichero | Suite | Qué prueba |
|---|---|---|
| `Arquitectura/PruebasArquitectura.cs` | Unitarias | Las cinco pruebas de forma del código |
| `Persistencia/PruebasEsquema.cs` | Integración | Restricciones e índices del esquema |
| `Persistencia/PruebasDatosIniciales.cs` | Integración | Siembra de datos iniciales |
| `Multiempresa/PruebasAislamientoEmpresas.cs` | Integración | Aislamiento entre empresas (SPEC §10.4) |
| `Reservas/PruebasConcurrenciaReservas.cs` | Integración | La prueba de concurrencia (`docs/arquitectura.md` §12.3) |
| `Persistencia/PruebasCifradoEnReposo.cs` | Integración | Cifrado en reposo con SQL crudo sobre todo el sistema (RN-162 a RN-167). *Fase 17, duda 1* |
| `Autorizacion/PruebasAutorizacionRutas.cs` | Integración | Toda ruta privada exige su política y ninguna queda sin proteger (SPEC §16.2). *Fase 17, duda 2* |

  Añadir una prueba transversal nueva es una **duda** de la especificación de fase, no una decisión de quien implementa.
- **Dobles y utilidades** (`BaseDatosFixture`, `ProtectorFalso`, `RemitenteCaptura`…) no llevan el prefijo `Pruebas`. Los compartidos van en `Infraestructura/` de su proyecto de pruebas. Un doble que solo usa un fichero puede vivir junto a él.
- Si el fichero de una clase crece demasiado, se agrupa por operación con clases anidadas (`public sealed class Cancelar`), **dentro del mismo fichero**. No se parte en varios ficheros.

Los ficheros anteriores a esta regla que agrupan varias clases (`PruebasObjetosValor`, `PruebasEntidades`, `PruebasIdentidad`, `PruebasProtectores`, `PruebasCifradoIdentidad`) se dividen en una tarea propia, sin cambiar la lógica de ninguna prueba.

### Catálogo de casos obligatorios

Cada clase probada lleva **siempre** estos casos, además de los que pidan su RN y SPEC §16. Un caso que no aplique se omite **con motivo** en §6 de la especificación de fase, nunca en silencio.

| Qué se prueba | Casos que lleva siempre |
|---|---|
| **Objeto de valor** | Valor típico válido · los dos extremos del rango (mínimo y máximo) · justo fuera de cada extremo · nulo, vacío y solo espacios (si es texto) · formato inválido · normalización (recortes, mayúsculas) · igualdad por valor |
| **Método de entidad** | Caso normal y su efecto en el estado · una prueba por cada invariante que puede romper · cada transición de estado legal e ilegal · el evento de dominio que registra (o que no registra) · repetir la operación (¿es idempotente o lanza?) |
| **Caso de uso** (integración) | Caso normal persistido · validación (`Validacion`) · recurso inexistente y **recurso de otra empresa** (`NoEncontrado`, RN-03) · conflicto, si aplica · fallo a mitad sin dejar nada, si hay transacción · eventos encolados |
| **Repositorio** | Filtra por empresa en la consulta · lectura sin seguimiento cuando lo dice `docs/arquitectura.md` · no encuentra lo de otra empresa |
| **Regla con números o fechas** | Extremos · valores contiguos (fin = inicio) · cambio de día · horas en UTC |

### Sabotajes

Cada especificación de fase incluye **dos o tres sabotajes** sobre sus reglas críticas (⚑): un cambio temporal en el código (quitar un filtro, cambiar `<` por `<=`, saltarse una comprobación) que tiene que hacer fallar una prueba concreta. Se ejecutan, se anota qué prueba falló y se restaura el código antes del commit. Una prueba que nunca se ha visto fallar no demuestra nada.

### Auditoría de pruebas

La pregunta "¿faltan pruebas?" no se hace en cada fase: la cubren el catálogo y `/revisar`. Se hace con `/auditar-pruebas` **solo** al cerrar las fases 10, 11, 16 y 17, o cuando el desarrollador lo pida.

---

## 12. Flujo de trabajo por tarea

1. Lee la especificación de la fase y las reglas RN-xx que cita.
2. **Propón el diseño y espera confirmación** si hay alguna decisión relevante.
3. Implementa **solo** lo pedido.
4. Escribe las pruebas (primero, en fases 10 y 11): las de §6 de la especificación y, para cada clase que hayas creado o modificado, **recorre el catálogo de §11 bis** y añade los casos que falten.
5. Ejecuta `dotnet build` y `dotnet test`.
6. Corrige hasta verde.
7. Explica qué has decidido y por qué, y **qué has dejado fuera**.

Commits: mensaje de texto libre, sin formato obligatorio.

---

## 13. Qué no hacer nunca

- Añadir funcionalidad que no esté en los documentos.
- Tocar ficheros fuera del radio indicado.
- Reformatear o reordenar código que no forma parte de la tarea.
- Cambiar el esquema de base de datos sin migración.
- Borrar físicamente `Empresa`, `Servicio`, `Profesional` o `Reserva`: se desactivan.
- Usar `DateTime.Now`, `DateTime.Today` o `TimeZoneInfo.Local`.
- Escribir `IgnoreQueryFilters()`.
- Meter secretos, cadenas de conexión o contraseñas en el repositorio.
- Registrar contraseñas, tokens o datos personales innecesarios en los logs.
- Suprimir avisos del compilador con `#pragma` para que compile. **Los avisos son errores** (`TreatWarningsAsErrors`): se corrigen, no se silencian.
- Marcar una prueba como omitida para que pase la suite.
- Decir que algo funciona sin haber ejecutado las pruebas.
- **Lanzar dos casos de uso en paralelo dentro de un componente Blazor.** Nada de `Task.WhenAll` sobre dos casos de uso, nada de manejadores `async void`: comparten el `ContextoDatos` del circuito y EF Core lanzará *"a second operation was started on this context"*.
- Llamar a `SaveChanges` desde un repositorio.
- Crear el esquema de pruebas con `EnsureCreated`: **no ejecuta el SQL manual de las migraciones**, así que la restricción `EXCLUDE` no existiría y la prueba de concurrencia pasaría sin probar nada.
- Devolver entidades de dominio desde un caso de uso.
- **Enviar un correo dentro de una transacción**, o antes de confirmarla.
- **Despachar eventos dentro de `SaveChanges`**: anula el propósito entero del diseño.
- Dejar el bucle del procesador de eventos sin `try/catch`.
- Poner `Correo:Habilitado` en `true` en `appsettings.json` o en la configuración de pruebas.

---

## 14. Cuándo parar y preguntar

Para, no improvises, y pregunta cuando:

- una regla de negocio necesaria no está en `reglas-negocio.md`;
- dos documentos se contradicen;
- la tarea exige un paquete nuevo;
- la tarea exige cambiar una decisión cerrada (D-xx);
- la tarea crece más allá de lo que se te pidió;
- no sabes si algo está dentro del alcance.

Preguntar cuesta un minuto. Reconstruir una entidad después de la Fase 8 cuesta migración, configuración de EF, pruebas y pantallas.
