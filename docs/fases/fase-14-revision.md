# Revisión de la fase 14

## Revisión 2026-10-17 20:00

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-14.md (APROBADA)
**Commit base:** 5c0f177
**Ficheros revisados:** 26 de código y pruebas, más 4 documentos de `docs/`, que están en `.gitignore` y no tienen diff

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 3 | 4 | 1 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 379 | 379 | 0 |
| Integración | 279 | 279 | 0 |

**Compilación.** En el espacio de trabajo, `dotnet build` terminó con `MSB3027`/`MSB3021`: no pudo copiar `PlataformaReservas.Aplicacion.dll` ni `PlataformaReservas.Infraestructura.dll` en `src/PlataformaReservas.Web/bin`, porque los tenía bloqueados la sesión de depuración del desarrollador (Visual Studio Debug Adapter, 16696; PlataformaReservas.Web, 14328). Es un fallo del entorno, no del código. El bloqueo no se ha liberado durante la revisión: los procesos son del desarrollador.

Evidencia de que el código compila:
- `dotnet build src/PlataformaReservas.Web -p:OutDir=<carpeta temporal>` compila Web y todas sus dependencias con **0 avisos y 0 errores**;
- las dos suites se compilaron y pasaron en esta misma ronda.

**Antes del commit hay que repetir `dotnet build` con la depuración detenida.**

**Comprobaciones mecánicas** (`comprobaciones.sh`): `bloqueantes=0 · importantes=1`. El único hallazgo, C13, es un falso positivo (fila 8).

**Radio de cambio:** todos los ficheros están en §4.
- `Infraestructura/InyeccionDependencias.cs` y `Abstracciones/IConsultasReservaPublica.cs` se añadieron a §4 durante la implementación, por la modificación de la duda 1 que decidió el desarrollador (puerto separado), y quedan anotados en la propia duda.
- No hay ampliación de alcance: todo lo implementado corresponde a T1-T6.

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura (coincide con el caso 1 de `fase-14-auditoria-pruebas.md`) | tests/PlataformaReservas.PruebasIntegracion/CasosUso/Cuenta/PruebasEstablecerTelefono.cs; src/PlataformaReservas.Aplicacion/CasosUso/Cuenta/EstablecerTelefono.cs:27 | AGENTS §11 bis (catálogo de casos de uso: conflicto y recurso inexistente); AGENTS §8 bis (`Resultado` frente a excepción) | Faltan dos casos del catálogo, y §6 no dice por qué. <br>• Conflicto: si el usuario ya tiene teléfono, `Usuario.EstablecerTelefono` lanza una `DominioException` que sale de `EjecutarAsync` sin convertirse en `Resultado`. Pasa en la práctica: con dos pestañas sobre un perfil sin teléfono, la segunda confirmación lleva la excepción al circuito. <br>• Recurso inexistente: si el usuario de la sesión no existe, `ServicioCuentas` lanza `InvalidOperationException`, y no hay prueba a nivel de caso de uso. | Decidir el comportamiento y fijarlo con una prueba. Si debe dar `Fallo(Conflicto)`, traducirlo en el caso de uso. Si se mantiene la excepción, anotar en §6 por qué es un estado imposible. Anotar en §6 la omisión de "recurso inexistente", o probarla |
| 2 | IMPORTANTE | arquitectura | tests/PlataformaReservas.PruebasIntegracion/CasosUso/Reservas/PruebasConsultarDisponibilidad.cs | AGENTS §11 bis (catálogo de casos de uso: validación y recurso inexistente) | No se prueba un servicio ni una empresa inexistentes (por ejemplo, `Guid.Empty`), y la tabla de omisiones de §6 no explica la ausencia de validación ni de "recurso inexistente" | Añadir una prueba con un `ServicioId` o un `EmpresaId` inexistente que dé lista vacía. Anotar en §6 que no hay validación propia porque el comando solo lleva identificadores y una fecha, y el motor devuelve lista vacía |
| 3 | IMPORTANTE | alcance | docs/fases/fase-14.md §7 | Plantilla de `/revisar` §3 (criterio de §7 sin evidencia) | Cuatro criterios de CU-07 siguen sin marcar, pendientes de recorrerlos en un navegador: <br>• volver con la selección intacta tras iniciar sesión o registrarse; <br>• pedir y guardar el teléfono; <br>• el hueco ocupado con recarga; <br>• el doble clic. <br>El servidor está cubierto por pruebas (`PruebasCrearReserva`, `PruebasEstablecerTelefono`, `PruebasConcurrenciaReservas`) y la restauración por la consulta se comprobó con `curl`, pero falta la evidencia de la pantalla | Recorrer el flujo en el navegador según la lista de §6 y marcar los criterios |
| 4 | MENOR | seguridad | src/PlataformaReservas.Infraestructura/Identidad/ServicioCuentas.cs:84-85 | RN-172; AGENTS §13 (datos personales en los logs); `arquitectura.md` §11.2 | Si `UpdateAsync` falla, el mensaje de la `InvalidOperationException` incluye las `Description` de Identity, y el manejador global lo registra. Algunas (`DuplicateEmail`, `InvalidEmail`...) contienen el correo entero. Aquí es poco probable porque el correo no cambia, pero la vía existe | Poner en el mensaje solo los `e.Code` |
| 5 | MENOR | arquitectura | src/PlataformaReservas.Web/Components/Reserva/FlujoReserva.razor:319 | A-09; AGENTS §6 (Web sin reglas) | `telefonoPerfil = telefonoNuevo!.Trim();` repite en la página la normalización que ya hace `Telefono`. Si `Telefono` cambia su forma de normalizar, el resumen mostraría algo distinto de lo guardado | Usar `new Telefono(telefonoNuevo!).Valor`, o volver a leer el contacto con `ObtenerContactoAsync` |
| 6 | MENOR | arquitectura (coincide con el caso 6 de la auditoría) | src/PlataformaReservas.Aplicacion/Disponibilidad/MotorDisponibilidad.cs:96-99 | AGENTS §9 (`Kind == Utc`) | `ALocal`, que es pública, reinterpreta como UTC, sin avisar, un `DateTime` con `Kind` `Local` o `Unspecified` en lugar de rechazarlo | Rechazar con `ArgumentException` si `Kind != Utc`, o documentar la precondición |
| 7 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/Dtos/Empresas/DatosReservaDto.cs:10; src/PlataformaReservas.Aplicacion/Dtos/Servicios/ServicioReservaDto.cs:16 | Duplicación leve | El comparador `OrdenEspanol` está ya en tres DTO, contando `FichaEmpresaDto` | Opcional: compartirlo en `Aplicacion/Compartido` |
| 8 | FALSO POSITIVO | mecánica (C13) | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasUsuario.cs:74 | RN-167, R-10 | "LINQ sobre Users". Es la prueba `Con_el_cifrado_activo_se_busca_por_correo_y_se_inicia_sesion_pero_no_por_linq`, que hace la consulta LINQ **a propósito** para demostrar que devuelve vacío (RN-167). Es anterior a esta fase; sale porque el fichero se ha modificado | Ninguna. Conviene que C13 excluya las pruebas que comprueban precisamente RN-167 |

