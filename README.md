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

Hay dos formas de arrancarla:

| Para qué | Qué necesitas | Apartados |
|---|---|---|
| **Usarla o enseñarla**, todo en contenedores | Solo **Docker** con Docker Compose | [6. Todo en contenedores](#6-todo-en-contenedores-solo-docker-sin-net) |
| **Desarrollar** | SDK de **.NET 10** y **Docker** | 1 a 5 |

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

**Datos de demostración en desarrollo:** se activan con `DatosDemostracion:Habilitado` (`true`) y `DatosDemostracion:Clave` en User Secrets. Qué crean y con qué cuentas se entra, en el apartado [6.4](#64-datos-de-demostración-opcional).

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

### 6. Todo en contenedores (solo Docker, sin .NET)

Este apartado se puede seguir sin haber leído los anteriores: levanta la aplicación y la base de datos **sin instalar .NET**.

#### 6.1. Qué necesitas

- **Docker** con Docker Compose (en Windows y macOS, Docker Desktop).
- El código del repositorio.

En desarrollo, los secretos se guardan con `dotnet user-secrets`, pero ese almacén vive en el perfil de tu usuario y .NET solo lo lee al ejecutar con `dotnet run`. **El contenedor no lo ve.** Dentro del contenedor toda la configuración llega como **variables de entorno**, que Docker Compose lee de un fichero `.env` en la raíz del repositorio.

#### 6.2. El fichero `.env`

Crea un fichero llamado `.env` en la raíz del repositorio (junto a `docker-compose.yml`). **No se versiona:** está en `.gitignore`. Plantilla completa:

```text
# Base de datos (obligatorias)
POSTGRES_USER=<usuario de la base de datos>
POSTGRES_PASSWORD=<contraseña de la base de datos>
# POSTGRES_PUERTO=5432          # opcional: puerto de PostgreSQL en tu máquina

# Claves de cifrado de los datos personales (obligatorias; ver 6.3)
CIFRADO_CLAVE_DATOS=<clave 1 en base64>
CIFRADO_CLAVE_BUSQUEDA=<clave 2 en base64, distinta de la 1>

# Puerto de la aplicación (opcional)
# WEB_PUERTO=8080

# Datos de demostración (opcional; ver 6.4)
# DEMO_HABILITADO=true
# DEMO_CLAVE=<contraseña común de todos los usuarios de demostración>

# Correo (opcional; ver 6.5). Por defecto no se envía nada
# CORREO_HABILITADO=true
# CORREO_SERVIDOR=<servidor SMTP>
# CORREO_PUERTO=587
# CORREO_USUARIO=<usuario SMTP>
# CORREO_CONTRASENA=<contraseña SMTP>
```

No hace falta escribir la cadena de conexión: la composición la monta sola con `POSTGRES_USER` y `POSTGRES_PASSWORD`.

**Si vienes de desarrollo**, esta es la equivalencia con lo que tenías en User Secrets (en las variables de entorno, el `:` de las claves de .NET se escribe `__`, y la composición hace esa traducción por ti):

| En User Secrets (desarrollo) | En el `.env` (Docker) | ¿Obligatoria? |
|---|---|---|
| `ConnectionStrings:PlataformaReservas` | `POSTGRES_USER` y `POSTGRES_PASSWORD` | Sí |
| `Cifrado:ClaveDatos` | `CIFRADO_CLAVE_DATOS` | Sí |
| `Cifrado:ClaveBusqueda` | `CIFRADO_CLAVE_BUSQUEDA` | Sí |
| `DatosDemostracion:Habilitado` | `DEMO_HABILITADO` (`false` si no la pones) | No |
| `DatosDemostracion:Clave` | `DEMO_CLAVE` | Solo con datos de demostración |
| `Correo:Habilitado` | `CORREO_HABILITADO` (`false` si no la pones) | No |
| `Correo:Servidor`, `Correo:Puerto` | `CORREO_SERVIDOR`, `CORREO_PUERTO` (587 si no la pones) | Solo con correo |
| `Correo:Usuario`, `Correo:Contrasena` | `CORREO_USUARIO`, `CORREO_CONTRASENA` | Solo con correo |
| `Correo:UrlBase` | Nada: la composición la fija a `http://localhost:<WEB_PUERTO>` | — |

> **Evita el carácter `$` en los valores del `.env`.** Docker Compose lo interpreta como el inicio de una variable y lo sustituye.

#### 6.3. Generar las claves de cifrado sin .NET

Los datos personales (nombre, correo y teléfono) se guardan cifrados. Hacen falta **dos claves distintas** de 32 bytes en base64. Ejecuta **dos veces** cualquiera de estos comandos y pega cada resultado en su variable:

| Dónde | Comando |
|---|---|
| Con el propio Docker | `docker run --rm alpine sh -c "head -c 32 /dev/urandom \| base64"` |
| Git Bash, Linux o macOS | `openssl rand -base64 32` |
| PowerShell | `$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)` |

No uses nunca generadores de claves web. **Si falta alguna clave, no mide 32 bytes o son iguales, la aplicación no arranca.**

> **⚠ Perder las claves equivale a perder las cuentas.** Guarda una copia de las dos **fuera de la máquina**, por ejemplo en un gestor de contraseñas. Si las cambias con datos ya creados, lo cifrado con las anteriores no se puede leer y nadie podrá iniciar sesión: tendrás que empezar desde cero (6.6).
>
> **Si también usas `dotnet run`**, tienen que ser **exactamente las mismas** que las de User Secrets, porque la aplicación en desarrollo y el contenedor comparten la misma base de datos (`datos-postgres`).

Ciclo de vida completo de las claves en [docs/arquitectura.md](docs/arquitectura.md) §8.5, "Gestión de la clave".

#### 6.4. Datos de demostración (opcional)

Para tener la aplicación llena de datos al primer arranque, añade al `.env`:

```text
DEMO_HABILITADO=true
DEMO_CLAVE=<contraseña común>
```

**Qué se crea:**

| Qué | Cuántos |
|---|---|
| Empresas | Unas 62, en 10 ciudades: Madrid, Barcelona, Valencia, Sevilla, Málaga, Zaragoza, Bilbao, Salamanca, Valladolid y Granada |
| Profesionales y servicios | De 3 a 5 profesionales y de 4 a 6 servicios por empresa, con horario de lunes a viernes |
| Clientes | 11 |
| Reservas | Unas 1 700: pasadas (completadas y canceladas) y futuras (confirmadas), sin enviar ningún correo |

**`DEMO_CLAVE` es la contraseña de todos los usuarios de demostración.**

- Tiene que cumplir la política de contraseñas: **al menos 8 caracteres, con una mayúscula, una minúscula, un número y un símbolo**, por ejemplo `Demo-2026!`. Si no la cumple, el contenedor `web` no arranca y el log dice "No se pudo sembrar…".
- **Sin `DEMO_CLAVE`** se crean las empresas, pero ningún usuario ni reserva.

**Cuentas para entrar**, todas con `DEMO_CLAVE`:

| Perfil | Correo |
|---|---|
| Clientes | `cliente@demo.example` y de `cliente01@demo.example` a `cliente10@demo.example` |
| Propietarios | `propietario.<categoría>-<ciudad>@demo.example`, por ejemplo `propietario.peluqueria-madrid@demo.example` o `propietario.fisioterapia-barcelona@demo.example` |
| Propietarios de las dos empresas originales | `propietario.barberia@demo.example` y `propietaria.estetica@demo.example` |

**Volver a arrancar no duplica nada.** Las fechas de las reservas se calculan desde el día del **primer** arranque. Si pasan semanas y quieres que vuelva a haber citas próximas, empieza desde cero (6.6).

#### 6.5. Correo (opcional)

Por defecto **no se envía ningún correo**: solo se registran en el log. Para verlos sin que salgan a internet, usa una bandeja de pruebas como **Mailtrap** y pon en el `.env` los datos SMTP que te dé:

```text
CORREO_HABILITADO=true
CORREO_SERVIDOR=<servidor SMTP de Mailtrap>
CORREO_PUERTO=587
CORREO_USUARIO=<usuario>
CORREO_CONTRASENA=<contraseña>
```

El usuario y la contraseña SMTP son secretos: van solo en el `.env`, que no se versiona. Más detalles del correo en el apartado 5.

#### 6.6. Arrancar, comprobar y parar

| Comando | Para qué |
|---|---|
| `docker compose up -d --build` | Construye la imagen y arranca la base de datos y la aplicación en segundo plano. El primer arranque tarda unos minutos |
| `docker compose ps` | Comprueba que los dos servicios están en marcha y que `postgres` aparece como `healthy` |
| `docker compose logs -f web` | Sigue el arranque de la aplicación: migraciones y siembra de datos |
| `docker compose down` | Para todo **conservando** los datos |
| `docker compose down -v` | Para todo y **borra** la base de datos |

La aplicación queda en **`http://localhost:8080`** (o en el puerto de `WEB_PUERTO`), publicada solo en `127.0.0.1`. Al arrancar crea las tablas y las categorías, y, si los activaste, los datos de demostración.

**Empezar desde cero** (base de datos vacía, datos de demostración recién sembrados):

```bash
docker compose down -v
docker compose up -d --build
```

Sirve HTTP. En un despliegue real, el TLS lo pone un proxy delante de la aplicación.

#### 6.7. Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| `Falta CIFRADO_CLAVE_DATOS en .env` (o la variable que sea) | El `.env` no existe, no está en la raíz o le falta esa variable |
| El contenedor `web` se reinicia una y otra vez | Mira `docker compose logs web`. Lo habitual: una clave que no mide 32 bytes, las dos claves iguales o una `DEMO_CLAVE` que no cumple la política |
| Nadie puede iniciar sesión | Las claves de cifrado cambiaron después de crear los datos. Vuelve a las anteriores o empieza desde cero |
| `port is already allocated` | El puerto está ocupado: cambia `WEB_PUERTO` o `POSTGRES_PUERTO` en el `.env` |
| No aparecen datos de demostración | `DEMO_HABILITADO` no es `true`, o la base ya existía de antes: los datos se siembran al arrancar, pero las reservas solo en empresas que aún no tienen ninguna |

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
