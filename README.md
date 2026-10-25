# Plataforma Reservas

Plataforma web **multiempresa (SaaS multi-tenant)** para reservar servicios con cita previa: peluquerías, centros de estética, fisioterapia y cualquier negocio que trabaje con una agenda de profesionales.

Cada empresa se da de alta, configura sus servicios, profesionales y horarios, y publica su ficha. Los clientes buscan por ciudad, ven la disponibilidad real y reservan en unos pocos pasos.

---

## Objetivo

Construir un **MVP funcional** con una arquitectura limpia, que resuelva con rigor tres problemas técnicos:

| Problema | Cómo se resuelve |
|---|---|
| **Disponibilidad** | Un motor calcula los huecos libres reales a partir del horario de cada profesional, sus excepciones, la duración del servicio, las reservas existentes y las políticas de la empresa |
| **Solapamientos** | Dos reservas nunca pueden ocupar al mismo profesional a la vez, ni siquiera con peticiones simultáneas. La garantía final la da PostgreSQL |
| **Aislamiento entre empresas** | Cada empresa solo ve y gestiona sus propios datos. El filtrado se hace en las consultas, nunca solo en la interfaz |

Principios que guían el diseño: MVP por encima de completitud, nada de sobreingeniería (sin microservicios, CQRS con buses, *event sourcing* ni MediatR) y toda regla de negocio importante comprobable sin la interfaz.

---

## Funcionalidad

### Visitante (sin registrarse)

- Landing con propuesta de valor, cómo funciona, categorías y preguntas frecuentes.
- Búsqueda de establecimientos por **ciudad** (obligatoria) y **categoría** (opcional), con resultados paginados.
- Ficha pública: descripción, dirección, servicios, profesionales, horario y disponibilidad.

### Cliente

- Registro, inicio de sesión y recuperación de contraseña por correo. Tras iniciar sesión, cada usuario va a su área: el propietario a su panel (`/app`) y el cliente a sus reservas (`/my-account/bookings`); si tiene los dos roles, gana el de propietario. Si venía de una página concreta (por ejemplo, a mitad de una reserva), vuelve a ella.
- Flujo de reserva por pasos: servicio → profesional → día → hueco → confirmación.
- Listado de reservas próximas e históricas, detalle y cancelación (hasta la hora de inicio).
- Perfil y baja de cuenta, que cancela sus reservas futuras.

### Propietario de empresa

- Alta de cuenta y empresa en un único formulario.
- Configuración de datos, categoría y políticas de reserva.
- Mantenimiento de servicios y profesionales, y asociación entre ambos.
- Horario semanal por profesional y excepciones de día completo (vacaciones, bajas).
- Panel con las reservas del día y las próximas; listado por fecha para cancelar o marcar como completadas.
- Baja y reactivación del establecimiento.

Un mismo usuario puede ser cliente y propietario a la vez. La empresa **no aparece en el buscador** hasta que tiene lo mínimo configurado para recibir reservas.

### Notificaciones

Las altas, reservas, cancelaciones y bajas envían correos a las partes afectadas: cliente, empresa o ambos. Si el servidor de correo falla, la operación no se pierde: el fallo queda registrado.

### Interfaz

Diseño propio, sin bibliotecas de componentes ni de estilos: un sistema de diseño en `wwwroot/app.css`, *responsive* (móvil, tableta y escritorio) y con modo oscuro automático. El área de empresa tiene su propia navegación, y Servicios y Profesionales se gestionan con un listado compacto y una ventana emergente para crear y editar. Todo funciona con la política de seguridad de contenidos activa: sin estilos ni *scripts* en línea.

### Fuera de alcance

Pagos, recordatorios, SMS, valoraciones, geolocalización, varias zonas horarias, reserva como invitado, área de administración de la plataforma y API REST pública, entre otros. Quedan documentados como trabajo futuro.

---

## Stack

