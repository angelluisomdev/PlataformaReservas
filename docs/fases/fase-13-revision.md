# Revisión de la fase 13

## Revisión 2026-10-17 18:58

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-13.md (APROBADA)
**Commit base:** 9e2d51b
**Ficheros revisados:** 32 de código y pruebas, más 11 documentos en `docs/`, que están en `.gitignore` y no tienen diff

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 4 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 365 | 365 | 0 |
| Integración | 252 | 252 | 0 |

- `dotnet build`: 0 avisos y 0 errores.
- Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=0`.

**Radio de cambio:** todos los ficheros de código están en §4.
- `ContextoDatosModelSnapshot.cs` forma parte de la migración.
- Los documentos tocados fuera de la fila `docs/` de §4 (`modelo-dominio.md`, `reglas-negocio.md`, `fase-07.md`, `fase-08.md` y `fase-09.md`) los autoriza la duda 15, resuelta con (b): "corregirlo todo ahora".

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | src/PlataformaReservas.Infraestructura/Consultas/ConsultasCatalogoPublico.cs:27-33 | A-09 (`arquitectura.md` §7, "cada regla vive en exactamente un sitio"); duda 2 de fase-13 | `BuscarAsync` repite reglas que la duda 2 (b) puso en el caso de uso: <br>• el recorte y la longitud mínima de la ciudad (RN-110); <br>• la página mínima 1; <br>• la comprobación de `Ciudad.LongitudMaxima`. <br>La duda 2 solo autoriza mantener en la consulta el tope de 20 como defensa. La nota de "Decisiones de implementación" amplía lo aprobado sin pasar por una duda. Además, Infraestructura queda acoplada a las constantes de una clase de caso de uso. | Dejar en la consulta solo el `Math.Clamp` del tamaño. Que el caso de uso construya `Ciudad`, o deseche las ciudades de más de 80 caracteres, para cubrir la longitud máxima. Si se quiere mantener la doble comprobación, registrarla como duda resuelta. |
| 2 | IMPORTANTE | seguridad (nota no verificable) · revisión | src/PlataformaReservas.Infraestructura/Consultas/ConsultasCatalogoPublico.cs:51 (`Skip((numero - 1) * tamano)`) | fase-13 duda 6 ("una página fuera de rango devuelve una lista vacía con el total real"); `arquitectura.md` §11.1 | Con un `page` enorme (por ejemplo, `?page=2147483647`), `(numero - 1) * tamano` desborda `int` y da un `OFFSET` negativo, que PostgreSQL rechaza. Una petición manipulada termina en un error de servidor en lugar de una página vacía. Ni el caso de uso ni la consulta acotan la página por arriba. | Calcular el desplazamiento en `long`, o acotar la página (por ejemplo, devolver una página vacía si `(pagina - 1) * tamano >= total`, tras el `CountAsync`). Añadir la prueba de `page = int.MaxValue`. |
| 3 | IMPORTANTE | arquitectura | docs/fases/fase-13.md §6 (tabla de omisiones); tests/PlataformaReservas.PruebasIntegracion/CasosUso/Empresas/PruebasBuscarEstablecimientos.cs | AGENTS.md §11 bis (catálogo, "Caso de uso (integración)") | `BuscarEstablecimientos` es una clase nueva. Le faltan del catálogo "recurso inexistente y de otra empresa", "fallo a mitad sin dejar nada" y "eventos encolados", y la tabla de omisiones de §6 no da motivo para ninguno. | Añadir a §6 una fila para `BuscarEstablecimientos` con el motivo: solo lectura, sin transacción ni eventos, y sin recurso propio de una empresa (RN-04). |
| 4 | IMPORTANTE | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs (ausente) | AGENTS.md §11 bis ("Regla con números o fechas: extremos"); duda 6 de fase-13 | Dos comportamientos fijados en la duda 6 no tienen prueba: <br>• una página fuera de rango devuelve una lista vacía con el total real; <br>• un `category` con el slug de una categoría **inactiva** devuelve una lista vacía. <br>Solo se prueba el slug inexistente. | Añadir dos casos: <br>• página 3 con 25 empresas, `Elementos` vacío y `Total == 25`; <br>• categoría desactivada con SQL, resultado vacío. |
| 5 | MENOR | arquitectura | docs/arquitectura.md:154 | `arquitectura.md` §2.3 | La lista de `CasosUso/Empresas/` no incluye `BuscarEstablecimientos`, aunque §4.1 sí lo nombra. | Añadirlo a la línea 154. |
| 6 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/Dtos/Empresas/FichaEmpresaDto.cs:31-32 | Sin regla (observación) | El orden de servicios y profesionales usa `StringComparer.CurrentCulture`, que depende de la cultura del proceso. Puede cambiar entre la máquina local y Docker, y las pruebas con `Equal(...)` dependen de ese orden. | Usar una cultura fija (`es-ES`) o un orden ordinal. |
| 7 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Consultas/ConsultasCatalogoPublico.cs:97-110 (`SlugExacto`) | Sin regla (observación) | Exigir un slug exacto, y validarlo con `try/catch DominioException`, es lógica de entrada que vive en Infraestructura. Está documentado (§11 y `arquitectura.md` §4.1) y no rompe ninguna regla. | Ninguna obligatoria. Anotarlo si la ficha llega a tener caso de uso. |

**Revisor de seguridad:** sin hallazgos. Comprobó:
- el filtrado en la consulta (activa más `EXISTS`, RN-04, RN-16 y RN-109);
- la comparación sin SQL hecho con cadenas;
- el tope de 20 en las dos capas;
- el 404 real para un slug no visible (RN-116);
- que los profesionales no exponen datos personales;
- que no hay `MarkupString`, y que los enlaces escapan la ciudad y la categoría (RN-171);
- que no hay secretos.

Dejó como no verificable el caso de una `page` enorme, que esta revisión confirma en el hallazgo 2.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-01: sin ciudad o con 1 carácter, ningún resultado, tampoco manipulando la petición.** Evidencia:
  - `PruebasBuscarEstablecimientos`, en unitarias y en integración;
  - la verificación manual con `?city=E`.
- [x] **CU-01: "Ea" ejecuta la búsqueda.** Evidencia: `Una_ciudad_de_dos_caracteres_encuentra_sus_empresas`.
- [x] **CU-01: solo empresas de esa ciudad, sin distinguir mayúsculas ni acentos.** Evidencia:
  - `Solo_devuelve_empresas_publicables_de_esa_ciudad`;
  - `La_ciudad_se_compara_sin_mayusculas_ni_acentos_y_completa`;
  - la verificación manual con "malaga" y "MÁLAGA".
- [x] **CU-01: la categoría refina.** Evidencia: `La_categoria_refina_el_resultado_dentro_de_la_ciudad`.
- [x] **CU-01: inactiva o no publicable nunca aparece.** Evidencia:
  - `Una_empresa_inactiva_no_aparece_ni_por_slug_y_reactivada_vuelve`;
  - `Una_empresa_solo_es_visible_mientras_es_publicable`;
  - el sabotaje de RN-16.
- [x] **CU-01: como máximo 20 por página.** Evidencia:
  - `Pagina_de_veinte_en_veinte_con_el_total_y_nunca_mas_de_veinte`;
  - las pruebas unitarias del tamaño;
  - el sabotaje de RN-113.
- [x] **CU-01: funciona sin autenticación.** Evidencia: verificación manual sin sesión, y las páginas no tienen `[Authorize]`.
- [x] **CU-02: contenido de la ficha y "no encontrado".** Evidencia:
  - `La_ficha_solo_muestra_lo_activo_sin_datos_personales_y_une_el_horario`;
  - `La_ficha_de_un_slug_inexistente_o_mal_formado_es_nula`;
  - la verificación manual: 404 y enlace a `book/`.
- [x] **CU-11: no aparece al darse de alta y aparece al completarse.** Evidencia: `Una_empresa_solo_es_visible_mientras_es_publicable`.
- [x] **CU-23: tras la baja no aparece y al reactivarse vuelve.** Evidencia: `Una_empresa_inactiva_no_aparece_ni_por_slug_y_reactivada_vuelve`.
- [x] **RN-171: `<script>` no se ejecuta en la ficha.** Evidencia:
  - el `grep MarkupString` sin resultados;
  - la prueba de que la descripción se conserva tal cual;
  - la verificación manual (`&lt;script&gt;`).
- [x] **SSR estático, sin estilos en línea ni recursos externos.** Evidencia:
  - no hay `@rendermode`;
  - el `grep` de estilos y URL no encuentra nada;
  - el HTML no tiene marcadores interactivos.
- [x] **Sin paquetes nuevos.** Evidencia: `Directory.Packages.props` sin cambios.
- [x] **Los sabotajes fallan.** Evidencia: los tres anotados en §6.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión.

## Revisión 2026-10-17 19:16

**Veredicto:** APTO
**Especificación:** docs/fases/fase-13.md (APROBADA)
**Commit base:** 9e2d51b
**Ficheros revisados:** 30 de código y pruebas, más 11 documentos en `docs/`, que están en `.gitignore` y no tienen diff

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 4 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 367 | 367 | 0 |
| Integración | 255 | 255 | 0 |

- `dotnet build`: 0 avisos y 0 errores.
- Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=0`.

