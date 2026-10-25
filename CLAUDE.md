# CLAUDE.md — PlataformaReservas

@AGENTS.md

Las instrucciones del proyecto están en `AGENTS.md`, importado arriba: es el fichero canónico y lo comparten todas las herramientas de IA que trabajan en este repositorio. **Léelo antes de tocar nada.**

Aquí solo va lo específico de Claude Code.

---

## Por qué las instrucciones son compartidas

En este repositorio pueden trabajar varias herramientas de IA (Claude Code, Codex, Copilot). Todas deben seguir las mismas reglas: si cada una tuviera las suyas, el código resultante sería incoherente.

Por eso todo lo sustantivo vive en `AGENTS.md`. **No se añaden aquí reglas de negocio, de arquitectura ni de estilo.** Si algo falta, se añade a `AGENTS.md`, donde lo ven todas.

---

## Antes de empezar cualquier tarea

Lee, en este orden:

1. `AGENTS.md` (importado arriba).
2. La sección del SPEC que cita la tarea.
3. Las reglas `RN-xx` que esa sección menciona.

No empieces a escribir código sin haber leído la regla concreta que vas a implementar. Las reglas están numeradas precisamente para poder citarlas.

---

## Radio de cambio

Cada tarea indica qué proyectos puedes tocar. Respétalo literalmente.

Si necesitas cambiar algo fuera de ese radio: **para, dilo y espera**. No lo hagas "de paso".

No reformatees, no reordenes `using`, no renombres variables ajenas a la tarea. Un diff limpio es parte del entregable: el ruido en los diffs dificulta la revisión.

---

## Pruebas primero en las fases 10 y 11

Motor de disponibilidad y reservas. En esas dos fases:

1. Escribe primero las pruebas que describe el SPEC §16.1.
2. Enséñamelas y espera confirmación.
3. Solo entonces implementa.

En el resto de fases, las pruebas van después de la implementación pero **antes** de dar la tarea por terminada.

---

## Planifica antes de cambios grandes

Si una tarea toca más de tres ficheros o implica una decisión de diseño:

1. Propón el plan.
2. Espera confirmación.
3. Implementa.

Para cambios pequeños y acotados, adelante directamente.

---

## Al terminar una tarea

Ejecuta siempre, y en este orden:

```bash
dotnet build      # sin avisos nuevos
dotnet test       # en verde
```

**No digas que algo funciona sin haber ejecutado las pruebas.** Si no has podido ejecutarlas, dilo explícitamente.

Después, resume en cuatro puntos:

- qué has hecho;
- qué decisiones has tomado y por qué;
- **qué has dejado fuera** a propósito;
- qué reglas `RN-xx` quedan cubiertas.

---

## Pruebas de arquitectura

En `PruebasUnitarias` hay cinco pruebas que comprueban la forma del código:

- ninguna entidad expone `set` o `init` público;
- ninguna entidad expone colecciones mutables;
- toda entidad tiene constructor privado sin parámetros;
- `Dominio` no referencia EF Core, Npgsql ni ASP.NET;
- ningún método público de una entidad recibe `string`, `int` ni `decimal`: recibe objetos de valor (AGENTS §7).

**Si alguna falla, el código está mal, no la prueba.** No las modifiques ni las omitas para que pase la suite. Si crees que una es incorrecta, dímelo y lo discutimos.

Existen porque el modelo anémico es la desviación más probable al generar código con IA.

---

## Recordatorios que se olvidan a menudo

- Castellano sin tildes ni eñes en identificadores. Rutas web en inglés.
- `DateTime.UtcNow`, nunca `DateTime.Now`.
- Filtrado por `EmpresaActualId` **en la consulta**, no después en memoria.
- `FranjaHoraria.SeSolapaCon` ya existe. No la reimplementes.
- Sin paquetes nuevos sin preguntar.
