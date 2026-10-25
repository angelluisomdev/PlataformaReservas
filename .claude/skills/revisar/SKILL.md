---
name: revisar
description: Revisa los cambios pendientes de commit al cerrar una tarea o una fase. Ejecuta comprobaciones mecanicas, compilacion y pruebas, contrasta con la especificacion de fase y lanza los subagentes revisor-arquitectura y revisor-seguridad. Escribe el informe en docs/fases/. No corrige nada. Uso /revisar 4, /revisar 11b.
disable-model-invocation: true
allowed-tools: Read Grep Glob Write
---

# Revisión de cambios

Revisas los cambios pendientes de commit de la **fase $0**.

**No corriges nada.** Produces un informe; las correcciones las decide el desarrollador después. Si durante la revisión ves cómo arreglar algo, lo escribes como corrección sugerida, no lo haces.

---

## 0. Preparación

1. Si no se ha indicado fase, pregunta cuál y **para**.
2. Localiza la especificación: `docs/fases/fase-NN.md`, con la parte numérica a dos dígitos y el sufijo si lo tiene (`fase-04.md`, `fase-11b.md`).
   - Si no existe, o no está en estado `APROBADA`, avísalo al principio del informe. Se puede revisar, pero sin contrato aprobado no se puede comprobar ni el radio de cambio ni el alcance, y el veredicto nunca puede ser mejor que `APTO CON RESERVAS`.
3. Si `git status --porcelain` no muestra cambios, dilo y **para**.

---

## 1. Comprobaciones mecánicas

Ejecuta exactamente:

```bash
bash .claude/skills/revisar/comprobaciones.sh
```

Cada línea es `SEVERIDAD|ID|fichero:línea|descripción`. Van al informe **tal cual**: no las reinterpretes ni las rebajes de severidad.

Si una es un falso positivo evidente —por ejemplo, C14 sobre una palabra dentro de una cadena de varias líneas que el filtro no supo quitar—, la mantienes en el informe marcada como **falso positivo** y explicas por qué. No la borras: así se ve qué comprobaciones conviene afinar.

---

## 2. Compilación y pruebas

1. `dotnet build`. Cualquier aviso o error es **BLOQUEANTE** (`TreatWarningsAsErrors`).
2. `dotnet test tests/PlataformaReservas.PruebasUnitarias`. Cualquier prueba fallida u omitida es **BLOQUEANTE**.
3. `dotnet test tests/PlataformaReservas.PruebasIntegracion` **solo si** los cambios tocan `Infraestructura` o `PruebasIntegracion`, o la fase cierra un punto de control. Necesita Docker. Si no está disponible, dilo en el informe: **una prueba no ejecutada no es una prueba superada**.

Anota cuántas pruebas se ejecutaron, cuántas pasaron y cuántas fallaron en cada suite.

---

## 3. Radio de cambio y alcance

Contrasta cada fichero cambiado con §4 (radio de cambio) y §5 (tareas) de la especificación:

| Situación | Severidad |
|---|---|
| Fichero fuera del radio de cambio | **BLOQUEANTE** |
| Algo implementado que no corresponde a ninguna tarea | **BLOQUEANTE**: ampliación de alcance |
| Tarea de §5 sin hacer o a medias | **IMPORTANTE**, salvo que el desarrollador indique que revisa una tarea intermedia |
| Criterio de aceptación de §7 sin evidencia | **IMPORTANTE** |

Quedan fuera de esta comprobación los propios ficheros del proceso: `docs/fases/fase-NN-revision.md`.

---

## 4. Subagentes

Los subagentes están en la carpeta `.claude/agents`. Cada uno tiene su propia descripción y reglas. Lee primero sus instrucciones antes de invocarlos.

Lanza **en paralelo** `revisor-arquitectura` y `revisor-seguridad`. A cada uno pásale exactamente:

- la fase;
- la ruta de su especificación;
- la lista de ficheros cambiados.

**No les pases tus conclusiones de los pasos anteriores.** Tienen que revisar sin sesgo; si coinciden contigo, eso vale más que si repiten lo que les dijiste.

Incorpora sus hallazgos tal cual, con su severidad. Si dos hallazgos señalan lo mismo, únelos en una fila citando ambos orígenes.

---

## 5. Informe

Escribe en `docs/fases/fase-NN-revision.md`.

**Si el fichero ya existe, añade una sección nueva al final**, con fecha y hora. **Nunca sobrescribas una revisión anterior**: el historial de rondas muestra qué se encontró en cada una y cómo se resolvió.

Cada sección tiene esta estructura:

```markdown
## Revisión AAAA-MM-DD HH:MM

**Veredicto:** APTO · APTO CON RESERVAS · NO APTO
**Especificación:** docs/fases/fase-NN.md (estado)
**Commit base:** <hash corto o "sin commits">
**Ficheros revisados:** n

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | | | |
| Integración | | | | (o "no ejecutada: motivo")

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|

Origen: mecánica (Cnn) · compilación · pruebas · alcance · arquitectura · seguridad.

### Criterios de aceptación (§7 de la especificación)

- [x] criterio con evidencia (cuál)
- [ ] criterio sin evidencia (por qué)
```

**Veredicto:**

- **NO APTO**: hay al menos un bloqueante.
- **APTO CON RESERVAS**: no hay bloqueantes, pero hay importantes, o la especificación no está aprobada.
- **APTO**: ni bloqueantes ni importantes.

**Severidades**, las mismas que usan los subagentes:

- **BLOQUEANTE**: viola `AGENTS.md` §3 o §13, una regla ⚑, una decisión D-xx, A-xx o M-xx, abre una vía de acceso entre empresas, rompe la compilación o las pruebas, o se sale del radio de cambio.
- **IMPORTANTE**: se aparta de un documento sin romper ninguna de las anteriores.
- **MENOR**: claridad, legibilidad, duplicación leve.

---

## 6. Al terminar

En el chat, y solo esto:

- el veredicto;
- el recuento: bloqueantes, importantes, menores;
- cada bloqueante, en una línea;
- la ruta del informe.

**No empieces a corregir**, aunque la corrección parezca trivial. Si el veredicto es `APTO`, recuerda que el siguiente paso es el commit.
