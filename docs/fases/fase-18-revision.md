# Revisión de la Fase 18

## Revisión 2026-10-25 12:07

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-18.md (APROBADA)
**Commit base:** 9b8c084
**Ficheros revisados:** 5 versionados (`Dockerfile`, `.dockerignore`, `docker-compose.yml`, `src/PlataformaReservas.Web/Program.cs` y `README.md`), más la documentación que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 0 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 448 | 448 | 0 |
| Integración | 439 | 439 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.
- Integración ejecutada porque `PruebasIntegracion` referencia a `Web`, que ha cambiado.
- Revisión de seguridad: un menor y ningún hallazgo en secretos, configuración, errores ni concurrencia.
- Comprobación manual del desarrollador con su `.env` (2026-10-25), anotada en `fase-18.md` §11.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance; arquitectura (#1, como IMPORTANTE) | `README.md:157`; además `AGENTS.md` §8 ter, `TFM_PlataformaReservas_SPEC.md` §15.2, §15.3 y §16b-§17 (fuera de §21), `PROJECT_PLAN.md` §3, `reglas-negocio.md` (RN-176), `modelo-dominio.md` l.649, `arquitectura.md` §11.5, §12.2, §13.3, §14.2 y §15 (R-7), y `fase-04.md`, `fase-13.md`, `fase-13-revision.md`, `fase-16b.md`, `fase-16b-revision.md` y `fase-17.md` | AGENTS §3.2 y §13; `fase-18.md` §4 | La retirada de la CI (duda 1, decisión del desarrollador del 2026-10-25) eliminó sus referencias de toda la documentación y redefinió E-2. Pero §4 no se actualizó: sigue diciendo que el README "no se toca" (Fase 20), y su fila `docs/` solo autoriza `arquitectura.md` §2, §11.3 y §11.4, SPEC §21 y este fichero. La especificación contradice al diff. Igual que en la Fase 17, la corrección es documental: el cambio lo pidió el desarrollador y está anotado en la duda 1 y en §11 | En §4, añadir una **excepción al radio autorizada por el desarrollador el 2026-10-25 (duda 1)** que nombre `README.md` (solo la línea de E-2) y los documentos de la lista. Quitar el README de "no se tocan" |
| 2 | MENOR | seguridad | `docker-compose.yml:45` | `arquitectura.md` §11.4; `fase-18.md` duda 4 | El documento dice "solo HTTP en `localhost:${WEB_PUERTO:-8080}`", pero `"${WEB_PUERTO:-8080}:8080"` publica el puerto en todas las interfaces. La aplicación queda accesible por HTTP en claro desde la red local, con la *cookie* de sesión y los formularios de inicio de sesión | Atarlo al bucle local: `"127.0.0.1:${WEB_PUERTO:-8080}:8080"` |
| 3 | MENOR | arquitectura (#2) | `Dockerfile:4-9` | `fase-18.md` §5 T1 | T1 dice que se copia "la solución y los `.csproj` de `src/`", pero el `Dockerfile` restaura sobre el `.csproj` de `Web` sin copiar `PlataformaReservas.slnx`. Funciona, pero se aparta de T1 sin justificarlo | Anotar en §11 que se omite a propósito, porque solo se publica un proyecto; o copiar la solución |
| 4 | MENOR | arquitectura (#3) | `docs/arquitectura.md:50` | `fase-18.md` §11 | La línea de `.dockerignore` del árbol de §2 da una lista parcial: faltan `.vscode/`, `**/appsettings.*.local.json`, `.env.*` y `*.md` | Igualarla al fichero o dejarla genérica ("secretos, artefactos de compilación, documentación y pruebas") |

**Notas de los revisores (no verificables, no son hallazgos)**
- Ninguno de los dos revisores puede ejecutar `docker` ni `dotnet`. La construcción de la imagen, `docker compose config` y T5 constan en `fase-18.md` §11. Las suites, en esta revisión.
- Limitaciones ya declaradas: claves de Data Protection sin cifrar en `claves-web` y HTTPS fuera de alcance (duda 4). El puerto de `postgres` en todas las interfaces es de la Fase 5 y queda fuera de este radio.

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1205), `docker compose up` desde cero.** Evidencia: T5 en un proyecto aislado (200 en las públicas, 302 en las privadas, 8 categorías, `ex_reservas_sin_solape`) y la comprobación del desarrollador con su `.env`.
- [x] **Ningún secreto en el repositorio ni en la imagen.** Evidencia: `.dockerignore` excluye `.env`, `.env.*` y `appsettings.*.local.json`; la imagen no contiene `.env` (T5); las claves solo se interpolan desde `.env` con `:?`; la revisión de seguridad.
- [x] **En `Development` no cambia nada.** Evidencia: la condición añade `|| BaseDatos:MigrarAlArrancar`, sin declararla en `appsettings*.json`; las suites siguen en verde.
- [x] **Los sabotajes de §6 detectan el fallo.** Evidencia: `/businesses` 500 sin migración; `docker compose` se niega sin `CIFRADO_CLAVE_DATOS`.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 448/448 y 439/439).

## Seguimiento 2026-10-25: corrección de los hallazgos

Corregidos a petición del desarrollador con la corrección sugerida. No es una ronda nueva de `/revisar`.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (BLOQUEANTE) | README y documentos fuera del radio de §4 | Resuelto | `fase-18.md` §4 recoge una **excepción al radio autorizada por el desarrollador el 2026-10-25 (duda 1)** que nombra `README.md` (solo la línea de E-2) y cada documento tocado al retirar la integración continua. La lista de "no se tocan" ya no excluye la línea del README |
| 2 | Puerto de `web` publicado en todas las interfaces | Resuelto | `docker-compose.yml` y el YAML de `arquitectura.md` §11.4: `"127.0.0.1:${WEB_PUERTO:-8080}:8080"`. La viñeta de §11.4 lo explica. Anotado en `fase-18.md` §11 |
| 3 | El `Dockerfile` no copia la solución, como decía T1 | Resuelto | Anotado en `fase-18.md` §11: se publica un único proyecto, y la solución solo añadiría los de pruebas |
| 4 | Lista parcial de `.dockerignore` en el árbol de `arquitectura.md` §2 | Resuelto | Descripción genérica: "secretos, artefactos de compilación, documentación y pruebas" |

## Revisión 2026-10-25 12:12

**Veredicto:** APTO
**Especificación:** docs/fases/fase-18.md (APROBADA)
**Commit base:** 9b8c084
**Ficheros revisados:** 5 versionados (`Dockerfile`, `.dockerignore`, `docker-compose.yml`, `src/PlataformaReservas.Web/Program.cs` y `README.md`), más la documentación que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 0 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 448 | 448 | 0 |
| Integración | 439 | 439 | 0 |

- `dotnet build`: 0 avisos, 0 errores.
- `comprobaciones.sh`: 0 bloqueantes, 0 importantes.
- **Radio:** los 5 ficheros versionados y los documentos cambiados figuran en `fase-18.md` §4, incluida la excepción autorizada por el desarrollador el 2026-10-25 (duda 1, retirada de la integración continua). El cambio de `README.md` se limita a la línea de E-2.
- **Alcance:** todo corresponde a T1, T2, T3, T5, T6 o a la duda 1.
- **Revisiones de arquitectura y de seguridad:** sin hallazgos. Confirman los cuatro hallazgos de la ronda anterior resueltos:
  - el puerto en `127.0.0.1`;
  - la omisión de la solución en el `Dockerfile`, justificada en §11;
  - la descripción genérica de `.dockerignore`;
  - el radio de §4.

### Hallazgos

Ninguno.

**Notas de los revisores (no verificables, no son hallazgos)**
- Ninguno de los dos puede ejecutar `docker` ni `dotnet`. La imagen y T5 constan en `fase-18.md` §11; las suites, en esta revisión.
- La coherencia del resto de documentos tras retirar la integración continua no la revisan los subagentes (no se versionan). La búsqueda sin distinguir mayúsculas de la retirada no dejó referencias, salvo la duda 1 y la nota de T4 de `fase-18.md`.
- Limitaciones declaradas: claves de Data Protection sin cifrar en `claves-web` y HTTPS fuera de alcance (duda 4).

### Criterios de aceptación (§7 de la especificación)

- [x] **SPEC §21 (l.1205), `docker compose up` desde cero.** Evidencia: T5 en un proyecto aislado y la comprobación del desarrollador con su `.env` (2026-10-25).
- [x] **Ningún secreto en el repositorio ni en la imagen.** Evidencia: `.dockerignore`, la imagen sin `.env` (T5), las claves interpoladas desde `.env` con `:?` y la revisión de seguridad.
- [x] **En `Development` no cambia nada.** Evidencia: la condición de `Program.cs` y las suites en verde.
- [x] **Los sabotajes de §6 detectan el fallo.** Evidencia: `/businesses` 500 sin migración; `docker compose` se niega sin `CIFRADO_CLAVE_DATOS`.
- [x] **`dotnet build` y `dotnet test`.** Evidencia: esta revisión (0 avisos; 448/448 y 439/439).
