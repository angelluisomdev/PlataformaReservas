# Auditoría de pruebas de la Fase 16

## Auditoría 2026-10-18 18:05

**Alcance:** código construido hasta la Fase 16 (pendiente de commit; revisión APTO, `fase-16-revision.md`)
**Commit base:** 1d95159

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 424 | 424 | 0 | 0 |
| Integración | 347 | 347 | 0 | 0 |

| Importantes | Menores |
|---|---|
| 3 | 8 |

`PROJECT_PLAN.md` §7 está desactualizada: la última actualización es del 2026-10-12, con las fases 0 a 9 cerradas. Según el historial de commits también están cerradas la 10, la 11, la 11b y de la 12 a la 15. Las fases 0 a 13 ya se auditaron en las auditorías de las fases 10, 11 y 14; esta se centra en la Fase 16 y en las regresiones que siguen abiertas.

### Clases sin fichero de pruebas

| Clase | Motivo, si no necesita prueba propia |
|---|---|
| `Infraestructura/Consultas/CondicionPublicable` | Es una expresión estática sin estado. La cubren `PruebasConsultasReservasEmpresa` (requisitos uno por uno) y `PruebasConsultasCatalogoPublico` (RN-16), y el sabotaje de la Fase 16 hizo fallar a las dos |
| `Aplicacion/CasosUso/Empresas/DatosEmpresa`, `Profesionales/DatosProfesional`, `Servicios/DatosServicio` | Auxiliares que traducen comandos a objetos de valor. Los cubren las pruebas de validación de sus casos de uso (`PruebasRegistrarEmpresa`, `PruebasCrearProfesional`, `PruebasCrearServicio`…) |
| `Aplicacion/Compartido/OrdenEspanol` | La cubre `PruebasFichaEmpresaDto`, pero de forma débil: es el caso 2 de esta auditoría |
| `Aplicacion/Compartido/HuecoOcupadoException`, `Dominio/Compartido/DominioException`, `EntidadBase` | Sin lógica propia. `EntidadBase` (eventos) la cubren las pruebas de las entidades |
| `Dominio/Compartido/Validacion` | La cubren las pruebas de los objetos de valor y de las entidades (`InstanteUtc`, identificadores) |
| `Dominio/Entidades/Horario`, `ExcepcionHorario`, `ProfesionalServicio` | Entidades hijas del agregado `Profesional`, que se prueban a través de `PruebasProfesional` (AGENTS §8: no hay repositorio de horarios) |
| `Aplicacion/Disponibilidad/HuecoDisponible` | Es un record sin lógica |
| `Infraestructura/Identidad/ClaimsSesion` | Auxiliar `internal` de `ContextoEmpresa` y `ContextoUsuario`, cubierto por sus pruebas |
| `Infraestructura/Persistencia/Conversores/*` (16 conversores) | Los cubren `Persistencia/Configuraciones/PruebasConfiguracion*` y `PruebasProtectorDatosPersonales` (`ConversorCifrado`) |
| `Infraestructura/Persistencia/DatosIniciales/SembradorDatos` | Lo cubre `PruebasDatosIniciales`, una prueba transversal de la lista cerrada |
| `Infraestructura/RelojSistema` | Devuelve `DateTime.UtcNow`; en las pruebas se usa `RelojFijo` |
| `Web/Cuenta/Identity*` | Plantilla de Identity (AGENTS §5, excepción de plantilla); la cubre la verificación manual |

### Casos que faltan

