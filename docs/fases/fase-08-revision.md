# Revisión de la Fase 8

## Revisión 2026-10-10 19:29

**Veredicto:** NO APTO
**Especificación:** docs/fases/fase-08.md (APROBADA)
**Commit base:** d6c92e6
**Ficheros revisados:** 133 de código y pruebas, más 4 `.md` (`AGENTS.md`, `docs/arquitectura.md`, `docs/modelo-dominio.md` y `docs/fases/fase-08.md`)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 2 | 4 | 4 | 2 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 211 | 211 | 0 |
| Integración | 113 | 113 | 0 |

La compilación (`dotnet build`) termina con 0 avisos y 0 errores.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | BLOQUEANTE | arquitectura | `src/PlataformaReservas.Dominio/Repositorios/IRepositorio*.cs:3` (los cinco) | SPEC D-09 y §8.2; AGENTS §2 y §14 | Los contratos de repositorio se han movido a `Dominio/Repositorios`, pero el SPEC, que es el documento de más autoridad, sigue diciendo "Interfaces en `Aplicacion`" (D-09) y sitúa las "interfaces de repositorio" en `Aplicacion` (§8.2). La decisión de la fase actualizó `AGENTS.md`, `arquitectura.md` y `modelo-dominio.md`, pero no el SPEC, así que el código contradice una decisión cerrada. | Reabrir D-09 en el SPEC: cambiar su fila y §8.1/§8.2, con la fecha y el motivo. La alternativa es devolver los contratos a `Aplicacion/Abstracciones`. |
| 2 | BLOQUEANTE | arquitectura | `src/PlataformaReservas.Web/Cuenta/Pages/AltaEmpresa.razor:23-34, 99, 132, 146, 150` | AGENTS §3.3 y §5 | La página es nueva y escrita a mano, pero usa identificadores en inglés: `Input`, `InputModel`, `Password` y `ConfirmPassword`. La excepción de la Fase 6 solo cubre las páginas copiadas de la plantilla. | Renombrarlos (`Entrada`/`ModeloEntrada`, `Clave`, `ConfirmacionClave`) y ajustar `[Compare(...)]` y los `id`/`for` del formulario. |
| 3 | IMPORTANTE | arquitectura | `.editorconfig:17-18` y `src/PlataformaReservas.Web/Cuenta/Pages/AltaEmpresa.razor` | A-18; `arquitectura.md` §3.3 (fila `Web/Cuenta/**`: "Páginas de cuenta copiadas de la plantilla") | `generated_code = true` sobre `Cuenta/**` apaga los analizadores también en `AltaEmpresa.razor`, que no viene de la plantilla. La página nueva se compila sin análisis. | Sacar la página de `Cuenta/` o excluirla del patrón de `.editorconfig`, para que se compile con los analizadores activos. |
| 4 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Empresas/PruebasRegistrarEmpresa.cs:68-81` | §6 de la fase (T7: "fallo a mitad, sin dejar usuario"); RN-15 | La prueba provoca el fallo con una clave corta, que Identity rechaza antes de insertar nada. No ejercita un fallo a mitad: sin la transacción de `RegistrarEmpresa` seguiría en verde. La atomicidad solo queda probada de forma genérica en `PruebasUnidadTrabajo`. | Provocar el fallo después de crear el usuario (por ejemplo, con un choque del índice único o de una clave foránea al guardar la empresa) y comprobar con `FindByEmailAsync` que no queda el usuario. |
| 5 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Infraestructura/Repositorios/RepositorioEmpresas.cs:25` y `src/PlataformaReservas.Infraestructura/Identidad/ServicioCuentas.cs:44` | AGENTS §3.6 y §8; D-11; `arquitectura.md` §8.2 | `ExisteSlugAsync` no filtra por empresa (RN-10), y `RenovarSelloMiembrosAsync` filtra por un `empresaId` que recibe como parámetro. Las dos excepciones están justificadas en §11 de la fase, pero `arquitectura.md` sigue diciendo que `ObtenerPorUsuarioAsync` es "la única excepción". | Anotar las dos excepciones, con su motivo, en `arquitectura.md` y sustituir "la única excepción" por la lista completa. |
| 6 | IMPORTANTE | alcance | `tests/PlataformaReservas.PruebasIntegracion/Consultas/PruebasConsultasCatalogoPublico.cs`, `Identidad/PruebasServicioCuentas.cs`, `Persistencia/PruebasUnidadTrabajo.cs`, `Persistencia/Configuraciones/PruebasConfiguracionServicio.cs`, `PruebasInyeccionDependencias.cs` | §4 de la fase; AGENTS §11 bis | Estos ficheros de pruebas están fuera de la lista literal del radio de `PruebasIntegracion`, que solo nombra `CasosUso/`, `Repositorios/` e `Infraestructura/`. Pero no amplían el alcance: prueban clases que crean o modifican tareas aprobadas (dudas 2, 3, 5 y 8, y T3), y §11 bis obliga a ponerlas en la carpeta espejo. Por eso no se clasifica como bloqueante: el defecto está en §4, que se quedó corto. | Añadir esas carpetas al radio de §4 de la fase, con la fecha y el motivo. |
| 7 | MENOR | seguridad | `src/PlataformaReservas.Infraestructura/Identidad/ServicioCuentas.cs:42-56` (contrato en `src/PlataformaReservas.Aplicacion/Abstracciones/IServicioCuentas.cs:11`) | AGENTS §8 (defensa en profundidad); D-11 | `RenovarSelloMiembrosAsync(empresaId)` no compara `empresaId` con `EmpresaActualId`. Hoy no se puede explotar: su único llamante le pasa una empresa ya filtrada por el contexto. Pero el método no se protege por sí mismo. | Inyectar `IContextoEmpresa` en `ServicioCuentas` y lanzar si `empresaId != EmpresaActualId`, o quitar el parámetro y leer la empresa del contexto. |
| 8 | MENOR | arquitectura | `tests/PlataformaReservas.PruebasUnitarias/Entidades/PruebasReserva.cs` | §5 de la fase (T5: "cancelada o completada lanza") | Para `CancelarPorEmpresa` se prueba el rechazo de una reserva cancelada, pero no el de una completada. El código lo cubre con `Estado != Confirmada`. | Añadir el caso de la reserva completada, o dejar anotado que se probará en la Fase 11 con `Completar`. |
| 9 | MENOR | arquitectura | `tests/PlataformaReservas.PruebasUnitarias/Compartido/PruebasResultado.cs` | AGENTS §11 bis | El fichero prueba dos clases: `Resultado` y `Resultado<T>`. Lo prescribe así §6 de la fase. | Separarlo en `PruebasResultado.cs` y `PruebasResultadoT.cs`, o anotar la excepción en la especificación. |
| 10 | MENOR | arquitectura | `src/PlataformaReservas.Infraestructura/InyeccionDependencias.cs:41-43` | Claridad | `IRelojSistema`, `IUnidadTrabajo` e `IConsultasCatalogoPublico` se registran dentro de `AgregarRepositorios`. | Moverlos a un método con un nombre adecuado, o renombrar el método. |
| — | BLOQUEANTE (falso positivo) | mecánica (C08) | `tests/PlataformaReservas.PruebasIntegracion/Infraestructura/BaseDatosFixture.cs:38` | R-3 | `EnsureCreated no aplica la restriccion EXCLUDE (R-3)`. **Falso positivo:** la línea es un comentario que explica por qué *no* se usa `EnsureCreated`. El esquema se crea con migraciones. | Afinar C08 para que ignore los comentarios. |
| — | IMPORTANTE (falso positivo) | mecánica (C16) | `.editorconfig` | A-18 | `Cambia .editorconfig: cada supresion debe figurar en arquitectura §3.3 (A-18)`. **Falso positivo:** la única supresión nueva (CA1000 en `ResultadoT.cs`) ya figura en `arquitectura.md` §3.3, aprobada por el desarrollador. | Ninguna. La comprobación salta con cualquier cambio en `.editorconfig`. |

