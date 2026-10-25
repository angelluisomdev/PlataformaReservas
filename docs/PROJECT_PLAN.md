# PLAN DE PROYECTO — PlataformaReservas

**Calendario y puntos de control.**

Este documento contiene **únicamente** la planificación temporal.
El alcance, el modelo de dominio, las reglas, la nomenclatura y las decisiones técnicas
están en `TFM_PlataformaReservas_SPEC.md`, que es la única fuente de verdad.

---

## 1. Presupuesto

```text
122,75 h en total, en 6 semanas a ~20 h/semana

  Semana 1   21,5 h
  Semana 2   22 h
  Semana 3   19,5 h
  Semana 4   19,5 h
  Semana 5   17,5 h
  Semana 6   22,75 h

  de las cuales 16,75 h son modelado y documentación (fases 0–3 y 20)
  y 106 h son desarrollo efectivo (fases 4–19)
```

La redacción de la memoria del TFM se contabiliza **aparte**.

### 1.1. Recorrido del presupuesto

| Origen | h |
|---|---|
| Alcance inicial tras la auditoría (Fase 0) | 90 |
| Revisión crítica de la Fase 1: rangos de políticas, teléfono obligatorio, bajas con cascada | +4,5 |
| Ficheros guía para agentes y cuatro pruebas de arquitectura | +1,5 |
| Notificaciones por correo, eventos de dominio y cola en memoria | +17 |
| Cifrado de datos personales en reposo | +5,5 |
| Endurecimiento completo de la Fase 19 | +4,25 |
| | **122,75** |

### 1.2. Por qué seis semanas y no cuatro (D-24)

Con 122,75 h, cuatro semanas serían ~31 h semanales sostenidas. **Seis semanas a ~20 h es el ritmo del planteamiento original** y el único realista.

La decisión está tomada y **no se reabre**. Si el calendario se comprime, lo que se ajusta es el alcance según §4, nunca el ritmo.

---

## 2. Calendario

### Semana 1 — Modelado y base de datos (21,5 h)

| Fase | Contenido | h |
|---|---|---|
| 0 | Auditoría de especificación *(hecha)* | 2 |
| 1 | Casos de uso y reglas de negocio *(hecha)* | 3 |
| 2 | Modelo de dominio y diagrama entidad-relación *(hecha)* | 3 |
| 3 | Arquitectura técnica *(hecha)* | 2 |
| 4 | Solución, proyectos, referencias, pruebas, `AGENTS.md` y pruebas de arquitectura *(hecha)* | 4,5 |
| 5 | Persistencia: entidades, EF Core, índices, `EXCLUDE`, migraciones, datos iniciales, conversores de cifrado, **base de datos local** en Docker Compose *(hecha)* | 7 |

**Objetivo:** la solución compila, `dotnet test` pasa, y la base de datos arranca con el esquema completo y la restricción `EXCLUDE` en su sitio.

Las cuatro primeras fases ya están cerradas, así que la semana 1 real son **11,5 h de trabajo**.

**La base de datos local se levanta al empezar la Fase 5** (D-26): `docker compose up -d postgres`, con las credenciales en `.env` y la cadena de conexión en User Secrets, ambas puestas por el desarrollador. Las pruebas de integración no la necesitan: Testcontainers levanta su propia base, y basta con tener Docker en marcha.

---

### Semana 2 — Identidad, seguridad y configuración de negocio (22 h)

| Fase | Contenido | h |
|---|---|---|
| 6 | Identity: registro, sesión, roles, políticas, cuentas inactivas, recuperación de contraseña, **cifrado de datos personales** *(hecha)*| 8,5 |
| 7 | Multi-tenancy: `IContextoEmpresa`, repositorios y pruebas de aislamiento *(hecha)*| 5 |
| 8 | Configuración de empresa, servicios, profesionales, asociación, baja y reactivación *(hecha)*| 8,5 |

**Objetivo:** usuarios, empresas y aislamiento funcionando, con los datos personales ya cifrados desde el primer registro.

Cifrar desde la Fase 6 y no después **no es casualidad**: activarlo con datos ya existentes obligaría a migrarlos.

---

### Semana 3 — Núcleo técnico (19,5 h)

