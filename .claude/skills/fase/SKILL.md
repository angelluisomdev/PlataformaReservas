---
name: fase
description: Genera la especificacion de ejecucion de una fase del proyecto (docs/fases/fase-NN.md) a partir del SPEC y de los documentos de docs/. Se invoca al empezar cada fase, antes de escribir codigo. Uso /fase 4, /fase 11b.
disable-model-invocation: true
allowed-tools: Read Grep Glob Write
---

# Especificación de fase

Vas a preparar la **fase $0** del proyecto.

**No escribes código.** Produces el documento que guiará la implementación y que el desarrollador revisará y aprobará antes de empezar. Ese documento es el contrato de la fase: todo lo que no esté en él, no se hace.

---

## 0. Comprobaciones previas

1. Si no se ha indicado fase, pregunta cuál y **para**.
2. La fase tiene que existir en la tabla de §19 de `docs/TFM_PlataformaReservas_SPEC.md`. Los identificadores válidos son del `0` al `20`, más `11b` y `16b`. Si no existe, dilo y **para**.
3. Nombre del fichero: `docs/fases/fase-NN.md`, con la parte numérica a dos dígitos y el sufijo si lo tiene. Ejemplos: `fase-04.md`, `fase-11b.md`, `fase-16b.md`.
4. Si el fichero ya existe, **no lo sobrescribas**: pregunta si se regenera o se mantiene, y espera la respuesta.

---

## 1. Lectura, en este orden

1. **`AGENTS.md`**, completo. Es la norma del proyecto: nada de lo que escribas puede contradecirlo.
2. **SPEC §19**: la fila de la fase $0 —contenido y horas— y el recorrido del presupuesto que la explica.
3. **`docs/PROJECT_PLAN.md`**: la semana en la que cae la fase (§2), el punto de control que cierra si cierra alguno (§3) y las tareas de verificación que le toquen (§6).
4. **Las secciones del SPEC** que la fase implementa.
5. **`docs/reglas-negocio.md`**: todas las RN que la fase implementa o de las que depende, incluidas las de la tabla de trazabilidad (§12).
6. **`docs/casos-uso.md`**: los CU afectados.
7. **`docs/modelo-dominio.md`** y **`docs/arquitectura.md`**: solo las secciones pertinentes. En `arquitectura.md`, revisa siempre la tabla de riesgos (§15).
8. **El estado real del repositorio**: `Glob` sobre `src/` y `tests/`, y las especificaciones de fases anteriores en `docs/fases/`.

Cuando baste un identificador, localízalo con `Grep` en lugar de leer el documento entero. El SPEC supera las 1 200 líneas: léelo por secciones.

---

## 2. Redacción

Escribe el fichero siguiendo **exactamente** la estructura de [plantilla.md](plantilla.md), que está en `${CLAUDE_SKILL_DIR}/plantilla.md`. No añadas ni quites secciones; si una no aplica, escribe "No aplica" y el motivo.

Reglas de redacción:

- **Cita, no parafrasees.** Todo requisito lleva su origen: `RN-xx`, `CU-xx`, `D-xx`, `A-xx`, `M-xx`, `E-xx`, `R-xx` o `SPEC §n`. Un requisito sin origen no entra.
- **No amplíes el alcance.** Si algo no está en los documentos, no existe. No añadas mejoras, ni "ya que estamos", ni tareas preparatorias de fases futuras.
- **No inventes nombres.** Clases, métodos, propiedades, tablas y rutas se toman de los documentos. Si falta alguno, va a Dudas.
- **Tareas pequeñas.** Cada tarea:
  - dura una hora o menos;
  - toca pocos ficheros, y los nombra todos;
  - se puede verificar por separado;
  - dice qué RN o CU implementa.
- **La suma de las tareas no supera el presupuesto de la fase.** Si no cabe, no recortes en silencio: explícalo en Dudas.
- **Radio de cambio explícito.** Qué proyectos y carpetas se tocan, y cuáles **no**.
- **Prerrequisitos comprobados.** Lo que tiene que existir ya en el repositorio, marcado como comprobado o como ausente. Si falta algo, va a Dudas.
- **Riesgos.** Todos los R-xx de `docs/arquitectura.md` §15 que apliquen a esta fase, con su mitigación concreta aquí.
- **Ficheros de pruebas según `AGENTS.md` §11 bis.** Cada tarea nombra el fichero de pruebas de cada clase que crea o modifica: `Pruebas<Clase>.cs`, uno por clase, en la misma carpeta relativa que la clase en `src/`, y uno por suite si la clase tiene pruebas unitarias y de integración. **Nunca** una tarea que cree un fichero con las pruebas de varias clases (`PruebasObjetosValor.cs`, `PruebasEntidades.cs`). Las pruebas transversales solo pueden ser las de la lista cerrada de §11 bis; una nueva va a Dudas.
- **§6 se construye, no se improvisa.** Lleva, como mínimo:
  - al menos una prueba por cada RN de §2, del tipo que indica su columna **Verificación** en `docs/reglas-negocio.md` (`U`, `I`, `C`; una `V` se cubre con una prueba del validador o del objeto de valor);
  - las pruebas de SPEC §16 que correspondan a la fase;
  - los casos del **catálogo de `AGENTS.md` §11 bis** para cada clase que la fase crea o modifica. Un caso que no aplique se anota con su motivo.
- **Sabotajes.** §6 incluye dos o tres sabotajes sobre las reglas ⚑ de la fase (`AGENTS.md` §11 bis), cada uno con la prueba que debe fallar.
- **Fases 10, 11, 16 y 17:** el cierre de §12 incluye `/auditar-pruebas`.
- **Fases 10 y 11: las pruebas son las primeras tareas**, antes que cualquier implementación (`CLAUDE.md`).
- **Sin código**, salvo firmas copiadas literalmente de los documentos.
- Castellano, con la nomenclatura de `AGENTS.md` §5.

---

## 3. Dudas

Si los documentos no resuelven algo, **no lo decidas tú**. Anótalo en la sección Dudas con:

- la pregunta;
- las opciones;
- tu recomendación y el motivo;
- qué tareas quedan bloqueadas hasta resolverla.

Estado del documento:

- con alguna duda abierta: **`BORRADOR`**;
- sin dudas: **`LISTA PARA REVISION`**.

Solo el desarrollador pasa un documento a **`APROBADA`**.

---

## 4. Al terminar

Responde en el chat, y solo esto:

- ruta del fichero generado y su estado;
- número de tareas y suma de horas frente al presupuesto;
- las dudas, si las hay, en una línea cada una;
- recordatorio: revisar y aprobar el documento, y arrancar después en **modo plan** con la tarea 1.

**No empieces a implementar**, aunque la fase parezca sencilla.
