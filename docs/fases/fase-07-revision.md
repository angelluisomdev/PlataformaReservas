# Revisión de la Fase 07

## Revisión 2026-10-04 18:34

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-07.md (APROBADA)
**Commit base:** 0ec9dd0
**Ficheros revisados:** 19, sin contar la documentación no versionada (`docs/arquitectura.md` y `docs/fases/fase-07.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 1 | 2 | 0 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 168 | 168 | 0 |
| Integración | 60 | 60 | 0 |

Comprobaciones mecánicas: `RESUMEN|bloqueantes=0|importantes=0`, sin ninguna línea de hallazgo. `dotnet build`: 0 avisos y 0 errores.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | alcance · arquitectura | `README.md:1` | AGENTS §3.2 y §13; `fase-07.md` §4 | `README.md` está fuera del radio de cambio de la fase. Su título pasa de `# PlataformaReservas` a `# Plataforma Reservas`, un cambio que no corresponde a ninguna tarea. No lo ha hecho la implementación de la fase: ya estaba modificado antes de empezar | Sacar `README.md` de este commit, o devolverlo a HEAD. Si el cambio es deliberado, el desarrollador lo autoriza y va en un commit aparte |
| 2 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Identidad/ContextoEmpresa.cs:17` | `fase-07.md` §11, duda 6, resolución (a) | Solo trata como "sin empresa" una tarea que no ha terminado. `ServerAuthenticationStateProvider`, de quien deriva `RevalidadorIdentidad`, lanza `InvalidOperationException` de forma síncrona si nadie ha fijado el estado, por ejemplo fuera del pipeline de componentes. En ese caso `TieneEmpresa` lanza en vez de devolver `false`. El revisor de seguridad considera seguro el resultado (la excepción deniega el acceso), pero se aparta de lo decidido en la duda 6. La prueba con la tarea pendiente no cubre este caso | Capturar esa `InvalidOperationException` en `ObtenerId` y devolver `null`. Añadir a `PruebasContextoEmpresa` un caso con un proveedor que lance al pedir el estado |
| 3 | IMPORTANTE | alcance | `docs/fases/fase-07.md` §7 | Skill `/revisar` §3: un criterio de §7 sin evidencia | La línea del Control 1 "La clave de cifrado esta respaldada FUERA de la maquina" sigue sin marcar. Es una comprobación manual (R-9) | El desarrollador lo confirma y marca la casilla |

### No verificable (de los revisores)

- **Seguridad.** El esquema no obliga a que el `empresa_id` de `horarios` y `excepciones_horario` coincida con el del profesional: la clave ajena es simple, por `profesional_id`. Que coincidan depende del dominio, que llega en la Fase 9. Hay que comprobarlo cuando lleguen `AgregarIntervalo` y `MarcarNoDisponible`. No es un hallazgo de esta fase.
- **Ambos revisores.** No ejecutaron `dotnet build` ni `dotnet test`, porque su rol es de solo lectura. Los ejecutó esta revisión: resultados en la tabla de arriba.

### Criterios de aceptación (§7 de la especificación)

- [x] `IContextoEmpresa` tiene la firma literal de SPEC §10.3, y `EmpresaActualId` lanza si no hay empresa. Evidencia: `PruebasContextoEmpresa`, con 5 casos. Ver el hallazgo 2.
- [x] Los repositorios filtran por `EmpresaActualId` en la consulta. `RepositorioMiembros` es la excepción documentada en la duda 2. No hay `HasQueryFilter` ni `IgnoreQueryFilters()`. Lo confirma el revisor de seguridad.
- [x] Ningún repositorio llama a `SaveChanges`. Lo confirma el revisor de seguridad.
- [x] No existe `IRepositorioHorarios`.
- [x] `FabricaClaimsUsuario` usa `IRepositorioMiembros`. Evidencia: `PruebasFabricaClaimsUsuario` en verde.
- [x] Ningún servicio con estado es singleton. Evidencia: `PruebasInyeccionDependencias.El_contexto_de_empresa_y_los_repositorios_son_scoped`.
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 168 y 60 pruebas.
- [x] Control 1: las 5 pruebas de aislamiento están en verde.
- [x] Control 1: `SELECT` crudo sobre `asp_net_users`, más inicio de sesión, correo duplicado y recuperación de contraseña. Todo en verde.
- [ ] Control 1: la clave de cifrado está respaldada fuera de la máquina. Pendiente de confirmación del desarrollador (hallazgo 3).

## Resolución de la revisión 1 (2026-10-04)

| # | Desenlace | Detalle |
|---|---|---|
| 1 | Verificado por el desarrollador | El cambio de título de `README.md` lo hizo el desarrollador, a propósito y fuera del trabajo de la fase. Queda autorizado. El fichero no se toca |
| 2 | Corregido | `ContextoEmpresa.ObtenerId` captura la `InvalidOperationException` de `GetAuthenticationStateAsync()` y devuelve "sin empresa", como fija la duda 6 (a). Prueba nueva: `PruebasContextoEmpresa.Con_el_estado_nunca_fijado_no_hay_empresa` |
| 3 | Confirmado por el desarrollador | Las dos claves de cifrado están respaldadas fuera de la máquina (R-9). Marcado en `fase-07.md` §7 |

## Revisión 2026-10-04 18:38

**Veredicto:** APTO
**Especificación:** docs/fases/fase-07.md (APROBADA)
**Commit base:** 0ec9dd0
**Ficheros revisados:** 19, sin contar la documentación no versionada (`docs/arquitectura.md` y `docs/fases/fase-07.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 3 | 1 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 168 | 168 | 0 |
| Integración | 61 | 61 | 0 |

Comprobaciones mecánicas: `RESUMEN|bloqueantes=0|importantes=0`, sin ninguna línea de hallazgo. `dotnet build`: 0 avisos y 0 errores.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE · **falso positivo** | arquitectura | `README.md:1` | AGENTS §3.2 y §13; `fase-07.md` §4 | El título de `README.md` cambia y el fichero está fuera del radio de cambio. **Falso positivo:** el cambio lo hizo el desarrollador, que lo autorizó y verificó en la resolución de la revisión 1. El revisor no lo sabía porque, por diseño, revisa sin conocer las conclusiones previas | Ninguna. Conviene que no vaya en el mismo commit que la fase |
| 2 | MENOR | seguridad | `src/PlataformaReservas.Infraestructura/Repositorios/RepositorioProfesionales.cs:21-22` | AGENTS §8, D-11 | `Include(Horarios)` e `Include(Excepciones)` cargan los hijos solo por `profesional_id`. No hay una clave ajena compuesta que obligue a que el `empresa_id` del hijo coincida con el del profesional. Hoy no hay ninguna forma de escribir horarios ni excepciones, así que no se puede explotar | Filtrar los hijos dentro del `Include` (`p.Horarios.Where(h => h.EmpresaId == empresaId)`), o garantizar la coherencia cuando la Fase 9 añada `AgregarIntervalo` y `MarcarNoDisponible` |
| 3 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/Repositorios/RepositorioMiembros.cs:10` | AGENTS §3.10 | El comentario repite la justificación que ya está en `arquitectura.md` §8.2 | Quitarlo, o dejar solo una referencia a §8.2 |
| 4 | MENOR | arquitectura | `docs/arquitectura.md:487` | `fase-07.md` duda 6 | El texto sigue diciendo "del `IHttpContextAccessor` o del `AuthenticationStateProvider`", pero la duda 6 se resolvió con solo `AuthenticationStateProvider` | Dejar solo `AuthenticationStateProvider` y explicar que se lee la tarea ya resuelta, y que "sin empresa" es el resultado si no hay estado |

### No verificable (de los revisores)

- **Seguridad.** En el área `InteractiveServer` no se puede comprobar todavía que el estado de autenticación esté resuelto al leer `EmpresaActualId`. Falla del lado seguro. Se verá con la primera pantalla de la Fase 8.
- **Ambos revisores.** No ejecutaron la compilación ni las pruebas, porque su rol es de solo lectura. Los ejecutó esta revisión: resultados en la tabla de arriba.

### Criterios de aceptación (§7 de la especificación)

- [x] `IContextoEmpresa` tiene la firma literal de SPEC §10.3, y `EmpresaActualId` lanza si no hay empresa (`PruebasContextoEmpresa`, con 6 casos).
- [x] Los repositorios filtran por `EmpresaActualId` en la consulta, con la excepción documentada de `RepositorioMiembros`. No hay `HasQueryFilter` ni `IgnoreQueryFilters()`.
- [x] Ningún repositorio llama a `SaveChanges`.
- [x] No existe `IRepositorioHorarios`.
- [x] `FabricaClaimsUsuario` usa `IRepositorioMiembros` (`PruebasFabricaClaimsUsuario` en verde).
- [x] Ningún servicio con estado es singleton (`PruebasInyeccionDependencias`).
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 168 y 61 pruebas.
- [x] Control 1: las 5 pruebas de aislamiento en verde.
- [x] Control 1: `SELECT` crudo sobre `asp_net_users`, más inicio de sesión, correo duplicado y recuperación de contraseña. Todo en verde.
- [x] Control 1: la clave de cifrado está respaldada fuera de la máquina. Lo confirmó el desarrollador el 2026-10-04.

## Seguimiento 2026-10-12

No es una ronda de revisión: no se ha vuelto a compilar ni a ejecutar las pruebas. Solo registra en qué estado están los hallazgos que la última ronda dejó abiertos, comprobado contra el código el 2026-10-12.

| Hallazgo | Estado | Evidencia |
|---|---|---|
| Menor 2 (`Include` de horarios y excepciones sin filtrar por empresa) | **Resuelto por diseño en la Fase 9**, con la segunda opción de la corrección sugerida | `Horario.Crear` y `ExcepcionHorario.Crear` son `internal` y solo los llama `Profesional` con su propio `EmpresaId` (`Profesional.cs:101` y `:119`). Los `Include` de `RepositorioProfesionales` siguen sin filtro |
| Menor 3 (comentario repetido en `RepositorioMiembros`) | **Abierto**: se cierra en la Fase 19 (refactorización) | `src/PlataformaReservas.Infraestructura/Repositorios/RepositorioMiembros.cs:10` |
| Menor 4 (`arquitectura.md` cita `IHttpContextAccessor`) | **Resuelto** el 2026-10-12 | `arquitectura.md` §8.2 dice ahora solo `AuthenticationStateProvider`, con el caso "sin estado" y la referencia a la duda 6 (a) |