| # | Severidad | Clase o regla | Caso que falta | Por qué importa | Fichero donde iría |
|---|---|---|---|---|---|
| 1 | IMPORTANTE | `ConsultarPanelEmpresa` (AGENTS §11 bis, "Regla con números o fechas": cambio de día y horas en UTC; AGENTS §9; `fase-16.md` T2) | Dos casos. (a) `ahora` entre las 22:00 y las 24:00 UTC, por ejemplo `2026-10-03T22:30Z`, que en Madrid son las 00:30 del día 4: `hoyDesde` tiene que ser `2026-10-03T22:00Z`. (b) Una semana de próximas que cruza el cambio de hora, por ejemplo hoy = 2026-10-20: `proximasHasta` tiene que ser `2026-10-27T23:00Z` y no `hoyHasta + 7×24 h` | El `RelojFijo` de integración marca `2026-10-03T08:00Z`, donde la fecha UTC y la local coinciden, y la semana no cruza ningún cambio de hora. Por eso dos sabotajes no hacen fallar nada: `DateOnly.FromDateTime(ahora)` en lugar de `ALocal(ahora)`, y `hoyHasta.AddDays(7)` en lugar de `DiaLocalEnUtc(hoy + 7)` | `tests/PlataformaReservas.PruebasUnitarias/CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` (nuevo), con un doble de `IConsultasReservasEmpresa` que capture los rangos y el `RelojFijo` unitario, como hace `PruebasUnitarias/CasosUso/Empresas/PruebasBuscarEstablecimientos` |
| 2 | IMPORTANTE | Regresión abierta: caso 2 de `fase-14-auditoria-pruebas.md` (hallazgo 6 de `fase-13-revision`, orden `es-ES` en la ficha) | Un nombre que empiece por tilde ("Álvaro" frente a "Zoe", "Ácido" frente a "Corte") en los servicios y profesionales de `FichaEmpresaDto` | `PruebasFichaEmpresaDto` usa nombres que salen en el mismo orden con la comparación ordinal, así que si se quita `OrdenEspanol.Comparador` nada falla. Ya está asignada a la Fase 17 (`fase-15.md`, duda 10) | `tests/PlataformaReservas.PruebasUnitarias/Dtos/Empresas/PruebasFichaEmpresaDto.cs` |
| 3 | MENOR | `ConsultasReservasEmpresa` (RN-24, RN-33: "sus reservas existentes se conservan"; CU-20 y CU-21) | Las reservas de un servicio desactivado y de un profesional desactivado siguen apareciendo en `ObtenerReservasAsync` y siguen siendo cancelables | Si alguien añade `s.Activo` o `p.Activo` a los `join` (copiando `CondicionPublicable`), la empresa deja de ver y de poder cancelar esas reservas sin que falle nada | `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasReservasEmpresa.cs` |
| 4 | MENOR | `ConsultasReservasEmpresa.ObtenerPanelAsync` (RN-19; `fase-16.md` duda 5) | Un profesional inactivo asociado a un servicio activo: `TieneProfesionalActivo` y `TieneAsociacion` tienen que dar `false` | Solo se prueba el servicio inactivo. Si se quita `p.Activo` de cualquiera de los dos indicadores, nada falla y el panel oculta un paso que falta | `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasReservasEmpresa.cs` |
| 5 | MENOR | `InyeccionDependencias` (AGENTS §8; caso 5 de la auditoría 14) | Añadir a las comprobaciones de `Scoped` `IConsultasReservasEmpresa`, `ConsultarReservasEmpresa` y `ConsultarPanelEmpresa`. Siguen faltando también `IConsultasReservaPublica` e `IConsultasReservasCliente` | Si la implementación se registrara como singleton, capturaría el contexto de empresa y el `ContextoDatos` del primer ámbito | `tests/PlataformaReservas.PruebasIntegracion/PruebasInyeccionDependencias.cs` |
| 6 | MENOR | `ConsultarReservasEmpresa` (catálogo: extremos) | `Fecha = DateOnly.MaxValue` (y `MinValue`) en el comando | `DiaLocalEnUtc` llama a `fecha.AddDays(1)`, que lanza `ArgumentOutOfRangeException`, y nada fija si eso tiene que ser `Validacion` o una excepción. La fecha llega de un selector que el cliente puede manipular | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` |
| 7 | MENOR | Pendientes de `fase-14-auditoria-pruebas.md` (casos 3, 4 y 8) | Siguen sin prueba: los datos para reservar con dos empresas publicables, `ObtenerDatosReservaAsync` sin seguimiento y el criterio de CU-03 l.200, marcado sin evidencia | Están asignados a la Fase 17; se repiten para que no se pierdan | Los que indica esa auditoría |

### Pruebas débiles

| # | Prueba | Problema | Mejora sugerida |
|---|---|---|---|
| 1 | **IMPORTANTE.** `PruebasConsultasCatalogoPublico.Los_datos_para_reservar_solo_llevan_servicios_y_profesionales_activos_y_asociados` (débil 1 de la auditoría 14, RN-33) | Sigue abierta. El profesional inactivo solo presta el servicio inactivo, así que si se quita `p.Activo` de `ObtenerDatosReservaAsync` la prueba sigue en verde. Además, la aserción es `NotBe(sinAsociar.Id)`. Está asignada a la Fase 17 | Asociar el profesional inactivo también al servicio activo y comprobar `ContainSingle().Which.Id.Should().Be(…)`. Confirmarlo con el sabotaje |
| 2 | `PruebasConsultarReservasEmpresa.Filtra_por_profesional_y_estado` y `Un_profesional_de_otra_empresa_como_filtro_devuelve_una_lista_vacia` (CU-19) | Ninguna falla si el caso de uso deja de pasar `comando.ProfesionalId` (por ejemplo, si pasa `null`). En la primera, las dos reservas son del mismo profesional. En la segunda, la empresa propia no tiene reservas ese día | En la primera, añadir una reserva de un segundo profesional de la misma empresa. En la segunda, dar a la empresa propia una reserva ese día y comprobar que tampoco aparece |
| 3 | `PruebasPanelEmpresaDto` (las dos pruebas) | Los indicadores se repiten (`false, false, false` y `true, true, true, true`), así que si `ConvertirADto` cruza dos de ellos las dos siguen en verde | Usar una combinación alterna, por ejemplo `(true, false, true, false, true)`, y su inversa |
| 4 | `PruebasConsultasReservasEmpresa.Las_consultas_no_dejan_entidades_en_seguimiento` | Las operaciones proyectan a `FilaReserva` y a `bool`, y el contexto ya usa `NoTracking` por defecto, así que con el código actual no puede fallar | Mantenerla como guarda frente a un cambio que pase a cargar entidades, y anotarlo en §6, sobre todo si se quita el `.AsNoTracking()` que sobra (menor 1 de `fase-16-revision.md`) |

**No verificable (según el revisor):**
- Las páginas `Inicio.razor` y `Reservas.razor` no tienen pruebas automáticas, porque no hay bUnit. Las respalda la verificación manual del desarrollador.
- RN-97 con origen `Empresa`: no hay una prueba de "la empresa cancela y el hueco se vuelve a reservar". Sí la hay con origen cliente y en la baja de cuenta, y el mecanismo es el mismo (estado `Cancelada` más el predicado de la `EXCLUDE`).

### Propuesta

| Fichero | Casos IMPORTANTES | Estimación | Cuándo |
|---|---|---|---|
| `PruebasUnitarias/CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` (nuevo) | Caso 1: "hoy" con `ahora` a las 22:30 UTC y semana que cruza el cambio de hora, con sus dos sabotajes | 0,5 h | Antes del commit de la Fase 16, porque es código de esta fase y el sabotaje lo deja al descubierto; o, como tarde, al principio de la 16b |
| `PruebasUnitarias/Dtos/Empresas/PruebasFichaEmpresaDto.cs` | Caso 2: orden `es-ES` con nombres que empiezan por tilde | 0,25 h | Fase 17, como ya se decidió (`fase-15.md`, duda 10) |
| `PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs` | Débil 1: profesional inactivo asociado al servicio activo, con su sabotaje | 0,25 h | Fase 17, como ya se decidió |

**Total de importantes: 1 h.** Los menores (casos 3 a 7 y débiles 2 a 4, ≈ 1 h) se dejan para la Fase 17, junto con los pendientes de la auditoría 14.

## Seguimiento 2026-10-18 18:19

Pruebas escritas a petición del desarrollador. Cada una se ha visto fallar con un sabotaje y el código está restaurado.

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 430 | 430 | 0 | 0 |
| Integración | 350 | 350 | 0 | 0 |

| Hallazgo | Estado | Prueba | Sabotaje que la hace fallar |
|---|---|---|---|
| Caso 1 (IMPORTANTE) | Resuelto | `PruebasUnitarias/CasosUso/Reservas/PruebasConsultarPanelEmpresa.cs` (nuevo, con el doble `ConsultasReservasEmpresaFalsas`): `Hoy_es_el_dia_local_aunque_en_utc_siga_siendo_el_dia_anterior` y `Las_proximas_acaban_al_final_del_septimo_dia_local_aunque_la_semana_cruce_el_cambio_de_hora` | `DateOnly.FromDateTime(ahora)` en lugar de `ALocal(ahora)`; `hoyHasta.AddDays(7)` en lugar de `DiaLocalEnUtc(hoy + 7)` |
| Caso 2 (IMPORTANTE) | Resuelto | `PruebasFichaEmpresaDto.Ordena_en_espanol_los_nombres_que_empiezan_por_tilde` | `StringComparer.Ordinal` en lugar de `OrdenEspanol.Comparador` |
| Débil 1 (IMPORTANTE) | Resuelto | `PruebasConsultasCatalogoPublico.Los_datos_para_reservar_solo_llevan_servicios_y_profesionales_activos_y_asociados`: el profesional inactivo presta también el servicio activo, y la aserción fija el profesional publicado | Quitar `p.Activo` en `ObtenerDatosReservaAsync` |
| Caso 3 | Resuelto | `PruebasConsultasReservasEmpresa.Las_reservas_de_un_servicio_o_un_profesional_desactivados_siguen_apareciendo_y_son_cancelables` | `join` con `Servicios.Where(x => x.Activo)` |
| Caso 4 | Resuelto | `PruebasConsultasReservasEmpresa.Un_profesional_inactivo_asociado_a_un_servicio_activo_no_cuenta_como_profesional_ni_como_asociacion` | Quitar `p.Activo` de `TieneProfesionalActivo` |
| Caso 5 | Resuelto en lo que es de esta fase | `PruebasInyeccionDependencias`: `IConsultasReservasEmpresa` en `El_contexto_de_empresa_y_los_repositorios_son_scoped`, y `Los_casos_de_uso_del_panel_y_del_listado_de_la_empresa_son_scoped` | No ejecutado |
| Débil 2 | Resuelto | `PruebasConsultarReservasEmpresa.Filtra_por_profesional_y_estado` (cancelada de un segundo profesional) y `Un_profesional_de_otra_empresa_como_filtro…` (la empresa propia tiene una reserva ese día) | El caso de uso pasa `null` en lugar de `comando.ProfesionalId`: fallan las dos |
| Débil 3 | Resuelto | `PruebasPanelEmpresaDto.ConvertirADto_no_cruza_los_indicadores`, con combinaciones alternas | No ejecutado |

**Siguen abiertos, para la Fase 17:**
- **Caso 5, parte anterior a esta fase:** `IConsultasReservaPublica` e `IConsultasReservasCliente` en la comprobación de `Scoped`.
- **Caso 6:** `DateOnly.MaxValue` en el selector. Decidir si debe dar `Validacion` es un cambio de comportamiento, no solo una prueba.
- **Caso 7:** los pendientes de la auditoría 14.
- **Débil 4:** la guarda de seguimiento.
