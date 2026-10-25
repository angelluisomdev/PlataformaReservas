# Fase 18 — `Dockerfile` y servicio `web` en la composición

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 2,75 h (SPEC §19, l.1110; 0,25 h pasaron a la Fase 5, l.1137) |
| **Semana** | 6 (`PROJECT_PLAN.md` §2, l.129-140) |
| **Cierra** | Ninguno. Prepara la línea del Control 6 `docker compose up` desde cero (`PROJECT_PLAN.md` l.217-224) |
| **Generada** | 2026-10-24 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar, la aplicación entera se ejecuta sin nada instalado salvo Docker:

- `docker compose up --build` levanta `postgres` y `web` desde cero, con el esquema migrado y las categorías sembradas (SPEC §17, §21 l.1205).

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §17 (l.1045-1058) | Fase 18: "`Dockerfile` multietapa para `PlataformaReservas.Web` y servicio `web` que depende de `postgres`", para "ejecutar la aplicación entera sin nada instalado salvo Docker". Cadena de conexión en variables de entorno dentro de la composición. `dotnet tool restore` deja el entorno listo "en cualquier máquina" |
| SPEC §19 (l.1110, l.1137) | 2,75 h. D-26 trasladó 0,25 h a la Fase 5 (`postgres` y `dotnet-ef` local) |
| SPEC §21 (l.1205) | `docker compose up` levanta aplicación y base de datos |
| D-26 (SPEC l.133) | La Fase 18 añade `web` y el `Dockerfile` a la composición de la Fase 5. Contraseña en `.env` sin versionar |
| `arquitectura.md` §2 (l.49) | `Dockerfile` (Fase 18) en el mapa de la solución |
| `arquitectura.md` §11.3 (l.917-919) | `ConnectionStrings__PlataformaReservas` como variable de entorno en `docker-compose.yml`. Nunca un secreto en el repositorio |
| `arquitectura.md` §11.4 (l.972) | `web` con `depends_on: postgres: condition: service_healthy` y la cadena de conexión con `Host=postgres`, no `localhost` |
| RN-168 | Las claves de cifrado y de búsqueda son distintas, de 256 bits, y nunca están en el repositorio |
| R-3, R-7, R-9, R-14 | Ver §8 |
| Control 6 (`PROJECT_PLAN.md` l.217-224) | "docker compose up levanta la aplicacion desde cero" |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `docker-compose.yml` con el servicio `postgres`, `healthcheck`, volumen `datos-postgres` y variables obligatorias de `.env` | ✔ comprobado |
| `nuget.config` con un único origen ("también hace la restauración reproducible en … Docker") | ✔ comprobado |
| `.config/dotnet-tools.json` con `dotnet-ef` 10.0.12 fijado | ✔ comprobado |
| `Directory.Build.props` (`TreatWarningsAsErrors`) y `Directory.Packages.props` (versiones centralizadas) | ✔ comprobado |
| `Program.cs` comprueba las claves de cifrado al arrancar (`ComprobarClavesCifrado`) | ✔ comprobado |
| `Program.cs` migra y siembra **solo en `Development`** | ✔ comprobado; fuera de `Development` no migra: duda 2 |
| `Correo:Habilitado` en `false` en los dos `appsettings*.json`, con prueba | ✔ comprobado (Fase 16b) |
| Suites en verde: 448 unitarias y 439 de integración | ✔ comprobado (Fase 17, commit `9b8c084`) |
| `Dockerfile` | ✘ no existe (T1) |
| `.dockerignore` | ✘ no existe y no figura en los documentos: duda 6 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| Raíz del repositorio | sí | `Dockerfile` (nuevo, T1). `.dockerignore` (nuevo, duda 6). `docker-compose.yml`: servicio `web`, volumen de claves de Data Protection (duda 5) y comentario de cabecera |
| `src/PlataformaReservas.Dominio` | no | — |
| `src/PlataformaReservas.Aplicacion` | no | — |
| `src/PlataformaReservas.Infraestructura` | no | — |
| `src/PlataformaReservas.Web` | sí | Solo `Program.cs`: la condición de migrar y sembrar al arrancar (duda 2) |
| `tests/PlataformaReservas.PruebasUnitarias` | no | — |
| `tests/PlataformaReservas.PruebasIntegracion` | no | — |
| `docs/` | sí | `arquitectura.md` §2 (árbol: `.dockerignore`), §11.3 (`BaseDatos:MigrarAlArrancar`) y §11.4 (YAML del servicio `web`, tabla de secretos con las variables nuevas de `.env`). `TFM_PlataformaReservas_SPEC.md` §21 (criterios con evidencia). Este fichero. **Excepción al radio, autorizada por el desarrollador el 2026-10-25 (duda 1, retirada de la integración continua):** `README.md` (solo la línea de E-2); `AGENTS.md` §8 ter; SPEC §15.2, §15.3, §16b y §17; `PROJECT_PLAN.md` §3 (Controles 5 y 6); `reglas-negocio.md` (RN-176); `modelo-dominio.md` l.649; `arquitectura.md` §11.5, §12.2, §13.3, §14.2 y §15 (R-7); `fase-04.md`, `fase-13.md`, `fase-13-revision.md`, `fase-16b.md`, `fase-16b-revision.md`, `fase-17.md` y `fase-18-revision.md` |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:

- ningún paquete;
- `appsettings.json` ni `appsettings.Development.json`: `BaseDatos:MigrarAlArrancar` no se declara en ellos y vale `false` por defecto (duda 2);
- el `.env` del desarrollador: **las credenciales y las claves las pone siempre el desarrollador**, nunca la IA (`arquitectura.md` §11.4);
- el README (Fase 20), salvo la línea de E-2 de la excepción de la duda 1;
- el paso de dependencias vulnerables (E-2, Fase 19);
- el servicio `postgres` de la Fase 5, salvo el comentario de cabecera.

## 5. Tareas

### T1 — `Dockerfile` multietapa y `.dockerignore` (≈ 0,75 h)

- **Ficheros:** crea `Dockerfile` y `.dockerignore`.
- **Implementa:** SPEC §17 (`Dockerfile` multietapa), RN-168 (ningún secreto entra en la imagen), duda 6.
- **Contenido:**
  - **Etapa de compilación** sobre `mcr.microsoft.com/dotnet/sdk:10.0`: copia `nuget.config`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, la solución y los `.csproj` de `src/`, y restaura. Después copia `src/` y publica `PlataformaReservas.Web` en `Release`. Compila con `TreatWarningsAsErrors`, igual que en local.
  - **Etapa final** sobre `mcr.microsoft.com/dotnet/aspnet:10.0`: solo la salida publicada, ejecutada con `USER $APP_UID` (no root).
  - **`.dockerignore`:** excluye `.env`, `**/bin/`, `**/obj/`, `.git`, `docs/`, `.claude/`, `.vscode/`, `tests/` y `**/appsettings.*.local.json`.
- **Hecho cuando:** `docker build -t plataformareservas-web .` termina sin avisos, y la imagen final no contiene `.env` ni el SDK.
- **Verificación:** `docker build`, y `docker run --rm --entrypoint ls plataformareservas-web -a /app` sin `.env`.

### T2 — Migrar y sembrar al arrancar fuera de `Development` (≈ 0,25 h)

- **Ficheros:** modifica `src/PlataformaReservas.Web/Program.cs`.
- **Implementa:** SPEC §17 (la aplicación arranca desde cero), SPEC §13 l.870 (datos iniciales), duda 2.
- **Contenido:** `MigrarYSembrarAsync` se ejecuta en `Development` **o** si `BaseDatos:MigrarAlArrancar` vale `true`. La clave no se declara en `appsettings*.json` y vale `false` por defecto; solo la pone el servicio `web` de la composición.
- **Hecho cuando:** en `Development` el comportamiento no cambia, y en `Production` solo se migra con la clave a `true`.
- **Verificación:** `dotnet build` sin avisos, `dotnet test` en verde y T5.