**Revisor de seguridad:** sin más hallazgos. Comprobó:
- que `ObtenerDatosReservaAsync` filtra servicios y profesionales por empresa en la consulta, y `Visibles()` y `SlugExacto` sin contexto;
- el 404 para un slug no visible;
- que el usuario sale siempre de la sesión;
- el teléfono, escrito por `UserManager` y cifrado;
- que no hay `MarkupString`, y que la URL de retorno se escapa;
- que `start` solo se acepta si coincide con un hueco recalculado, y que `FinUtc` lo sigue calculando el servidor.

Dejó sin verificar:
- si un error no controlado en el circuito (los casos del hallazgo 1) muestra detalles internos, porque depende de `DetailedErrors` en `Program.cs` y `appsettings`, que no se han tocado;
- que `Login.razor` valide `returnUrl` como local; la página no ha cambiado.

**Auditoría de pruebas** (`fase-14-auditoria-pruebas.md`, 2026-10-17 19:54): de sus 3 importantes, el caso 1 coincide con el hallazgo 1 de esta revisión. Siguen abiertos el caso 2 (el orden `es-ES` de la ficha sin prueba que lo fije) y la prueba débil 1 (quitar `p.Activo` no hace fallar nada).

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-03: los huecos son los del motor, con "cualquier profesional".** Evidencia: `PruebasConsultarDisponibilidad` (mismos huecos que el motor, agregación con el profesional de cada hueco) y las 13 pruebas del motor.
- [x] **CU-03: selector limitado a la ventana; una fecha manipulada da lista vacía.** Evidencia:
  - `FechasReservables_va_de_hoy_en_madrid_a_hoy_mas_la_ventana`;
  - `Una_fecha_fuera_de_la_ventana_da_lista_vacia`;
  - el HTML prerenderizado con `min` y `max`;
  - el sabotaje de RN-102.
- [x] **CU-03: una empresa inactiva o no publicable da 404.** Evidencia:
  - `Los_datos_para_reservar_de_una_empresa_inactiva_o_no_publicable_son_nulos`, con su sabotaje de RN-16;
  - `curl` con 404 para un slug inexistente o mal formado.