| Capa | Tecnología |
|---|---|
| Lenguaje y plataforma | C# · **.NET 10** |
| Backend | **ASP.NET Core** |
| Frontend | **Blazor**: páginas públicas con SSR estático, áreas privadas y reserva con `InteractiveServer` |
| Base de datos | **PostgreSQL** con extensión `btree_gist` |
| Acceso a datos | **Entity Framework Core** + Npgsql, con nombres en `snake_case` |
| Identidad | **ASP.NET Core Identity**, con cifrado de datos personales |
| Correo | **MailKit** |
| Registro | **Serilog** |
| Pruebas | **xUnit**, FluentAssertions, **Testcontainers** y Respawn |
| Entorno local | **Docker Compose** |

Las versiones de los paquetes se gestionan de forma centralizada en `Directory.Packages.props`, y la compilación trata los avisos como errores.

---

## Arquitectura

Monolito modular con **Clean Architecture**:

```text
src/
├── PlataformaReservas.Dominio          entidades, value objects, eventos y reglas de negocio
├── PlataformaReservas.Aplicacion       casos de uso, motor de disponibilidad e interfaces
├── PlataformaReservas.Infraestructura  EF Core, repositorios, Identity, correo y cifrado
└── PlataformaReservas.Web              componentes Blazor, autorización y arranque
tests/
├── PlataformaReservas.PruebasUnitarias      dominio, motor y pruebas de arquitectura
└── PlataformaReservas.PruebasIntegracion    PostgreSQL real en contenedor
```

Las dependencias apuntan siempre hacia el dominio: `Web → Infraestructura → Aplicacion → Dominio`. El dominio no conoce EF Core, Npgsql ni ASP.NET.

### Modelo de dominio

**Modelo rico**: las entidades protegen sus invariantes con métodos y no exponen `set` públicos ni colecciones mutables. Se organiza en **siete agregados pequeños** que se referencian entre sí por identificador:

`Usuario` · `Categoria` · `Empresa` · `MiembroEmpresa` · `Servicio` · `Profesional` (con sus horarios y excepciones) · `Reserva`

Una reserva solo puede estar `Confirmada`, `Cancelada` o `Completada`, y solo admite dos transiciones: de confirmada a cancelada y de confirmada a completada.

Los identificadores son `Guid` v7, ordenables en el tiempo, y todas las horas se guardan en UTC.

### Convenciones

- Código en **castellano, sin tildes ni eñes** en los identificadores (`Reserva`, `Profesional`, `HoraInicio`).
- Rutas web en **inglés** (`/businesses`, `/book/{slug}`, `/my-account/bookings`, `/app/schedules`).
- Textos de la interfaz en castellano.

---

## Cómo funciona

### 1. Configuración de la empresa

El propietario define sus servicios (duración y precio), los profesionales que prestan cada uno y el horario semanal de cada profesional. Las **políticas de reserva** —intervalo entre huecos, antelación mínima y antelación máxima— se fijan para la empresa y cada servicio puede sobrescribirlas.

### 2. Cálculo de disponibilidad

Cuando el cliente elige servicio y día, el motor de disponibilidad:

1. resuelve las políticas efectivas del servicio;
2. selecciona los profesionales activos que lo prestan;
3. descarta los que tienen una excepción ese día;
4. recorre su horario generando huecos según el intervalo configurado;
5. elimina los que se solapan con reservas existentes o no respetan la antelación.

El resultado son los huecos realmente libres, por profesional. Detalle en [docs/motor-disponibilidad.md](docs/motor-disponibilidad.md).

### 3. Creación de la reserva y concurrencia

La prevención de reservas duplicadas se apoya en tres capas:

| Capa | Mecanismo | Papel |
|---|---|---|
| Interfaz | Botón deshabilitado tras el primer clic y testigo de idempotencia | Evita el doble envío accidental |
| Aplicación | Recomprobación de la disponibilidad dentro de la transacción | Da un mensaje de error claro |
| Base de datos | Restricción `EXCLUDE USING gist` sobre profesional e intervalo | **La garantía**: rechaza el solapamiento aunque dos peticiones lleguen a la vez |

