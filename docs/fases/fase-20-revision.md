# Revisión de la Fase 20

## Revisión 2026-10-25 18:58

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-20.md (APROBADA)
**Commit base:** 96f05dd
**Ficheros revisados:** 15. `.gitignore` excluye `*.md`: `git status` solo muestra `README.md`, y el resto se ha revisado leyéndolo en disco.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 3 | 4 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 449 | 449 | 0 |
| Integración | 536 | 536 | 0 |

- `dotnet build`: 0 avisos y 0 errores.
- `comprobaciones.sh`: 0 bloqueantes y 0 importantes. Sin líneas: la fase no toca código.
- Integración ejecutada porque la fase cierra el Control 6.

**Ficheros revisados:**

- `README.md` y `AGENTS.md`;
- `docs/motor-disponibilidad.md` y `docs/concurrencia.md`, los dos nuevos;
- `docs/TFM_PlataformaReservas_SPEC.md`, `docs/PROJECT_PLAN.md`, `docs/arquitectura.md`, `docs/seguridad.md` y `docs/pruebas.md`;
- `docs/casos-uso.md`, `docs/modelo-dominio.md` y `docs/reglas-negocio.md`;
- `docs/fases/fase-16b.md`, `fase-19.md` y `fase-20.md`.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance | `docs/fases/fase-16b.md:544` | `fase-20.md` §4 (radio) | Al registrar las comprobaciones manuales del desarrollador (2026-10-25), se marcó la casilla del Control 4b en §12 de `fase-16b.md`. Ese fichero no figura en el radio de §4, que de las especificaciones de fase solo autoriza `fase-19.md` (§6 y §12) y la propia `fase-20.md` | O se amplía §4 con una excepción autorizada por el desarrollador que nombre `fase-16b.md` §12 (solo la casilla del Control 4b), o se revierte la marca y la comprobación queda anotada solo en `PROJECT_PLAN.md` §7 y en las notas de `fase-20.md` |
| 2 | IMPORTANTE | arquitectura | `docs/arquitectura.md:1304-1316` (§13.5) | `fase-20.md` T2, "Hecho cuando: §13 no nombra ninguna clase ni plantilla que no exista". La nota de implementación de T2 afirma lo contrario | El ejemplo de `EnviarCorreosReservaConfirmada` llama a `render.CorreoClienteAsync` y a `render.CorreoNegocioAsync`, que no existen. Los métodos reales son `plantillas.ReservaConfirmadaClienteAsync` y `ReservaConfirmadaNegocioAsync` (`EnviarCorreosReservaConfirmada.cs:20-21`), y los parámetros también difieren. Comprobado con `Grep`: los dos nombres solo aparecen en `arquitectura.md` | Usar los nombres reales de métodos y parámetros, como en §13.4, y corregir la nota de T2 en §11 |
| 3 | IMPORTANTE | alcance | `docs/casos-uso.md:251` y `:544` | `fase-20.md` §4: de `casos-uso.md` solo autoriza l.705 | Se marcaron dos criterios de CU-07 y CU-24 con las comprobaciones manuales del desarrollador. El cambio es correcto en contenido, pero está fuera de las líneas autorizadas | Añadir esas dos líneas a la fila `docs/` de §4, con la misma excepción del hallazgo 1 |
| 4 | IMPORTANTE | alcance | `docs/arquitectura.md:1606` | `fase-20.md` §4: de `arquitectura.md` autoriza §8.5, §11.4, §13, §14.5 y l.1507 | La línea final "Siguiente: Fase 4" pasó a "Estado (Fase 20)". Lo justifica la nota de T6, pero no figura en §4 | Añadir la línea final a la fila `docs/` de §4 |
| 5 | MENOR | seguridad | `README.md:241`; `docs/arquitectura.md:717` (§8.5, paso 3) | `arquitectura.md` §11.4 ("nunca a través de Claude Code"), R-9 | El procedimiento de copia manda ejecutar `dotnet user-secrets list`, que muestra en claro las claves y la cadena de conexión. A diferencia del paso 1, no avisa de hacerlo fuera de una sesión de IA | Añadir al paso 3 y al README "en la terminal del desarrollador, nunca a través de una sesión de IA" |
| 6 | MENOR | arquitectura | `docs/arquitectura.md:1300` (§13.4) | `fase-20.md` T2 | El paréntesis sitúa `RenderizadorPlantillas` en `Correo/Plantillas/`, pero la clase está en `Correo/`; en `Plantillas/` solo están los `.razor` | Separar las dos ubicaciones |
| 7 | MENOR | arquitectura | `docs/motor-disponibilidad.md:57-66` | `fase-20.md` T4; `AGENTS.md` §10 | La tabla de consumidores omite que el área de empresa usa `MotorDisponibilidad.DiaLocalEnUtc` (`ConsultarReservasEmpresa`, `ConsultarPanelEmpresa`). §3 lo menciona sin nombrarlos | Añadir una nota con los dos casos de uso: usan el punto único de conversión, no `ObtenerHuecosAsync` |
| 8 | MENOR | arquitectura | `docs/arquitectura.md:1076` (§11.4, tabla "Secretos") | `fase-20.md` T3 (remitir a §8.5 en lugar de repetir) | La fila `.env` repite completo el aviso R-9, que ya es el paso 5 de §8.5. No hay contradicción, solo duplicación | Dejar "⚠ R-9: las mismas claves que User Secrets (§8.5, paso 5)" |

