# Revisión de la Fase 15

## Revisión 2026-10-18 16:51

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-15.md (APROBADA)
**Commit base:** 99a8735
**Ficheros revisados:** 43 (más la documentación de `docs/`, ignorada por git)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 1 | 5 | 1 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 391 | 391 | 0 |
| Integración | 316 | 316 | 0 |

`dotnet build`: 0 avisos, 0 errores.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE (**falso positivo**) | mecánica (C13) | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasUsuario.cs:74 | RN-167, R-10 | LINQ sobre Users: los campos cifrados devuelven vacío sin error. **Falso positivo:** es la prueba de RN-167, que hace la consulta LINQ a propósito para comprobar que devuelve vacío (`porLinq.Should().BeEmpty()`). La línea viene de la Fase 6 (`PruebasCifradoIdentidad`, a0b6f817); la Fase 15 solo añade pruebas al fichero. | Ninguna en el código. Conviene que `comprobaciones.sh` excluya esta prueba de C13. |
| 2 | IMPORTANTE | arquitectura | docs/fases/fase-15.md:231-239 (§6, tabla de omisiones) | AGENTS.md §11 bis (catálogo, fila "Caso de uso") | Faltan casos del catálogo y §6 no da motivo para omitirlos. **Validación:** falta en `ConsultarMisReservas`, `ConsultarDetalleReserva` y `DarDeBajaCuenta`. **Recurso inexistente:** falta en `ConsultarMisReservas` y en `DarDeBajaCuenta`. En este último, un usuario de la sesión que no existe hace que `ServicioCuentas.ObtenerDeLaSesionAsync` lance `InvalidOperationException`. | Una de las dos: (a) añadir los casos a la tabla de omisiones de §6, con el motivo de cada uno (no hay comando, o el comando es solo un `Guid`; el usuario sale del claim); (b) añadir las pruebas, por ejemplo una en la que el usuario de la sesión no existe y el caso de uso lanza, como en `PruebasEstablecerTelefono`. |
| 3 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/CasosUso/Cuenta/DarDeBajaCuenta.cs:43-53 | `docs/arquitectura.md` §6 (l.465) | "Cuenta ya dada de baja" (`EstadoNoValido`) se descubre después de cancelar las reservas y de `GuardarCambiosAsync`. El rollback lo deshace en la base de datos, pero el `ContextoDatos` del circuito conserva en el rastreador las reservas marcadas como `Cancelada`. El orden está anotado en "Decisiones de implementación". | Comprobar `Activo` antes de abrir la transacción. `DarDeBajaAsync` queda como el paso que puede fallar técnicamente, y la prueba 8a sigue siendo válida. |
| 4 | MENOR | arquitectura | src/PlataformaReservas.Web/Components/MiCuenta/Perfil.razor:117 | AGENTS.md §6; RN-137 | La página filtra el recuento de la baja con `Proximas.Count(r => r.Estado == Confirmada)`. `Proximas` usa `InicioUtc >= ahora` y la cascada usa `> ahora`, así que una reserva que empieza justo en ese instante se cuenta y no se cancela. | Exponer el recuento en `MisReservasDto` con la misma condición que la cascada, para que la página solo lo pinte. |
| 5 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/Dtos/Reservas/DetalleReservaClienteDto.cs:51 | A-09 (`docs/arquitectura.md` §7) | `Cancelable` repite por tercera vez la precondición de `CancelarPorCliente`, que ya está en `Reserva` y en `CancelarReservaCliente`. La decisión está anotada en §11. | Se puede dejar como está. Para tener un único punto, `Reserva` expondría la condición y el DTO la consumiría. |
| 6 | MENOR | arquitectura | src/PlataformaReservas.Web/Components/MiCuenta/Perfil.razor:4-5, 91-92 | A-01 (`docs/arquitectura.md` §5.1) | La página lee el perfil con `IServicioCuentas.ObtenerContactoAsync`, sin pasar por un caso de uso. Es el mismo patrón que `FlujoReserva.razor` en la Fase 14. | Ninguna obligatoria en esta fase. Para unificarlo, un caso de uso de lectura del perfil. |
| 7 | MENOR | alcance | docs/fases/fase-15.md §6 (verificación manual, último punto) | AGENTS.md §11 bis | Falta marcar la comprobación en el navegador de los enlaces "Mis reservas" y "Mi perfil" con sesión, en `MainLayout.razor`. Este fichero está fuera del radio, pero su excepción está **documentada y autorizada en §4**. Con curl se comprobó que sin sesión no aparecen. | Comprobarlo en el navegador y marcar el punto. |