| Fase | Contenido | h |
|---|---|---|
| 9 | Horarios y excepciones *(hecha)*| 4 |
| 10 | **Motor de disponibilidad + 13 pruebas unitarias** e invariantes de políticas *(hecha)*| 8,5 |
| 11 | **Reservas: transacción, concurrencia, cancelación, completar** *(hecha)* | 7 |

**Objetivo:** el sistema calcula disponibilidad correcta y crea reservas sin solapamientos, todo verificado. Sin interfaz pública todavía.

**Es la semana crítica del proyecto.** Si algo se desvía, se recorta de las semanas 4 y 5, nunca de aquí.

---

### Semana 4 — Eventos y producto visible (19,5 h)

| Fase | Contenido | h |
|---|---|---|
| 11b | **Eventos de dominio, publicador, cola en memoria y procesador** *(hecha)*| 6 |
| 12 | Landing pública SSR *(hecha)* | 4 |
| 13 | Buscador y ficha pública *(hecha)* | 4 |
| 14 | Flujo de reserva público, incluida la petición de teléfono *(hecha)* | 5,5 |

**Objetivo:** un visitante puede buscar, entrar en una ficha y completar una reserva, con la infraestructura de eventos en pie aunque todavía sin manejadores.

La 11b va aquí porque necesita que las reservas existan, y debe estar **antes** que las notificaciones.

---

### Semana 5 — Áreas privadas y notificaciones (17,5 h)

| Fase | Contenido | h |
|---|---|---|
| 15 | Área de cliente, perfil y baja de cuenta *(hecha)* | 4,5 |
| 16 | Panel y gestión de reservas de la empresa *(hecha)* | 4 |
| 16b | **Servicio de correo, 11 plantillas, manejadores y pruebas** *(hecha)* | 9 |

**Objetivo:** los tres flujos completos de extremo a extremo, con las notificaciones funcionando.

La 16b va la última de las funcionales porque necesita que **todos** los casos de uso existan: notifica sobre reservas, empresas y cuentas.

Es la semana más ligera a propósito: deja margen para la deriva acumulada de las cuatro anteriores.

---

### Semana 6 — Calidad, seguridad y entrega (22,75 h)

| Fase | Contenido | h |
|---|---|---|
| 17 | Pruebas finales: integración, autorización, concurrencia, regresión, cifrado *(hecha)* | 6 |
| 18 | `Dockerfile` y servicio `web` en la composición *(hecha)* | 2,75 |
| 19 | **Endurecimiento (6 medidas), refactorización, revisión de seguridad y `docs/seguridad.md`** *(hecha)*| 7,25 |
| 20 | Documentación técnica | 6,75 |

**Objetivo:** proyecto entregable, reproducible desde cero y endurecido.

Es la semana más cargada. Está al final a propósito: nada de esto puede hacerse antes de que exista el resto.

---

## 3. Puntos de control

Ningún control se da por superado sin ejecutar las pruebas. Si falla, se detiene el avance y se corrige antes de continuar.

Al cerrar las fases 10, 11, 16 y 17 se ejecuta además `/auditar-pruebas` (`AGENTS.md` §11 bis). Sus casos importantes se escriben en la fase siguiente o, si no caben, en la Fase 17.

### Control 1 — fin de Fase 7

```text
dotnet build           OK
dotnet test            OK
Un usuario de la empresa A no puede leer ni modificar recursos de la empresa B
(5 pruebas de aislamiento en verde)

Un SELECT directo sobre asp_net_users no muestra correos ni telefonos en claro
Iniciar sesion, registrar correo duplicado y recuperar contrasena funcionan
La clave de cifrado esta respaldada FUERA de la maquina
```

La última no es una prueba automática y es la que más caro sale olvidar: **perder esa clave deja las cuentas irrecuperables** (riesgo R-9).

**Superado el 2026-10-04.** Evidencias en `fase-07.md` §7: 168 pruebas unitarias y 60 de integración en verde, las 5 de aislamiento con sabotaje, y el respaldo de la clave confirmado por el desarrollador.

### Control 2 — fin de Fase 10

```text
Las 13 pruebas del motor de disponibilidad en verde
El motor no depende de la interfaz ni de Infraestructura
PoliticasReserva rechaza intervalo 0, negativo y mayor de 480
```

