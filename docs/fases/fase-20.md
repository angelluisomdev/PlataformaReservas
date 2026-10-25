# Fase 20 — Documentación técnica

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 6,75 h (SPEC §19) |
| **Semana** | 6 (`PROJECT_PLAN.md` §2) |
| **Cierra** | Control 6 |
| **Generada** | 2026-10-25 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

La documentación técnica queda completa y coherente con el código entregado:

- el README permite levantar, configurar y probar el proyecto desde cero;
- existen los nueve documentos de SPEC §18;
- los recuentos y estados de todos los documentos coinciden con la realidad;
- cada criterio de SPEC §21 está marcado con su evidencia.

Con eso se puede cerrar el Control 6.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 | Fila 20: "Documentación técnica", 6,75 h. Recorrido del presupuesto: 1 h de documentación del correo (l.1129) y 0,75 h de "modelo de amenaza y gestión de la clave" (l.1131) |
| SPEC §18 | Lista cerrada de documentos: README, `AGENTS.md`, `CLAUDE.md` y nueve ficheros en `docs/`, entre ellos `motor-disponibilidad.md` (Fase 10) y `concurrencia.md` (Fase 11), que no existen |
| SPEC §21 | Criterio "Existe documentación técnica" y el resto de casillas sin marcar |
| SPEC §21 bis | Estado de cierre y recuentos (l.1240-1243) |
| SPEC §15.1, D-22 | Modelo de amenaza del cifrado en reposo |
| SPEC §16 bis, §17, D-20, D-26 | Correo, composición y base de datos local que el README tiene que explicar |
| `PROJECT_PLAN.md` §3 | Control 6, copiado en §7 |
| `PROJECT_PLAN.md` §6 y §7 | Tareas de verificación y estado de avance, sin actualizar desde el 2026-10-12 |
| `fase-12.md`, duda 10 | Incoherencias aplazadas "a la revisión documental de la Fase 20" |
| `fase-18.md` §9 | "Instrucciones de uso en el README (incluida la copia de seguridad de la clave, R-9) → Fase 20" |
| `fase-19.md` §9 · `fase-19-revision.md` (ronda de las 14:12) | Cuatro menores abiertos: 2 y 4 son documentales; 1 y 3, de código (duda 2) |
| R-7, R-8, R-9 | Riesgos que la documentación de uso tiene que dejar escritos (§8) |
| CU-05 | Destino tras iniciar sesión, cambiado en la tarea de interfaz fuera de fase (commit `96f05dd`, duda 3) |

La fase no implementa ninguna regla de negocio: no hay RN en §2.

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| Fase 19 cerrada: `/revisar 19` `APTO` (ronda de las 14:12) y commit `98aa768` | ✔ comprobado |
| Árbol de trabajo limpio; tarea de interfaz en el commit `96f05dd` | ✔ comprobado |
| `README.md`, `AGENTS.md` y `CLAUDE.md` | ✔ comprobado |
| `docs/` con `casos-uso.md`, `reglas-negocio.md`, `modelo-dominio.md`, `arquitectura.md`, `base-datos.md`, `seguridad.md` y `pruebas.md` | ✔ comprobado |
| `docs/motor-disponibilidad.md` y `docs/concurrencia.md` | ✘ ausentes. Resuelto en la duda 1: se crean en T4 y T5 |
| Código que documentan T4 y T5: `MotorDisponibilidad.cs`, `PruebasMotorDisponibilidad.cs`, `TraductorErroresPostgres.cs`, `PruebasConcurrenciaReservas.cs` y la migración con `EXCLUDE` | ✔ comprobado |
| `Dockerfile`, `docker-compose.yml` y `.config/dotnet-tools.json` | ✔ comprobado |
| Docker disponible para la suite de integración (T8) | ✔ comprobado |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | |
| `src/PlataformaReservas.Aplicacion` | no | |
| `src/PlataformaReservas.Infraestructura` | no | |
| `src/PlataformaReservas.Web` | no | |
| `tests/PlataformaReservas.PruebasUnitarias` | no | |
| `tests/PlataformaReservas.PruebasIntegracion` | no | |
| `docs/` | sí | Crear `motor-disponibilidad.md` y `concurrencia.md`. Modificar `TFM_PlataformaReservas_SPEC.md` (§18, §20, §21, §21 bis y §22), `PROJECT_PLAN.md` (l.103, §6 y §7), `arquitectura.md` (§8.5, §11.4, §13, §14.5 y l.1507), `seguridad.md` (§3), `pruebas.md` (recuentos), `casos-uso.md` (l.705), `modelo-dominio.md` (l.771), `reglas-negocio.md` (l.209), `fases/fase-19.md` (§6 y §12) y este fichero. **Excepción al radio, autorizada por el desarrollador el 2026-10-25** al pedir la corrección de `/revisar 20`: para registrar sus comprobaciones manuales del Control 6 se tocan además `fases/fase-16b.md` (solo la casilla del Control 4b en §12), `casos-uso.md` (l.251 y l.544, los criterios de CU-07 y CU-24) y la línea final de `arquitectura.md` ("Siguiente: Fase 4" pasa a "Estado (Fase 20)", nota de T6) |
| `README.md` (raíz) | sí | T1, T2 y T6 |
| `AGENTS.md` (raíz) | sí | Solo los recuentos de §2 (l.24 y l.27), en T6 |