**Notas de los revisores (no verificable, no son hallazgos):**

- La prueba 8a usa `CuentasQueFallanAlDarDeBaja`, que lanza antes de llegar a Identity. Por eso no demuestra que se deshaga una escritura de Identity ya hecha, como el sello renovado. *Seguridad.*
- La expulsión por el revalidador a los 5 minutos (A-06) es código anterior a esta fase y no ha cambiado. *Seguridad.*

**Radio y alcance:**

- Todos los ficheros están en §4.
- `MainLayout.razor` figura como excepción autorizada.
- No se tocan `PruebasCancelarReservaCliente.cs` ni `PruebasInyeccionDependencias.cs`. Ninguno de los dos hacía falta: la prueba de origen `Cliente` ya existe en [PruebasCancelarReservaCliente.cs:171](../../tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasCancelarReservaCliente.cs).
- Las tareas T1 a T6 están hechas.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-08: las dos pestañas, en orden y solo con las reservas del usuario.** Evidencia: `PruebasConsultasReservasCliente` y `PruebasMisReservasDto`, más la verificación manual.
- [x] **CU-09: el detalle muestra `PrecioAplicado` y una reserva ajena da "no encontrado".** Evidencia: `PruebasConsultarDetalleReserva` y `PruebasDetalleReservaClienteDto`.
- [x] **CU-10: no se puede cancelar la reserva de otro cliente.** Evidencia: `PruebasCancelarReservaCliente` (Fase 11) y el sabotaje de RN-05.
- [x] **CU-10: no se puede cancelar una reserva ya iniciada.** Evidencia: `PruebasReserva`.
- [x] **CU-10: no se puede cancelar dos veces.** Evidencia: `PruebasReserva`.
- [x] **CU-22: editar el perfil no altera las reservas ya creadas.** Evidencia: `PruebasActualizarPerfil` (RN-78).
- [x] **CU-22: un propietario con empresa activa no puede darse de baja.** Evidencia: `PruebasDarDeBajaCuenta` (`Conflicto`).
- [x] **CU-22: la baja cancela solo las reservas futuras confirmadas.** Evidencia: `PruebasDarDeBajaCuenta`, `PruebasRepositorioReservasCliente` y el sabotaje de RN-137.
- [x] **CU-22: tras la baja se rechaza el inicio de sesión.** Evidencia: `PruebasDarDeBajaCuenta` y el sabotaje de RN-135.
- [x] **Las páginas son `InteractiveServer`, usan `PoliticaCliente` y no tienen `MarkupString`.** Evidencia: `MiCuenta/_Imports.razor`, el grep y la revisión de seguridad.
- [x] **Sin paquetes nuevos y una sola migración de datos.** Evidencia: el diff (`RolClienteParaPropietarios`) y `has-pending-model-changes`.
- [x] **Los sabotajes hacen fallar sus pruebas.** Evidencia: la tabla de sabotajes de §6.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión.

## Revisión 2026-10-18 17:08

**Veredicto:** APTO
**Especificación:** docs/fases/fase-15.md (APROBADA)
**Commit base:** 99a8735
**Ficheros revisados:** 49 (más la documentación y `comprobaciones.sh`, ignorados por git)

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 0 | 2 | 0 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 404 | 404 | 0 |
| Integración | 329 | 329 | 0 |

`dotnet build`: 0 avisos, 0 errores. `comprobaciones.sh`: `bloqueantes=0|importantes=0`.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | MENOR | arquitectura | src/PlataformaReservas.Web/Components/MiCuenta/DetalleReserva.razor:48 | AGENTS §6; fase-15 §11 ("La página solo pinta") | Para explicar por qué no se puede cancelar, la página deduce el caso: si la reserva está confirmada y no es `Cancelable`, da por hecho que ya ha empezado (3a). Eso repite RN-93 en el componente. Hoy es correcto, pero si cambia `EsCancelablePorCliente`, el mensaje de 2a/3a dejaría de coincidir sin que fallara ninguna prueba. | Que `DetalleReservaClienteDto.ConvertirADto` devuelva el motivo ya resuelto, como código o texto, y que la página solo lo pinte. |
| 2 | MENOR | alcance | docs/fases/fase-15.md §6 (verificación manual, último punto) | AGENTS.md §11 bis | Sigue pendiente comprobar en el navegador, con sesión iniciada, los enlaces del menú de `MainLayout.razor`. Es la excepción autorizada en §4. Sin sesión ya se comprobó con curl que no aparecen. | Que el desarrollador lo compruebe y marque el punto. |