### Control 3 — fin de Fase 11

```text
Pruebas de integracion de reservas en verde
Prueba de concurrencia en verde contra PostgreSQL real (Testcontainers)
La restriccion ex_reservas_sin_solape EXISTE en la base de datos de pruebas
```

La tercera línea se comprueba **consultando el catálogo de PostgreSQL**, no fiándose de que la migración se aplicó. Si el esquema se hubiera creado con `EnsureCreated`, la restricción no existiría y la prueba de concurrencia pasaría sin probar nada (riesgo R-3).

### Control 4 — fin de Fase 16

```text
Los tres flujos completos funcionan de extremo a extremo:
  la empresa se registra, se configura y se publica sola
  el visitante busca, encuentra y reserva
  cliente y empresa gestionan la reserva
```

### Control 4b — fin de Fase 16b

```text
Con el servicio de correo caido, la reserva se crea igualmente
Dar de baja una empresa con N reservas responde igual de rapido con 3 que con 300
El procesador sigue vivo tras un manejador que lanza
Recuperacion de contrasena funciona para cliente y para propietario
```

El tercero es el que más fácil se pasa por alto: una excepción que escape del procesador lo mata en silencio y a partir de ahí **no vuelve a salir ningún correo** sin que nada lo indique (riesgo R-6).

### Control 5 — fin de Fase 19

```text
Cinco intentos fallidos bloquean la cuenta 15 min, y quedan registrados
Superar el umbral en /login y /businesses devuelve 429
Las cabeceras estan presentes y la aplicacion FUNCIONA con la CSP activa
Una descripcion con <script> no se ejecuta ni en la ficha ni en el correo
La auditoria de dependencias vulnerables hace fallar la compilacion
```

La tercera exige **navegación manual por las tres áreas** revisando la consola del navegador: una CSP incompatible con Blazor Server deja la página inerte sin error visible (riesgo R-12).

### Control 6 — antes de entregar

```text
Todas las pruebas en verde
docker compose up levanta la aplicacion desde cero
Documentacion tecnica completa
Todos los criterios de aceptacion de §21 del SPEC marcados
```

---

## 4. Regla de desviación

Si una fase supera su presupuesto en más de un 30 %:

1. Parar.
2. Identificar qué parte se ha salido de la especificación.
3. Recortar hasta volver al alcance del SPEC.
4. Lo eliminado va a trabajo futuro (§6.2 del SPEC), no a "lo hago luego".

**Orden de recorte si falta tiempo**, de primero a último:

```text
Fase 16b (correo: solo los 4 del ciclo de reserva, sin altas ni bajas)   -4 h
Fase 16  (panel de empresa: reducir a listado de reservas)               -2 h
Fase 15  (area de cliente: sin pestana de historico)                     -1,5 h
Fase 13  (buscador: sin refinamiento visual ni sugerencia de quitar categoria; nunca sin ciudad, RN-110) -1 h
Fase 12  (landing: sin refinamiento visual)                              -1 h
```

Y, dentro de la fase 19, la medida **E-6 (CSP)** se saca si consume más de 1 h: es la única de las seis con riesgo de bloqueo y las otras cinco no dependen de ella.

**Nunca se recortan** las fases 10, 11, 11b, 17 ni 19. Son las cinco que sostienen el valor técnico del TFM: motor, concurrencia, eventos, pruebas y seguridad.

La 11b no se recorta ni aunque se recorten los correos: los eventos y la cola son el argumento arquitectónico, y sin ellos las cascadas de RN-150 y RN-151 no tienen dónde apoyarse.

---

## 5. Ritmo de trabajo

Una fase por sesión siempre que sea posible. Cada sesión termina con:

- código compilando, sin avisos nuevos;
- pruebas de la fase en verde;
- `/revisar` sin hallazgos bloqueantes;
- commit descriptivo.

No se empieza una fase nueva con la anterior a medias.

---

## 6. Tareas de verificación pendientes

No son decisiones abiertas: son comprobaciones que solo pueden hacerse con código delante. Cada una tiene su fase asignada y su plan B escrito.