**Todo lo que no figure como "sí" no se toca en esta fase.** `CLAUDE.md`, `.claude/`, `docker-compose.yml`, `Dockerfile` y `appsettings*.json` no cambian: se documentan tal como están.

## 5. Tareas

### T1 — README: puesta en marcha completa y mapa de la documentación (≈ 1 h)

- **Ficheros:** modifica `README.md`.
- **Implementa:** SPEC §17 y §18; `fase-18.md` §9; R-9; duda 3.
- **Contenido:**
  - **Puesta en marcha:** una sección "Todo en contenedores" con `docker compose up --build`. Variables del `.env` que exige el servicio `web`: `CIFRADO_CLAVE_DATOS`, `CIFRADO_CLAVE_BUSQUEDA` y, opcional, `WEB_PUERTO` (`arquitectura.md` §11.4, l.1066).
  - **Aviso R-9:** deben ser las mismas claves que `Cifrado:*` de User Secrets, porque los dos comparten el volumen `datos-postgres`. Al aviso de copia de seguridad se le añade el procedimiento de `arquitectura.md` l.1093.
  - **Mapa de la documentación:** tabla con los nueve documentos de SPEC §18, más `AGENTS.md` y `CLAUDE.md`, con una línea sobre qué contiene cada uno.
  - **Funcionalidad:** se refleja lo que cambió la tarea de interfaz (`96f05dd`, duda 3):
    - el destino tras iniciar sesión según el rol (CU-05);
    - la interfaz propia, *responsive* y con modo oscuro, sin bibliotecas.
- **Hecho cuando:** quien solo tenga .NET 10 y Docker puede seguir el README de principio a fin sin consultar otro documento para arrancar, y el README enlaza los once documentos.
- **Verificación:** lectura del README contra `docker-compose.yml`, `Dockerfile` y `arquitectura.md` §11.4: cada variable y cada comando existe con ese nombre exacto.

### T2 — Documentación del correo (≈ 1 h)

- **Ficheros:** modifica `README.md` y `docs/arquitectura.md` §13.
- **Implementa:** SPEC §16 bis, D-20; reparto de SPEC §19 (l.1129); R-7, R-8.
- **Contenido:**
  - **En el README, sección "Correo":**
    - las claves de `Correo:*` tal como están en `appsettings.json`: `Habilitado`, `Servidor`, `Puerto`, `Usuario`, `Contrasena`, `Remitente`, `NombreRemitente` y `UrlBase`;
    - `Habilitado` vale `false` por defecto, y entonces no se envía nada (R-7);
    - los secretos (`Usuario`, `Contrasena`) van en User Secrets o en variables de entorno, nunca en `appsettings.json`;
    - Mailtrap en desarrollo;
    - la entregabilidad (SPF y DKIM, R-8);
    - la cola volátil (RN-144).
  - **En `arquitectura.md` §13:** se repasa contra el código que la lista de plantillas, manejadores y registro en DI coincide. Las 11 plantillas de SPEC §16 bis y la recuperación de contraseña, §13.6.