**Estado de los hallazgos de la revisión de las 16:51:**

| # | Hallazgo | Estado | Cómo se resolvió |
|---|---|---|---|
| 1 | C13, falso positivo | Resuelto | C13 busca solo en `src/` (excepción autorizada en §4) |
| 2 | Casos del catálogo sin prueba ni motivo | Resuelto | Pruebas nuevas en `PruebasConsultarMisReservas`, `PruebasConsultarDetalleReserva` y `PruebasDarDeBajaCuenta`, y tabla de omisiones de §6 completa |
| 3 | "Ya de baja" detectado después de cancelar | Resuelto | La precondición `EstaActivaAsync` se comprueba antes de abrir la transacción y `DarDeBajaAsync` ya no devuelve `bool`. Una prueba comprueba que el rastreador queda limpio, y el sabotaje está anotado |
| 4 | Recuento de la baja calculado en la página | Resuelto | Lo da `MisReservasDto.Cancelables`, con la condición de RN-137 |
| 5 | `Cancelable` repetido por tercera vez | Resuelto | La regla está en un solo sitio, `Reserva.EsCancelablePorCliente`. El sabotaje está anotado |
| 6 | Perfil leído sin caso de uso | Resuelto | `ConsultarPerfil` y `PerfilDto`, que usan también `FlujoReserva.razor` (excepción autorizada en §4) |
| 7 | Menú pendiente de comprobar en el navegador | Pendiente | Es el hallazgo 2 de esta ronda |

**Notas de los revisores (no verificable, no son hallazgos):**
- La expulsión del circuito por el revalidador es código de la Fase 6 y no ha cambiado.
- Que la migración esté aplicada en desarrollo consta en §6 de la especificación.

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-08:** dos pestañas, en orden y solo con las reservas del usuario. Evidencia: `PruebasConsultasReservasCliente` y `PruebasMisReservasDto`.
- [x] **CU-09:** el detalle muestra `PrecioAplicado`, y una reserva ajena o un identificador vacío dan "no encontrado". Evidencia: `PruebasConsultarDetalleReserva`.
- [x] **CU-10:** un cliente no puede cancelar la reserva de otro. Evidencia: `PruebasCancelarReservaCliente` y el sabotaje de RN-05.
- [x] **CU-10:** no se puede cancelar una reserva ya iniciada. Evidencia: `PruebasReserva`, también `EsCancelablePorCliente` con los extremos a un tick.
- [x] **CU-10:** no se puede cancelar dos veces. Evidencia: `PruebasReserva`.
- [x] **CU-22:** editar el perfil no altera las reservas ya creadas. Evidencia: `PruebasActualizarPerfil`.
- [x] **CU-22:** un propietario con empresa activa no puede darse de baja. Evidencia: `PruebasDarDeBajaCuenta`.
- [x] **CU-22:** la baja cancela solo las reservas futuras confirmadas. Evidencia: `PruebasDarDeBajaCuenta`, `PruebasRepositorioReservasCliente` y el sabotaje de RN-137.
- [x] **CU-22:** tras la baja se rechaza el inicio de sesión. Evidencia: `PruebasDarDeBajaCuenta` y el sabotaje de RN-135.
- [x] **Páginas:** son `InteractiveServer`, usan `PoliticaCliente` y no tienen `MarkupString`. Evidencia: `MiCuenta/_Imports.razor`, el grep y la revisión de seguridad.
- [x] **Paquetes y migraciones:** ningún paquete nuevo y una sola migración, de datos. Evidencia: el diff.
- [x] **Sabotajes:** hacen fallar sus pruebas. Evidencia: §6, cinco sabotajes anotados.
- [x] **Compilación y pruebas:** `dotnet build` sin avisos y `dotnet test` en verde. Evidencia: esta revisión.
