# Auditoría de pruebas — Fase 14

> **Auditoría fuera de calendario.** `AGENTS.md` §11 bis la prevé solo al cerrar las fases 10, 11, 16 y 17. Esta se hace porque el desarrollador la ha pedido.

## Auditoría 2026-10-17 19:54

**Alcance:** el código construido hasta la Fase 14. El de la Fase 14 está todavía sin commit.
**Commit base:** 5c0f177

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 379 | 379 | 0 | 0 |
| Integración | 279 | 279 | 0 | 0 |

| Importantes | Menores |
|---|---|
| 3 | 7 |

### Clases sin fichero de pruebas

| Clase | Motivo, si no necesita prueba propia |
|---|---|
| `Dominio/Compartido/DominioException`, `EntidadBase` | Sin lógica propia; se ejercitan en todas las pruebas de entidades |
| `Dominio/Compartido/Validacion` | Es interna; la cubren las pruebas de las entidades que la usan (los instantes UTC y los identificadores) |
| `Dominio/Entidades/Horario`, `ExcepcionHorario`, `ProfesionalServicio` | Son hijas del agregado `Profesional`: se crean y modifican a través de él y se prueban en `PruebasProfesional` |
| `Aplicacion/CasosUso/*/DatosEmpresa`, `DatosProfesional`, `DatosServicio` | Ayudantes internos de traducción del comando (`Desde`); los cubren las pruebas de validación de los casos de uso que los usan |
| `Aplicacion/Compartido/CodigoError`, `HuecoOcupadoException` | La enumeración no tiene comportamiento. La excepción la cubren `PruebasUnidadTrabajo` y `PruebasCrearReserva` |
| `Aplicacion/Disponibilidad/HuecoDisponible`, `IMotorDisponibilidad` | Son un registro y un contrato; los cubre `PruebasMotorDisponibilidad` |
| `Infraestructura/Identidad/ClaimsSesion` | Clase estática interna; la cubren `PruebasContextoEmpresa` y `PruebasContextoUsuario` |
| `Infraestructura/Persistencia/Conversores/*` (16) | Los cubren las pruebas de `Persistencia/Configuraciones/` (ida y vuelta por la base de datos) y las de cifrado de `PruebasConfiguracionReserva` |
| `Infraestructura/Persistencia/DatosIniciales/SembradorDatos` | Prueba transversal `PruebasDatosIniciales` |
| `Infraestructura/RelojSistema` | Devuelve `DateTime.UtcNow`; en las pruebas se sustituye por `RelojFijo` |

Las demás clases con comportamiento tienen su `Pruebas<Clase>.cs`, en la misma carpeta relativa que la clase.

### Casos que faltan

| # | Severidad | Clase o regla | Caso que falta | Por qué importa | Fichero donde iría |
|---|---|---|---|---|---|
| 1 | IMPORTANTE | Caso de uso `EstablecerTelefono` (catálogo de AGENTS §11 bis: conflicto y recurso inexistente; CU-07 2a) | Ninguna prueba cubre dos situaciones: <br>• el usuario de la sesión ya tiene teléfono: hoy la `DominioException` de `Usuario.EstablecerTelefono` sale del caso de uso sin convertirse en `Resultado`; <br>• el usuario de la sesión no existe (`InvalidOperationException`). <br>La tabla de omisiones de §6 de fase-14 no los menciona | Es la frontera entre capas. Nada fija qué recibe la página si el perfil ya tiene teléfono (por ejemplo, con dos pestañas): hoy es una excepción sin tratar dentro del circuito, y podría cambiar sin que ninguna prueba falle. Hay que fijar el comportamiento con una prueba, o anotar la omisión con su motivo | `PruebasIntegracion/CasosUso/Cuenta/PruebasEstablecerTelefono.cs` |
| 2 | IMPORTANTE | Regresión: hallazgo 6 de fase-13-revision (orden `es-ES` fijo en la ficha), dado por corregido | Un caso en el que la tilde cambie el orden de la ficha (por ejemplo, "Álvaro" frente a "Zoe") en servicios y profesionales. La prueba actual usa "Ana Garcia" frente a "Luis Perez" y "Corte" frente a "Tinte", que salen igual con orden ordinal | Si se vuelve al orden ordinal o se quita el comparador fijo, la prueba sigue en verde: la corrección no está fijada. `ServicioReservaDto` sí la fija, con "Ángel" | `PruebasUnitarias/Dtos/Empresas/PruebasFichaEmpresaDto.cs` |
| 3 | MENOR | `ConsultasCatalogoPublico.ObtenerDatosReservaAsync` (RN-04, RN-109) | Dos empresas publicables a la vez: los datos para reservar de A no llevan servicios ni profesionales de B | No hay `Respawn` y la base de datos es compartida. Si se quita `s.EmpresaId == empresa.Id`, que falle el `ContainSingle` depende de qué pruebas se hayan ejecutado antes | `PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs` |
| 4 | MENOR | `ConsultasCatalogoPublico` (catálogo: lectura sin seguimiento; A-05) | Que `ObtenerDatosReservaAsync` no deje entidades en seguimiento, como `Las_consultas_no_dejan_entidades_en_seguimiento` en `PruebasConsultasDisponibilidad` | Es la primera vez que este puerto se usa dentro de un circuito `InteractiveServer`, donde el contexto vive mucho tiempo. Un `AsTracking()` no haría fallar nada | `PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs` |
| 5 | MENOR | `InyeccionDependencias` | `IConsultasReservaPublica`, `ConsultarDisponibilidad` y `EstablecerTelefono` no están en las comprobaciones de ciclo de vida `Scoped` | Si `IConsultasReservaPublica` se registrara como singleton, capturaría el `ContextoDatos` | `PruebasIntegracion/PruebasInyeccionDependencias.cs` |
| 6 | MENOR | `MotorDisponibilidad.ALocal` (RN-102, R-2) | `ALocal` con un `DateTime` de `Kind` `Local` o `Unspecified`. Hoy `SpecifyKind` lo reinterpreta como UTC sin avisar | Es un método público nuevo que usa la página, y nada fija qué hace con una entrada que no es UTC | `PruebasUnitarias/Disponibilidad/PruebasMotorDisponibilidad.cs` |
| 7 | MENOR | `CrearReserva` (hallazgo menor 2 de fase-11-revision, sin corregir) | `Una_empresa_inexistente_da_validacion`. La T6 de la Fase 11 pide "inactiva o inexistente", y solo se prueba la inactiva | `CrearReserva` es ahora el destino del flujo público | `PruebasIntegracion/CasosUso/Reservas/PruebasCrearReserva.cs` |
| 8 | MENOR | Criterio de CU-03 en `casos-uso.md` l.200 | "Dos consultas simultáneas del mismo hueco lo muestran disponible a ambas" está marcado como cumplido sin prueba ni comprobación manual anotada | Es un criterio marcado como cumplido sin evidencia. Basta con dos `ConsultarDisponibilidad` sobre el mismo hueco, o con anotar el motivo | `PruebasIntegracion/CasosUso/Reservas/PruebasConsultarDisponibilidad.cs` |