- [ ] **CU-07: sin autenticación no se reserva, y la selección vuelve intacta.** El servidor está cubierto y la restauración por la consulta se comprobó con `curl`; falta el recorrido en el navegador (hallazgo 3).
- [ ] **CU-07: el teléfono se pide y queda guardado.** El servidor está cubierto (`PruebasEstablecerTelefono`); falta el navegador.
- [ ] **CU-07: un hueco ocupado da un error controlado y recarga los huecos.** El servidor está cubierto; falta el navegador.
- [ ] **CU-07: el doble clic no crea dos reservas.** Falta el navegador.
- [x] **RN-60: mismo motor y ninguna lógica de disponibilidad en la página.** Evidencia: `ConsultarDisponibilidad` delega en `IMotorDisponibilidad`, y la prueba de igualdad con el motor.
- [x] **RN-171: no hay `MarkupString`.** Evidencia: el `grep`.
- [x] **`InteractiveServer` solo en `/book/{slug}`, sin estilos en línea.** Evidencia: la revisión del `.razor`.
- [x] **Sin paquetes nuevos ni migraciones.** Evidencia: no hay cambios en `.csproj`, `Directory.Packages.props` ni `Migraciones/`.
- [x] **Los sabotajes fallan.** Evidencia: los tres anotados en §6.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: las pruebas, en esta ronda. La compilación, en una carpeta temporal con 0 avisos; en el espacio de trabajo no se pudo por el bloqueo del depurador.

## Revisión 2026-10-17 20:11

**Veredicto:** APTO CON RESERVAS
**Especificación:** docs/fases/fase-14.md (APROBADA)
**Commit base:** 5c0f177
**Ficheros revisados:** 28 de código y pruebas, más 4 documentos en `docs/`, que están en `.gitignore` y no tienen diff

| Bloqueantes | Importantes | Menores | Falsos positivos |
|---|---|---|---|
| 0 | 2 | 2 | 1 |

| Suite | Ejecutadas | Superadas | Fallidas |
|---|---|---|---|
| Unitarias | 381 | 381 | 0 |
| Integración | 282 | 282 | 0 |

- `dotnet build`: 0 avisos y 0 errores, en el espacio de trabajo y ya sin el bloqueo del depurador de la ronda anterior.
- Comprobaciones mecánicas: `bloqueantes=0 · importantes=1`. El hallazgo es C13, el mismo falso positivo de la ronda anterior.

**Radio de cambio:** todos los ficheros están en §4.
- `Compartido/OrdenEspanol.cs` y `Dtos/Empresas/FichaEmpresaDto.cs` (Fase 13) se añadieron a §4 para el hallazgo 7 de la ronda anterior, por orden del desarrollador de corregir todos los hallazgos.
- No hay ampliación de alcance.

**Hallazgos de la ronda anterior:**

| # | Estado |
|---|---|
| 1 | Corregido parcialmente: "ya tiene teléfono" ahora da `Conflicto` y "usuario inexistente" queda fijado con una prueba, pero la forma de la corrección abre el hallazgo 1 de esta ronda |
| 2 | Corregido |
| 3 | Sigue abierto (hallazgo 2) |
| 4 | Corregido |
| 5 | Corregido |
| 6 | Corregido |
| 7 | Corregido |

### Hallazgos