**Ficheros fuera del radio de §4, con autorización del desarrollador registrada en §11 de la fase.** No cuentan como hallazgo:

- `.editorconfig`, por la supresión de CA1000.
- `AGENTS.md` §6 y `src/PlataformaReservas.Dominio/Repositorios/`, por el traslado de los contratos de repositorio. Su efecto sobre el SPEC es el hallazgo 1.
- `src/PlataformaReservas.Infraestructura/Persistencia/Conversores/`, los `using` de `ConfiguracionCategoria`, `ConfiguracionEmpresa`, `ConfiguracionProfesional` y `ConfiguracionReserva`, y el de `Identidad/FabricaClaimsUsuario.cs`, por los traslados de los conversores y de los repositorios.

**Observación para el commit:** `.gitignore` contiene `*.md` (línea 5). Por eso `AGENTS.md`, la especificación, este informe y los cambios de `docs/` no aparecen en `git status` y no entrarán en el commit salvo que se añadan con `git add -f`.

### Criterios de aceptación (§7 de la especificación)

- [x] El alta crea usuario, rol, empresa y pertenencia, o nada (RN-15). Evidencia: `PruebasRegistrarEmpresa` y `PruebasUnidadTrabajo`. Ver el hallazgo 4: la prueba del caso de uso no ejercita un fallo a mitad.
- [x] Dos empresas con el mismo nombre obtienen slugs distintos (RN-11). Evidencia: `PruebasRegistrarEmpresa`.
- [x] El propietario aterriza en `/app` con la sesión iniciada (CU-11). Evidencia: prueba manual del 2026-10-10.
- [x] Un usuario que ya tiene empresa no puede crear otra (RN-06). Evidencia: `PruebasRegistrarEmpresa`, por el correo duplicado.
- [x] Una política fuera de rango se rechaza en el dominio (CU-12, RN-29). Evidencia: `PruebasActualizarEmpresa`, `PruebasCrearServicio` y las pruebas unitarias de los objetos de valor.
- [x] Servicios y profesionales se desactivan, nunca se borran (RN-25, RN-34, RN-120). Evidencia: `PruebasDesactivarServicio` y `PruebasDesactivarProfesional`.
- [x] No se puede asociar un servicio de otra empresa (RN-31). Evidencia: `PruebasAsociarServicio`.
- [x] La baja cancela solo las reservas futuras confirmadas y reactivar no las restaura (RN-132, RN-133). Evidencia: `PruebasDarDeBajaEmpresa`, `PruebasReactivarEmpresa` y una prueba de sabotaje.
- [x] Tras la baja, el propietario sigue entrando en `/app` (RN-134). Evidencia: comprobación manual del desarrollador.
- [x] La empresa nunca se borra físicamente (RN-120). Ningún caso de uso ni repositorio borra.
- [x] Ningún componente de `App/` usa tipos de `Infraestructura` ni entidades. Confirmado también por los dos revisores.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta ronda, con 211 pruebas unitarias y 113 de integración.