Si la base de datos rechaza la inserción, el error se traduce a un error de dominio, la interfaz recarga los huecos del día y pide al usuario que elija otro. Detalle en [docs/concurrencia.md](docs/concurrencia.md).

### 4. Aislamiento entre empresas

La empresa del usuario autenticado viaja en un *claim* y se expone a la aplicación mediante `IContextoEmpresa`. Cada consulta del área privada filtra por ella **en la propia consulta SQL**. No se usan filtros globales de EF Core, para que el filtrado sea siempre explícito y comprobable. El aislamiento tiene pruebas de integración específicas.

### 5. Eventos y notificaciones

Las entidades registran **eventos de dominio** (reserva creada, cancelada, empresa dada de baja…). Se publican **después** de confirmar la transacción en una cola en memoria (`Channel<T>`), y un servicio en segundo plano los procesa y envía los correos. Así, un fallo del correo nunca deshace una reserva.

---

## Seguridad

- **Identity** para contraseñas, sesiones y recuperación de cuenta. Autorización por roles (`Cliente`, `Propietario`) y por pertenencia a la empresa.
- **Cifrado en reposo de los datos personales**: nombre, correo y teléfono de los usuarios, y las copias de nombre y teléfono de cada reserva, se guardan cifrados (AES-256-GCM). La búsqueda por correo usa un valor HMAC-SHA256. Una copia robada de la base de datos no expone datos personales en claro. Ver [Claves de cifrado](#claves-de-cifrado).
- **Medidas de endurecimiento**: bloqueo tras intentos fallidos, registro de intentos fallidos, limitación de frecuencia en las rutas sensibles, cabeceras de seguridad con CSP, escapado en las plantillas de correo y análisis de dependencias vulnerables en la compilación.
- **Sin secretos en Git**: contraseña de la base de datos en `.env`, cadena de conexión y claves de cifrado en User Secrets.

---

## Pruebas

- **Unitarias**: reglas del dominio, value objects y motor de disponibilidad.
- **De integración**: contra un **PostgreSQL real** levantado con Testcontainers y migrado con las migraciones del proyecto, para que la restricción `EXCLUDE` exista de verdad. Cubren persistencia, concurrencia, aislamiento entre empresas, autorización y cifrado.
- **De arquitectura**: cinco pruebas comprueban la forma del código. Ninguna entidad expone `set` públicos ni colecciones mutables, todas tienen constructor privado sin parámetros, el dominio no depende de la infraestructura y ningún método público de una entidad recibe un primitivo (`string`, `int`, `decimal`) en lugar de un objeto de valor.

Las reglas de negocio están numeradas (`RN-xx`) y cada prueba cita la regla que verifica.

---

## Puesta en marcha

### Requisitos

- SDK de **.NET 10**
- **Docker** con Docker Compose

### 1. Base de datos local

Crea un fichero `.env` en la raíz (no se versiona) con estas variables:

```text
POSTGRES_USER=<tu usuario>
POSTGRES_PASSWORD=<tu contraseña>
POSTGRES_PUERTO=<tu puerto>
```

Y levanta el contenedor:

```bash
docker compose up -d postgres
docker compose ps        # debe aparecer como healthy
```

### 2. Cadena de conexión

```powershell
dotnet user-secrets set "ConnectionStrings:PlataformaReservas" "Host=localhost;Port=<tu puerto>;Database=plataforma_reservas;Username=<tu usuario>;Password=<tu contraseña>" --project src/PlataformaReservas.Web
```

Va en una sola línea para que funcione igual en PowerShell y en Bash. Si la clave ya existe, el valor nuevo sobrescribe al anterior. Usuario, puerto y contraseña deben coincidir con los del `.env`.

### 3. Claves de cifrado

Los datos personales se guardan cifrados en la base de datos. Hacen falta dos claves **distintas** de 256 bits, en base64:

| Clave | Uso |
|---|---|
| `Cifrado:ClaveDatos` | Cifrado AES-256-GCM de los datos personales |
| `Cifrado:ClaveBusqueda` | Valor de búsqueda HMAC-SHA256 del correo |

Se configuran fuera del repositorio:

- **Desarrollo:** User Secrets del proyecto `src/PlataformaReservas.Web`.
- **Otros entornos:** variables de entorno `Cifrado__ClaveDatos` y `Cifrado__ClaveBusqueda`.

Para generarlas en desarrollo, en PowerShell (dos ejecuciones, para que sean distintas):

```powershell
$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Cifrado:ClaveDatos" ([Convert]::ToBase64String($b)) --project src/PlataformaReservas.Web

$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Cifrado:ClaveBusqueda" ([Convert]::ToBase64String($b)) --project src/PlataformaReservas.Web
```

En Git Bash, `openssl rand -base64 32` produce lo mismo. No uses nunca generadores de claves web.

**Sin ellas la aplicación no arranca.**

> **⚠ Perder las claves equivale a perder las cuentas.** Nadie podrá volver a iniciar sesión y los datos cifrados no se pueden recuperar. Haz una copia de seguridad de las dos claves **fuera de la máquina**, por ejemplo en un gestor de contraseñas. No las guardes en un fichero sincronizado ni las envíes por correo.

Para hacer la copia, justo después de generarlas:

```bash
dotnet user-secrets list --project src/PlataformaReservas.Web
```

Copia los dos valores de `Cifrado:*` a una nota segura del gestor de contraseñas y limpia la terminal. El ciclo de vida completo de las claves (despliegue, pérdida y qué hacer si se filtran) está en [docs/arquitectura.md](docs/arquitectura.md) §8.5, "Gestión de la clave".

### 4. Compilar, probar y ejecutar

```bash
dotnet tool restore                               # dotnet-ef como herramienta local
dotnet build
dotnet test                                       # las de integración necesitan Docker
dotnet ef database update --project src/PlataformaReservas.Infraestructura --startup-project src/PlataformaReservas.Web
dotnet run --project src/PlataformaReservas.Web
```

En desarrollo, la aplicación aplica las migraciones y siembra los datos iniciales al arrancar, así que `dotnet ef database update` solo hace falta si quieres migrar sin ejecutarla.

### 5. Correo (opcional)

**Por defecto no se envía ningún correo**: `Correo:Habilitado` vale `false` en `appsettings.json` y los correos solo se registran en el log. Así, olvidarse de configurarlo nunca envía nada por descuido.

| Clave | Para qué | Dónde se pone |
|---|---|---|
| `Correo:Habilitado` | `true` para enviar de verdad | User Secrets o variable de entorno, **nunca** en un `appsettings*.json` versionado |
| `Correo:Servidor`, `Correo:Puerto` | Servidor SMTP (587 por defecto) | User Secrets o variable de entorno |
| `Correo:Usuario`, `Correo:Contrasena` | Credenciales del SMTP | **User Secrets o variable de entorno**: son secretos |
| `Correo:Remitente`, `Correo:NombreRemitente` | Dirección y nombre del remitente | Tienen valor en `appsettings.json` |
| `Correo:UrlBase` | Raíz de los enlaces de los correos, como el de restablecer la contraseña. Nunca se toma de la petición | `appsettings.Development.json` en desarrollo; la composición la fija sola |

Para verlos en desarrollo sin que salgan a internet, usa una bandeja de pruebas como **Mailtrap** con los datos SMTP que te dé:

```bash
dotnet user-secrets set "Correo:Habilitado" "true" --project src/PlataformaReservas.Web
dotnet user-secrets set "Correo:Servidor" "<servidor SMTP>" --project src/PlataformaReservas.Web
dotnet user-secrets set "Correo:Usuario" "<usuario>" --project src/PlataformaReservas.Web
dotnet user-secrets set "Correo:Contrasena" "<contraseña>" --project src/PlataformaReservas.Web
```

Ten en cuenta que:

- **Un fallo de correo nunca deshace la operación.** Los correos se encolan **después** de confirmar y los envía un servicio en segundo plano; si el envío falla, se registra y la reserva sigue confirmada.
- **La cola está en memoria.** Si la aplicación se para entre la confirmación y el envío, ese correo se pierde (RN-144). Es una limitación declarada del MVP.
- **En producción hace falta un proveedor transaccional con SPF y DKIM**, o los correos acabarán en spam (R-8).

Detalle del diseño en [docs/arquitectura.md](docs/arquitectura.md) §13.

### 6. Todo en contenedores

Para levantar la aplicación entera sin instalar .NET, solo con Docker:

```bash
docker compose up --build
```

El servicio `web` necesita, además de las variables de la base de datos, estas otras en el mismo `.env`:

```text
CIFRADO_CLAVE_DATOS=<el mismo valor que Cifrado:ClaveDatos de User Secrets>
CIFRADO_CLAVE_BUSQUEDA=<el mismo valor que Cifrado:ClaveBusqueda de User Secrets>
WEB_PUERTO=<opcional; 8080 por defecto>
```

> **⚠ Deben ser exactamente las mismas claves que las de User Secrets.** La aplicación en desarrollo y el contenedor `web` comparten el mismo volumen de datos (`datos-postgres`). Con claves distintas, lo que cifra uno no lo puede leer el otro y nadie puede iniciar sesión.

La aplicación queda en `http://localhost:8080` (o en el puerto de `WEB_PUERTO`), publicada solo en `127.0.0.1`. Sirve HTTP: en un despliegue real, el TLS lo pone el proxy de delante.

`docker compose down` la para conservando los datos. `docker compose down -v` **borra** la base de datos.

---

## Documentación

| Documento | Qué contiene |
|---|---|
| [AGENTS.md](AGENTS.md) | Instrucciones para cualquier agente de IA que trabaje en el repositorio. Es el fichero canónico |
| [CLAUDE.md](CLAUDE.md) | Lo específico de Claude Code; importa `AGENTS.md` |
| [docs/TFM_PlataformaReservas_SPEC.md](docs/TFM_PlataformaReservas_SPEC.md) | Especificación: alcance, decisiones cerradas (D-xx), entidades, rutas, fases y criterios de aceptación |
| [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) | Calendario, puntos de control y estado de avance |
| [docs/casos-uso.md](docs/casos-uso.md) | Los 24 casos de uso (CU-xx) y sus flujos |
| [docs/reglas-negocio.md](docs/reglas-negocio.md) | Las reglas de negocio numeradas (RN-xx), con su origen y la prueba que las verifica |
| [docs/modelo-dominio.md](docs/modelo-dominio.md) | Agregados, invariantes, objetos de valor y comportamiento |
| [docs/arquitectura.md](docs/arquitectura.md) | Proyectos, casos de uso, autenticación, cifrado, persistencia, eventos, endurecimiento y riesgos |
| [docs/base-datos.md](docs/base-datos.md) | Esquema de PostgreSQL y lo que lo protege |
| [docs/motor-disponibilidad.md](docs/motor-disponibilidad.md) | Cómo se calculan los huecos libres |
| [docs/concurrencia.md](docs/concurrencia.md) | Por qué no pueden existir dos reservas solapadas |
| [docs/seguridad.md](docs/seguridad.md) | OWASP Top 10, medidas de endurecimiento, modelo de amenaza y limitaciones |
| [docs/pruebas.md](docs/pruebas.md) | Estrategia, organización y evidencia de las pruebas |
| [docs/fases/](docs/fases/) | Especificación e informe de revisión de cada fase |

---

## Licencia

Uso restringido. Consulta [LICENSE.md](LICENSE.md).