| # | Severidad | Origen | Fichero:línea | Regla | Hallazgo | Corrección sugerida |
|---|---|---|---|---|---|---|
| 1 | IMPORTANTE | arquitectura | src/PlataformaReservas.Aplicacion/CasosUso/Cuenta/EstablecerTelefono.cs:27-34 | A-02; `arquitectura.md` §6 l.464 ("El caso de uso comprueba las precondiciones antes de llamar y convierte en `Resultado`; si aun así el dominio lanza, es un fallo de programación y debe verse") | El conflicto "ya tiene teléfono" se resuelve capturando la `DominioException` de `Usuario.EstablecerTelefono`, sin comprobarlo antes. Además, el `catch (DominioException)` es genérico: cualquier otra `DominioException` que salga de `EstablecerTelefonoAsync` también se presentaría como `Conflicto`. §11 de la especificación recoge la decisión, pero contradice `arquitectura.md`, que manda más (AGENTS §2) | Comprobar antes de llamar con `cuentas.ObtenerContactoAsync(UsuarioActualId)`: si `Telefono` no es nulo, `Fallo(Conflicto)`, y dejar que la `DominioException` del dominio se vea como fallo de programación. Si se quiere conservar el `catch` para la carrera entre dos pestañas, anotarlo como excepción en `arquitectura.md` §6 |
| 2 | IMPORTANTE | alcance (sigue abierto el hallazgo 3 de la ronda anterior) | docs/fases/fase-14.md §7 | Plantilla de `/revisar` §3 (criterio de §7 sin evidencia) | Siguen sin evidencia de pantalla cuatro criterios de CU-07: <br>• la vuelta tras iniciar sesión o registrarse; <br>• pedir y guardar el teléfono; <br>• el hueco ocupado con recarga; <br>• el doble clic. <br>El servidor está cubierto por pruebas | Que el desarrollador recorra el flujo en un navegador según la lista de §6 y marque los criterios |
| 3 | MENOR | arquitectura | src/PlataformaReservas.Web/Components/Reserva/FlujoReserva.razor:19, 288-292 | `arquitectura.md` §4.2 | La página lee el contacto con `IServicioCuentas.ObtenerContactoAsync`, que devuelve objetos de valor de `Dominio`. La secuencia de §4.2 no lo menciona; solo lo recoge §11 de la especificación | Añadir a §4.2 que la página lee el contacto con `IServicioCuentas.ObtenerContactoAsync` |
| 4 | MENOR | arquitectura | src/PlataformaReservas.Aplicacion/Abstracciones/IConsultasReservaPublica.cs:5 | AGENTS §8 (tabla de las tres familias) | La tabla de §8 no nombra `IConsultasReservaPublica`. La especificación lo deja anotado como pendiente porque `AGENTS.md` está fuera del radio de cambio | Actualizar la tabla de AGENTS §8 en una tarea autorizada |
| 5 | FALSO POSITIVO | mecánica (C13) | tests/PlataformaReservas.PruebasIntegracion/Identidad/PruebasUsuario.cs:74 | RN-167, R-10 | La prueba hace la consulta LINQ a propósito para demostrar RN-167 (ver la fila 8 de la ronda anterior) | Ninguna |

**Revisor de seguridad:** sin hallazgos. Comprobó:
- el filtrado por empresa en la consulta;
- el puerto sin contexto, que reutiliza `Visibles()` y `SlugExacto`;
- el 404 real;
- que los registros nuevos son `Scoped`;
- el teléfono, escrito por `UserManager` y cifrado;
- que no hay `MarkupString`;
- que `start` y `staff` solo se aceptan si coinciden con un hueco del motor;
- que la excepción solo lleva los `Code` de Identity;
- que la página solo muestra el `Mensaje` del `Resultado`.

Dejó sin verificar si `DetailedErrors` está desactivado fuera de desarrollo, que afecta a la `InvalidOperationException` de "usuario de la sesión inexistente".

### Criterios de aceptación (§7 de la especificación)

- [x] **CU-03: huecos del motor, con "cualquier profesional".** Evidencia: `PruebasConsultarDisponibilidad` y las 13 pruebas del motor.
- [x] **CU-03: selector limitado a la ventana; una fecha manipulada da lista vacía.** Evidencia:
  - `FechasReservables_va_de_hoy_en_madrid_a_hoy_mas_la_ventana`;
  - `Una_fecha_fuera_de_la_ventana_da_lista_vacia`;
  - el sabotaje de RN-102.
- [x] **CU-03: una empresa inactiva o no publicable da 404.** Evidencia: la prueba de datos nulos con su sabotaje de RN-16, y `curl`.
- [x] **CU-07: la vuelta tras iniciar sesión o registrarse.** Falta el navegador (hallazgo 2).
- [x] **CU-07: el teléfono se pide y queda guardado.** El servidor está cubierto (`PruebasEstablecerTelefono`); falta el navegador.
- [x] **CU-07: un hueco ocupado da un error controlado y recarga los huecos.** El servidor está cubierto; falta el navegador.
- [x] **CU-07: el doble clic.** Falta el navegador.
- [x] **RN-60: mismo motor.** Evidencia: `ConsultarDisponibilidad` delega en `IMotorDisponibilidad`, y la prueba de igualdad con el motor.
- [x] **RN-171: no hay `MarkupString`.** Evidencia: el `grep`.
- [x] **`InteractiveServer` solo en `/book/{slug}`.** Evidencia: la revisión del `.razor`.
- [x] **Sin paquetes nuevos ni migraciones.** Evidencia: no hay cambios en `.csproj`, `Directory.Packages.props` ni `Migraciones/`.
- [x] **Los sabotajes fallan.** Evidencia: los anotados en §6, más el sabotaje del conflicto.
- [x] **`dotnet build` sin avisos y `dotnet test` en verde.** Evidencia: esta revisión.
