# Revisión de la fase 12

## Revisión 2026-10-17 17:48

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-12.md (APROBADA)
**Commit base:** d9ffddf
**Ficheros revisados:** 9 de código y pruebas, más 3 documentos en `docs/` (`casos-uso.md`, `arquitectura.md` y `fase-12.md`; están en `.gitignore`, así que no tienen diff)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 2 | 0 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 328 | 328 | 0 |
| Integración | 233 | 233 | 0 |

`dotnet build`: 0 avisos y 0 errores. Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=1`.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance · arquitectura | Directory.Packages.props; docs/fases/fase-12.md §4 (l.77) y §7 (l.210) | fase-12 §4 ("Todo lo que no figure como 'sí' no se toca"; "ningún paquete"); AGENTS §3.2 | `Directory.Packages.props` cambia: añade `CentralPackageTransitivePinningEnabled` y fija `System.Security.Cryptography.Pkcs` 10.0.13 por CVE-2026-70162 y CVE-2026-84008. El desarrollador aprobó el cambio y está anotado en §11 y en `arquitectura.md` §3.1, pero el fichero no figura en el radio de §4. Además, el criterio de §7 "sin paquetes nuevos (`Directory.Packages.props` sin cambios)" está marcado [x] y es falso: la especificación se contradice. | Añadir `Directory.Packages.props` a §4 como excepción aprobada (corrección de seguridad, E-2, ver §11). Reescribir el criterio de §7: "sin paquetes nuevos; `Directory.Packages.props` solo fija la versión transitiva de `System.Security.Cryptography.Pkcs` (§11, E-2)". |
| 2 | IMPORTANTE | mecánica (C17) | Directory.Packages.props | `arquitectura.md` §2.2, §3.1 y §3.2 | "Cambian los ajustes comunes: contrastar con arquitectura §2.2, §3.1 y §3.2". Contrastado: el cambio coincide con la nota nueva de §3.1 (fijar dependencias transitivas vulnerables, E-2), no añade ningún paquete de aplicación a §2.2 y no toca `Directory.Build.props` (§3.2). Lo que falta en el radio se trata en el hallazgo 1. | Ninguna, aparte de la del hallazgo 1. |
| 3 | IMPORTANTE | alcance (§7) | docs/fases/fase-12.md §7 (l.204) | fase-12 §7 criterio 1; D-01 | El criterio "`/` sirve la landing en SSR estático, sin `@rendermode` y sin circuito" no tiene evidencia completa. Sí está comprobado que la página no lleva `@rendermode` y que el HTML servido (`curl` sobre `/`) no contiene marcadores de componente interactivo. Falta lo que exige un navegador: ver en la pestaña de red que no hay `_blazor` ni WebSocket. Tampoco está comprobada la vuelta a `/` tras iniciar y cerrar sesión (verificación manual de §6). | Abrir `/` en el navegador con las herramientas de desarrollo, confirmar que no hay conexión `_blazor`, y marcar el criterio y las dos casillas de §6. |

**Revisor de seguridad: sin hallazgos.** Comprobó:
- que no hay `MarkupString` ni HTML montado a mano (RN-171);
- que la página es SSR estática (D-01);
- que solo usa `IConsultasCatalogoPublico`, que no recibe contexto de empresa (RN-04);
- que el SQL de la prueba va parametrizado;
- que el aviso de error no muestra detalles internos;
- que no hay secretos.

No pudo verificar la CSP en ejecución.

### Criterios de aceptación (§7 de la especificación)

- [ ] `/` sirve la landing en SSR estático, sin `@rendermode` y sin circuito. Hay evidencia parcial: la página no lleva `@rendermode` y el HTML no tiene marcadores de componente interactivo. Falta la comprobación en el navegador (hallazgo 3).
- [x] Las secciones de SPEC §5.1 y el contacto en el pie, con los textos en castellano y `lang="es"`. Evidencia: `curl` sobre `/` (el `h1`, cinco `h2`, el pie y `lang="es"`).
- [x] Las categorías salen de `ObtenerCategoriasAsync` y cada una enlaza a `/businesses?category={slug}`. Evidencia:
  - los 8 enlaces en el HTML servido;
  - `PruebasCategoriaDto`;
  - `PruebasConsultasCatalogoPublico`.
- [x] El buscador envía por GET `city` y `category` a `/businesses`. Evidencia: el `<form method="get" action="businesses">` del HTML servido.
- [x] Las llamadas a la acción llevan a `/register` y `/for-business/register`. Evidencia: el HTML servido.
- [x] `Home.razor` ya no existe y solo hay un `@page "/"`. Evidencia: el diff (`D Components/Pages/Home.razor`).
- [ ] Sin estilos en línea, recursos externos ni paquetes nuevos (`Directory.Packages.props` sin cambios). Lo de los estilos y los recursos se cumple (el `grep` de T4 no encuentra nada), pero `Directory.Packages.props` sí cambia (hallazgo 1).
- [x] Los sabotajes de §6 hacen fallar sus pruebas. Evidencia: los dos anotan la prueba que falló, en unitarias y en integración.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta revisión, con 328 unitarias y 233 de integración.

## Revisión 2026-10-17 18:14

**Veredicto:** APTO
**Especificación:** docs/fases/fase-12.md (APROBADA)
**Commit base:** d9ffddf
**Ficheros revisados:** 9 de código y pruebas, más 3 documentos en `docs/`, que están en `.gitignore` y no tienen diff

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 0 | 1 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 328 | 328 | 0 |
| Integración | 233 | 233 | 0 |

`dotnet build`: 0 avisos y 0 errores. Comprobaciones mecánicas (`comprobaciones.sh`): `bloqueantes=0 · importantes=1`. Ese importante es el falso positivo de abajo.

**Ronda anterior, todo resuelto:**
1. **Bloqueante (radio de cambio).** El desarrollador ordenó el cambio de `Directory.Packages.props` para corregir una vulnerabilidad que detectó. Ahora figura en §4 como excepción, el criterio de §7 está reescrito y la decisión queda explicada en §11.
2. **C17.** Ver el falso positivo de abajo.
3. **Criterio "sin circuito".**
   - El HTML servido no tiene marcadores interactivos, ni sin sesión ni con ella.
   - Con un usuario desechable, autorizado expresamente por el desarrollador y borrado al terminar, se comprobó que el registro, el inicio de sesión y los dos cierres de sesión responden `302` a `/`.
   - Está anotado en §6 y §7. La pestaña Red del navegador no se miró.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE (**falso positivo**) | mecánica (C17) | Directory.Packages.props | `arquitectura.md` §2.2, §3.1 y §3.2 | "Cambian los ajustes comunes: contrastar con arquitectura §2.2, §3.1 y §3.2". **Por qué es falso positivo:** C17 salta con cualquier cambio en el fichero y lo que pide es contrastarlo, y el contraste es conforme. <br>• §3.1 recoge expresamente cómo fijar la versión de una dependencia transitiva vulnerable (E-2). <br>• No se añade ningún paquete de aplicación a §2.2. <br>• `Directory.Build.props` (§3.2) no cambia. <br>• El cambio figura en §4 de la fase como excepción ordenada por el desarrollador. <br>Hay una sola línea nueva: `System.Security.Cryptography.Pkcs` 10.0.13, con el motivo comentado, y `dotnet list package --vulnerable --include-transitive` no encuentra ningún paquete vulnerable. | Ninguna. Para afinar la comprobación, C17 podría no avisar cuando el diff solo añade `PackageVersion` con comentario y `CentralPackageTransitivePinningEnabled`. |

**Revisor de arquitectura:** sin hallazgos. **Revisor de seguridad:** sin hallazgos.

### Criterios de aceptación (§7 de la especificación)

- [x] **`/` en SSR estático, sin `@rendermode` y sin circuito.** Evidencia:
  - la página no lleva `@rendermode`;
  - el HTML servido no tiene marcadores `<!--Blazor:`, ni sin sesión ni con ella.

  La pestaña Red del navegador no se comprobó.
- [x] **Secciones de SPEC §5.1, contacto en el pie, castellano y `lang="es"`.** Evidencia: `curl` sobre `/`.
- [x] **Categorías desde `ObtenerCategoriasAsync` con enlace `/businesses?category={slug}`.** Evidencia:
  - los 8 enlaces en el HTML servido;
  - `PruebasCategoriaDto`;
  - `PruebasConsultasCatalogoPublico`.
- [x] **Buscador GET con `city` y `category` hacia `/businesses`.** Evidencia: el formulario en el HTML servido.
- [x] **Llamadas a la acción a `/register` y `/for-business/register`.** Evidencia: el HTML servido.
- [x] **`Home.razor` eliminado y una sola página con `@page "/"`.** Evidencia: el diff.
- [x] **Sin estilos en línea, recursos externos ni paquetes nuevos (salvo la excepción de §4).** Evidencia:
  - el `grep` de T4;
  - `dotnet list package --vulnerable --include-transitive`.
- [x] **Los sabotajes hacen fallar sus pruebas.** Evidencia: §6, en unitarias y en integración.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión.
