# Fase 12 — Landing pública SSR

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 4 h (SPEC §19) |
| **Semana** | 4 (`PROJECT_PLAN.md` §2), junto a las fases 11b, 13 y 14 |
| **Cierra** | Ninguno. Contribuye al Control 4: "el visitante busca, encuentra y reserva" (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-17 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Que la ruta `/` sirva, en SSR estático, la landing pública de la plataforma en castellano. Tendrá estas secciones:
- hero con la propuesta de valor;
- cómo funciona;
- las categorías leídas del catálogo público, cada una enlazada al buscador con la categoría preseleccionada;
- un buscador por ciudad y categoría;
- preguntas frecuentes;
- las dos llamadas a la acción;
- el pie de página.

Sustituye a la página "Hello, world!" de la plantilla.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §5.1 (l.191) | "Landing única con: hero, propuesta de valor, cómo funciona, categorías, preguntas frecuentes, llamada a la acción para clientes, llamada a la acción para empresas y pie de página." |
| SPEC §5.4 (l.234) | "Páginas 'cómo funciona', 'preguntas frecuentes' y 'contacto'": convertidas en secciones de la landing |
| SPEC §7 (l.263-265) | SSR estático en la landing. "JavaScript solo si es estrictamente necesario." |
| SPEC §11 (l.615, l.620, l.625-627) | `/` es la landing. "Las categorías de la landing enlazan [a `/businesses`] con la categoría preseleccionada y la ciudad pendiente de indicar." Destinos de las llamadas a la acción: `/register` y `/for-business/register` |
| SPEC §3.3 (l.98), D-12 | Rutas en inglés; textos de página en castellano (l.610). La interfaz dice "establecimiento" (l.89-94) |
| D-01 | SSR estático en las páginas públicas |
| SPEC §15.2 E-6, `arquitectura.md` §14.6 | La CSP futura (`style-src 'self'`, `img-src 'self' data:`) condiciona el CSS y las imágenes desde ahora |
| `arquitectura.md` §2.3 (l.185-187) | `Components/Publico/`: "landing, buscador, ficha" |
| `arquitectura.md` §4.1 (l.306-308) | Página SSR: sin circuito; el ámbito es la petición. `IConsultasCatalogoPublico` no recibe `IContextoEmpresa` |
| `arquitectura.md` l.279, AGENTS §6 | Lógica mínima en `.razor`; `Web` sin reglas de negocio |
| RN-04 | Las consultas del catálogo público no aplican contexto de empresa |
| RN-122 | Las categorías se cargan por datos iniciales y no se mantienen desde la aplicación |
| CU-01 (l.114, l.150) | Disparador: "el visitante usa el buscador de la landing". Las categorías de la portada enlazan a `/businesses` con la categoría preseleccionada |
| CU-11 (l.305) | Disparador: la llamada a la acción para negocios desde la landing |
| CU-05, CU-06 (l.553-554) | La portada es el destino tras iniciar y cerrar sesión |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `IConsultasCatalogoPublico.ObtenerCategoriasAsync`, que devuelve las activas ordenadas por nombre y no recibe contexto (`Infraestructura/Consultas/ConsultasCatalogoPublico.cs`) | ✔ comprobado |
| Las 8 categorías de SPEC §9.2 sembradas en desarrollo (`SembradorDatos.cs`) | ✔ comprobado |
| Las rutas `/register`, `/login` y `/for-business/register` (`Cuenta/Pages/`) | ✔ comprobado |
| `MainLayout` como layout por defecto, y `FocusOnNavigate` sobre `h1` (`Routes.razor`) | ✔ comprobado |
| Sin render mode global en `App.razor`: las páginas sin `@rendermode` se sirven en SSR estático | ✔ comprobado |
| `Slug` en `CategoriaDto` | ✘ ausente. Se añade en T2 (duda 3) |
| La ruta `/businesses` | ✘ ausente, de la Fase 13. La landing solo construye el enlace y el formulario. Hasta la 13, el destino responde "no encontrado" |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | no | |
| `src/PlataformaReservas.Aplicacion` | sí | `Dtos/Categorias/CategoriaDto.cs`: añadir `Slug` y copiarlo en `ConvertirADto` (duda 3) |
| `src/PlataformaReservas.Infraestructura` | no | `ConsultasCatalogoPublico` ya proyecta con `CategoriaDto.ConvertirADto` |
| `src/PlataformaReservas.Web` | sí | Crear `Components/Publico/Landing.razor` y `Components/Publico/Landing.razor.css`. Borrar `Components/Pages/Home.razor`. En `Components/App.razor`, `lang="es"`. En `Components/Layout/MainLayout.razor`, traducir el texto de `blazor-error-ui` (duda 7). Ajustar `wwwroot/app.css` si hace falta una base común (T4) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Dtos/Categorias/PruebasCategoriaDto.cs` (nuevo) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Consultas/PruebasConsultasCatalogoPublico.cs` |
| `Directory.Packages.props` (raíz) | sí, excepción | Fija la versión transitiva de `System.Security.Cryptography.Pkcs` en 10.0.13 con `CentralPackageTransitivePinningEnabled`. Es una corrección de seguridad que ordenó el desarrollador al detectar la vulnerabilidad (E-2, CVE-2026-70162 y CVE-2026-84008), ajena a las tareas de la fase. Ver §11 |
| `docs/` | sí | `casos-uso.md` §5.1 l.592: `/` deja de ser "contenido estático" (duda 2, duda 10). Este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:
- `Program.cs`, porque la landing no necesita registro nuevo;
- `Routes.razor`;
- `Cuenta/`;
- las páginas de `Components/App/`;
- `IConsultasCatalogoPublico`, porque no se añade ningún método;
- ningún paquete nuevo; `Directory.Packages.props` solo cambia por la excepción de la tabla;
- la CSP ni las cabeceras, que son de la Fase 19.

## 5. Tareas

### T1 — Página de la landing con sus secciones estáticas (≈ 1,25 h)

- **Ficheros:**
  - crea `src/PlataformaReservas.Web/Components/Publico/Landing.razor`;
  - borra `src/PlataformaReservas.Web/Components/Pages/Home.razor`;
  - modifica `src/PlataformaReservas.Web/Components/App.razor` (`lang="es"`).
- **Implementa:** SPEC §5.1 (l.191), §11 (l.615 y l.625-627), D-01, CU-11 (disparador), CU-05 y CU-06 (destino `/`).
- **Contenido:**
  - `@page "/"` sin `@rendermode`, para que se sirva en SSR estático;
  - `PageTitle` en castellano y un único `h1`;
  - las secciones, en este orden:
    - hero con la propuesta de valor;
    - cómo funciona;
    - preguntas frecuentes;
    - llamada a la acción para clientes (`/register`);
    - llamada a la acción para empresas (`/for-business/register`);
    - pie de página, con la línea de contacto en texto (duda 1).
  - Las categorías y el buscador se añaden en T2 y T3.
  - Textos estáticos sin ninguna lógica. Se usa "establecimiento", nunca "empresa" (SPEC l.89-94).
- **Hecho cuando:** `/` muestra la landing en castellano, `<html lang="es">`, ya no existe `Home.razor` y no hay dos componentes con `@page "/"`.
- **Verificación:**
  - `dotnet build` sin avisos;
  - verificación manual (§6): la página se carga sin abrir el WebSocket del circuito, y las dos llamadas a la acción llegan a sus formularios.

### T2 — Categorías desde el catálogo público, con enlace al buscador (≈ 1 h)

- **Ficheros:**
  - modifica `src/PlataformaReservas.Aplicacion/Dtos/Categorias/CategoriaDto.cs` (añade `string Slug`, que `ConvertirADto` copia de `categoria.Slug.Valor`);
  - modifica `Components/Publico/Landing.razor`;
  - crea `tests/PlataformaReservas.PruebasUnitarias/Dtos/Categorias/PruebasCategoriaDto.cs`;
  - modifica `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs`.
  - Las llamadas existentes que construyen `CategoriaDto` a mano se ajustan a la nueva firma. Hoy solo lo hace `PruebasConsultasCatalogoPublico`.
- **Implementa:** RN-04, RN-122, SPEC §11 (l.620), CU-01 (l.150).
- **Contenido:**
  - en `OnInitializedAsync`, la página inyecta `IConsultasCatalogoPublico` y llama a `ObtenerCategoriasAsync`, sin ninguna lógica adicional (duda 2);
  - cada categoría es un enlace a `/businesses?category={slug}`, con la ciudad vacía (duda 3).
- **Hecho cuando:**
  - la landing lista las 8 categorías sembradas, ordenadas por nombre, cada una con su enlace por slug;
  - el DTO lleva `Slug`;
  - las pruebas de §6 de esta tarea están en verde.
- **Verificación:** `dotnet test`, y comprobación manual de los enlaces en el HTML servido.

### T3 — Buscador de la landing (≈ 0,5 h)

- **Ficheros:** modifica `Components/Publico/Landing.razor`.
- **Implementa:** CU-01 (disparador, l.114). Es el contrato con la Fase 13 (SPEC §11 l.616).
- **Contenido:**
  - un formulario HTML `method="get"` con `action="/businesses"`;
  - un campo de texto `city` para la ciudad (el nombre sigue el ejemplo de `arquitectura.md` l.298) y un selector `category`, con una opción "Todas" vacía y una opción por categoría cuyo valor es el slug;
  - sin validación en la página: RN-110 y RN-112 se aplican en el servidor de `/businesses`, en la Fase 13;
  - sin `EditForm` ni antifalsificación, porque es una petición GET que no modifica nada (duda 4).
- **Hecho cuando:** al enviar el formulario se navega a `/businesses?city=<texto>&category=<slug o vacío>`.
- **Verificación:** manual (§6).

### T4 — Estilos compatibles con la CSP futura (≈ 0,75 h)

- **Ficheros:**
  - crea `Components/Publico/Landing.razor.css`;
  - modifica `Components/Layout/MainLayout.razor` (texto de `blazor-error-ui` en castellano, duda 7);
  - modifica `wwwroot/app.css` solo para una base tipográfica común, si hace falta.
- **Implementa:** E-6 y `arquitectura.md` §14.6 de forma preventiva (R-12); SPEC l.610.
- **Contenido:**
  - CSS propio y aislado por componente;
  - sin atributos `style=""` ni bloques `<style>` en línea;
  - sin fuentes, imágenes ni hojas externas (CDN), sin framework CSS y sin paquetes;
  - diseño legible en el móvil.
- **Hecho cuando:** `grep -E 'style="|<style|https?://' Components/Publico/Landing.razor` no da resultados y la página se ve con los estilos aplicados.
- **Recorte:** si la fase se desvía, esta tarea se reduce a lo mínimo (PROJECT_PLAN l.245: "landing: sin refinamiento visual, -1 h").
- **Verificación:** el `grep` anterior y la revisión visual.

### T5 — Sabotajes, verificación manual y documentación (≈ 0,5 h)

- **Ficheros:**
  - modifica `docs/casos-uso.md` (§5.1 l.592: `/` pasa a "contenido estático y categorías de `IConsultasCatalogoPublico`");
  - modifica este fichero (§6, sabotajes y verificación manual; §11).
- **Implementa:** AGENTS §11 bis (sabotajes); duda 10.
- **Hecho cuando:**
  - los dos sabotajes de §6 han fallado su prueba y el código está restaurado;
  - la lista de verificación manual está marcada;
  - `casos-uso.md` está corregido.
- **Verificación:** `dotnet build` y `dotnet test`.

**Suma: 4 h de 4 h presupuestadas.**

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `ConvertirADto` copia `Id`, `Nombre` (el valor) y `Slug` (el valor) de la categoría | U | `Dtos/Categorias/PruebasCategoriaDto.cs` | SPEC §11 l.620 (contrato del enlace) | T2 |
| Devuelve las categorías activas ordenadas por nombre, con su slug y sin contexto de empresa (la prueba existente, ampliada al `Slug`) | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-04, RN-122 | T2 |
| Una categoría inactiva no se devuelve (se desactiva con SQL directo, porque `Categoria` no tiene método para desactivarla, RN-122) | I | `Consultas/PruebasConsultasCatalogoPublico.cs` | RN-122, SPEC §10.3 | T2 |

**Verificación manual de la página.** El 2026-10-17 se comprobó con la aplicación arrancada en un puerto aparte y `curl` sobre `/`: `lang="es"`, un único `h1`, las secciones, los 8 enlaces `businesses?category=<slug>` en orden, el formulario GET con `city` y `category`, los dos enlaces de las llamadas a la acción y ningún marcador de componente interactivo en el HTML. Quedan sin marcar las comprobaciones que exigen un navegador: la pestaña de red sin WebSocket y la vuelta a `/` tras iniciar y cerrar sesión. No hay infraestructura de pruebas de páginas: ni bUnit ni `WebApplicationFactory`, y SPEC §8 l.330 excluye las pruebas por HTTP. Se anota en T5:
- [x] `/` carga en SSR estático. En la pestaña de red no aparece `_blazor` ni un WebSocket al abrirla. Comprobado el 2026-10-17 sin navegador: la página no lleva `@rendermode` y el HTML servido no tiene ningún marcador `<!--Blazor:`, tampoco con la sesión iniciada; sin componentes interactivos, `blazor.web.js` no abre el circuito.
- [x] `<html lang="es">`, `PageTitle` en castellano y un único `h1`.
- [x] Aparecen las secciones de T1, en orden.
- [x] Aparecen las 8 categorías, y cada enlace apunta a `/businesses?category=<slug>`.
- [x] El formulario navega a `/businesses?city=…&category=…`.
- [x] Las llamadas a la acción llevan a `/register` y `/for-business/register`.
- [x] Iniciar y cerrar sesión devuelve a `/` (CU-05, CU-06). Comprobado el 2026-10-17 con un usuario desechable creado por Claude con autorización expresa del desarrollador; la contraseña se generó en memoria sin mostrarse, y el usuario se borró al terminar. El registro y el inicio de sesión responden `302` a `/`, los dos cierres de sesión responden `302` a `/`, y la portada muestra "Cerrar sesión" con la sesión abierta e "Iniciar sesión" tras cerrarla.
- [x] Sin estilos en línea ni recursos externos (el `grep` de T4).

Casos del catálogo de `AGENTS.md` §11 bis que no aplican:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `CategoriaDto` | Invariantes y validación | Un DTO no valida: copia valores que ya validaron `NombreCategoria` y `Slug` |
| `ConsultasCatalogoPublico` | "Filtra por empresa en la consulta" y "no encuentra lo de otra empresa" | Es una consulta pública: por diseño no filtra por empresa (RN-04, SPEC §10.3). Ya lo cubre la prueba existente "sin contexto de empresa" |
| `ConsultasCatalogoPublico` | Lectura sin seguimiento | El contexto ya es `NoTracking` por defecto y la consulta proyecta a DTO. No hay ninguna entidad que seguir |
| `Landing` | Todos | Es una página sin lógica. Sin infraestructura de pruebas de páginas (SPEC l.330); se cubre con la verificación manual |

### Sabotajes

La fase no implementa ninguna regla ⚑. Los dos sabotajes van sobre la consulta de categorías, que es la única lógica de la fase (duda 8).

| Regla | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-122 / SPEC §10.3 | Quitar `.Where(c => c.Activa)` en `ConsultasCatalogoPublico.ObtenerCategoriasAsync` | `PruebasConsultasCatalogoPublico`: la de la categoría inactiva | ✔ 2026-10-17: falló `Una_categoria_inactiva_no_se_devuelve`. Código restaurado |
| SPEC §11 l.620 (contrato del enlace) | En `CategoriaDto.ConvertirADto`, copiar el nombre en lugar del slug | `PruebasCategoriaDto` y la prueba ampliada de `PruebasConsultasCatalogoPublico` | ✔ 2026-10-17: fallaron `ConvertirADto_copia_el_identificador_el_nombre_y_el_slug_de_la_categoria` (unitaria) y `Devuelve_las_categorias_activas_ordenadas_por_nombre_sin_contexto_de_empresa` (integración). Código restaurado |

## 7. Criterios de aceptación

- [x] `/` sirve la landing en SSR estático, sin `@rendermode` y sin circuito (D-01, SPEC §7). Ver las dos primeras comprobaciones manuales de §6.
- [x] La landing tiene las secciones de SPEC §5.1 (l.191), con contacto en el pie (duda 1), y sus textos están en castellano con `lang="es"`.
- [x] Las categorías salen de `IConsultasCatalogoPublico.ObtenerCategoriasAsync`, y cada una enlaza a `/businesses?category={slug}` con la ciudad vacía (SPEC l.620, CU-01).
- [x] El buscador de la landing envía por GET `city` y `category` a `/businesses`, sin validar en la página.
- [x] Las llamadas a la acción llevan a `/register` y `/for-business/register`.
- [x] `Home.razor` ya no existe y solo hay una página con `@page "/"`.
- [x] No hay estilos en línea, recursos externos ni paquetes nuevos. `Directory.Packages.props` solo fija la versión transitiva de `System.Security.Cryptography.Pkcs` (excepción de §4, E-2). Evidencia: el `grep` de T4 no encuentra nada, y `dotnet list package --vulnerable --include-transitive` no encuentra ningún paquete vulnerable.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 328 pruebas unitarias y 233 de integración.

Esta fase no cierra ningún punto de control.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-12 (CSP incompatible con Blazor Server) | Una landing con `style=""`, `<style>` en línea, fuentes de Google o imágenes de una CDN quedaría rota en la Fase 19, cuando entre `style-src 'self'; img-src 'self' data:` | T4: solo CSS aislado en `.razor.css` y `app.css`, sin recursos externos. Se comprueba con el `grep` de T4 |
| R-4 (fricción Identity + Blazor Web App) | Un formulario SSR con POST en la landing exigiría antifalsificación y podría causar bucles de redirección | El buscador es un formulario GET a otra ruta: no envía nada al servidor de la landing y no necesita antifalsificación |
| R-1 (ámbito del `DbContext` en Blazor Server) | No aplica en la práctica: en SSR estático el ámbito es la petición (`arquitectura.md` l.306) y la página hace una sola consulta | La página no lleva `@rendermode`. Si alguien le pusiera `InteractiveServer`, se rompería D-01 |

El resto de riesgos (R-2, R-3 y R-5 a R-11, R-13, R-14) no aplica: la fase no toca fechas, persistencia, eventos, correo, cifrado, inicio de sesión ni limitación de frecuencia.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| La ruta `/businesses`, la búsqueda, RN-109 a RN-113 y la paginación | Fase 13 |
| La condición de publicable (RN-16 a RN-18), y los criterios de CU-11 y CU-23 "no aparece en la búsqueda ni por slug directo" | Fase 13 (duda 9) |
| La ficha `/businesses/{slug}`, RN-114 a RN-116 y RN-171 | Fase 13 |
| Recordar la última ciudad usada (CU-01 l.150) | Fase 13 (duda 5) |
| Las empresas de demostración sembradas (SPEC l.873) | Cuando las necesite la Fase 13 o la 17 |
| La limitación de frecuencia en `/businesses` (RN-173), las cabeceras y la CSP (E-5, E-6, RN-175) | Fase 19 |
| Las incoherencias de documentos que no tocan la landing (duda 10) | Fase 20 |
| Metaetiquetas SEO, sitemap y accesibilidad formal | No documentados: no se hacen (AGENTS §2) |
| Un formulario o una página de contacto | Sustituidos por una línea en el pie (SPEC l.234, duda 1) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-04 | T2 | `PruebasConsultasCatalogoPublico`: la de las activas ordenadas, sin contexto de empresa |
| RN-122 | T2 | `PruebasConsultasCatalogoPublico`: la de la categoría inactiva; sabotaje 1 |
| SPEC §11 l.620 (enlace por categoría) | T2 | `PruebasCategoriaDto`; sabotaje 2 |
| CU-01 (disparador desde la landing) | T3 | Verificación manual: el formulario navega a `/businesses` |
| CU-11 (disparador) | T1 | Verificación manual: llamada a la acción para empresas |
| CU-05, CU-06 (destino `/`) | T1 | Verificación manual |
| D-01 | T1 | Verificación manual: sin circuito en `/` |

## 11. Dudas

### Duda 1 — Secciones de la landing y "contacto"

- **Pregunta:** SPEC §5.1 (l.191) enumera hero, propuesta de valor, cómo funciona, categorías, preguntas frecuentes, las dos llamadas a la acción y el pie. SPEC §11 (l.615) y §5.4 (l.234) añaden "contacto". No hay formulario de contacto ni ningún dato de contacto definido. ¿Qué secciones lleva la landing y qué es "contacto"?
- **Opciones:**
  - (a) Las secciones de l.191, con "contacto" como una línea de texto en el pie;
  - (b) una sección de contacto propia, con un correo leído de configuración;
  - (c) un formulario de contacto.
- **Recomendación y motivo:** (a). Es la lista más completa y no inventa ni datos ni configuración. (b) añade una clave de configuración y (c) añade un endpoint, que no figuran en ningún documento (AGENTS §3.1).
- **Bloquea:** T1.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 2 — Origen de las categorías

- **Pregunta:** `casos-uso.md` §5.1 (l.592) dice que `/` es "contenido estático". `fase-08.md` (l.422) creó `ObtenerCategoriasAsync` porque "la landing también las muestra". ¿Se leen las categorías de la base de datos o se escriben en la página?
- **Opciones:**
  - (a) Leerlas con `IConsultasCatalogoPublico.ObtenerCategoriasAsync` y corregir l.592;
  - (b) escribir a mano en la página las 8 de SPEC §9.2.
- **Recomendación y motivo:** (a). Reutiliza la consulta existente. Con (b) los enlaces por slug se desincronizarían de los datos sembrados en cuanto cambiara uno.
- **Bloquea:** T2.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 3 — Contrato del enlace de categoría a `/businesses`

- **Pregunta:** SPEC l.620 y CU-01 l.150 piden enlazar "con la categoría preseleccionada", pero ningún documento fija el nombre del parámetro ni su valor. El único ejemplo es `?city=Malaga` (`arquitectura.md` l.298). `CategoriaDto` no lleva `Slug`. La decisión fija el contrato con la Fase 13.
- **Opciones:**
  - (a) `?category={slug}`, añadiendo `Slug` a `CategoriaDto`;
  - (b) `?category={id}`, con el Guid, sin tocar el DTO.
- **Recomendación y motivo:** (a). Las URL quedan legibles y estables, siguen el criterio de §3.3 (inglés, minúsculas, guiones) y aprovechan el `Slug` único que ya existe en la entidad (SPEC §9.2). Coincide con el estilo de `/businesses/{slug}`.
- **Bloquea:** T2 y T3.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 4 — Buscador en la landing

- **Pregunta:** el disparador de CU-01 (l.114) es "el visitante usa el buscador de la landing", pero SPEC §5.1 no incluye un buscador entre las secciones. ¿Lleva la landing un buscador?
- **Opciones:**
  - (a) Un formulario GET con ciudad y categoría hacia `/businesses`, sin validar en la página;
  - (b) solo un enlace "Buscar establecimientos" a `/businesses`;
  - (c) ninguno.
- **Recomendación y motivo:** (a). Cumple el disparador de CU-01. Al ser GET no necesita antifalsificación (R-4), y RN-110 y RN-112 se validan en el servidor de `/businesses` (Fase 13), sin lógica en `Web` (AGENTS §6).
- **Bloquea:** T3.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 5 — Recordar la última ciudad

- **Pregunta:** CU-01 l.150 dice que "conviene que el campo recuerde la última ciudad usada en ese navegador". Exige una cookie o JavaScript, y SPEC §7 (l.265) pide JavaScript "solo si es estrictamente necesario". ¿Entra en esta fase?
- **Opciones:**
  - (a) Fuera de la Fase 12; lo decide la Fase 13, que es la que recibe la ciudad;
  - (b) una cookie que escriba la Fase 12.
- **Recomendación y motivo:** (a). Es una recomendación ("conviene"), no una RN. Quien conoce la ciudad enviada es `/businesses`.
- **Bloquea:** ninguna.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 6 — Nombre y ubicación de la página

- **Pregunta:** `arquitectura.md` §2.3 (l.187) prevé `Components/Publico/` para "landing, buscador, ficha", pero la ruta `/` la sirve hoy `Components/Pages/Home.razor`, de la plantilla. Ningún documento da nombre al componente.
- **Opciones:**
  - (a) `Components/Publico/Landing.razor`, borrando `Home.razor`;
  - (b) reescribir `Components/Pages/Home.razor` en su sitio.
- **Recomendación y motivo:** (a). Sigue la estructura de `arquitectura.md` y usa el término de SPEC §5.1 y §11. "Inicio" ya lo usa la página `/app` (`Components/App/Inicio.razor`), y "Home" está en inglés.
- **Bloquea:** T1.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 7 — Estilo visual y textos de la plantilla

- **Pregunta:** ningún documento define el diseño ni un framework CSS. Además, el bloque `blazor-error-ui` de `MainLayout` está en inglés ("An unhandled error has occurred.", "Reload"), y la landing será la primera página pública en castellano (SPEC l.610).
- **Opciones:**
  - (a) CSS propio mínimo y aislado (`.razor.css`), compatible con la CSP de `arquitectura.md` §14.6, y traducir las dos cadenas de `MainLayout`;
  - (b) Bootstrap u otro framework, por paquete o por CDN;
  - (c) CSS propio sin traducir `MainLayout`.
- **Recomendación y motivo:** (a). (b) es un paquete o un recurso externo: lo prohíbe AGENTS §4 y lo rompería la CSP (R-12). La traducción son dos cadenas, y sin ella la primera página pública mezclaría idiomas.
- **Bloquea:** T4.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada. Con ello `Components/Layout/MainLayout.razor` entra en el radio de §4.

### Duda 8 — Sabotajes sin reglas ⚑

- **Pregunta:** AGENTS §11 bis pide dos o tres sabotajes sobre las reglas ⚑ de la fase, y esta fase no implementa ninguna. ¿Qué se sabotea?
- **Opciones:**
  - (a) Dos sabotajes sobre la consulta de categorías (RN-122 y el contrato del enlace), que es la única lógica de la fase, más la verificación manual de la página;
  - (b) ninguno, omitido con su motivo.
- **Recomendación y motivo:** (a). Así se demuestra que las pruebas nuevas pueden fallar, que es para lo que existen los sabotajes, aunque las reglas no lleven ⚑.
- **Bloquea:** T5.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 9 — Reparto de la condición de publicable entre las fases 12 y 13

- **Pregunta:** `fase-07.md` (l.237 y l.251), `fase-08.md` (l.280 y l.297), `fase-09.md` (l.298) y CU-11 (l.341-342) asignan a "las fases 12 y 13" la condición de publicable (RN-16 a RN-18), RN-109 y los criterios "no aparece en la búsqueda ni por slug directo". SPEC §19 limita la 12 a la "Landing pública SSR".
- **Opciones:**
  - (a) Todo eso pasa a la Fase 13: la landing no lista empresas;
  - (b) la Fase 12 implementa ya la consulta de publicable.
- **Recomendación y motivo:** (a). Ninguna sección de la landing muestra empresas. Una consulta de publicable sin consumidor sería trabajo preparatorio de una fase futura, y eso no se hace.
- **Bloquea:** ninguna.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Duda 10 — Incoherencias de documentos encontradas en la lectura

- **Pregunta:** ¿qué se corrige en esta fase? Lo encontrado:
  - `casos-uso.md` l.592 dice "contenido estático", en contra de la duda 2;
  - `PROJECT_PLAN.md` §7 l.295 sigue marcando la Fase 10 como "Siguiente";
  - el nombre de la fase es "Landing pública SSR" en el SPEC y "Landing pública (SSR)" en el plan;
  - `reglas-negocio.md` l.209 llama a la limitación de frecuencia "fuera de alcance", y RN-173 la incluye;
  - los recuentos no coinciden: 100 frente a 137 reglas; 21, 23 o 24 casos de uso; "los cinco" riesgos frente a los 14 de la tabla de `arquitectura.md` §15.
- **Opciones:**
  - (a) Corregir solo `casos-uso.md` l.592, que es la única que toca la landing, y anotar el resto para la Fase 20;
  - (b) corregirlo todo ahora.
- **Recomendación y motivo:** (a). Respeta el radio de cambio. El resto no afecta a la landing y cabe en la revisión documental de la Fase 20.
- **Bloquea:** ninguna (T5 aplica la corrección de l.592).
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-17 con la opción recomendada.

### Decisiones de implementación

- **`@using` en la propia página.** `Landing.razor` declara sus dos `@using` (`Aplicacion.Abstracciones` y `Aplicacion.Dtos.Categorias`) en lugar de crear un `_Imports.razor` en `Components/Publico/`, que no figura en §4.
- **El hero lleva el buscador** (T3), y el orden de las secciones queda así: hero con buscador, cómo funciona, categorías, preguntas frecuentes, las dos llamadas a la acción y el pie.
- **Las preguntas frecuentes solo afirman lo documentado:**
  - no hay pagos (SPEC §5.4, trabajo futuro);
  - se busca sin cuenta y se reserva con ella (CU-01, actor Visitante; CU-03);
  - se cancela mientras la cita no ha empezado (RN-93);
  - un negocio aparece al completar servicios, profesionales y horarios (RN-16).
- **Contacto en el pie** (duda 1): remite a los datos de contacto de la ficha del establecimiento, sin inventar ningún dato de la plataforma.
- **Las rutas son relativas** a `<base href="/">` (`businesses`, `register`, `for-business/register`), igual que los enlaces de `MainLayout`.

- **Corrección de seguridad ajena a la fase**, ordenada por el desarrollador el 2026-10-17 al detectar la vulnerabilidad. Por eso no cuenta como salida del radio. La restauración empezó a fallar con `NU1902` y `NU1903` sobre `System.Security.Cryptography.Pkcs` 10.0.0, que llega por MailKit 4.18.0 → MimeKit 4.18.0. Los avisos se publicaron el 2026-10-13 y MimeKit 4.18.1 sigue pidiendo la 10.0.0. Se fija la 10.0.13 en `Directory.Packages.props` con `CentralPackageTransitivePinningEnabled`, y se documenta en `arquitectura.md` §3.1. `Directory.Packages.props` figura en §4 como excepción.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica (solo en las fases 10, 11, 16 y 17)
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado: no aplica, esta fase no cierra ninguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6: ninguna es de esta fase