Origen: mecánica (Cnn) · compilación · pruebas · alcance · arquitectura · seguridad.

**Notas de los revisores (no verificables, no son hallazgos):**

- **Diff:** sin diff para los `*.md` ignorados; se leyeron enteros.
- **Recuento de 140 RN:** el revisor de arquitectura no lo pudo confirmar con sus herramientas. En esta revisión se confirmó con `awk` sobre la tabla de definiciones, antes de §12 Trazabilidad: 140 distintas y 29 ⚑.
- **Nombres de prueba:** los 38 nombres de prueba del motor y los 15 de concurrencia citados existen en `tests/`.
- **Código citado:** el SQL de `ex_reservas_sin_solape`, §14.5, `AnilloClaves` y las claves `Correo:*` coinciden con el código.

### Criterios de aceptación (§7 de la especificación)

- [x] Los nueve documentos de SPEC §18 existen, y `motor-disponibilidad.md` y `concurrencia.md` citan clases, métodos y pruebas que existen. Evidencia: los ficheros y la comprobación del revisor de arquitectura.
- [x] El README permite arrancar en local y en contenedores, configurar el correo y hacer la copia de las claves, y enlaza toda la documentación. Evidencia: README, secciones 3 a 6 y "Documentación". Matiz en el hallazgo 5.
- [x] La gestión de la clave tiene su ciclo de vida completo en un solo sitio. Evidencia: `arquitectura.md` §8.5 y las remisiones de §11.4 y `seguridad.md` §3. Duplicación menor en el hallazgo 8.
- [x] Ningún documento vigente da recuentos de RN, CU, riesgos o pruebas de arquitectura distintos de los reales. Evidencia: `Grep` sin resultados fuera de los contextos históricos de la Fase 4 y de los informes de fase.
- [x] Ninguna casilla de SPEC §21 queda sin marcar ni sin evidencia. Evidencia: `Grep` de `- [ ]`, cero.
- [x] `PROJECT_PLAN.md` §7 tiene fila para cada fase, de la 0 a la 20, más la tarea fuera de fase.
- [x] **Control 6:**
  - pruebas en verde (449/449 y 536/536);
  - `docker compose up` desde cero, comprobado por el desarrollador el 2026-10-25;
  - documentación técnica completa, salvo los hallazgos 2 y 6;
  - criterios de §21 marcados.

## Seguimiento 2026-10-25: corrección del bloqueante y los importantes

Corregidos a petición del desarrollador. No es una ronda nueva de `/revisar`.

| # | Corrección |
|---|---|
| 1 | Excepción al radio en `fase-20.md` §4, autorizada por el desarrollador el 2026-10-25 al pedir esta corrección. Nombra `fases/fase-16b.md` (solo la casilla del Control 4b de §12). Se mantiene la marca, porque refleja una comprobación que el desarrollador hizo de verdad |
| 2 | El ejemplo de `arquitectura.md` §13.5 usa ahora los nombres reales de `EnviarCorreosReservaConfirmada`: los parámetros `(consultas, plantillas, correo)` y los métodos `ReservaConfirmadaClienteAsync` y `ReservaConfirmadaNegocioAsync`. También se corrige la nota de T2 en `fase-20.md` §11. `Grep` de `CorreoClienteAsync` y `CorreoNegocioAsync` en `docs/`: ya no aparecen en ningún ejemplo vigente. Solo quedan en citas históricas (`fase-16b.md`, duda sobre §13.4 y §13.5), en la nota de T2 y en este informe |
| 3 | La misma excepción de §4 nombra `casos-uso.md` l.251 y l.544 |
| 4 | La misma excepción de §4 nombra la línea final de `arquitectura.md` |

Los menores 5 a 8 no se han tocado: no se pidió su corrección.