| Qué verificar | Fase | Si no se cumple |
|---|---|---|
| ~~Que `Guid.CreateVersion7()` conserve la monotonía **tal y como Npgsql almacena el `uuid`**~~ | 5 | **Resuelta**: se conserva (`docs/base-datos.md` §8) |
| ~~Que estén corregidos los hallazgos menores abiertos de las fases anteriores (seguimiento del 2026-10-12 en cada `fase-NN-revision.md`, y los que dejen las fases 10 a 18)~~ | 19 | **Resuelta**: R1 a R11 hechos en la Fase 19; R12 a R23, deuda técnica con su motivo (`fase-19.md` §9). De `/revisar 19` quedan cuatro menores: los documentales (2 y 4) se corrigen en la Fase 20 y los de código (1 y 3) son deuda (§7) |
| ~~Que la CSP sea compatible con Blazor Server~~ | 19 | **Resuelta**: entra en menos de 1 h, sin `<ImportMap />`, y la navegación manual del Control 5 (2026-10-25) no da errores |
| Que la entregabilidad de correo sea aceptable (SPF, DKIM, carpeta de spam) | 16b | **No está presupuestado en horas.** Con proveedor transaccional y Mailtrap en desarrollo es manejable, pero cuenta con que se lleve una tarde |

---

## 7. Estado de avance

Actualizado el 2026-10-25 (Fase 20). Una fase está **cerrada** cuando su revisión da `APTO` y está en un commit. Las fases 4 y 5 se cerraron sin `/revisar`. El detalle de cada una está en §12 de `docs/fases/fase-NN.md` y en `docs/fases/fase-NN-revision.md`.

| Fase | Estado | Revisión | Pruebas (unit. / integr.) | Commits | Pendiente de la fase |
|---|---|---|---|---|---|
| 0–3 | Cerrada | — | — | — (documentación no versionada) | — |
| 4 | Cerrada sin revisión | No consta | En verde | `8f49ad1` | Comprobación negativa de las pruebas de arquitectura; `/revisar 4` |
| 5 | Cerrada sin revisión | No consta | 46 / 24 (en la primera revisión de la Fase 6) | `be4e1bb`, `31c073d` | `/revisar 5` |
| 6 | Cerrada | `APTO` 2026-10-04, 3 rondas | 46 / 27 | `a0b6f81`, `85aeb98` | 1 menor: `CancellationToken` en `RegistroCliente` |
| 7 | Cerrada · **Control 1 superado** | `APTO` 2026-10-04, 2 rondas | 168 / 61 | `4f7e539` (antes, `5bb8767` y `0ec9dd0`: refactorización de dominio y pruebas entre las fases 6 y 7) | 1 menor: comentario repetido en `RepositorioMiembros` |
| 8 | Cerrada | `APTO` 2026-10-10, 3 rondas | 211 / 115 | `547f191`, `bd71111`, `cdbac62` | 1 menor: `@using` de objetos de valor en `App/_Imports.razor`. 1 importante aplazado a la Fase 11 |
| 9 | Cerrada | `APTO` 2026-10-10, 2 rondas | 234 / 139 | `6b1ac13` | — |
| 10 | Cerrada · cierra el Control 2 (evidencias en `fase-10.md` §7; la casilla de §12 sigue sin marcar) | `APTO` 2026-10-12, 3 rondas | 297 / 146 | `50a95c4` (reorganización de DTO), `7693bc9` | — |
| 11 | Cerrada · cierra el Control 3 (evidencias en `fase-11.md` §7; la casilla de §12 sigue sin marcar) | `APTO` 2026-10-12, 5 rondas | 314 / 212 | `f647a1d` | — |
| 11b | Cerrada | `APTO` 2026-10-13, 3 rondas | 327 / 232 | `d9ffddf` | — |
| 12 | Cerrada | `APTO` 2026-10-17, 2 rondas | 328 / 233 | `9e2d51b` | — |
| 13 | Cerrada | `APTO` 2026-10-17, 2 rondas | 367 / 255 | `5c0f177` | — |
| 14 | Cerrada | `APTO CON RESERVAS` 2026-10-17, 2 rondas | 381 / 282 | `99a8735` | Sus tres puntos abiertos se resolvieron en la Fase 15 (`fase-15.md`, duda 10) |
| 15 | Cerrada | `APTO` 2026-10-18, 2 rondas | 404 / 329 | `1d95159` | — |
| 16 | Cerrada · **Control 4 superado** (guion recorrido a mano, 2026-10-18) | `APTO` 2026-10-18, 1 ronda | 424 / 347 | `c8f15cf` | — |
| 16b | Cerrada · **Control 4b superado** (prueba automática en sus cuatro líneas y comprobación con Mailtrap del desarrollador, 2026-10-25) | `APTO CON RESERVAS` 2026-10-24, 2 rondas, más un seguimiento de los menores | 447 / 402 | `f1f6ed6` | — |
| 17 | Cerrada | `NO APTO` 2026-10-24 19:07, 2 rondas: un bloqueante (`TryObtener` en inglés) corregido antes del commit, sin ronda nueva (hoy `UrlBaseCorreo.IntentarObtener`) | 448 / 439 | `9b8c084` | — |
| 18 | Cerrada | `APTO` 2026-10-25, 2 rondas | 448 / 439 | `b941d79` | — (`docker compose up` del desarrollador hecho en el Control 6) |
| 19 | Cerrada · **Control 5 superado** | `APTO` 2026-10-25, 3 rondas | 449 / 527 | `98aa768` | Menores de código 1 y 3 de `/revisar 19`: deuda técnica (abajo) |
| — | **Fuera de fase**, sin horas del presupuesto | Sin `/revisar` propio; se revisa con la Fase 20 | 449 / 536 | `96f05dd` | Tarea de interfaz pedida por el desarrollador el 2026-10-25: sistema de diseño propio en `app.css`, navegación del área de empresa, listado con ventana emergente en Servicios y Profesionales, y destino tras iniciar sesión según el rol (CU-05). Detalle en `arquitectura.md` §2.3 (`fase-20.md`, duda 3) |
| 20 | Implementada · **Control 6 superado** (`docker compose up` desde cero, desarrollador, 2026-10-25) | Pendiente de `/revisar 20` | 449 / 536 (clon limpio, `docs/pruebas.md` §6) | Pendiente | — |