- **Hecho cuando:** el README explica cómo activar el envío en desarrollo sin tocar ningún fichero versionado, y §13 no nombra ninguna clase ni plantilla que no exista.
- **Verificación:** `Grep` de cada clase y plantilla citada en §13 contra `src/PlataformaReservas.Infraestructura/Correo/` y `Notificaciones/`. Ninguna instrucción del README pone `Habilitado` a `true` en un fichero versionado.

### T3 — Modelo de amenaza y gestión de la clave (≈ 0,75 h)

- **Ficheros:** modifica `docs/arquitectura.md` §8.5 y §11.4, y `docs/seguridad.md` §3.
- **Implementa:** SPEC §15.1, D-22; reparto de SPEC §19 (l.1131); R-9.
- **Contenido:** en `arquitectura.md` §8.5, "Gestión de la clave", el ciclo de vida completo en un solo sitio:
  1. **Generar:** 32 bytes aleatorios, dos claves distintas.
  2. **Guardar:** User Secrets en desarrollo y `.env` o variables de entorno en la composición.
  3. **Copiar:** en un gestor de contraseñas, fuera de la máquina.
  4. **Validar al arrancar:** `AnilloClaves`.
  5. **Desplegar:** las mismas claves en todos los entornos que comparten la base.
  6. **Pérdida:** es irreversible (R-9).
  7. **Compromiso:** no hay rotación en el MVP (H-16, `seguridad.md` §4); qué se haría y por qué queda como trabajo futuro.

  `seguridad.md` §3 y `arquitectura.md` §11.4 remiten a esa sección en lugar de repetirla.
- **Hecho cuando:** los siete pasos están en §8.5, y no hay dos documentos que los describan de forma distinta.
- **Verificación:** lectura cruzada de los tres sitios. Las referencias a `AnilloClaves`, `ProtectorDatosPersonales` y `ProtectorBusqueda` existen en `src/`.

### T4 — `docs/motor-disponibilidad.md` (≈ 1 h)

- **Ficheros:** crea `docs/motor-disponibilidad.md`.
- **Implementa:** SPEC §18 (documento de la Fase 10), SPEC §12; duda 1.
- **Contenido:**
  - el contrato (SPEC §12.1) y el algoritmo paso a paso (SPEC §12.2), con la clase y el método reales de `MotorDisponibilidad.cs`;
  - la resolución de políticas y sus rangos (`PoliticasReserva`);
  - el único punto de conversión entre hora local y UTC (`AGENTS.md` §9);
  - la regla de solapamiento y su única implementación (`FranjaHoraria.SeSolapaCon`, `AGENTS.md` §3.7);
  - el motor compartido entre el flujo público y el área de empresa (`AGENTS.md` §10);
  - las decisiones de `fase-10.md` §11;
  - una tabla de las 13 pruebas de SPEC §16.1, con su nombre real en `PruebasMotorDisponibilidad.cs`.
- **Hecho cuando:** cada paso del algoritmo cita el método que lo implementa y cada una de las 13 pruebas aparece con su nombre exacto.
- **Verificación:** `Grep` de cada nombre de prueba y de método citado.

### T5 — `docs/concurrencia.md` (≈ 0,75 h)

- **Ficheros:** crea `docs/concurrencia.md`.
- **Implementa:** SPEC §18 (documento de la Fase 11), SPEC §13; duda 1.
- **Contenido:**
  - **Las tres capas:** interfaz, recomprobación dentro de la transacción y restricción `EXCLUDE`;
  - **la restricción:** su SQL tal como está en la migración (`base-datos.md`) y por qué se escribe a mano;
  - **el error `23P01`:** cómo se traduce a `HuecoYaNoDisponible` en `TraductorErroresPostgres`;
  - **el ámbito del contexto en Blazor Server:** R-1, `arquitectura.md` §4.3;
  - **la prueba de concurrencia** (`arquitectura.md` §12.3, `PruebasConcurrenciaReservas`), con su variante sobre un hueco recién cancelado (Fase 17);
  - **R-3:** por qué el esquema sale de las migraciones y no de `EnsureCreated`.
