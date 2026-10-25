# Auditoría de pruebas de la Fase 17

## Auditoría 2026-10-24 18:26

**Alcance:** código construido hasta la Fase 17. La Fase 17 está implementada y pendiente de commit.
**Commit base:** f1f6ed6

| Suite | Ejecutadas | Superadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 448 | 448 | 0 | 0 |
| Integración | 421 | 421 | 0 | 0 |

| Importantes | Menores |
|---|---|
| 1 | 8 |

`PROJECT_PLAN.md` §7 sigue desactualizada (2026-10-12). Según el historial de commits, están cerradas de la 0 a la 16b. Las fases anteriores ya se auditaron en las fases 10, 11, 14 y 16; esta se centra en lo que cambia la 17 y en lo que quedaba abierto.

### Clases sin fichero de pruebas

| Clase | Motivo, si no necesita prueba propia |
|---|---|
| `Infraestructura/Consultas/CondicionPublicable` | La cubren `PruebasConsultasReservasEmpresa`, `PruebasConsultasNotificacion` y `PruebasConsultasCatalogoPublico` (sabotaje de la Fase 16) |
| `Infraestructura/Correo/ServicioEmailSmtp`, `CorreoNoEnviadoException` | Sin servidor SMTP ni credenciales en las pruebas (`fase-16b.md` §6). Verificación manual con Mailtrap |
| `Infraestructura/Correo/Plantillas/FormatoCorreo` | La cubre `PruebasRenderizadorPlantillas` (hora local, cambio de día en invierno) |
| `Infraestructura/Persistencia/DatosIniciales/SembradorDatos` | Lo cubre `PruebasDatosIniciales`, transversal de la lista cerrada |
| `Aplicacion/Abstracciones/MensajeCorreo`, `Aplicacion/Disponibilidad/HuecoDisponible` | Records sin lógica |
| `Aplicacion/CasosUso/*/Datos*` | Auxiliares de traducción de comandos, cubiertos por las pruebas de validación de sus casos de uso |
| `Aplicacion/Compartido/OrdenEspanol` | La cubren `PruebasFichaEmpresaDto` y `PruebasDatosReservaDto` (nombres con tilde inicial) |
| `Aplicacion/Compartido/CodigoError`, `HuecoOcupadoException`; `Dominio/Compartido/DominioException`, `EntidadBase`, `Validacion` | Sin lógica propia, o cubiertos por las pruebas de entidades y objetos de valor |
| `Dominio/Entidades/Horario`, `ExcepcionHorario`, `ProfesionalServicio` | Hijas del agregado `Profesional` (`PruebasProfesional`) |
| `Dominio/Enumeraciones/*`, `Dominio/Eventos/*` | Enumeraciones y records sin lógica; los eventos los cubren las pruebas de entidades y casos de uso |
| `Infraestructura/Identidad/ClaimsSesion` | Auxiliar `internal` de los contextos, cubierto por `PruebasContextoEmpresa` y `PruebasContextoUsuario` |
| `Infraestructura/Persistencia/Conversores/*` (16) | Los cubren `Persistencia/Configuraciones/PruebasConfiguracion*` y `PruebasCifradoEnReposo` (`ConversorCifrado`, sabotaje de la Fase 17) |
| `Infraestructura/RelojSistema` | Devuelve `DateTime.UtcNow`; en las pruebas se usa `RelojFijo` |
| `Web/Cuenta/*`, `Web/Program.cs` | Plantilla de Identity y arranque. La autorización la cubre `PruebasAutorizacionRutas` (reflexión); el resto, de forma manual |

### Casos que faltan