### T3 — Servicio `web` en `docker-compose.yml` (≈ 0,5 h)

- **Ficheros:** modifica `docker-compose.yml`.
- **Implementa:** SPEC §17, `arquitectura.md` §11.4 l.972, RN-168, dudas 2, 3, 4 y 5.
- **Contenido:**
  - `build` con el `Dockerfile` de la raíz, y `depends_on: postgres: condition: service_healthy`.
  - Variables de entorno:
    - `ConnectionStrings__PlataformaReservas=Host=postgres;Database=plataforma_reservas;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}`;
    - `Cifrado__ClaveDatos=${CIFRADO_CLAVE_DATOS:?…}` y `Cifrado__ClaveBusqueda=${CIFRADO_CLAVE_BUSQUEDA:?…}` (duda 3);
    - `BaseDatos__MigrarAlArrancar=true` (duda 2);
    - `Correo__UrlBase=http://localhost:${WEB_PUERTO:-8080}` (duda 4).
  - Puerto `${WEB_PUERTO:-8080}:8080`, solo HTTP (duda 4).
  - Volumen nombrado `claves-web` para las claves de Data Protection del usuario de la imagen (duda 5).
  - Comentario de cabecera: la Fase 18 añade `web` y las variables nuevas de `.env`.
- **Hecho cuando:** `docker compose config` resuelve sin errores con un `.env` completo, y se niega a arrancar si falta una clave de cifrado.
- **Verificación:** `docker compose config` y T5.

### T4 — Retirada (0 h)

Retirada por decisión del desarrollador el 2026-10-25 (duda 1). No se crea ningún fichero.

### T5 — Verificación desde cero (≈ 0,5 h)

- **Ficheros:** ninguno del repositorio. Solo ficheros temporales en el scratchpad (duda 7).
- **Implementa:** SPEC §21 l.1205, Control 6 (preparación), RN-168.
- **Contenido:**
  - Un proyecto de composición aparte, `docker compose -p pr-verificacion`, con `--env-file` en el scratchpad (valores aleatorios) y un fichero adicional que cambia `container_name` y los puertos. Así no se lee `.env` ni se toca `plataformareservas-db`.
  - Con imagen nueva y volumen vacío:
    - `/`, `/businesses` y `/login` responden 200;
    - las ocho categorías están sembradas;
    - `/app` y `/my-account/bookings` dan 302 a `/login`.
  - Al terminar, `down -v` de ese proyecto.
- **Hecho cuando:** las respuestas son las esperadas y no queda ningún contenedor ni volumen de `pr-verificacion`.
- **Verificación:** salida de `curl` anotada en §11, y `docker ps -a` y `docker volume ls` limpios.

### T6 — Cierre (≈ 0,25 h)

- **Ficheros:** modifica `docs/arquitectura.md` §2, §11.3 y §11.4, `docs/TFM_PlataformaReservas_SPEC.md` §21 y este fichero.
- **Implementa:** documentación de las dudas 2 a 6. Criterio de SPEC §21 l.1205 con la evidencia de T5.
- **Hecho cuando:** la tabla de secretos de §11.4 lista `CIFRADO_CLAVE_DATOS`, `CIFRADO_CLAVE_BUSQUEDA` y `WEB_PUERTO` (opcional), con la advertencia de R-9. Los sabotajes de §6 están anotados.
- **Verificación:** lectura de los documentos y `/revisar 18`.

**Suma: 2,25 h de 2,75 h presupuestadas** (T4 retirada, duda 1).

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Suites existentes en verde tras el cambio de `Program.cs` | U · I | Las dos suites | — | T2 |
| `docker compose up` desde cero: 200 en las públicas, categorías sembradas, 302 en las privadas | Manual | — (salida anotada en §11) | SPEC §21 l.1205 | T5 |
| Sin `CIFRADO_CLAVE_DATOS`, `docker compose` se niega a arrancar | Manual | — | RN-168 | T3, T5 |