### Deuda técnica al cierre

| Deuda | Origen | Motivo para no saldarla |
|---|---|---|
| R12 a R23 de la refactorización | `fase-19.md` §9, duda 7 | No cabían en la 1,5 h de refactorización; cada una lleva su motivo |
| La baja de empresa solo se registra de forma indirecta ("Sesiones de los miembros de la empresa … invalidadas") | `/revisar 19`, menor 1 | Menor y de código; la Fase 20 es documental (`fase-20.md`, duda 2) |
| Se importa el espacio de nombres `Infraestructura.Identidad` entero para dos constantes de `RolesIdentidad`, en lugar de un alias de tipo | `/revisar 19`, menor 3 | Ídem. Hoy afecta a `App/_Imports.razor`, `MiCuenta/_Imports.razor` y `Layout/MenuNavegacion.razor` |

### Pendientes que arrastran las fases cerradas

| Pendiente | Origen | Cuándo se cierra |
|---|---|---|
| ~~Rechazo de `CancelarPorEmpresa` sobre una reserva **completada**, con prueba~~ | `fase-08-revision.md`, importante 4 | **Hecho**: `PruebasReserva.CancelarPorEmpresa_rechaza_una_reserva_completada` |
| Comprobación negativa de las pruebas de arquitectura ante una entidad anémica | `fase-04.md` §7 | Cuando decida el desarrollador. Basta con hacerla una vez |
| ~~Menores abiertos de las fases 6, 7 y 8~~ | Seguimiento del 2026-10-12 en cada `fase-NN-revision.md` | **Hecho en la Fase 19** (R1 a R7) |
| ~~Criterio "el token de restablecimiento caduca y no se reutiliza" (RN-159) sin prueba propia~~ | `casos-uso.md` CU-24 | **Hecho**: comprobado a mano por el desarrollador el 2026-10-25 |
| ~~Recorrer en el navegador la petición de teléfono al reservar~~ | `casos-uso.md` CU-07 | **Hecho**: comprobado a mano por el desarrollador el 2026-10-25 |
| ~~Criterios de aceptación parciales de CU-11, CU-16, CU-23 y CU-24~~ | `casos-uso.md` §3 | **Hechos** en sus fases, incluidas las dos comprobaciones manuales de arriba |