## Revisión 2026-10-10 20:04

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-08.md (APROBADA)
**Commit base:** d6c92e6
**Ficheros revisados:** 134 de código y pruebas (la misma lista que en la primera ronda), más 5 `.md`: `AGENTS.md`, `docs/TFM_PlataformaReservas_SPEC.md`, `docs/arquitectura.md`, `docs/modelo-dominio.md` y `docs/fases/fase-08.md`

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 4 | 1 | 2 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 211 | 211 | 0 |
| Integración | 115 | 115 | 0 |

`dotnet build`: 0 avisos y 0 errores.

**Cómo quedan los hallazgos de la ronda anterior:**

| Hallazgo | Estado |
|---|---|
| 1 (D-09) | Resuelto: D-09 y SPEC §8.1/§8.2 alineados. |
| 2 (identificadores en inglés) | Resuelto: excepción de plantilla autorizada en `AGENTS.md` §5. |
| 3 (analizadores) | Cerrado como falso positivo: los `.razor` no se analizan en ningún caso. Anotado en `arquitectura.md` §3.3. |
| 4 (RN-15) | Resuelto: hay una prueba de fallo a mitad, con prueba de sabotaje. |
| 5 y 7 (excepciones al filtro por empresa) | Resueltos: documentados en `arquitectura.md` §8.2 y blindado `RenovarSelloMiembrosAsync`. |
| 6 (radio de cambio) | Resuelto: §4 de la fase ampliado. |
| 8 y 9 (menores) | No se corrigieron. Esta ronda los vuelve a señalar, ahora como importantes 3 y 4. |
| 10 (menor) | No se corrigió y esta ronda no lo vuelve a señalar. |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Aplicacion/CasosUso/Empresas/DarDeBajaEmpresa.cs:16`, `ReactivarEmpresa.cs:14` | fase-08 §5 T8 ("crea los tres casos de uso con sus comandos"); `arquitectura.md` §5.2; AGENTS §8 bis | `EjecutarAsync(CancellationToken ct)` no recibe comando: no existen `DarDeBajaEmpresaComando` ni `ReactivarEmpresaComando`. | Crear los dos `record`, aunque estén vacíos. Otra opción: anotar en fase-08 §11 y en `arquitectura.md` §5.2 que un caso de uso sin datos de entrada no lleva comando. |
| 2 | IMPORTANTE | arquitectura | `src/PlataformaReservas.Web/Components/App/Inicio.razor:1` | SPEC D-01 y §11 (área de empresa en `InteractiveServer`); AGENTS §4 | `/app` no declara `@rendermode InteractiveServer` y se sirve con SSR estático. Las otras tres páginas de `App/` sí lo declaran. | Añadir `@rendermode InteractiveServer`, o anotar en la duda 9 que la página mínima es SSR hasta la Fase 16. |
| 3 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasUnitarias/Compartido/PruebasResultado.cs:6` | AGENTS §11 bis | Un mismo fichero prueba `Resultado` y `Resultado<T>`, que son dos clases. fase-08 §6 nombra un solo fichero, así que la especificación y AGENTS no coinciden. Es el menor 9 de la ronda anterior. | Separar las pruebas en `PruebasResultadoT.cs`, o anotar la decisión en la especificación. |
| 4 | IMPORTANTE | arquitectura | `tests/PlataformaReservas.PruebasUnitarias/Entidades/PruebasReserva.cs` (`CancelarPorEmpresa_*`) | fase-08 §5 T5, verificación ("cancelada o completada lanza") | No hay prueba para la reserva completada: `Completar` es de la Fase 11 y desde el dominio todavía no se puede construir una. Es el menor 8 de la ronda anterior. | Anotar en fase-08 §11 que esa parte pasa a la Fase 11, o cubrirla en integración marcando la reserva como completada por SQL, como ya hace `PruebasDarDeBajaEmpresa`. |
| 5 | MENOR | arquitectura | `src/PlataformaReservas.Web/Components/App/Configuracion.razor:77-85`, `Servicios.razor:83-91` | `arquitectura.md` §7 | Las etiquetas escriben los rangos a mano ("de 1 a 480"…). Si cambia un rango, también hay que tocar estas pantallas. | Quitar los rangos del texto, o tomarlos de las constantes de los objetos de valor a través de un DTO. |
| — | BLOQUEANTE (falso positivo) | mecánica (C08) | `tests/PlataformaReservas.PruebasIntegracion/Infraestructura/BaseDatosFixture.cs:38` | R-3 | `EnsureCreated no aplica la restriccion EXCLUDE (R-3)`. **Falso positivo:** la línea es un comentario que explica por qué no se usa `EnsureCreated`. | Afinar C08 para que ignore los comentarios. |
| — | IMPORTANTE (falso positivo) | mecánica (C16) | `.editorconfig` | A-18 | `Cambia .editorconfig: cada supresion debe figurar en arquitectura §3.3 (A-18)`. **Falso positivo:** la única supresión nueva (CA1000) figura en §3.3. | Ninguna. |