Un fichero por clase probada y por suite (`AGENTS.md` §11 bis). Esta fase no crea ni modifica ninguna clase con lógica, así que no añade ficheros de pruebas.

Casos del catálogo que no aplican, con su motivo:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `Program.cs` (arranque) | Todos | No es una clase probada: es código de nivel superior sin lógica de negocio. Se comprueba con la aplicación arrancada (T5) y con las suites existentes. Una prueba de arranque exigiría `WebApplicationFactory`, que no está en el stack (`fase-17.md`, duda 2) |
| `Dockerfile`, `docker-compose.yml` | Todos | No son código C#. Se verifican ejecutándolos (T1, T3 y T5) |

### Sabotajes

No hay reglas ⚑ en esta fase. Los sabotajes comprueban que la verificación de T5 detecta de verdad el fallo:

| Regla | Cambio temporal | Lo que debe fallar | Resultado |
|---|---|---|---|
| SPEC §21 l.1205 | En el fichero adicional de T5, `BaseDatos__MigrarAlArrancar=false` | Con el volumen vacío, `/businesses` no responde 200: no hay esquema | Hecho el 2026-10-25: `/businesses` respondió **500**. Restaurado (el override era temporal) |
| RN-168 | En el `--env-file` de T5, sin `CIFRADO_CLAVE_DATOS` | `docker compose` se niega a arrancar y nombra la variable que falta | Hecho el 2026-10-25: `required variable CIFRADO_CLAVE_DATOS is missing a value: Falta CIFRADO_CLAVE_DATOS en .env` |

## 7. Criterios de aceptación

- [x] **SPEC §21 (l.1205):** "`docker compose up` levanta aplicación y base de datos sin nada más instalado."
- [x] **Ningún secreto en el repositorio ni en la imagen:** `.env` excluido por `.dockerignore`, y las claves solo en `.env` (RN-168).
- [x] **En `Development` no cambia nada:** la aplicación sigue migrando y sembrando como antes, y las suites siguen en verde.
- [x] **Los sabotajes de §6** detectan el fallo.
- [x] **`dotnet build`** sin avisos y **`dotnet test`** en verde (448 unitarias y 439 de integración).

**Control 6** (`PROJECT_PLAN.md` l.217-224). Esta fase **no lo cierra**; deja preparada esta línea, que se vuelve a comprobar antes de entregar:

```text
docker compose up levanta la aplicacion desde cero
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-3 | El contenedor arranca sin la restricción `EXCLUDE` | El esquema sale de `MigrateAsync` (migraciones con el SQL manual), nunca de `EnsureCreated`. T5 parte de un volumen vacío |
| R-7 | El contenedor envía correos de verdad | `Correo:Habilitado` sigue en `false` por defecto y el servicio `web` no define ninguna variable de correo salvo `Correo__UrlBase` (T3) |
| R-9 ⚠ | El servicio `web` y la aplicación en desarrollo **comparten el volumen `datos-postgres`**. Con claves de `.env` distintas de las de User Secrets, los datos cifrados quedan ilegibles y nadie puede iniciar sesión | La tabla de secretos de `arquitectura.md` §11.4 lo advierte: `CIFRADO_CLAVE_DATOS` y `CIFRADO_CLAVE_BUSQUEDA` son **las mismas** que `Cifrado:ClaveDatos` y `Cifrado:ClaveBusqueda` de User Secrets. T5 usa un proyecto y un volumen aparte |
| R-14 | Varias réplicas multiplicarían el límite de frecuencia | La composición tiene una sola instancia de `web` |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Auditoría de dependencias vulnerables en la compilación (E-2) y resto del endurecimiento | Fase 19 |
| HTTPS, certificados y proxy inverso de producción | Despliegue; no está en el SPEC (duda 4) |
| Instrucciones de uso en el README (incluida la copia de seguridad de la clave, R-9) | Fase 20 |
| Datos de demostración en la composición | Siguen desactivados por defecto (`DatosDemostracion:Habilitado`, Fase 17). Se activan con configuración propia del desarrollador |
| Despliegue en la nube, registro de imágenes y firma de artefactos | No está en el SPEC (`reglas-negocio.md` l.406) |

## 10. Trazabilidad

| RN · CU · criterio | Tarea | Prueba |
|---|---|---|
| SPEC §17 (`Dockerfile` multietapa y servicio `web`) | T1, T3 | T5 |
| SPEC §21 l.1205 | T2, T3, T5 | T5 y su sabotaje |
| R-7 | T3 | `PruebasInyeccionDependencias.La_configuracion_del_repositorio_no_habilita_el_envio_de_correo` |
| RN-168 | T1, T3 | `.dockerignore` (T1), sabotaje de T5 |
| R-9 | T6 | Revisión de `arquitectura.md` §11.4 |

## 11. Dudas

Resueltas a petición del desarrollador con la opción recomendada (2026-10-24).

### Decisiones de implementación

- **Hecho el 2026-10-25 (T1-T6).** `dotnet build` sin avisos; 448 unitarias y 439 de integración en verde; `comprobaciones.sh` sin hallazgos.
- **T1, `Dockerfile`:**
  - restaura copiando primero `nuget.config`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` y los cuatro `.csproj` de `src/`, para que la capa de restauración se reutilice;
  - publica en `Release` con `TreatWarningsAsErrors`.
  - La etapa final crea `/home/app/.aspnet/DataProtection-Keys` con el propietario `$APP_UID`. Sin eso, el volumen nombrado nacería de `root` y el usuario sin privilegios no podría escribir (duda 5). Es el único comentario del fichero.
  - Comprobado: la imagen se ejecuta como `uid=1654(app)` y `/app` no contiene `.env`.
- **T1, `.dockerignore`:** además de lo previsto, excluye `.env.*` y `*.md`, que tampoco hacen falta para compilar.
- **T1, sin la solución:** el `Dockerfile` no copia `PlataformaReservas.slnx`, a diferencia de lo que decía T1. Restaura y publica directamente `PlataformaReservas.Web.csproj`, que arrastra sus tres proyectos referenciados; la solución solo añadiría los de pruebas, que no entran en la imagen (`/revisar 18`, menor 3).
- **T3, puerto solo en el bucle local:** `127.0.0.1:${WEB_PUERTO:-8080}:8080`, para que la aplicación, que va por HTTP en claro (duda 4), no quede accesible desde la red local (`/revisar 18`, menor 2).
- **T2:** `Program.cs` migra y siembra con `IsDevelopment() || BaseDatos:MigrarAlArrancar`. No se declara en `appsettings*.json`.
- **T3:** el comentario de cabecera de `docker-compose.yml` lista las variables de `.env` y advierte de R-9. El servicio `postgres` no cambia.
- **T4, retirada (duda 1):** el desarrollador decidió el 2026-10-25 que el proyecto no tiene integración continua. Se borró el fichero que se había creado y se quitaron sus referencias de toda la documentación. E-2 (Fase 19) pasa a la auditoría de NuGet en la compilación.
- **T5, verificación desde cero (duda 7):**
  - proyecto `pr-verificacion` con un `--env-file` de valores aleatorios y un override de `container_name` en el scratchpad. No se leyó `.env` ni se tocó `plataformareservas-db`;
  - resultado con volumen vacío: `/` 200, `/businesses` 200, `/login` 200, `/app` y `/my-account/bookings` 302 a `/login?ReturnUrl=…`; 8 categorías; `ex_reservas_sin_solape` en `pg_constraint` (R-3);
  - la clave de Data Protection (`key-4c21a90e-….xml`) sobrevive a `--force-recreate` del contenedor `web` (duda 5);
  - al terminar, `down -v --rmi local` y borrado de los ficheros temporales: no queda ningún contenedor, volumen ni imagen de `pr-verificacion`.