### Pruebas débiles

| # | Prueba | Problema | Mejora sugerida |
|---|---|---|---|
| 1 | **IMPORTANTE.** `PruebasConsultasCatalogoPublico.Los_datos_para_reservar_solo_llevan_servicios_y_profesionales_activos_y_asociados` (§6 de fase-14 le atribuye RN-33) | El profesional inactivo solo presta el servicio inactivo, que ya se descarta por `s.Activo`, así que nunca podría aparecer. Si se quita `p.Activo` de la consulta, la prueba sigue en verde. Además, la aserción final es `NotBe(sinAsociar.Id)` en lugar de comprobar que el profesional es el esperado | Asociar también el profesional inactivo al servicio activo y comprobar `ContainSingle().Which.Id.Should().Be(<profesional publicable>)`. Confirmarlo con el sabotaje de quitar `p.Activo` |
| 2 | MENOR. `PruebasDatosReservaDto.ConvertirADto_ordena_los_servicios_y_resuelve_la_ventana_efectiva_de_cada_uno` | "Corte" frente a "Tinte" sale igual con orden ordinal, así que la prueba no fija el comparador `es-ES` | Usar un servicio con tilde inicial (por ejemplo, "Ácido" frente a "Corte") |

**Lo que el revisor no pudo verificar:**
- **Pruebas y sabotajes:** no los ejecutó; los resultados son los de esta sesión y los anotados en §6 de fase-14.
- **Fases 0 a 13:** no las auditó enteras, porque las cubrieron las auditorías de las fases 10 y 11. Contrastó los hallazgos corregidos de las revisiones de las fases 10 a 13 con las pruebas que existen; salvo el caso 2, todos tienen prueba.
- **`FlujoReserva.razor`:** no tiene pruebas automáticas (no hay bUnit). Su recorrido interactivo sigue pendiente en el navegador, según §6 y §7 de fase-14.

### Propuesta

| Fichero | Casos IMPORTANTES | Estimación |
|---|---|---|
| `PruebasIntegracion/CasosUso/Cuenta/PruebasEstablecerTelefono.cs` | Caso 1. Antes hay que decidir qué hace el caso de uso si el usuario ya tiene teléfono: convertirlo en `Fallo(Conflicto)` o dejar que lance y anotarlo como omisión | 0,25 h |
| `PruebasUnitarias/Dtos/Empresas/PruebasFichaEmpresaDto.cs` | Caso 2 | 0,1 h |
| `PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs` | Prueba débil 1, con su sabotaje | 0,15 h |

**Total de los IMPORTANTES: ≈ 0,5 h.** Pertenecen a la Fase 14, que aún no tiene commit, así que lo natural es cerrarlos antes de `/revisar 14`.

Los MENORES (casos 3 a 8 y la prueba débil 2, ≈ 0,75 h) pueden ir con ellos si hay margen o, si no, quedarse para la Fase 17. El caso 7 arrastra un menor de la Fase 11 que `PROJECT_PLAN.md` §6 ya asigna a la Fase 19.