El revisor de seguridad no encuentra nada en esta ronda. Señala, para la Fase 19, que `Login.razor:80` es anterior a esta fase y sigue con `lockoutOnFailure: false` (RN-169).

**Radio de cambio:** todos los ficheros cambiados están dentro del §4 ampliado o tienen la autorización del desarrollador registrada en §11 (`.editorconfig`, `AGENTS.md`, el SPEC, `Dominio/Repositorios`, `Persistencia/Conversores` y los `using` de las configuraciones y de `FabricaClaimsUsuario`).

### Criterios de aceptación (§7 de la especificación)

- [x] El alta crea usuario, rol, empresa y pertenencia, o nada (RN-15). Evidencia: `PruebasRegistrarEmpresa.Un_fallo_al_guardar_la_empresa_deshace_la_cuenta_creada`, con prueba de sabotaje, y `PruebasUnidadTrabajo`.
- [x] Dos empresas con el mismo nombre obtienen slugs distintos (RN-11). Evidencia: `PruebasRegistrarEmpresa`.
- [x] El propietario aterriza en `/app` con la sesión iniciada (CU-11). Evidencia: prueba manual del 2026-10-10, repetida tras las correcciones.
- [x] Un usuario que ya tiene empresa no puede crear otra (RN-06). Evidencia: `PruebasRegistrarEmpresa`.
- [x] Una política fuera de rango se rechaza en el dominio (CU-12, RN-29). Evidencia: `PruebasActualizarEmpresa`, `PruebasCrearServicio` y las pruebas unitarias de los objetos de valor.
- [x] Servicios y profesionales se desactivan, nunca se borran (RN-25, RN-34, RN-120). Evidencia: `PruebasDesactivarServicio` y `PruebasDesactivarProfesional`.
- [x] No se puede asociar un servicio de otra empresa (RN-31). Evidencia: `PruebasAsociarServicio`.
- [x] La baja cancela solo las reservas futuras confirmadas y reactivar no las restaura (RN-132, RN-133). Evidencia: `PruebasDarDeBajaEmpresa` y `PruebasReactivarEmpresa`.
- [x] Tras la baja, el propietario sigue entrando en `/app` (RN-134). Evidencia: comprobación manual del desarrollador.
- [x] La empresa nunca se borra físicamente (RN-120).
- [x] Ningún componente de `App/` usa tipos de `Infraestructura` ni entidades. Confirmado por los dos revisores.
- [x] `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta ronda, con 211 pruebas unitarias y 115 de integración.

## Revisión 2026-10-10 20:22

**Veredicto:** APTO
**Especificación:** docs/fases/fase-08.md (APROBADA)
**Commit base:** bd71111
**Ficheros revisados:** 7 de código y pruebas. A ellos se suman los ficheros ignorados por `.gitignore` que también cambian: `AGENTS.md`, `docs/arquitectura.md`, `docs/fases/fase-08.md`, `docs/fases/fase-09.md` y `.claude/skills/revisar/comprobaciones.sh`.

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 1 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 211 | 211 | 0 |
| Integración | 115 | 115 | 0 |

`dotnet build`: 0 avisos y 0 errores. `comprobaciones.sh`: sin hallazgos.

**Estado de los hallazgos de la ronda anterior (20:04):**

| Hallazgo | Estado |
|---|---|
| Importante 1 (comandos) | Resuelto por documentación: la regla "un caso de uso sin datos de entrada no lleva comando" queda en AGENTS §8 bis y en `arquitectura.md` §5.2 |
| Importante 2 (`/app`) | Resuelto: `@rendermode InteractiveServer` en `Inicio.razor` |
| Importante 3 (un fichero, dos clases) | Resuelto: `PruebasResultado.cs` y `PruebasResultadoT.cs` |
| Importante 4 (reserva completada) | Aplazado a la Fase 11 por decisión del desarrollador, anotado en `fase-08.md` §11 y en `fase-09.md` §9 |
| Menor 5 (rangos en las etiquetas) | Resuelto: las etiquetas leen las constantes `Minimo` y `Maximo` |
| Menor 10 de la primera ronda (`AgregarRepositorios`) | Resuelto: `AgregarPuertosAplicacion` |
| Falsos positivos C08 y C16 | C08 se corrige en el script y ya ignora los comentarios. C16 deja de salir porque `.editorconfig` ya está en el commit `547f191` |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | `src/PlataformaReservas.Web/Components/App/_Imports.razor:9` | `arquitectura.md` §2.1 y §5.2 (observación, sin una regla explícita que lo prohíba) | `@using PlataformaReservas.Dominio.ValueObjects` se aplica a todos los componentes de `App/`, pero solo `Configuracion.razor` y `Servicios.razor` usan las constantes. Con ese `using`, cualquier página podría construir objetos de valor sin pasar por un caso de uso. | Mover el `@using` a `Configuracion.razor` y `Servicios.razor`, o escribir el nombre completo del tipo en las seis etiquetas. |

El revisor de seguridad no encuentra nada. Que `/app` funcione en modo interactivo con la CSP se comprobará a mano en la Fase 19. En el smoke de esta ronda, `/app` respondió 200 con el marcador interactivo de Blazor.

**Radio de cambio:** todos los ficheros están dentro del §4 de la fase o son correcciones de la revisión anotadas en §11.

### Criterios de aceptación (§7 de la especificación)

Sin cambios respecto a la ronda anterior: los 12 tienen evidencia. Esta ronda vuelve a pasar `dotnet build` sin avisos y `dotnet test` con 211 pruebas unitarias y 115 de integración.

## Seguimiento 2026-10-12

No es una ronda de revisión: no se ha vuelto a compilar ni a ejecutar las pruebas. Solo registra en qué estado están los hallazgos que la última ronda dejó abiertos, comprobado contra el código el 2026-10-12.

| Hallazgo | Estado | Evidencia |
|---|---|---|
| Menor 1 de la ronda 20:22 (`@using PlataformaReservas.Dominio.ValueObjects` en `App/_Imports.razor`) | **Abierto**: se cierra en la Fase 19 (refactorización) | `src/PlataformaReservas.Web/Components/App/_Imports.razor:10` |
| Importante 4 de la ronda 20:04 (rechazo de `CancelarPorEmpresa` sobre una reserva completada) | **Aplazado a la Fase 11** | Decisión del desarrollador, anotada en `fase-08.md` §11 y `fase-09.md` §9. Se cierra cuando exista `Completar` |