**Radio de cambio:** sin cambios respecto a la ronda anterior.
- Todos los ficheros de código y pruebas están en §4.
- Los documentos fuera de la fila `docs/` los autoriza la duda 15 (b).

**Hallazgos de la ronda anterior:** los siete están corregidos.

| # | Corrección |
|---|---|
| 1 | La consulta ya solo conserva el `Math.Clamp` del tamaño. La ciudad (2-80) y la página desde 1 viven en `BuscarEstablecimientos`. |
| 2 | El desplazamiento se calcula en `long` después del `CountAsync`, y fuera de rango devuelve una página vacía. Lo prueba el caso `int.MaxValue`, y un sabotaje confirma que la prueba falla sin la protección. |
| 3 | §6 tiene la fila de omisiones de `BuscarEstablecimientos`. |
| 4 | Hay pruebas nuevas: página fuera de rango (3 y `int.MaxValue`) y categoría inactiva. |
| 5 | `arquitectura.md` l.154 lista `BuscarEstablecimientos`. |
| 6 | La ficha ordena con un comparador `es-ES` fijo. |
| 7 | §11 anota que `SlugExacto` se moverá al caso de uso si la ficha llega a tener uno. |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/CasosUso/Empresas/BuscarEstablecimientos.cs:27; src/PlataformaReservas.Infraestructura/Consultas/ConsultasCatalogoPublico.cs:33, 47, 61 | AGENTS §8 bis ("ni el caso de uso ni `Infraestructura` montan un DTO campo a campo"); `arquitectura.md` §3.3 | `PaginaResultados<T>` está en `Dtos/Empresas/`, no tiene `ConvertirADto` y se monta con `new`. La excepción por CA1000 solo consta en `fase-13.md` §11, no en `arquitectura.md`. La tabla de §3.3 ya recoge un precedente CA1000 con su motivo (`ResultadoT.cs`). | Anotar la excepción en `arquitectura.md` (§3.3 o §5.2), o suprimir CA1000 como en `Resultado<T>` y dar al tipo una fábrica estática. |
| 2 | MENOR | arquitectura | tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs:104 | `fase-13.md` §6 y §10 (CU-23) | La prueba citada para CU-23 cambia `activa` con un `UPDATE` en SQL, sin pasar por `DarDeBajaEmpresa` ni `ReactivarEmpresa`. Hoy el efecto es el mismo, pero el criterio queda probado sin ejecutar los casos de uso reales. | Hacer la baja y la reactivación con los casos de uso reales, con un contexto de empresa fijado. |
| 3 | MENOR | arquitectura | tests/PlataformaReservas.PruebasIntegracion/CasosUso/Empresas/PruebasBuscarEstablecimientos.cs:33 | `fase-13.md` §6, fila RN-04 | §6 dice "desde un ámbito sin `IContextoEmpresa`". El ámbito sí lo registra (`ContextoEmpresaAjustable`), solo que sin empresa fijada. La prueba sigue siendo válida, porque leer `EmpresaActualId` sin empresa lanzaría. | Cambiar el texto de §6 a "con el contexto de empresa sin fijar", o comprobar en la prueba que `TieneEmpresa` es `false`. |
| 4 | MENOR | alcance | docs/fases/fase-13.md:289 (§9) | Duda 15, resuelta con (b) | La fila "Las incoherencias de documentos que no tocan la búsqueda → Fase 20 (duda 15)" contradice la resolución (b), "corregirlo todo ahora". | Quitar la fila de §9, porque ya no hay nada aplazado por la duda 15. |