| # | Severidad | Clase o regla | Caso que falta | Por qué importa | Fichero donde iría |
|---|---|---|---|---|---|
| 1 | IMPORTANTE | SPEC §16.2, RN-02 ⚑, RN-05 (`PruebasAutorizacionRutas`) | Ninguna ruta fuera de `RutasPublicas` lleva `[AllowAnonymous]` | La prueba solo lee `AuthorizeAttribute`. Una página de `/app` con `@attribute [AllowAnonymous]` sigue heredando `[Authorize(Policy = "EsPropietario")]` de `_Imports.razor`, pero `[AllowAnonymous]` la anula en tiempo de ejecución. Las pruebas seguirían en verde con el área abierta | `tests/PlataformaReservas.PruebasIntegracion/Autorizacion/PruebasAutorizacionRutas.cs` |
| 2 | MENOR | SPEC l.873, `fase-17.md` T5 (`SembradorDatos`) | Cada propietario de demostración es miembro de **su** empresa, y su clave funciona | La prueba solo cuenta 2 miembros. Dos propietarios cruzados o unidos a la misma empresa seguirían dando 2 | `tests/PlataformaReservas.PruebasIntegracion/Persistencia/PruebasDatosIniciales.cs` |
| 3 | MENOR | `SembradorDatos.MigrarYSembrarAsync` | Que, habilitado y con clave, siembre las empresas de demostración | La prueba llama directamente a `SembrarDemostracionAsync`. Si se borra la llamada del arranque, deja de sembrar sin que nada falle | `tests/PlataformaReservas.PruebasIntegracion/Persistencia/PruebasDatosIniciales.cs` |
| 4 | MENOR | `ConsultarReservasEmpresa` (duda 4; AGENTS §11 bis, "valores contiguos") | `DateOnly.MaxValue.AddDays(-1)` y `DateOnly.MinValue.AddDays(1)` dan éxito | Solo se prueban los extremos. Una guarda demasiado amplia rechazaría fechas válidas sin que nada falle | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasConsultarReservasEmpresa.cs` |
| 5 | MENOR | CU-16 (`casos-uso.md` l.391), pendiente de las fases 10 y 13 | Integración: `AgregarIntervalo` y después `ConsultarDisponibilidad` devuelve huecos de ese profesional ese día | El criterio sigue sin marcar y sin evidencia: solo lo cubre el motor con dobles en memoria | `tests/PlataformaReservas.PruebasIntegracion/CasosUso/Horarios/PruebasAgregarIntervalo.cs` |
| 6 | MENOR | SPEC §16.2; `fase-17.md` §7 y T2 | La comprobación manual con `curl` (302 a `login` en `/app/*` y `/my-account/*` sin sesión) | **El criterio de §7 está marcado aunque el `curl` sigue pendiente.** La reflexión prueba los atributos, no que la canalización los aplique (por ejemplo, que `Routes.razor` siga usando `AuthorizeRouteView`) | Anotación manual en `docs/fases/fase-17.md` §7 y en `docs/pruebas.md` |

### Pruebas débiles

| # | Prueba | Problema | Mejora sugerida |
|---|---|---|---|
| 1 | `PruebasCifradoEnReposo.Con_el_cifrado_activo_funcionan_la_busqueda_por_correo…` (RN-166) | `duplicado.Succeeded.Should().BeFalse()` no comprueba el motivo. Un fallo por otra causa dejaría la prueba en verde aunque la unicidad por HMAC estuviera rota | `duplicado.Errors.Should().Contain(e => e.Code == "DuplicateEmail")` |
| 2 | `PruebasConsultarDisponibilidad.Dos_consultas_simultaneas_del_mismo_hueco_lo_muestran_disponible_a_ambas` (CU-03 l.200) | Con el código actual no puede fallar: dos lecturas sin escrituras siempre coinciden | Mantenerla como guarda frente a una consulta que bloquee o reserve, y anotarla en `docs/pruebas.md` |
| 3 | MENOR, redundancia: `PruebasEstablecerTelefono.Si_el_usuario_ya_tiene_telefono_da_conflicto_y_no_lo_cambia` frente a `Si_ya_tiene_telefono_da_conflicto_sin_llegar_a_escribir`, y la línea del propietario con claim en la matriz nueva de políticas | Mismo escenario y mismo resultado que otra prueba ya existente | Fundir o quitar la antigua |

**Pendientes anteriores ya cerrados:**
- **Auditoría 14:** casos 1-6 y 8, y prueba débil 2. El caso 7 tiene omisión anotada (Fase 19).
- **Auditoría 16:** casos 5, 6 y 7. La prueba débil 4 está anotada.
- **`/revisar 16b`:** hallazgos 1-4 y menor 6.

**No verificable (según el revisor):**
- Que `[AllowAnonymous]` anule la política en las rutas de componentes de esta versión de .NET. Es el comportamiento documentado de ASP.NET Core.
- El token de CU-24 (RN-159) y el `curl`, que quedan para el desarrollador.

### Propuesta

| Fichero | Casos | Estimación | Cuándo |
|---|---|---|---|
| `Autorizacion/PruebasAutorizacionRutas.cs` | Importante 1: ninguna ruta privada con `[AllowAnonymous]`, con sabotaje | 0,25 h | Antes del commit de la Fase 17: es un hueco de la autorización recién añadida |
| `docs/fases/fase-17.md` §7, `docs/pruebas.md` | Menor 6: desmarcar el criterio de §16.2 hasta que se haga el `curl`, o anotar que es parcial | 0,1 h | Antes del commit |
| `Persistencia/PruebasDatosIniciales.cs`, `PruebasConsultarReservasEmpresa.cs`, `PruebasAgregarIntervalo.cs`, `PruebasCifradoEnReposo.cs` | Menores 2-5 y débil 1 | ≈ 0,75 h | Con la Fase 17 si hay margen; si no, en la refactorización de la Fase 19 |

## Seguimiento 2026-10-24: corrección de los hallazgos

Corregidos a petición del desarrollador. No es una auditoría nueva.

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 (IMPORTANTE) | `[AllowAnonymous]` no se detectaba | Resuelto | `PruebasAutorizacionRutas.Ninguna_ruta_fuera_de_las_publicas_declaradas_admite_anonimos`. Sabotaje: `[AllowAnonymous]` en `App/Configuracion.razor` la hace fallar |
| 2 | Propietarios de demostración sin comprobar su empresa ni su clave | Resuelto | `PruebasDatosIniciales`: cada propietario es miembro solo de su empresa (por slug), su clave funciona y tiene el rol |
| 3 | `MigrarYSembrarAsync` sin prueba | Resuelto | La siembra con clave pasa por `MigrarYSembrarAsync` con un contenedor de `BaseDatosFixture.CrearServicios` (configuración adicional, mismas claves de cifrado). Sabotaje: quitar la llamada del arranque la hace fallar |
| 4 | Valores contiguos de `DateOnly` | Resuelto | `PruebasConsultarReservasEmpresa.La_fecha_contigua_al_extremo_del_calendario_se_consulta_sin_error`. Sabotaje: ampliar la guarda un día la hace fallar |
| 5 | CU-16 sin integración con CU-03 | Resuelto | `PruebasAgregarIntervalo.Tras_guardar_el_intervalo_sus_huecos_aparecen_en_la_disponibilidad_de_ese_profesional`. Criterio de `casos-uso.md` marcado |
| 6 | Criterio de §16.2 marcado sin el `curl` | Resuelto en el documento | `fase-17.md` §7 lo desmarca como parcial y `docs/pruebas.md` §5 deja el `curl` como pendiente del desarrollador |
| Débil 1 | El duplicado no comprobaba el motivo | Resuelto | Comprueba el código `DuplicateEmail` |
| Débil 2 | Consultas simultáneas que no pueden fallar | Anotada | `docs/pruebas.md` §5, como guarda |
| Débil 3 | Redundancias | Resuelto | `Si_ya_tiene_telefono_da_conflicto_sin_llegar_a_escribir` comprueba también que el teléfono no cambia, y se quita la prueba antigua. Se quita de la matriz de políticas la línea repetida del propietario con claim |

**Verificación:** `dotnet build` sin avisos; unitarias 448/448; integración 437/437; `comprobaciones.sh` 0 bloqueantes, 0 importantes.