- **Hecho cuando:** el documento explica qué impide dos reservas solapadas aunque lleguen a la vez, con el SQL y la prueba que lo demuestran.
- **Verificación:** `Grep` del nombre de la restricción (`ex_reservas_sin_solape`), del código `23P01` y de las pruebas citadas.

### T6 — Coherencia documental (≈ 1 h)

- **Ficheros:** modifica `AGENTS.md` (l.24 y l.27), `README.md` (sección Pruebas), `docs/TFM_PlataformaReservas_SPEC.md` (§20, regla 8, y l.1240-1243), `docs/casos-uso.md` (l.705), `docs/modelo-dominio.md` (l.771), `docs/reglas-negocio.md` (l.209), `docs/arquitectura.md` (l.1507 y §14.5), `docs/PROJECT_PLAN.md` (l.103) y `docs/fases/fase-19.md` (§6).
- **Implementa:** `fase-12.md`, duda 10; menores 2 y 4 de `fase-19-revision.md`.
- **Correcciones:**
  - **Recuento de reglas:** el número real de RN, comprobado con `Grep` (140 a 2026-10-25, hasta RN-179), donde ponga 100 o 137. Las 29 críticas se comprueban también.
  - **Casos de uso:** 24, donde ponga 23.
  - **Riesgos:** "Los cinco que más probablemente…" pasa a la tabla real de 14 riesgos.
  - **Pruebas de arquitectura:** cinco, donde ponga cuatro (README y SPEC §20, regla 8), como dice `CLAUDE.md`.
  - **`reglas-negocio.md` l.209:** la limitación de frecuencia por IP deja de figurar como "fuera de alcance", porque RN-173 y E-5 la incluyen.
  - **`PROJECT_PLAN.md` l.103:** el nombre de la Fase 12 pasa a "Landing pública SSR", igual que en SPEC §19.
  - **`arquitectura.md` §14.5 (menor 4):** el ejemplo pasa al limitador global particionado y la tabla lista todas las rutas del grupo `autenticacion` de `LimitacionFrecuencia.cs`.
  - **`fase-19.md` §6 (menor 2):** fila para `EventosSeguridad`: "se prueba a través de las clases que lo usan".
- **Hecho cuando:** `Grep` de "100 reglas", "137 reglas", "23 casos", "Los cinco" y "cuatro pruebas de arquitectura" no da resultados fuera de los informes históricos (`fase-NN-revision.md` y especificaciones de fases cerradas, que no se reescriben).
- **Verificación:** esos `Grep`, más la lectura de §14.5 contra `LimitacionFrecuencia.cs`.

### T7 — Estado de cierre (≈ 1 h)

- **Ficheros:** modifica `docs/TFM_PlataformaReservas_SPEC.md` (§18, §21, §21 bis y §22), `docs/PROJECT_PLAN.md` (§6 y §7), `docs/pruebas.md` y `docs/fases/fase-19.md` (§12).
- **Implementa:** Control 6; SPEC §21; duda 2 y duda 3.
- **Contenido:**
  - **SPEC §18:** se marcan con ✔ los nueve documentos.
  - **SPEC §21:** cada criterio marcado con su evidencia, como el resto de la lista. Los funcionales (empresa, visitante, cliente y empresa con sus reservas) citan la prueba o la fase que los sostiene. "Compila desde cero" cita T8. "Existe documentación técnica" se marca al terminar T1 a T6.
  - **SPEC §21 bis y §22:** el estado final del proyecto, en lugar de "Próximo paso: Fase 1".
  - **`PROJECT_PLAN.md` §7:**
    - una fila por fase de la 10 a la 20, con su revisión, sus pruebas y sus commits, sacados de cada `fase-NN-revision.md` y de `git log`;
    - la tarea de interfaz fuera de fase (`96f05dd`), sin horas del presupuesto (duda 3);
    - los menores de código de la Fase 19 como deuda técnica (duda 2).
  - **`PROJECT_PLAN.md` §6:** la CSP, resuelta (Control 5), y los menores, saldados o en deuda.
  - **`pruebas.md`:** los recuentos actuales de las dos suites, que salen de T8.
  - **`fase-19.md` §12:** se marcan `/revisar` y el commit, que ya están hechos.