## Revisión 2026-10-25 19:02

**Veredicto:** APTO
**Especificación:** docs/fases/fase-20.md (APROBADA)
**Commit base:** 96f05dd
**Ficheros revisados:** 15. Son los mismos que en la ronda anterior; `.gitignore` excluye `*.md` y se revisaron en disco.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 4 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 449 | 449 | 0 |
| Integración | 536 | 536 | 0 |

- `dotnet build`: 0 avisos y 0 errores.
- `comprobaciones.sh`: 0 bloqueantes y 0 importantes.
- **Radio:** todos los ficheros tocados figuran en `fase-20.md` §4, incluida la excepción autorizada el 2026-10-25 para `fase-16b.md`, `casos-uso.md` l.251 y l.544 y la línea final de `arquitectura.md`.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `docs/motor-disponibilidad.md:57-66` | `fase-20.md` T4; `AGENTS.md` §10 | La tabla de consumidores solo cita `ConsultarDisponibilidad` y `CrearReserva`. No explica que el área de empresa (`ConsultarPanelEmpresa`, `ConsultarReservasEmpresa`) y `FormatoCorreo` usan solo los métodos de conversión `ALocal` y `DiaLocalEnUtc`, no el cálculo de huecos. Sigue abierto el menor 7 de la ronda anterior | Añadir esos consumidores y precisar qué se comparte con el área de empresa |
| 2 | MENOR | arquitectura | `README.md:145` | `arquitectura.md` §9.5; `AGENTS.md` §8 bis | Dice que el error "se traduce a un error de dominio". En realidad `UnidadTrabajo` lanza `HuecoOcupadoException` (`Aplicacion/Compartido`) y `CrearReserva` devuelve `Resultado.Fallo(HuecoYaNoDisponible)`, sin `DominioException`. Contradice a `concurrencia.md` §3 | "se traduce a un resultado `HuecoYaNoDisponible`" |
| 3 | MENOR | arquitectura | `docs/arquitectura.md:1141` (§12.1) | `AGENTS.md` §2 (coherencia entre documentos) | Dice que el motor "recibe repositorios dobles" y que tiene "13 pruebas". `motor-disponibilidad.md` describe dobles de `IConsultasDisponibilidad` y 14 casos con varias pruebas cada uno. §12.1 está fuera del radio de §4 | Anotarlo en `fase-20.md` §9 o ampliar el radio para corregir la frase |
| 4 | MENOR | arquitectura | `docs/concurrencia.md:67-69` | El propio documento (l.7): si discrepa del código, manda el código | El diagrama pone "desengancha" antes que "registra un aviso". `UnidadTrabajo.GuardarCambiosAsync` hace `RegistrarHuecoOcupado` y después `DesengancharReservasRechazadas` | Ordenar las dos líneas como en el código |

Origen: mecánica (Cnn) · compilación · pruebas · alcance · arquitectura · seguridad.

**Revisión de seguridad:** sin hallazgos.

- Sin secretos, solo marcadores.
- R-7 respetado.
- El ciclo de vida de las claves está en un solo sitio, con el aviso de no usar una sesión de IA.
- §14.5 coincide con `LimitacionFrecuencia.cs`.

**Menores de la ronda anterior:**

- **5:** el revisor de seguridad no lo repite.
- **6 y 8:** no se repiten en esta ronda.
- **7:** coincide con el 1 de esta.

### Criterios de aceptación (§7 de la especificación)

- [x] **Documentos de SPEC §18:** existen los nueve, y los dos nuevos citan nombres reales. Evidencia: el revisor de arquitectura comprobó 54 nombres de prueba y el SQL de `ex_reservas_sin_solape`.
- [x] **README:** permite arrancar en local y en contenedores, configurar el correo, copiar las claves y llegar a toda la documentación. Evidencia: sus secciones 3 a 6 y "Documentación".
- [x] **Gestión de la clave:** el ciclo de vida está en un solo sitio, `arquitectura.md` §8.5, con las remisiones de §11.4 y `seguridad.md` §3.
- [x] **Recuentos:** coinciden con los reales en los documentos vigentes. Evidencia: 140 RN, 29 ⚑, 24 CU y 5 pruebas de arquitectura, comprobados por el revisor.
- [x] **SPEC §21:** ninguna casilla sin marcar.
- [x] **`PROJECT_PLAN.md` §7:** tiene fila para todas las fases.
- [x] **Control 6:**
  - pruebas en verde;
  - `docker compose up` desde cero, por el desarrollador el 2026-10-25;
  - documentación completa;
  - §21 marcado.
