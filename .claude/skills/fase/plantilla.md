# Fase NN — <título tal como aparece en SPEC §19>

| | |
|---|---|
| **Estado** | BORRADOR · LISTA PARA REVISION · APROBADA |
| **Presupuesto** | X h (SPEC §19) |
| **Semana** | N (`PROJECT_PLAN.md` §2) |
| **Cierra** | Control N · ninguno |
| **Generada** | AAAA-MM-DD con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Una o dos frases. Qué existe al terminar que no existía antes.

## 2. Origen

Todo lo que esta fase implementa, con su referencia.

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §n | |
| D-xx · A-xx · M-xx · E-xx | |
| RN-xx | |
| CU-xx | |

## 3. Prerrequisitos

Lo que tiene que existir ya en el repositorio.

| Prerrequisito | Estado |
|---|---|
| | ✔ comprobado · ✘ ausente |

Si algún prerrequisito está ausente, la fase no puede empezar: va a §11.

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí · no | |
| `src/PlataformaReservas.Aplicacion` | sí · no | |
| `src/PlataformaReservas.Infraestructura` | sí · no | |
| `src/PlataformaReservas.Web` | sí · no | |
| `tests/PlataformaReservas.PruebasUnitarias` | sí · no | |
| `tests/PlataformaReservas.PruebasIntegracion` | sí · no | |
| `docs/` | sí · no | |

**Todo lo que no figure como "sí" no se toca en esta fase.**

## 5. Tareas

### T1 — <nombre> (≈ X h)

- **Ficheros:** crea `...` · modifica `...`
- **Implementa:** RN-xx, CU-xx
- **Hecho cuando:** criterio verificable, no una intención
- **Verificación:** comando o prueba concreta

### T2 — …

**Suma: X h de Y h presupuestadas.**

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| | U · I · C · A | `Carpeta/Pruebas<Clase>.cs` | RN-xx | Tn |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

Un fichero por clase probada y por suite (`AGENTS.md` §11 bis). La columna Fichero nunca repite el mismo nombre para pruebas de clases distintas.

Cada clase lleva los casos del catálogo de `AGENTS.md` §11 bis. Casos del catálogo que no aplican, con su motivo:

| Clase | Caso omitido | Motivo |
|---|---|---|
| | | |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-xx | | | pendiente |

## 7. Criterios de aceptación

- [ ] …

Si esta fase cierra un punto de control, sus comprobaciones van aquí, copiadas literalmente de `PROJECT_PLAN.md` §3.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-xx | | |

## 9. Fuera de alcance de esta fase

Lo que podría parecer parte de la fase y no lo es, con la fase donde sí se hace.

| Qué | Dónde se hace |
|---|---|
| | Fase N |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| | Tn | |

Toda regla de §2 aparece aquí. Una regla sin tarea o sin prueba es un hueco.

## 11. Dudas

"Ninguna" si no hay. Si hay, cada una con:

- **Pregunta:**
- **Opciones:**
- **Recomendación y motivo:**
- **Bloquea:** Tn, Tm

## 12. Cierre de la fase

- [ ] `dotnet build` sin avisos
- [ ] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [ ] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`, solo en las fases 10, 11, 16 y 17
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado, si esta fase cierra alguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase
