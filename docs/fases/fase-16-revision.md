# Revisión de la Fase 16

## Revisión 2026-10-18 17:58

**Veredicto:** APTO
**Especificación:** docs/fases/fase-16.md (APROBADA)
**Commit base:** 1d95159
**Ficheros revisados:** 25, más la documentación (`AGENTS.md` y `docs/`), que git ignora

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 3 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 424 | 424 | 0 |
| Integración | 347 | 347 | 0 |

- `dotnet build`: 0 avisos y 0 errores.
- `comprobaciones.sh`: 0 bloqueantes y 0 importantes.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | src/PlataformaReservas.Infraestructura/Consultas/ConsultasReservasEmpresa.cs:44 | A-05, `arquitectura.md` §9 | `db.Reservas.AsNoTracking()` sobra. El contexto ya usa `NoTracking` por defecto (`Infraestructura/InyeccionDependencias.cs:24`), y ninguna otra clase de `Consultas/` lo escribe | Quitar `.AsNoTracking()` |
| 2 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/Dtos/Reservas/PanelEmpresaDto.cs:27-28 | Claridad | El orden por inicio está en dos sitios: `ConvertirADto` vuelve a ordenar listas que la consulta ya devuelve ordenadas (`ConsultasReservasEmpresa.cs:51`) | Dejar el orden en un solo sitio, o mantenerlo si se quiere que el DTO no dependa del orden de la consulta |
| 3 | MENOR | arquitectura | docs/arquitectura.md:176 | `fase-16.md` §5, T5 | En §2.3, `Infraestructura/Consultas/` solo cita `ConsultasCatalogoPublico`. Faltan `ConsultasReservasEmpresa` y `CondicionPublicable`, y ya faltaba `ConsultasReservasCliente` de la Fase 15 | Añadir las tres clases a esa línea |

**Radio y alcance**
- Todos los ficheros están en §4.
- Las cinco tareas están hechas.
- `PruebasCompletarReserva.cs` no cambia: su caso `-45` ya cubría el extremo de fin igual a ahora, como anota §11, "Decisiones de implementación".
- No hay migraciones ni paquetes.

**Notas de los revisores (no verificables, no son hallazgos)**
- `CancelarReservaEmpresa`, `CompletarReserva` y `RepositorioReservas` no cambian. Su aislamiento ya se revisó en las Fases 11 y 11b.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-18**: hoy y próximas, más el aviso con los requisitos que faltan. Evidencia: `PruebasConsultarPanelEmpresa`, `PruebasConsultasReservasEmpresa` (los requisitos uno por uno) y la prueba en el navegador del desarrollador.
- [x] **CU-19**: selector de fecha y filtros, sin reservas de otra empresa. Evidencia: `PruebasConsultasReservasEmpresa`, `PruebasConsultarReservasEmpresa`, el sabotaje del filtro por empresa (fallaron 12 pruebas) y el navegador.
- [x] **CU-20**: cancelar sin restricción temporal y encolar el evento después del commit. Evidencia: `PruebasCancelarReservaEmpresa` (Fases 11 y 11b), `PruebasReservaEmpresaDto.Cancelable…` y el navegador.
- [x] **CU-21**: completar solo si `FinUtc < ahora`. Evidencia: `PruebasReserva` (`EsCompletable`), `PruebasReservaEmpresaDto`, `PruebasCompletarReserva`, el sabotaje y el navegador.
- [x] **CU-23 (l.492)**: el histórico tras la baja. Evidencia: `El_panel_de_una_empresa_inactiva_sigue_devolviendo_sus_reservas` y el navegador.
- [x] **SPEC §21**: ver, cancelar y completar. Evidencia: todo lo anterior.
- [x] **Páginas**: `InteractiveServer`, protegidas por `EsPropietario` y sin `MarkupString`. Evidencia: `App/_Imports.razor`, el grep, `curl` (302) y la revisión de seguridad.
- [x] **Sin migraciones ni paquetes**, y siguen siendo 16 vistas. Evidencia: el diff.
- [x] **Sabotajes**: hacen fallar sus pruebas. Evidencia: los tres anotados en §6.
- [x] **Compilación y pruebas**: `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta revisión.
- [x] **Control 4**: guion de `casos-uso.md` §5.3 recorrido en el navegador por el desarrollador (2026-10-18).
