---
name: auditar-pruebas
description: Audita en profundidad si faltan pruebas en todo el codigo construido hasta una fase. Ejecuta las pruebas, inventaria clases y ficheros de prueba, lanza el subagente revisor-pruebas y escribe un informe con los casos que faltan, priorizados. No escribe pruebas. Solo al cerrar las fases 10, 11, 16 y 17, o cuando lo pida el desarrollador. Uso /auditar-pruebas 10.
disable-model-invocation: true
allowed-tools: Read Grep Glob Write
---

# Auditoría de pruebas

Auditas las pruebas de **todo el código construido hasta la fase $0**, no solo los cambios de esa fase. La pregunta de fondo es: **¿hay algún comportamiento del código que podría romperse sin que ninguna prueba se pusiera en rojo?**

**No escribes pruebas ni tocas código.** Produces un informe con los casos que faltan; el desarrollador decide cuáles se escriben y cuándo.

Esta auditoría no sustituye a `/revisar`: `/revisar` comprueba el catálogo de `AGENTS.md` §11 bis en cada fase. Aquí se busca lo que el catálogo no cubre.

---

## 0. Preparación

1. Si no se ha indicado fase, pregunta cuál y **para**.
2. Si la fase no es 10, 11, 16 ni 17, avisa al principio del informe de que es una auditoría fuera de calendario (`AGENTS.md` §11 bis) y sigue.
3. Lee `AGENTS.md` §11 bis (organización, catálogo y sabotajes) y la especificación `docs/fases/fase-NN.md` de la fase.

---

## 1. Ejecución

1. `dotnet test tests/PlataformaReservas.PruebasUnitarias`
2. `dotnet test tests/PlataformaReservas.PruebasIntegracion`. Necesita Docker. Si no está disponible, dilo en el informe: la auditoría sigue, pero sin confirmar que la suite de integración está en verde.

Anota ejecutadas, superadas, fallidas y omitidas por suite. Si alguna falla, la auditoría sigue, pero el informe lo dice en la primera línea.

---

## 2. Inventario

Construye dos listas con `Glob` y `Grep`:

1. **Clases con comportamiento** en `src/`: entidades y objetos de valor de `Dominio`, casos de uso y motor de `Aplicacion`, repositorios y servicios de `Infraestructura`. Excluye DTOs, comandos, configuraciones de EF, migraciones y componentes `.razor`.
2. **Ficheros de prueba** en `tests/`, emparejados con su clase según `AGENTS.md` §11 bis.

De ahí sale la primera tabla del informe: **clases sin fichero de pruebas**. Las clases que no necesitan prueba propia (por ejemplo, un caso de uso de solo lectura cubierto por la prueba de su repositorio) se listan igualmente, con el motivo.

---

## 3. Subagente

Lanza `revisor-pruebas`. Pásale exactamente:

- la fase;
- las fases ya cerradas, según `docs/PROJECT_PLAN.md` §7;
- las dos listas del inventario.

**No le pases tus conclusiones.** Incorpora sus hallazgos tal cual, con su severidad.

---

## 4. Informe

Escribe en `docs/fases/fase-NN-auditoria-pruebas.md`. **Si el fichero ya existe, añade una sección nueva al final**, con fecha y hora; nunca sobrescribas una auditoría anterior.

Estructura de cada sección:

```markdown
## Auditoría AAAA-MM-DD HH:MM

**Alcance:** código construido hasta la Fase NN
**Commit base:** <hash corto>

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|

| Importantes | Menores |
|---|---|

### Clases sin fichero de pruebas

| Clase | Motivo, si no necesita prueba propia |
|---|---|

### Casos que faltan

| # | Severidad | Clase o regla | Caso que falta | Por qué importa | Fichero donde iría |
|---|---|---|---|---|---|

### Pruebas débiles

| # | Prueba | Problema | Mejora sugerida |
|---|---|---|---|

### Propuesta

Los casos IMPORTANTES, agrupados por fichero, con una estimación en horas. Indica cuáles caben en la fase siguiente y cuáles se dejan para la Fase 17.
```

Severidades:

- **IMPORTANTE**: una RN implementada sin prueba de su tipo (columna Verificación), un caso del catálogo ausente sin motivo, un hallazgo corregido en una revisión anterior sin prueba que lo fije, o un comportamiento que puede romperse sin que nada falle.
- **MENOR**: casos adicionales que mejoran la cobertura sin cubrir un riesgo claro, y pruebas redundantes.

---

## 5. Al terminar

Muestra al desarrollador:

- el resultado de las suites;
- el recuento de importantes y menores;
- los importantes, uno por línea;
- la ruta del informe.

**No empieces a escribir pruebas**, aunque parezcan triviales.