- **Avisos esperables del primer arranque**, anotados en `arquitectura.md` §11.4: la consulta fallida a `__EFMigrationsHistory` en una base vacía, `libgssapi_krb5.so.2` (sonda de Kerberos de Npgsql), `XmlKeyManager[35]` (claves de Data Protection sin cifrar en el volumen) y `HttpsRedirectionMiddleware[3]` (solo HTTP, duda 4). El aviso `Model[10631]` sobre `ConfiguracionReserva` ya existía en desarrollo y no es de esta fase.
- **Comprobación del desarrollador con su entorno real (2026-10-25):**
  - `CIFRADO_CLAVE_DATOS` y `CIFRADO_CLAVE_BUSQUEDA` añadidas a `.env`, con los mismos valores que `Cifrado:ClaveDatos` y `Cifrado:ClaveBusqueda` de User Secrets (`dotnet user-secrets list`, R-9);
  - `docker compose up --build -d`: `postgres` *healthy* y `web` en marcha, con solo los avisos esperables en el registro;
  - en `http://localhost:8080`: la portada y `/businesses` cargan, inicia sesión un usuario que ya existía (las claves coinciden con los datos cifrados de desarrollo), y `/app` y `/my-account/bookings` funcionan;
  - tras `docker compose up -d --force-recreate web`, la sesión sigue abierta (volumen `claves-web`, duda 5).

### Duda 1 — La integración continua

- **Pregunta:** SPEC §19 (l.1110) describe la Fase 18 solo como "`Dockerfile` y servicio `web` en la composición", pero varios documentos mencionaban una integración continua.
- **Opciones:**
  - (a) Crearla en esta fase.
  - (b) Crearla en la Fase 19.
  - (c) No tenerla.
- **Recomendación y motivo:** (a), propuesta al generar la especificación.
- **Bloquea:** T4.
- **Resolución:** ~~(a), la recomendada. Resuelta por el desarrollador el 2026-10-24.~~ **(c): el proyecto no tiene integración continua.** Decisión del desarrollador el 2026-10-25: no está en el stack (AGENTS §4, SPEC §7). Se retira T4 y se eliminan sus referencias de toda la documentación. E-2 se redefine como auditoría de NuGet en la compilación (Fase 19).

### Duda 2 — Migrar y sembrar fuera de `Development`

- **Pregunta:** `Program.cs` solo ejecuta `MigrarYSembrarAsync` en `Development`. En el contenedor (`Production`), la base arrancaría vacía y SPEC §21 l.1205 no se cumpliría.
- **Opciones:**
  - (a) Una clave de configuración, `BaseDatos:MigrarAlArrancar`: `false` por defecto, sin declararla en `appsettings*.json`, y `true` solo en el servicio `web` de la composición.
  - (b) `ASPNETCORE_ENVIRONMENT=Development` en la composición.
  - (c) Un servicio de migración aparte, con el paquete de migraciones de `dotnet ef`.
- **Recomendación y motivo:** (a). (b) expone las páginas de error de desarrollo y desactiva HSTS. (c) necesita otra imagen con el SDK y no cabe en el presupuesto. (a) cambia una condición y deja `Development` igual.
- **Bloquea:** T2, T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 3 — Secretos del servicio `web`

- **Pregunta:** el contenedor necesita la cadena de conexión y las dos claves de cifrado. Los documentos solo fijan `POSTGRES_USER`, `POSTGRES_PASSWORD` y `POSTGRES_PUERTO` en `.env` (`arquitectura.md` §11.4).
- **Opciones:**
  - (a) Dos variables nuevas y obligatorias en `.env`, `CIFRADO_CLAVE_DATOS` y `CIFRADO_CLAVE_BUSQUEDA` (con `:?`). La cadena de conexión se compone en la composición con `POSTGRES_USER` y `POSTGRES_PASSWORD`. Las rellena el desarrollador, con las mismas claves que User Secrets (R-9).
  - (b) Secretos de Docker montados como ficheros.