- **Hecho cuando:** ninguna casilla de SPEC §21 queda sin marcar ni sin evidencia, y `PROJECT_PLAN.md` §7 tiene fila para todas las fases.
- **Verificación:** `Grep` de `- [ ]` en SPEC §21, que debe dar cero.

### T8 — Verificación del Control 6 (≈ 0,25 h)

- **Ficheros:** ninguno; los resultados van a T7 y a §12 de este fichero.
- **Implementa:** Control 6.
- **Pasos:**
  - `dotnet build` y `dotnet test` (las dos suites) desde un clon limpio del commit, en una carpeta temporal fuera del repositorio;
  - **`docker compose up` desde cero lo ejecuta el desarrollador** con su `.env`: las credenciales las pone siempre el desarrollador (SPEC §21, criterio de la composición).
- **Hecho cuando:** las dos suites están en verde en el clon limpio y el desarrollador confirma que la composición levanta.
- **Verificación:** la salida de `dotnet test` con los recuentos, anotada en `pruebas.md` y en SPEC §21.

**Suma: 6,75 h de 6,75 h presupuestadas.**

## 6. Pruebas

No aplica: la fase es documental y no crea ni modifica ninguna clase de `src/`. No hay casos del catálogo de `AGENTS.md` §11 bis que añadir.

La única ejecución de pruebas es la de T8: las dos suites completas desde un clon limpio, como evidencia del Control 6.

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Suites completas desde un clon limpio | U · I · C · A | Todas | Control 6, SPEC §21 | T8 |

Casos del catálogo que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| — | Todos | La fase no crea ni modifica clases |

### Sabotajes

No aplica: la fase no implementa ni toca ninguna regla ⚑. Los sabotajes de las reglas críticas se hicieron en sus fases y constan en §6 de cada `fase-NN.md`.

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| — | — | — | no aplica |

## 7. Criterios de aceptación

- [ ] Los nueve documentos de SPEC §18 existen; `motor-disponibilidad.md` y `concurrencia.md` citan clases, métodos y pruebas que existen (T4, T5).
- [ ] El README permite arrancar en local y en contenedores, configurar el correo y hacer la copia de las claves, y enlaza toda la documentación (T1, T2).
- [ ] La gestión de la clave tiene su ciclo de vida completo en un solo sitio (T3).
- [ ] Ningún documento vigente da recuentos de RN, CU, riesgos o pruebas de arquitectura distintos de los reales (T6).
- [ ] Ninguna casilla de SPEC §21 queda sin marcar ni sin evidencia (T7).
- [ ] `PROJECT_PLAN.md` §7 tiene fila para cada fase, de la 0 a la 20 (T7).

Control 6, copiado de `PROJECT_PLAN.md` §3:

```text
Todas las pruebas en verde
docker compose up levanta la aplicacion desde cero
Documentacion tecnica completa
Todos los criterios de aceptacion de §21 del SPEC marcados
```

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-9 ⚑ Pérdida de la clave de cifrado | Un README que explica cómo generar las claves pero no cómo conservarlas, o que no avisa de que la composición necesita **las mismas** que User Secrets | T1 y T3: copia fuera de la máquina, pérdida irreversible y la misma clave en todos los entornos que comparten la base, en el README y en `arquitectura.md` §8.5 |
| R-7 Correos reales por descuido | Una instrucción del README que pone `Correo:Habilitado` a `true` en `appsettings.json` | T2: activarlo solo con User Secrets o variables de entorno, y decir que el valor por defecto es `false` |
| R-8 Entregabilidad | El correo "funciona" en desarrollo y en producción acaba en spam | T2: el README menciona SPF y DKIM y que no está presupuestado |
| R-3 `EXCLUDE` ausente en pruebas | `concurrencia.md` describe la garantía sin decir de qué depende | T5: por qué el esquema sale de las migraciones, y la comprobación en `pg_constraint` |