**Revisor de seguridad:** sin hallazgos. Comprobó:
- que el puerto no recibe el contexto de empresa;
- `Visibles()` en SQL (RN-04, RN-16 y RN-109);
- que los servicios y profesionales de la ficha se filtran por `EmpresaId` en la consulta;
- el tope de 20 y que el desplazamiento no desborda;
- el 404 para un slug no visible o no canónico (RN-116);
- que los profesionales no exponen datos personales;
- que no hay `MarkupString`, y que se usan `@` y `Uri.EscapeDataString` (RN-171);
- que el SQL a mano de la colación es estático;
- que no hay secretos.

La limitación de frecuencia (RN-173) es de la Fase 19 (duda 14).

**No verificable por los revisores:**
- que la imagen de PostgreSQL de Docker tenga ICU, necesario para `CREATE COLLATION … provider = icu`. Las pruebas de integración pasan con Testcontainers, lo que lo confirma para la imagen de pruebas;
- el render SSR y el 404 real, que se apoyan en la verificación manual de §6.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-01: sin ciudad, con 1 carácter o con más de 80, ningún resultado.** Evidencia: `PruebasBuscarEstablecimientos`, en unitarias y en integración, y la verificación manual con `?city=E`.
- [x] **CU-01: "Ea" ejecuta la búsqueda.** Evidencia: `Una_ciudad_de_dos_caracteres_encuentra_sus_empresas`.
- [x] **CU-01: sin distinguir mayúsculas ni acentos.** Evidencia: `La_ciudad_se_compara_sin_mayusculas_ni_acentos_y_completa`.
- [x] **CU-01: la categoría refina; una categoría inactiva o desconocida da vacío.** Evidencia:
  - `La_categoria_refina_el_resultado_dentro_de_la_ciudad`;
  - `Una_categoria_inactiva_no_filtra_resultados_y_devuelve_vacio`.
