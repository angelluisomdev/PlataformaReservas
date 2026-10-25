# Fase 13 — Buscador y ficha pública

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4 h (SPEC §19) |
| **Semana** | 4 (`PROJECT_PLAN.md` §2), junto a las fases 11b, 12 y 14 |
| **Cierra** | Ninguno. Contribuye al Control 4: "el visitante busca, encuentra y reserva" (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-17 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Dos páginas nuevas, servidas en SSR estático:

- `/businesses` permite buscar establecimientos activos y publicables por ciudad (obligatoria, sin distinguir mayúsculas ni acentos) y categoría (opcional). Los resultados salen paginados en el servidor, con un máximo de 20 por página.
- `/businesses/{slug}` muestra la ficha pública del establecimiento: datos, servicios y profesionales activos, horario resumido y botón de reserva. Si el establecimiento no existe, está inactivo o no es publicable, responde "no encontrado".

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §5.1 (l.192-194) | "Búsqueda de establecimientos por **ciudad (texto libre, obligatoria) y categoría (selector, opcional)**, con resultados paginados y tamaño de página fijado en servidor." "Ficha pública del establecimiento: nombre, categoría, descripción, dirección, teléfono, servicios, profesionales, horario resumido y botón de reserva." |
| SPEC §9.3 (l.392-401) | Activa es "condición necesaria, no suficiente". "**Publicación.** Una empresa solo es visible al público si está activa **y es publicable**" y la condición "**se calcula en cada consulta** con un `EXISTS`" |
| SPEC §10.3 (l.592-596) | `IConsultasCatalogoPublico`: no recibe `IContextoEmpresa`, solo lee y devuelve objetos de transferencia |
| SPEC §11 (l.616-620, l.635-638) | `/businesses` y `/businesses/{slug}` en SSR estático. "`/businesses` sin ciudad muestra el formulario, no un listado." "El botón de la ficha pública navega [a `/book/{slug}`]" |
| SPEC §14 (l.846) | `INDEX empresas (ciudad, categoria_id) -- consulta del buscador` |
| SPEC §15 (l.901), E-3 | Los datos de la empresa van en claro: `Ciudad` es el filtro del buscador. `Empresa.Descripcion` es texto libre del propietario |
| D-01 | SSR estático en las páginas públicas |
| `arquitectura.md` §4.1 (l.293-310) | `BuscarAsync(ciudad, categoria, pagina)` → `IReadOnlyList<ResumenEmpresaDto>`; `WHERE activa AND EXISTS(...) LIMIT 20` |
| `modelo-dominio.md` l.310, l.588 | "`Empresa` no conoce su condición de publicable"; es una **consulta de lectura** (`EXISTS` en `IConsultasCatalogoPublico`) |
| RN-04 | El catálogo público nunca aplica contexto de empresa |
| RN-10 | El `Slug` es la clave de `/businesses/{slug}` |
| RN-13, RN-134 | Una empresa inactiva no aparece en el buscador y su ficha no es accesible |
| RN-16 ⚑, RN-17, RN-18 | Publicable: al menos un camino servicio activo → profesional activo asociado → con horario. Si no es publicable, no aparece en la búsqueda y la ficha da "no encontrado". La condición se calcula en cada consulta |
| RN-24, RN-33 | Un servicio o un profesional inactivo no aparece en la ficha pública |
| RN-40, RN-115 | El horario es siempre por profesional. La ficha muestra la unión de los horarios de los profesionales activos |
| RN-109 | Solo empresas activas **y** publicables, en toda consulta del catálogo público |
| RN-110 ⚑ | Ciudad obligatoria, de 2 caracteres como mínimo tras recortar; se compara sin distinguir mayúsculas ni acentos |
| RN-111 | La categoría es opcional y refina el resultado dentro de la ciudad |
| RN-112 | Sin ciudad, o con la ciudad en blanco, el resultado es una lista vacía y el formulario. No es un error |
| RN-113 ⚑ | Página de 20 como máximo, fijada en el servidor. Si el cliente pide un tamaño mayor, se aplica el máximo |
| RN-114 | La ficha muestra únicamente servicios y profesionales activos |
| RN-116 | Una empresa inexistente, inactiva o no publicable da "no encontrado" |
| RN-164 | Los datos de la empresa no se cifran, para que `Ciudad` se pueda buscar |
| RN-171 ⚑ | `MarkupString` prohibido en la ficha pública. Todo texto de usuario se pinta codificado |
| CU-01 (l.109-150) | Buscar establecimientos: flujo, alternativos 2a, 2b, 3a, 3b y 4a, y criterios de aceptación |
| CU-02 (l.551) | Consultar ficha pública |
| CU-11 (l.341-342), CU-23 (l.483, l.485), CU-16 (l.385) | Criterios pendientes de la Fase 13: no aparece al darse de alta, aparece al completar la configuración, desaparece tras la baja y vuelve al reactivarse si es publicable |
| `fase-12.md`, dudas 3, 4, 5 y 9 | Contrato fijado: `city`, `category={slug}`. Recordar la ciudad y la condición de publicable pasan a esta fase |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `IConsultasCatalogoPublico`, con `ObtenerCategoriasAsync`, y su implementación `ConsultasCatalogoPublico` registrada como scoped | ✔ comprobado |
| `Empresa` con `Slug` único, `Ciudad`, `CategoriaId`, `Activa`, `Descripcion`, `Direccion` y `Telefono` (`Empresa.cs` l.19-45) | ✔ comprobado |
| Asociación servicio-profesional (`_servicios`, tabla `profesional_servicio`) y `Profesional.Horarios`, accesibles desde LINQ como en `ConsultasDisponibilidad` | ✔ comprobado |
| `IntervaloHorarioDto` y `CategoriaDto` con `Slug` | ✔ comprobado |
| La landing envía `city` y `category={slug}` a `businesses` (Fase 12) | ✔ comprobado |
| `EscenarioReserva.CrearAsync`, que crea una empresa publicable completa | ✔ comprobado |
| Comparación de ciudad sin acentos ni mayúsculas en la base de datos | ✘ ausente. `Ciudad` lleva conversor, y no hay `unaccent` ni colación no determinista (`base-datos.md` l.147). Duda 4 |
| Métodos de búsqueda y ficha, sus DTO y el tipo paginado | ✘ ausentes y sin nombre en los documentos. Duda 3 |
| Empresa de prueba con ciudad parametrizable | ✘ ausente. `DominioPrueba.Empresa` fija "Salamanca" y la base de datos de pruebas es compartida |
| Infraestructura de pruebas de páginas | ✘ ausente (`fase-12.md` l.175). Duda 10 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | La condición de publicable es una consulta, no una propiedad (`modelo-dominio.md` l.310) |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IConsultasCatalogoPublico.cs` (métodos de búsqueda y ficha). `Dtos/Empresas/` (DTO de resumen, de la ficha y de sus hijos, y el tipo paginado; duda 3). `CasosUso/Empresas/BuscarEstablecimientos.cs` si la duda 2 lo decide, más su registro en `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Consultas/ConsultasCatalogoPublico.cs`. `Persistencia/Configuraciones/ConfiguracionEmpresa.cs` (colación de `Ciudad`) y su migración (duda 4) |
| `src/PlataformaReservas.Web` | sí | `Components/Publico/` (las páginas del buscador y de la ficha con su `.razor.css`; duda 3). `Components/Pages/NotFound.razor` (duda 12) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Dtos/Empresas/` (una prueba por DTO nuevo). `CasosUso/Empresas/PruebasBuscarEstablecimientos.cs` y su doble `CatalogoPublicoFalso.cs` |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Consultas/PruebasConsultasCatalogoPublico.cs`. `CasosUso/Empresas/PruebasBuscarEstablecimientos.cs` (la cadena real del caso de uso, duda 2). `Infraestructura/DominioPrueba.cs` (ciudad y nombre parametrizables, sin cambiar los usos existentes) |
| `docs/` | sí | `casos-uso.md` (CU-01, CU-02, CU-11 y CU-23; dudas 9 y 15). `TFM_PlataformaReservas_SPEC.md` §10.3 l.594 (duda 9). `base-datos.md` l.147 (duda 15). `arquitectura.md` §4.1 (duda 3). `PROJECT_PLAN.md` l.244 (duda 1). Este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:
- `Program.cs`: la limitación de frecuencia es de la Fase 19 (duda 14);
- `/book/{slug}` ni el motor de disponibilidad (Fase 14);
- el panel de la empresa (RN-19, Fase 16);
- el sembrador de datos (duda 13);
- ningún paquete.

## 5. Tareas

### T1 — Búsqueda de establecimientos (≈ 1,25 h)

- **Ficheros:**
  - modifica `Aplicacion/Abstracciones/IConsultasCatalogoPublico.cs`;
  - crea los DTO de resumen y de página en `Aplicacion/Dtos/Empresas/` (duda 3);
  - crea `Aplicacion/CasosUso/Empresas/BuscarEstablecimientos.cs`, si la duda 2 lo decide;
  - modifica `Infraestructura/Consultas/ConsultasCatalogoPublico.cs`;
  - modifica `tests/PlataformaReservas.PruebasIntegracion/Infraestructura/DominioPrueba.cs`;
  - modifica `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs`;
  - crea las pruebas unitarias de los DTO y del caso de uso (§6).
- **Implementa:** RN-04, RN-13, RN-16 ⚑, RN-18, RN-109, RN-110 ⚑, RN-111, RN-112, RN-113 ⚑, RN-164 y CU-01, pasos 3 y 4 y alternativos 2b, 3a y 4a.
- **Contenido:**
  - **Ciudad:** se recorta y, si tiene menos de 2 caracteres, el resultado es una lista vacía (RN-110, RN-112).
  - **Filtros en la base de datos:**
    - ciudad, con la comparación que fije la duda 4 y la coincidencia que fije la duda 5;
    - categoría por slug, opcional (RN-111);
    - `Activa` (RN-13);
    - publicable con `EXISTS`: servicio activo de la empresa → profesional activo de la empresa que lo presta → con algún horario (RN-16, RN-18).
  - **Orden:** el de la duda 7.
  - **Tamaño de página:** como máximo 20, fijado en el servidor (RN-113).
  - **Total:** la consulta devuelve también el total de resultados (CU-01 paso 4).
  - **Sin contexto de empresa:** no recibe `IContextoEmpresa` (RN-04).
- **Hecho cuando:** las pruebas de T1 de §6 están en verde.
- **Verificación:** `dotnet test`.

### T2 — Comparación de ciudad sin mayúsculas ni acentos (≈ 0,5 h)

- **Ficheros:**
  - modifica `Infraestructura/Persistencia/Configuraciones/ConfiguracionEmpresa.cs`;
  - crea una migración en `Infraestructura/Persistencia/Migraciones/`;
  - modifica `docs/base-datos.md` (l.147 y la tabla de índices).
- **Implementa:** RN-110 ⚑ (la comparación sin mayúsculas ni acentos) y CU-01 3b.
- **Contenido:** el mecanismo que fije la duda 4, aplicado en la base de datos para que el total y la paginación sean correctos.
- **Hecho cuando:**
  - "malaga" y "MÁLAGA" encuentran "Málaga";
  - `dotnet ef migrations has-pending-model-changes` responde "No changes";
  - la prueba de esquema existente sigue en verde.
- **Verificación:** `dotnet test`.

### T3 — Ficha pública en la consulta (≈ 0,75 h)

- **Ficheros:**
  - modifica `IConsultasCatalogoPublico.cs` y `ConsultasCatalogoPublico.cs`;
  - crea los DTO de la ficha en `Aplicacion/Dtos/Empresas/` (duda 3);
  - pruebas en `PruebasConsultasCatalogoPublico.cs` y en las pruebas unitarias de los DTO.
- **Implementa:** RN-10, RN-13, RN-16 ⚑, RN-17, RN-24, RN-33, RN-40, RN-114, RN-115, RN-116, CU-02, y los criterios de CU-11 (l.341-342) y CU-23 (l.483, l.485).
- **Contenido:**
  - Busca la empresa por slug. Si no existe, está inactiva o no es publicable, devuelve `null` (RN-116).
  - Datos de la empresa: nombre, categoría, descripción, dirección y teléfono.
  - Servicios activos.
  - Profesionales activos, solo con el nombre (duda 8).
  - Horario resumido: la unión por día de los horarios de los profesionales activos (RN-115; formato en la duda 8).
- **Hecho cuando:** las pruebas de T3 de §6 están en verde, incluida la de publicación.
- **Verificación:** `dotnet test`.

### T4 — Página del buscador (≈ 0,75 h)

- **Ficheros:** crea `Web/Components/Publico/<buscador>.razor` y su `.razor.css` (duda 3).
- **Implementa:** CU-01 (pasos 1 a 5 y alternativos 2a, 3a y 4a), RN-112, D-01 y SPEC §11 l.620.
- **Contenido:**
  - `@page "/businesses"`, sin `@rendermode`;
  - lee `city`, `category` y `page` de la query (duda 6);
  - formulario GET con la ciudad y el selector de categorías (`ObtenerCategoriasAsync`), con la categoría recibida preseleccionada;
  - sin ciudad válida: solo el formulario y el aviso de que la ciudad es obligatoria (CU-01 2a, RN-112);
  - con resultados: nombre, categoría, ciudad y descripción breve, cada uno enlazado a `/businesses/{slug}`, más el total y la paginación;
  - sin resultados: lo indica y, si había categoría, sugiere quitarla (CU-01 3a);
  - la página no contiene ninguna regla: la consulta (o el caso de uso) decide (AGENTS §6);
  - CSS aislado, sin estilos en línea ni recursos externos (R-12).
- **Hecho cuando:** la verificación manual de §6 está marcada para el buscador.
- **Verificación:** manual (§6).

### T5 — Página de la ficha (≈ 0,5 h)

- **Ficheros:**
  - crea `Web/Components/Publico/<ficha>.razor` y su `.razor.css` (duda 3);
  - modifica `Web/Components/Pages/NotFound.razor` (duda 12).
- **Implementa:** CU-02, RN-116, RN-171 ⚑, D-01 y SPEC §11 l.638.
- **Contenido:**
  - `@page "/businesses/{slug}"`, sin `@rendermode`;
  - si la consulta devuelve `null`, "no encontrado" (duda 12);
  - muestra los datos, los servicios, los profesionales y el horario resumido;
  - el botón "Reservar" enlaza a `/book/{slug}`, que es de la Fase 14;
  - **ningún `MarkupString`**: la descripción se pinta con `@` (RN-171);
  - CSS aislado.
- **Hecho cuando:**
  - la verificación manual de §6 está marcada para la ficha;
  - `grep -rn MarkupString src/PlataformaReservas.Web/Components/Publico` no da resultados.
- **Verificación:** manual (§6) y el `grep`.

### T6 — Sabotajes, verificación manual y documentación (≈ 0,25 h)

- **Ficheros:** `docs/casos-uso.md`, `docs/TFM_PlataformaReservas_SPEC.md` §10.3, `docs/arquitectura.md` §4.1, `docs/PROJECT_PLAN.md` l.244 (según las dudas 1, 3, 9 y 15) y este fichero.
- **Implementa:** AGENTS §11 bis (sabotajes).
- **Hecho cuando:**
  - los tres sabotajes de §6 han fallado su prueba y el código está restaurado;
  - la verificación manual está marcada;
  - los documentos están corregidos.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 4 h de 4 h presupuestadas.** No queda margen. Si hay desviación, el recorte lo fija la duda 1.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| Sin ciudad, con ciudad en blanco o de 1 carácter: lista vacía y total 0 | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-110 ⚑, RN-112 | T1 |
| "Ea" (2 caracteres) ejecuta la búsqueda y encuentra la empresa de "Ea" | I | ídem | RN-110 ⚑ | T1 |
| Solo devuelve empresas de esa ciudad | I | ídem | RN-110 ⚑ | T1 |
| La categoría refina el resultado dentro de la ciudad | I | ídem | RN-111 | T1 |
| Una empresa inactiva no aparece; la misma, activa, sí (RN-134) | I | ídem | RN-13, RN-109 | T1 |
| Publicación: recién creada no aparece en la búsqueda ni por slug; al completar servicio, profesional, asociación y horario aparece; al desactivar el único profesional desaparece | I | ídem | RN-16 ⚑, RN-17, RN-18, CU-11 | T1, T3 |
| Un servicio activo sin profesional asociado con horario no hace publicable a la empresa | I | ídem | RN-16 ⚑ | T1 |
| Con 25 empresas en la ciudad: la página 1 trae 20 y la 2 trae 5; el total es 25; pedir un tamaño de 100 devuelve 20 | I | ídem | RN-113 ⚑ | T1 |
| Los resultados salen en el orden de la duda 7 | I | ídem | CU-01 paso 4 | T1 |
| La búsqueda funciona sin contexto de empresa, desde un ámbito sin `IContextoEmpresa` | I | ídem | RN-04 | T1 |
| "malaga" y "MÁLAGA" encuentran "Málaga" | I | ídem | RN-110 ⚑, CU-01 3b | T2 |
| La ficha de una empresa publicable trae sus datos, solo los servicios y profesionales activos, y profesionales sin email ni teléfono | I | ídem | RN-114, RN-24, RN-33, CU-02 | T3 |
| La ficha une el horario de dos profesionales activos por día (solapados y contiguos se funden) y excluye el de un profesional inactivo | I | ídem | RN-115, RN-40 | T3 |
| La ficha de un slug inexistente, de una empresa inactiva o de una no publicable devuelve `null` | I | ídem | RN-116, RN-13, RN-17 | T3 |
| Tras la baja la empresa no aparece ni por búsqueda ni por slug; al reactivarla vuelve si es publicable | I | ídem | CU-23 (l.483, l.485), RN-134 | T3 |
| La descripción de la ficha conserva el texto tal cual, incluido `<script>`, sin sanearlo ni marcarlo como HTML | I | ídem | RN-171 ⚑ (la mitad de datos; la de presentación, en la verificación manual) | T3 |
| La empresa se lee en claro y la búsqueda por ciudad funciona | I | ídem | RN-164 | T1 |
| `ConvertirADto` de cada DTO nuevo copia sus campos | U | `Dtos/Empresas/Pruebas<Dto>.cs`, uno por DTO | AGENTS §8 bis | T1, T3 |
| Una página fuera de rango (la 3 de 25 y `int.MaxValue`) devuelve una lista vacía con el total real, sin desbordar el desplazamiento | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | duda 6, CU-01 2b | T1 (`/revisar 13`, hallazgos 2 y 4) |
| Una categoría inactiva no filtra resultados: devuelve una lista vacía | I | ídem | duda 6, RN-111 | T1 (`/revisar 13`, hallazgo 4) |
| Una ciudad de más de 80 caracteres no es válida y no llega al puerto | U | `CasosUso/Empresas/PruebasBuscarEstablecimientos.cs` | RN-110 ⚑ (la regla vive solo en el caso de uso) | T1 (`/revisar 13`, hallazgo 1) |
| Caso de uso de búsqueda: recorta la ciudad, descarta las de menos de 2 caracteres sin llamar al puerto, limita el tamaño a 20 y normaliza la página | U | `CasosUso/Empresas/PruebasBuscarEstablecimientos.cs` (solo si la duda 2 decide b) | RN-110 ⚑, RN-112, RN-113 ⚑ | T1 |

**Verificación manual.** Hecha el 2026-10-17 con la aplicación en el puerto 5199, `curl` y una empresa de "Málaga Pruebafase" creada con SQL y borrada al terminar. Blazor codifica en el HTML los caracteres no ASCII de los datos (`á` sale como `&#xE1;`), y el navegador los muestra bien. Con la aplicación en marcha y `curl`, como en la Fase 12. Los datos los crea un script desechable que se borra al terminar (duda 13).
- [x] `/businesses` sin `city` muestra solo el formulario (RN-112).
- [x] `/businesses?city=E` no lista nada y avisa de que la ciudad es obligatoria (CU-01 2a).
- [x] `/businesses?city=malaga&category=<slug>` lista la empresa de "Málaga" con nombre, categoría, ciudad y descripción breve, el total y la paginación.
- [x] `?pageSize=100` no muestra más de 20 resultados.
- [x] La categoría llega preseleccionada desde el enlace de la landing.
- [x] `/businesses/<slug>` muestra la ficha. Una descripción con `<script>alert(1)</script>` aparece como `&lt;script&gt;` en el HTML (RN-171).
- [x] `/businesses/<slug-inexistente>` responde "no encontrado".
- [x] El botón "Reservar" enlaza a `/book/<slug>`.
- [x] Ninguna de las dos páginas tiene marcadores interactivos (`<!--Blazor:`), estilos en línea ni recursos externos.

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `ConsultasCatalogoPublico` | "Filtra por empresa en la consulta" y "no encuentra lo de otra empresa" | Es una consulta pública: por diseño no filtra por empresa (RN-04, SPEC §10.3). La prueba "sin contexto de empresa" cubre lo que aplica |
| `ConsultasCatalogoPublico` | Lectura sin seguimiento | El contexto es `NoTracking` por defecto y la consulta proyecta a DTO |
| DTO nuevos | Invariantes y validación | Un DTO no valida: copia valores ya validados por sus objetos de valor |
| Páginas de buscador y ficha | Todos | No hay infraestructura de pruebas de páginas (duda 10). Se cubren con la verificación manual |
| `BuscarEstablecimientos` | "Recurso inexistente y de otra empresa (`NoEncontrado`)", "fallo a mitad sin dejar nada" y "eventos encolados" | Es una consulta de solo lectura: no tiene recurso propio de una empresa (RN-04, el catálogo es público), ni transacción, ni eventos. Su único desenlace es `Exito` con una página, vacía si la entrada no es válida (RN-112). `/revisar 13`, hallazgo 3 |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-16 | Quitar del `EXISTS` la condición de que el profesional tenga horario | La de publicación | ✔ 2026-10-17: falló `Sin_un_camino_completo_servicio_profesional_horario_no_es_publicable` (el profesional asociado sin horario pasaba a publicar la empresa). Código restaurado |
| RN-110 | Aceptar ciudades de 1 carácter | "Sin ciudad, con ciudad en blanco o de 1 carácter: lista vacía" | ✔ 2026-10-17: `LongitudMinimaCiudad = 1` en el caso de uso. Fallaron `Sin_ciudad_o_con_menos_de_dos_caracteres_devuelve_vacio_sin_consultar` ("E" y " E ") y `EsCiudadValida_exige_dos_caracteres_tras_recortar` en unitarias, y `Sin_ciudad_o_con_un_solo_caracter_no_devuelve_nada_aunque_haya_empresas` ("E") en integración. Código restaurado |
| RN-113 | Respetar el tamaño de página que pide el cliente | "Con 25 empresas… pedir un tamaño de 100 devuelve 20" | ✔ 2026-10-17: sin el `Math.Min` del caso de uso, falló `El_tamano_de_pagina_nunca_supera_el_maximo_del_servidor` (100 y 21) en unitarias. La de integración sigue en verde porque la consulta mantiene el tope como defensa (§11). Código restaurado |

## 7. Criterios de aceptación

- [x] CU-01: sin ciudad, o con un solo carácter, no se devuelve ningún resultado, ni desde el formulario ni manipulando la petición.
- [x] CU-01: buscar `"Ea"` ejecuta la búsqueda.
- [x] CU-01: buscar por una ciudad existente devuelve solo empresas de esa ciudad, sin distinguir mayúsculas ni acentos.
- [x] CU-01: añadir una categoría refina el resultado dentro de la ciudad.
- [x] CU-01: una empresa inactiva o no publicable nunca aparece en los resultados.
- [x] CU-01: una ciudad con muchas empresas devuelve como máximo 20 por página, aunque se pidan más.
- [x] CU-01: la búsqueda funciona sin autenticación.
- [x] CU-02: la ficha muestra nombre, categoría, descripción, dirección, teléfono, servicios y profesionales activos, horario resumido y botón de reserva. Si la empresa no existe, está inactiva o no es publicable, "no encontrado".
- [x] CU-11: recién dada de alta, la empresa no aparece en la búsqueda ni por slug directo; al completar servicio, profesional, asociación y horario, aparece sin más pasos.
- [x] CU-23: tras la baja, la empresa no aparece en la búsqueda ni por slug; al reactivarla vuelve si es publicable.
- [x] RN-171: una descripción con `<script>` no se ejecuta en la ficha pública. No hay `MarkupString` en `Components/Publico`.
- [x] Las dos páginas son SSR estático, sin `@rendermode`, sin estilos en línea ni recursos externos.
- [x] Sin paquetes nuevos.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde.

Esta fase no cierra ningún punto de control.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-12 (CSP incompatible con Blazor Server) | Estilos en línea o recursos externos en el buscador o en la ficha romperían las páginas cuando entre la CSP de la Fase 19 | CSS aislado en `.razor.css`, sin `style=""`, sin `<style>` y sin URL externas. Se comprueba en la verificación manual |
| R-1 (ámbito del `DbContext`) | No aplica en la práctica: en SSR estático el ámbito es la petición. La página del buscador hace dos consultas seguidas (categorías y búsqueda), nunca en paralelo | Sin `@rendermode` y sin `Task.WhenAll` sobre consultas (AGENTS §13) |
| R-14 (limitación de frecuencia en memoria) | Todavía no aplica: la limitación de `/businesses` es de la Fase 19 | Anotado en la duda 14 |
| R-10 (LINQ sobre un campo protegido) | No aplica: los datos de `empresas` van en claro (RN-164). Los profesionales no se exponen con datos personales | La ficha no muestra el email ni el teléfono de los profesionales (duda 8) |

El resto de riesgos no aplica: la fase no toca fechas UTC, la restricción `EXCLUDE`, Identity, eventos, correo ni cifrado.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `/book/{slug}`, el flujo de reserva, la disponibilidad real en la ficha y la precondición de publicable en CU-03 | Fase 14 |
| La limitación de frecuencia en `/businesses` (RN-173 ⚑, E-5), las cabeceras y la CSP (RN-175, E-6) | Fase 19 (duda 14) |
| La revisión de E-3 de las plantillas de correo | Fases 16b y 19 |
| El aviso de "aún no visible" en el panel (RN-19, CU-18) | Fase 16 |
| Las empresas de demostración sembradas (SPEC l.873) | Fase 17 (duda 13) |
| Recordar la última ciudad usada (CU-01 l.150) | Trabajo futuro (duda 11) |
| Búsqueda parcial, por código postal, por provincia o por radio | Fuera del MVP (SPEC l.236 y l.247; duda 5) |
| Las incoherencias de documentos que no tocan la búsqueda | Fase 20 (duda 15) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-04 | T1 | "La búsqueda funciona sin contexto de empresa" |
| RN-10 | T3 | "La ficha de una empresa publicable…" (búsqueda por slug) |
| RN-13, RN-134 | T1, T3 | "Una empresa inactiva no aparece…"; ficha `null` si está inactiva |
| RN-16 ⚑, RN-17, RN-18 | T1, T3 | Publicación; "Un servicio activo sin profesional…"; sabotaje 1 |
| RN-24, RN-33, RN-114 | T3 | "La ficha… solo servicios y profesionales activos" |
| RN-40, RN-115 | T3 | "La ficha une el horario…" |
| RN-109 | T1 | Inactiva y no publicable no aparecen |
| RN-110 ⚑ | T1, T2 | 1 carácter, "Ea", la ciudad y la comparación sin acentos; sabotaje 2 |
| RN-111 | T1 | "La categoría refina…" |
| RN-112 | T1, T4 | "Sin ciudad… lista vacía"; verificación manual |
| RN-113 ⚑ | T1 | "Con 25 empresas…"; sabotaje 3 |
| RN-116 | T3, T5 | "La ficha de un slug inexistente…"; verificación manual |
| RN-164 | T1 | "La empresa se lee en claro…" |
| RN-171 ⚑ | T3, T5 | "La descripción… conserva el texto"; `grep MarkupString`; verificación manual |
| CU-01 | T1, T2, T4 | Las de T1 y T2 y la verificación manual del buscador |
| CU-02 | T3, T5 | Las de T3 y la verificación manual de la ficha |
| CU-11 (l.341-342) | T1, T3 | Publicación |
| CU-23 (l.483, l.485) | T3 | "Tras la baja…" |
| CU-16 (l.385) | — | Parcial: "los huecos aparecen en CU-03" depende de la Fase 14. Aquí solo se comprueba que el horario hace publicable a la empresa (prueba de publicación) |

## 11. Dudas

### Duda 1 — El recorte previsto contradice RN-110 ⚑

- **Pregunta:**
  - `PROJECT_PLAN.md` l.244 fija el recorte de la fase como "buscador: solo listado por categoria".
  - Un listado por categoría sin ciudad es justo el volcado del catálogo que prohíben RN-110 ⚑ y RN-112, y las reglas ⚑ "no se relajan bajo ninguna circunstancia" (`reglas-negocio.md` l.14).
  - ¿Qué se recorta si la fase se desvía?
- **Opciones:**
  - (a) Cambiar el recorte por "sin refinamiento visual y sin sugerencia de quitar la categoría (CU-01 3a)", y corregir l.244;
  - (b) mantener el recorte del plan.
- **Recomendación y motivo:** (a). (b) incumple una regla ⚑. El recorte de (a) no toca ninguna RN.
- **Bloquea:** ninguna tarea. Solo afecta al recorte, y a la corrección en T6.
- **Resolución** Opción a.

### Duda 2 — Dónde viven RN-110, RN-112 y RN-113

- **Pregunta:** recortar la ciudad, descartarla si tiene menos de 2 caracteres y limitar el tamaño a 20 son reglas. ¿En qué capa van?
- **Opciones:**
  - (a) en la implementación de `ConsultasCatalogoPublico`, en `Infraestructura`;
  - (b) en un caso de uso nuevo, `BuscarEstablecimientos`, en `Aplicacion/CasosUso/Empresas/`, que normaliza la entrada y llama al puerto. La página inyecta el caso de uso;
  - (c) en la página.
- **Recomendación y motivo:** (b). Las reglas quedan en `Aplicacion`, que es su sitio (AGENTS §6 y §8 bis), y se prueban de forma unitaria con un puerto falso. (a) mete reglas de negocio en `Infraestructura`, y (c) en `Web`, que AGENTS §6 prohíbe. La consulta sigue aplicando el tope de 20 por defensa en profundidad.
- **Bloquea:** T1 y T4.
- **Resolución** Opción b.

### Duda 3 — Nombres y firmas

- **Pregunta:** los documentos solo citan `BuscarAsync(ciudad, categoria, pagina)` y `ResumenEmpresaDto` (`arquitectura.md` l.301-304), sin tipos ni campos. No hay nombre para el método de la ficha, sus DTO, el tipo paginado ni las páginas. ¿Qué nombres se usan?
- **Opciones:**
  - (a) Propuesta:
    - `BuscarAsync(string ciudad, string? categoriaSlug, int pagina, int tamanoPagina, CancellationToken ct)` → `PaginaResultados<ResumenEmpresaDto>`, con `Elementos`, `Total`, `Pagina` y `TamanoPagina`;
    - `ResumenEmpresaDto(string Nombre, string Slug, string Categoria, string Ciudad, string? DescripcionBreve)`;
    - `ObtenerFichaAsync(string slug, CancellationToken ct)` → `FichaEmpresaDto?`, con `ServicioFichaDto`, `ProfesionalFichaDto` y `HorarioDiaDto(DayOfWeek Dia, IReadOnlyList<IntervaloHorarioDto> Intervalos)`;
    - páginas `Components/Publico/Buscador.razor` y `Components/Publico/FichaEstablecimiento.razor`;
    - caso de uso `BuscarEstablecimientos` con `BuscarEstablecimientosComando(string? Ciudad, string? Categoria, int? Pagina, int? TamanoPagina)`.
  - (b) Otros nombres que decida el desarrollador.
- **Recomendación y motivo:** (a). Respeta los dos nombres ya documentados, sigue la convención de DTO con `ConvertirADto` en `Dtos/Empresas/` y usa "establecimiento" en lo que ve el visitante (SPEC §3.2). Se anota en `arquitectura.md` §4.1.
- **Bloquea:** T1, T3, T4 y T5.
- **Resolución** Opción a.

### Duda 4 — Comparación de ciudad sin mayúsculas ni acentos en la base de datos

- **Pregunta:**
  - RN-110 exige comparar sin mayúsculas ni acentos.
  - Para que el total y la paginación sean correctos, la comparación tiene que hacerse en PostgreSQL.
  - `Ciudad` lleva conversor (`EF.Functions.ILike` no aplica, `base-datos.md` l.147), y no hay `unaccent` ni colación.
  - ¿Con qué mecanismo se hace?
- **Opciones:**
  - (a) una colación ICU no determinista (`und-u-ks-level1`: ignora mayúsculas y acentos) creada en una migración y aplicada a `empresas.ciudad` con `UseCollation`. La igualdad LINQ con el conversor sigue funcionando y el índice `(ciudad, categoria_id)` se mantiene;
  - (b) la extensión `unaccent` con `lower(unaccent(ciudad)) = lower(unaccent(@ciudad))` en SQL a mano;
  - (c) una columna normalizada que se mantiene desde el dominio.
- **Recomendación y motivo:** (a). No necesita SQL a mano en las consultas, no añade columnas ni cambia el dominio, y usa ICU, que ya trae la imagen de PostgreSQL. (b) obliga a SQL crudo y su índice no sirve. (c) amplía el modelo (M-05).
- **Bloquea:** T1 y T2.
- **Resolución** Opción a.

### Duda 5 — Coincidencia exacta o parcial de la ciudad

- **Pregunta:** RN-110 dice "por texto libre, sin distinguir mayúsculas ni acentos", pero no si la coincidencia es exacta o parcial. `base-datos.md` l.147 habla de búsqueda "parcial (`ILIKE`)".
- **Opciones:**
  - (a) Exacta, salvo mayúsculas, acentos y espacios de los extremos;
  - (b) parcial: la ciudad contiene el texto.
- **Recomendación y motivo:** (a). Con (b), "Ea" encontraría "Leganés", lo que contradice la justificación de RN-110 (l.211). Además, las colaciones no deterministas no admiten `LIKE`.
- **Bloquea:** T1 y T2.
- **Resolución** Opción a.

### Duda 6 — Parámetros de la query y casos límite

- **Pregunta:** solo están fijados `city` y `category={slug}` (Fase 12). RN-113 presupone que el cliente puede pedir un tamaño. ¿Cómo se llaman la página y el tamaño, y qué pasa en los casos límite?
- **Opciones:**
  - (a) Propuesta:
    - `page`, que empieza en 1; un valor ausente o menor que 1 se toma como 1;
    - `pageSize`, que se acepta pero se limita a 20; un valor ausente o menor que 1 se toma como 20;
    - una página fuera de rango devuelve una lista vacía con el total real;
    - un `category` con un slug inexistente o inactivo devuelve una lista vacía.
  - (b) Sin `pageSize`, con tamaño fijo de 20.
- **Recomendación y motivo:** (a). Permite probar RN-113 ("tamaño forzado desde cliente → se aplica el máximo", `reglas-negocio.md` l.374) y no revela nada.
- **Bloquea:** T1 y T4.
- **Resolución** Opción a.

### Duda 7 — Orden de los resultados

- **Pregunta:** ningún documento fija el orden, y sin un orden estable la paginación repite o salta empresas.
- **Opciones:**
  - (a) por nombre y, en caso de empate, por slug, que es único;
  - (b) por fecha de alta.
- **Recomendación y motivo:** (a). Es estable, predecible para el visitante y no favorece a nadie.
- **Bloquea:** T1.
- **Resolución** Opción a.

### Duda 8 — Contenido de la ficha y del listado

- **Pregunta:** CU-02 enumera los bloques de la ficha, pero no qué datos lleva cada uno. RN-115 no fija el formato del "horario resumido", y CU-01 tampoco fija qué es la "descripción breve".
- **Opciones:**
  - (a) Propuesta:
    - servicios con nombre, descripción, duración y precio;
    - profesionales **solo con el nombre**;
    - horario por día de la semana, de lunes a domingo, con la unión de intervalos de todos los profesionales activos, fundiendo los que se solapan o son contiguos; un día sin intervalos sale como "cerrado";
    - descripción breve: los primeros 160 caracteres de la descripción, cortados en el servidor;
  - (b) el horario por profesional, sin unir.
- **Recomendación y motivo:** (a). RN-115 pide la unión. No exponer el email ni el teléfono de los profesionales evita filtrar datos personales que CU-02 no pide.
- **Bloquea:** T3, T4 y T5.
- **Resolución** Opción a.

### Duda 9 — Los documentos olvidan la condición de publicable

- **Pregunta:** CU-01 (l.120 "empresas activas", l.138 y l.146), CU-02 (l.551 "Slug inexistente o empresa inactiva") y SPEC §10.3 (l.594 "Solo empresas con `Activa = true`") exigen solo "activa", mientras que SPEC §9.3, RN-109 y RN-116 exigen activa **y** publicable.
- **Opciones:**
  - (a) Aplicar RN-109 y RN-116, que son más estrictas y más recientes, y corregir CU-01, CU-02 y SPEC §10.3 en T6;
  - (b) aplicar solo "activa".
- **Recomendación y motivo:** (a). Con (b) aparecerían fichas sin ningún servicio reservable, en contra de RN-16 ⚑.
- **Bloquea:** T1 y T3.
- **Resolución** Opción a.

### Duda 10 — Cómo se prueba RN-171 en la ficha

- **Pregunta:**
  - La trazabilidad (`reglas-negocio.md` l.385) pide una prueba de integración: `<script>` aparece **escapado** en la ficha pública.
  - No hay infraestructura de pruebas de páginas, y SPEC §8 l.330 excluye las pruebas por HTTP.
- **Opciones:**
  - (a) Una prueba de integración de que la consulta devuelve la descripción tal cual, más el `grep` de `MarkupString` y la verificación manual con `curl`. La prueba automática de la página queda para la revisión de E-3 de la Fase 19;
  - (b) añadir bUnit, que es un paquete nuevo y exige autorización (AGENTS §4).
- **Recomendación y motivo:** (a). No añade paquetes. El riesgo real es `MarkupString` (`reglas-negocio.md` l.353), y lo vigila el `grep`.
- **Bloquea:** T5 y T6.
- **Resolución** Opción a.

### Duda 11 — Recordar la última ciudad

- **Pregunta:** CU-01 l.150 dice que "conviene" que el campo recuerde la última ciudad usada. La Fase 12 lo dejó para esta (su duda 5). Exige una cookie o JavaScript, y SPEC l.265 pide JavaScript solo si es estrictamente necesario.
- **Opciones:**
  - (a) No se hace; se anota como trabajo futuro;
  - (b) una cookie que escribe el buscador y lee la landing.
- **Recomendación y motivo:** (a). No es una RN. Con 0 h de margen, (b) desplaza trabajo obligatorio.
- **Bloquea:** ninguna.
- **Resolución** Opción a.

### Duda 12 — "No encontrado" en la ficha

- **Pregunta:** `NotFound.razor` sigue con el texto en inglés de la plantilla. ¿Cómo responde la ficha a un slug no visible?
- **Opciones:**
  - (a) La ficha pinta el mismo bloque que `NotFound` y responde con el código 404, y se traducen las dos cadenas de `NotFound.razor`;
  - (b) un mensaje propio dentro de la ficha, con código 200.
- **Recomendación y motivo:** (a). "No encontrado" debe ser un 404 real, para que no se distinga de una ruta inexistente (AGENTS §8, "no se revela que existe"). Y la traducción es coherente con la Fase 12, duda 7.
- **Bloquea:** T5.
- **Resolución** Opción a.

### Duda 13 — Datos de demostración para la verificación manual

- **Pregunta:** SPEC l.873 prevé 2 empresas de demostración completas, que hoy no existen. Sin ellas no hay ninguna empresa publicable en la base de datos de desarrollo para la verificación manual.
- **Opciones:**
  - (a) Fuera de la fase. La verificación manual crea una empresa publicable con un script desechable que la borra al terminar, como el usuario de prueba de la Fase 12;
  - (b) sembrar ya las 2 empresas de demostración.
- **Recomendación y motivo:** (a). (b) toca el sembrador, Identity y el cifrado, y no cabe en 4 h. Se deja para la fase de datos de demostración (17).
- **Bloquea:** T6 (verificación manual).
- **Resolución** Opción a.

### Duda 14 — Limitación de frecuencia en `/businesses` y en la ficha

- **Pregunta:** RN-173 ⚑ pide 60 peticiones por minuto en el buscador. La Fase 12 la dejó para la Fase 19 (E-5). Además, `arquitectura.md` l.1334 solo nombra `/businesses`. ¿Entra también `/businesses/{slug}` en la política `catalogo`?
- **Opciones:**
  - (a) Todo queda para la Fase 19, y allí la política `catalogo` cubre `/businesses` y `/businesses/{slug}`;
  - (b) implementarla ya en esta fase.
- **Recomendación y motivo:** (a). E-5 es de la Fase 19 (SPEC l.934) y se prueba con las demás medidas. Incluir la ficha evita extraer el catálogo recorriendo slugs. Se anota en `arquitectura.md` §14.5 cuando llegue la Fase 19.
- **Bloquea:** ninguna.
- **Resolución** Opción a.

### Duda 15 — Incoherencias de documentos encontradas en la lectura

- **Pregunta:** ¿qué se corrige en esta fase? Lo encontrado:
  - `base-datos.md` l.147 dice "Pendiente para la Fase 9" sobre la búsqueda por ciudad;
  - `modelo-dominio.md` l.631 dice que los objetos de valor se aplanan con `ComplexProperty`, pero `Ciudad` usa un conversor;
  - RN-16 cita §5.1 y la definición está en SPEC §9.3;
  - CU-11 (l.341-342) y las fases 07, 08 y 09 dicen "fases 12 y 13";
  - CU-03 (l.158) solo exige "activa" (afecta a la Fase 14).
- **Opciones:**
  - (a) Corregir solo lo que toca a la búsqueda y la ficha (`base-datos.md` l.147, y CU-11 y CU-23 al marcar sus criterios) y anotar el resto para la Fase 20;
  - (b) corregirlo todo ahora.
- **Recomendación y motivo:** (a). Respeta el radio y el presupuesto. CU-03 se resuelve en la especificación de la Fase 14.
- **Bloquea:** ninguna (T6 aplica las correcciones).
- **Resolución** Opción b.

### Decisiones de implementación

- **`HorarioDiaDto` usa el objeto de valor `IntervaloHorario`**, no `IntervaloHorarioDto` como proponía la duda 3. Ese DTO lleva el `Id` de un horario concreto, que no existe en un intervalo fundido. La unión usa `IntervaloHorario.SeSolapaCon` más la contigüidad (fin = inicio), sin reimplementar el solapamiento (AGENTS §3.7). `ReservaOcupadaDto` ya trabaja con objetos de valor de tiempo.
- **`PaginaResultados<T>` no tiene fábrica estática.** El analizador CA1000 prohíbe miembros estáticos en tipos genéricos, así que la página vacía se construye con `new` en el caso de uso y en la consulta.
- **Cada regla vive en un solo sitio** (A-09, `/revisar 13`, hallazgo 1). RN-110, RN-112 y RN-113 viven solo en `BuscarEstablecimientos` (duda 2): ciudad recortada de 2 a 80 caracteres (la máxima es la de `Ciudad`, para que el puerto nunca reciba una ciudad imposible), página desde 1 y tamaño de 20 como máximo. La consulta solo mantiene el `Math.Clamp` del tamaño, como autoriza la duda 2. Por eso el sabotaje de RN-113 en el caso de uso solo lo detecta la prueba unitaria.
- **Página fuera de rango sin desbordamiento** (`/revisar 13`, hallazgo 2). La consulta calcula el desplazamiento en `long` después del `CountAsync`. Si no queda nada que saltar, devuelve una página vacía con el total real, en lugar de un `OFFSET` negativo cuando `page` es enorme. Comprobado con un sabotaje: sin la protección, `page = int.MaxValue` hace fallar `Una_pagina_fuera_de_rango_devuelve_vacio_con_el_total_real`.
- **Orden de la ficha con cultura fija** (`/revisar 13`, hallazgo 6). Servicios y profesionales se ordenan con un comparador `es-ES`, no con la cultura del proceso, para que el orden no cambie entre la máquina local y Docker.
- **La colación se crea en la migración con SQL a mano** (`CREATE COLLATION IF NOT EXISTS ciudad_sin_acentos …`), como el `btree_gist` de la migración inicial, sin tocar `ContextoDatos`, que no está en §4. `ConfiguracionEmpresa` aplica `UseCollation` a `Ciudad`.
- **El slug de la ficha tiene que ser exacto.** `ObtenerFichaAsync` solo acepta textos que ya son un slug canónico: con mayúsculas o mal formado devuelve `null` (404), sin normalizar la entrada. Esa validación vive en `Infraestructura` porque la ficha llama directamente al puerto (`arquitectura.md` §4.1). **Si la ficha llega a tener caso de uso, se mueve a él** (`/revisar 13`, hallazgo 7).
- **"No encontrado" es un 404 real** con `NavigationManager.NotFound()`, que pinta `NotFound.razor` (traducida) a través de `NotFoundPage`.
- **El nombre de la categoría del listado** se toma de las categorías activas que ya se cargan para resolver el slug. Así se evita una consulta por empresa.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo en las fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado: no aplica, esta fase no cierra ninguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6: ninguna es de esta fase