- **Recomendación y motivo:** (a). Es coherente con D-26 (todo en `.env` sin versionar), no añade mecanismos nuevos y falla con un mensaje claro si falta una clave.
- **Bloquea:** T3, T6.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 4 — HTTP o HTTPS en el contenedor

- **Pregunta:** la imagen de ASP.NET escucha HTTP en el puerto 8080 y no tiene certificado. Los documentos no dicen cómo se sirve la aplicación en la composición.
- **Opciones:**
  - (a) Solo HTTP, publicado en `localhost:${WEB_PUERTO:-8080}`, con `Correo__UrlBase=http://localhost:${WEB_PUERTO:-8080}`. HTTPS queda para el despliegue real, detrás de un proxy inverso.
  - (b) Montar un certificado de desarrollo en el contenedor.
- **Recomendación y motivo:** (a). La composición es para ejecutar el proyecto en local. Un certificado montado añade pasos manuales por máquina y no es lo que pide SPEC §17.
- **Bloquea:** T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 5 — Claves de Data Protection en el contenedor

- **Pregunta:** ASP.NET guarda las claves de las *cookies* de sesión y del *antiforgery* en el sistema de ficheros del contenedor. No son las claves de D-22. Al recrear el contenedor se pierden, y con ellas las sesiones abiertas.
- **Opciones:**
  - (a) Un volumen nombrado, `claves-web`, montado en la ruta de claves del usuario de la imagen. No toca código.
  - (b) No persistirlas y documentar que recrear el contenedor cierra las sesiones.
- **Recomendación y motivo:** (a). Es una línea en la composición, evita avisos al arrancar y sesiones que caducan sin motivo aparente.
- **Bloquea:** T3.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 6 — `.dockerignore` y usuario no root

- **Pregunta:** ninguno de los dos figura en los documentos. Sin `.dockerignore`, un `COPY` amplio mete `.env` (con la contraseña y las claves), `bin/` y `obj/` en el contexto de compilación.
- **Opciones:**
  - (a) Crear `.dockerignore` (excluye `.env`, `bin/`, `obj/`, `.git`, `docs/`, `.claude/`, `.vscode/`, `tests/` y `appsettings.*.local.json`), ejecutar la etapa final con `USER $APP_UID` y añadir `.dockerignore` al árbol de `arquitectura.md` §2.
  - (b) Un `COPY` selectivo, sin `.dockerignore`, y el usuario por defecto de la imagen.
- **Recomendación y motivo:** (a). Protege RN-168 aunque un `COPY` futuro se amplíe, y ejecutar como no root es la configuración por defecto que proponen las imágenes oficiales de .NET.
- **Bloquea:** T1.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

### Duda 7 — Verificar "desde cero" sin borrar la base de desarrollo

- **Pregunta:** "desde cero" exige un volumen vacío. El servicio `postgres` usa `container_name: plataformareservas-db` y el volumen `datos-postgres`, que contienen los datos de desarrollo. Además, la IA no puede leer `.env`.
- **Opciones:**
  - (a) La IA usa un proyecto de composición aparte: `-p pr-verificacion`, un `--env-file` en el scratchpad con valores aleatorios y un fichero adicional, también en el scratchpad, que cambia `container_name` y los puertos. Al terminar, `down -v` de ese proyecto.
  - (b) El desarrollador ejecuta `docker compose down -v` y `docker compose up --build` con su `.env`, y pierde los datos locales.
  - (c) Las dos.
- **Recomendación y motivo:** (a) como evidencia de la fase: no toca los datos del desarrollador ni lee sus secretos, y prueba exactamente el `docker-compose.yml` versionado. El desarrollador lo repite con su `.env` en el Control 6.
- **Bloquea:** T5.
- **Resolución:** (a), la recomendada. Resuelta por el desarrollador el 2026-10-24.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde (448 / 439)
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con lo que falló anotado, y la configuración restaurada
- [ ] `/auditar-pruebas`: no aplica (solo fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control: ninguno se cierra; el Control 6 queda preparado
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6: ninguna es de esta fase