- [x] **CU-01: inactiva o no publicable nunca aparece.** Evidencia:
  - las pruebas de publicación y de inactiva/reactivada;
  - el sabotaje de RN-16.
- [x] **CU-01: como máximo 20 por página; fuera de rango, vacío con el total real.** Evidencia:
  - `Pagina_de_veinte_en_veinte_con_el_total_y_nunca_mas_de_veinte`;
  - `Una_pagina_fuera_de_rango_devuelve_vacio_con_el_total_real`;
  - el sabotaje de RN-113.
- [x] **CU-01: funciona sin autenticación.** Evidencia: no hay `[Authorize]`, y la verificación manual se hizo sin sesión.
- [x] **CU-02: contenido de la ficha y "no encontrado".** Evidencia: las pruebas de la ficha y la verificación manual (404 y enlace a `book/`).
- [x] **CU-11: no aparece al darse de alta y aparece al completarse.** Evidencia: `Una_empresa_solo_es_visible_mientras_es_publicable`.
- [x] **CU-23: tras la baja no aparece y al reactivarse vuelve.** Evidencia: la prueba de inactiva/reactivada, que hace la baja por SQL (ver hallazgo 2).
- [x] **RN-171: no hay `MarkupString` y `<script>` se conserva tal cual.** Evidencia: el `grep` sin resultados y la prueba de la descripción.
- [x] **SSR estático, sin estilos en línea ni recursos externos.** Evidencia: no hay `@rendermode` y el HTML no tiene marcadores interactivos.
- [x] **Sin paquetes nuevos.** Evidencia: `Directory.Packages.props` sin cambios.
- [x] **Los sabotajes fallan.** Evidencia: los anotados en §6, más el sabotaje de desbordamiento.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión.