El resto de riesgos de `arquitectura.md` §15 (R-1, R-2, R-4 a R-6 y R-10 a R-14) son de código y no se manifiestan en una fase documental. `concurrencia.md` cita R-1 solo como contexto.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Menores de código de `/revisar 19` (1: registro de la baja de empresa; 3: alias de `RolesIdentidad`) | Deuda técnica registrada en `PROJECT_PLAN.md` §7 (duda 2) |
| Deuda R12 a R23 de la refactorización de la Fase 19 | Ya documentada en `fase-19.md` §9; no cambia |
| Cualquier cambio en `src/`, `tests/`, `docker-compose.yml`, `Dockerfile` o `appsettings*.json` | No se hace. Si la documentación revela un fallo, se anota aquí como hallazgo |
| `docs/desarrollo-asistido-ia.md` (métricas de la IA) | `/metricas`; no figura en SPEC §18 |
| La memoria del TFM | Fuera del repositorio |
| Reescribir especificaciones e informes de fases cerradas | No se hace: son registro histórico. Solo `fase-19.md` §6 y §12, por los menores y las casillas pendientes |

## 10. Trazabilidad

La fase no implementa reglas RN ni casos de uso. Traza sus orígenes:

| Origen | Tarea | Comprobación |
|---|---|---|
| SPEC §17, §18; `fase-18.md` §9; R-9 | T1 | Lectura contra `docker-compose.yml` y `arquitectura.md` §11.4 |
| SPEC §16 bis, D-20, l.1129; R-7, R-8 | T2 | `Grep` de clases y plantillas de §13 |
| SPEC §15.1, D-22, l.1131; R-9 | T3 | Lectura cruzada de §8.5, §11.4 y `seguridad.md` §3 |
| SPEC §18 (Fase 10), SPEC §12 | T4 | `Grep` de los 13 nombres de prueba |
| SPEC §18 (Fase 11), SPEC §13; R-3 | T5 | `Grep` de `ex_reservas_sin_solape` y `23P01` |
| `fase-12.md`, duda 10; menores 2 y 4 de la Fase 19 | T6 | `Grep` de los recuentos antiguos |
| SPEC §21, §21 bis, §22; `PROJECT_PLAN.md` §6 y §7; dudas 2 y 3 | T7 | `Grep` de `- [ ]` en SPEC §21 |
| Control 6 | T8 | `dotnet test` en un clon limpio y `docker compose up` del desarrollador |
| CU-05 (cambio de `96f05dd`) | T1 | El README lo describe igual que `casos-uso.md` |

## 11. Dudas

### Duda 1 — `motor-disponibilidad.md` y `concurrencia.md` no existen

- **Pregunta:** SPEC §18 los asigna a las fases 10 y 11, pero no se crearon. Su contenido está repartido entre SPEC §12 y §13, `arquitectura.md` §9.5 y §12.3, `base-datos.md` y las especificaciones de esas fases. ¿Se crean ahora?
- **Opciones:**
  - (a) crearlos en esta fase (T4 y T5, 1,75 h);
  - (b) quitarlos de SPEC §18 y remitir a las secciones existentes.
- **Recomendación y motivo:** (a). Son el núcleo técnico del TFM ("Nunca se recortan las fases 10, 11…", `PROJECT_PLAN.md` §4), y un documento propio por problema es lo que la memoria necesita citar. Caben en el presupuesto de la fase.
- **Bloquea:** T4 y T5.
- **Resolución:** (a), la recomendada, por indicación del desarrollador el 2026-10-25.

### Duda 2 — Menores de código abiertos de la Fase 19

- **Pregunta:** `/revisar 19` (ronda de las 14:12) deja dos menores de código:
  - el 1: la baja de empresa solo se registra de forma indirecta;
  - el 3: se importa el espacio de nombres `Infraestructura.Identidad` para dos constantes.

  Esta fase es documental. ¿Se corrigen?
- **Opciones:**
  - (a) registrarlos como deuda técnica, con su motivo, en `PROJECT_PLAN.md` §7;
  - (b) abrir una excepción de código en esta fase.
- **Recomendación y motivo:** (a). Son menores y no afectan a ningún criterio de §21. Abrir código en la fase de documentación obligaría a rehacer pruebas y revisión justo antes de entregar, y `PROJECT_PLAN.md` §6 prevé exactamente esto ("se documenta como deuda técnica con su motivo").
- **Bloquea:** T6 y T7.
- **Resolución:** (a), la recomendada, por indicación del desarrollador el 2026-10-25.

### Duda 3 — Registro de la tarea de interfaz fuera de fase

- **Pregunta:** el commit `96f05dd` ("css y reorganización parte web") incluye cuatro cambios sin rastro en el plan ni en el SPEC. Su detalle solo consta en `arquitectura.md` §2.3 y en CU-05. ¿Se registra?
  - un sistema de diseño propio, sin bibliotecas;
  - la navegación del área de empresa;
  - el listado con ventana emergente en Servicios y Profesionales;
  - el destino tras iniciar sesión según el rol (CU-05).
- **Opciones:**
  - (a) anotarla en `PROJECT_PLAN.md` §7 como trabajo fuera de fase y sin horas del presupuesto, y reflejarla en el README;
  - (b) no registrarla.
- **Recomendación y motivo:** (a). La regla de oro de `AGENTS.md` §2 pide que lo construido esté en los documentos. Sin registro, una revisión futura lo vería como ampliación de alcance no autorizada, y las 122,75 h del plan dejarían de explicar todo el trabajo.
- **Bloquea:** T1 y T7.
- **Resolución:** (a), la recomendada, por indicación del desarrollador el 2026-10-25.

### Notas de implementación (2026-10-25)

- **T4 y T5.** `motor-disponibilidad.md` y `concurrencia.md` se escriben desde el código, no desde el SPEC. Cada método y cada prueba citados se comprobaron con `Grep`. Dos diferencias con SPEC §12 y §13, que el documento explica:
  - el paso 0 de la empresa inactiva (fase 10, duda 3);
  - la traducción del error, que no devuelve directamente `HuecoYaNoDisponible`: lanza `HuecoOcupadoException` desde `UnidadTrabajo` y el caso de uso la convierte en `Resultado` (también traduce el interbloqueo `40P01`, fase 11, desviación 1).
- **T2.** El ejemplo de `arquitectura.md` §13.4 usaba `RenderizarAsync<PlantillaReservaConfirmada>`, que no existe. Se sustituye por el método real, `RenderizadorPlantillas.ReservaConfirmadaClienteAsync`. El ejemplo de §13.5 también llamaba a métodos inexistentes (`CorreoClienteAsync` y `CorreoNegocioAsync`); se corrigió tras `/revisar 20` con los nombres reales de `EnviarCorreosReservaConfirmada`. El primer repaso solo había buscado nombres de tipo, no de método.
- **T6.**
  - **Recuento de objetos de valor:** además de lo previsto, SPEC §21 bis decía "4 objetos de valor"; son 22 (`AGENTS.md` §7).
  - **Menciones a "cuatro pruebas de arquitectura" con contexto de la Fase 4** (presupuesto, comprobación negativa e historia): se mantienen porque son correctas en su momento. Solo cambian las afirmaciones en presente: README, SPEC §20 regla 8 y la línea "Siguiente: Fase 4" de `arquitectura.md`, ahora "Estado".
- **T7.**
  - **Fase 17:** su informe termina en `NO APTO` (19:07), con un bloqueante: `TryObtener` en inglés. Se corrigió antes del commit, sin ronda nueva; hoy es `UrlBaseCorreo.IntentarObtener`. Se anota así en `PROJECT_PLAN.md` §7.
  - **Controles 2, 3 y 4b:** no se dan por superados, porque sus casillas no constan marcadas en las especificaciones de sus fases.
- **T8.**
  - **Clon con rutas largas:** el clon en el *scratchpad* necesitó `core.longpaths` solo para ese comando, porque la ruta de algunas migraciones superaba el límite de Windows.
  - **Resultado:** el árbol del clon coincide con `HEAD`.
  - **Pendiente del desarrollador:** `docker compose up` desde cero.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos (T8, clon limpio del commit `96f05dd`)
- [x] `dotnet test` en verde (T8, clon limpio: 449 unitarias y 536 de integración)
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6: no aplican (fase documental)
- [x] `/auditar-pruebas`: no aplica (solo fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [x] **Control 6 superado**, incluida la `docker compose up` desde cero hecha por el desarrollador (comprobado a mano por el desarrollador el 2026-10-25)
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas: los menores de la Fase 19, saldados (2 y 4) o en deuda (1 y 3)
