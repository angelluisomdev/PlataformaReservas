# Fase 11 — Reservas: creación transaccional, concurrencia, cancelación, completar

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 7 h (SPEC §19) |
| **Semana** | 3 (`PROJECT_PLAN.md` §2), la semana crítica: no se recorta |
| **Cierra** | Control 3 (`PROJECT_PLAN.md` §3, "fin de Fase 11") |
| **Generada** | 2026-10-12 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Existen los cuatro casos de uso de reserva de `arquitectura.md` §2.3: `CrearReserva`, `CancelarReservaCliente`, `CancelarReservaEmpresa` y `CompletarReserva`.

- La creación recomprueba la disponibilidad dentro de la transacción con el motor de la Fase 10 (RN-76).
- La restricción `ex_reservas_sin_solape` garantiza que no hay solapes (RN-77).
- El error 23P01 se traduce a `HuecoYaNoDisponible` (RN-80).
- La prueba de concurrencia de SPEC §16.3 está en verde contra PostgreSQL real.

Las pruebas se escriben **antes** que la implementación (CLAUDE.md).

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §9.10, §9.11 | Propiedades de `Reserva`, estados y transiciones legales |
| SPEC §10.3 | El área de cliente filtra por `UsuarioId`, un eje distinto del de empresa |
| SPEC §13 | Las tres capas de la creación, la restricción `EXCLUDE`, la traducción de 23P01 y las siete validaciones de la creación |
| SPEC §16.2, §16.3 | Integración: creación, hueco ocupado y cancelación. Concurrencia: N tareas, exactamente 1 éxito y N−1 `HuecoYaNoDisponible`, repetido varias iteraciones |
| `modelo-dominio.md` §2.4, §4.10, §6 | RN-77 la garantiza la base de datos. Contrato de `Reserva` (`CancelarPorCliente`, `CancelarPorEmpresa`, `Completar`). RN-73 la comprueba el caso de uso |
| `arquitectura.md` §2.3 | `CasosUso/Reservas/` (`CrearReserva`, `CancelarReservaCliente`, `CancelarReservaEmpresa`, `CompletarReserva`) e `Infraestructura/Errores/TraductorErroresPostgres` |
| `arquitectura.md` §4.2, §6, §9.4, §9.5 | Flujo de la creación. `HuecoYaNoDisponible` es `Resultado`. Un caso de uso, una transacción. `"23P01"` solo aparece en `TraductorErroresPostgres` |
| `arquitectura.md` §12.3 | Prueba de concurrencia: cada tarea con su propio ámbito de DI y su propio `ContextoDatos` |
| AGENTS §8, §8 quater | Familias de acceso a datos. Los datos personales se leen con `UserManager` |
| RN-03, RN-05 | Un recurso ajeno se responde como "no encontrado". El eje cliente filtra por `UsuarioId` |
| RN-13 | Una empresa inactiva no admite reservas nuevas |
| RN-70 a RN-83 | Creación de reserva (RN-74 ⚑, RN-76 ⚑, RN-77 ⚑) |
| RN-90 a RN-98 | Ciclo de vida (RN-97 ⚑) |
| RN-102 | La conversión entre hora local y UTC solo ocurre en el motor |
| CU-07, CU-10, CU-20, CU-21 | Crear, cancelar como cliente, cancelar como empresa y completar |
| `PROJECT_PLAN.md` §7 | Pendiente de la Fase 8: el rechazo de `CancelarPorEmpresa` sobre una reserva **completada**, con prueba |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `ex_reservas_sin_solape` y `btree_gist` en la migración `Inicial`, con `DROP` en `Down` | ✔ comprobado (`Migraciones/20260930185037_Inicial.cs`) |
| La restricción existe en la base de pruebas, consultando `pg_constraint` | ✔ comprobado (`Persistencia/PruebasEsquema.cs`) |
| Las pruebas aplican migraciones, nunca `EnsureCreated` (R-3) | ✔ comprobado (`BaseDatosFixture`) |
| `CodigoError.HuecoYaNoDisponible`, `EstadoNoValido`, `NoEncontrado`, `Validacion` y `NoAutorizado` | ✔ comprobado |
| `IUnidadTrabajo` e `ITransaccion`, implementadas en `UnidadTrabajo` | ✔ comprobado |
| `IMotorDisponibilidad`, `IConsultasDisponibilidad` y `ServicioDisponibilidadDto` (Fase 10) | ✔ comprobado. El DTO no lleva `Precio` (duda 3) |
| `Reserva.Crear(..., Servicio servicio, ...)` y `CancelarPorEmpresa` | ✔ comprobado |
| `Reserva.CancelarPorCliente` y `Completar` | ✘ ausentes: se crean en esta fase (`modelo-dominio.md` §4.10) |
| `IRepositorioReservas` con `ObtenerParaModificarAsync`, filtrado por empresa | ✔ comprobado |
| Un puerto con el usuario autenticado | ✘ ausente: duda 1 |
| Acceso a reservas por el eje cliente, sin contexto de empresa | ✘ ausente: duda 2 |
| Lectura del nombre y el teléfono del cliente | ✘ ausente: duda 4 |
| `TraductorErroresPostgres` | ✘ ausente: se crea en esta fase (duda 6) |
| `Reservas/PruebasConcurrenciaReservas.cs` en la lista cerrada de pruebas transversales | ✔ comprobado (AGENTS §11 bis) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Entidades/Reserva.cs`: `CancelarPorCliente`, `Completar` y la sobrecarga de `Crear` (duda 3). `Repositorios/`: el contrato del eje cliente (duda 2), **solo con tipos de `Dominio`** |
| `src/PlataformaReservas.Aplicacion` | sí | `CasosUso/Reservas/`: los cuatro casos de uso y sus comandos. `Abstracciones/`: el contexto de usuario (duda 1) y el método de contacto en `IServicioCuentas` (duda 4). `Compartido/`: la excepción de la duda 6. `Dtos/Servicios/ServicioDisponibilidadDto.cs`: `Precio` (duda 3). `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Identidad/`: el contexto de usuario y el método de contacto en `ServicioCuentas`; además `ContextoEmpresa.cs` y `ClaimsSesion.cs` (lectura del claim compartida, menor 4 de la revisión 2026-10-12 18:39, pedido por el desarrollador). `Repositorios/`: la implementación del eje cliente. `Errores/TraductorErroresPostgres.cs`. `Persistencia/UnidadTrabajo.cs`. `Consultas/ConsultasDisponibilidad.cs` (`Precio`). `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Web` | no | — |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Entidades/PruebasReserva.cs`, `Dtos/Servicios/PruebasServicioDisponibilidadDto.cs` (`Precio`) e `Infraestructura/ConsultasDisponibilidadEnMemoria.cs` (`Precio`) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `CasosUso/Reservas/` (cuatro ficheros). `Reservas/PruebasConcurrenciaReservas.cs`. `Repositorios/` (el del eje cliente). `Identidad/` (el contexto de usuario y `PruebasServicioCuentas.cs`). `Errores/PruebasTraductorErroresPostgres.cs`. `Persistencia/PruebasUnidadTrabajo.cs`. `Consultas/PruebasConsultasDisponibilidad.cs` (`Precio`). `PruebasInyeccionDependencias.cs`. `Infraestructura/`: el doble del contexto de usuario y su ámbito en `BaseDatosFixture` |
| `docs/` y `AGENTS.md` | sí | AGENTS §8 (tercera familia, duda 2). `arquitectura.md` §2.3, §5.1 y §9.5; además §4.2 (importante de la revisión 18:39), §10 (menor 3 de la 18:39) y §7 (menor 2 de la 21:59). `modelo-dominio.md` §4.10. Este fichero |

*Ampliación del radio ratificada por el desarrollador el 2026-10-12, tras `/revisar 11` (ronda 22:26): son las correcciones de las rondas 18:39 y 21:59.*

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:
- `Web`, porque las pantallas son de las fases 14, 15 y 16;
- las migraciones, porque la restricción ya existe;
- los eventos, que son de la Fase 11b;
- el motor, salvo lo que lea del DTO.

**Dirección de referencias.** Los contratos de `Dominio/Repositorios` solo usan tipos de `Dominio` y de la biblioteca base, y **no nombran ningún puerto de `Aplicacion`**. Un contexto (de empresa o de usuario) lo recibe únicamente la **implementación** en `Infraestructura`, igual que hoy `RepositorioReservas` recibe `IContextoEmpresa` mientras `IRepositorioReservas` no lo menciona.

## 5. Tareas

**Orden obligatorio (CLAUDE.md, AGENTS §10):** T1 y T2 escriben las pruebas, en rojo contra esqueletos que lanzan `NotImplementedException`, y **se paran** hasta que el desarrollador las confirme. Solo entonces empieza T3.

### T1 — Pruebas unitarias de `Reserva`, en rojo (≈ 0,5 h)

- **Ficheros:**
  - modifica `PruebasUnitarias/Entidades/PruebasReserva.cs`;
  - modifica `Dominio/Entidades/Reserva.cs`, solo con esqueletos de `CancelarPorCliente`, `Completar` y la sobrecarga de `Crear`.
- **Implementa:** RN-91, RN-92, RN-93, RN-96, RN-98 y RN-74. Además, el pendiente de la Fase 8. Ver §6.
- **Hecho cuando:** las pruebas compilan y fallan por los esqueletos, y siguen pasando las que ya existían.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias --filter PruebasReserva`.

### T2 — Contratos, esqueletos y pruebas de integración, en rojo (≈ 1,75 h)

- **Ficheros:**
  - **Contratos y esqueletos:**
    - el contrato del eje cliente en `Dominio/Repositorios/` (duda 2);
    - el contexto de usuario en `Aplicacion/Abstracciones/` (duda 1);
    - el método de contacto en `IServicioCuentas` (duda 4);
    - la excepción de la duda 6 en `Aplicacion/Compartido/`;
    - los cuatro casos de uso con sus comandos en `Aplicacion/CasosUso/Reservas/`;
    - las implementaciones de `Infraestructura` como esqueletos.
  - **Dobles:** el contexto de usuario en `PruebasIntegracion/Infraestructura/` y su ámbito en `BaseDatosFixture`.
  - **Pruebas:** todos los ficheros de integración de §6, incluido `Reservas/PruebasConcurrenciaReservas.cs`.
- **Implementa:** la parte de pruebas de RN-70 a RN-98, CU-07, CU-10, CU-20, CU-21 y SPEC §16.3.
- **Hecho cuando:**
  - todo compila y las pruebas nuevas fallan por los esqueletos;
  - el resto de la suite sigue en verde;
  - **el desarrollador ha revisado y confirmado las pruebas de T1 y T2**.
- **Verificación:** `dotnet test` en rojo, solo en las pruebas nuevas, y la confirmación por escrito del desarrollador.

### T3 — Dominio (≈ 0,5 h)

- **Ficheros:** modifica `Dominio/Entidades/Reserva.cs`.
- **Implementa:**
  - `CancelarPorCliente(DateTime ahoraUtc)`: exige `Confirmada` e `InicioUtc > ahora` (RN-93), registra `FechaCancelacion` y `FechaModificacion` (RN-98) y lanza `DominioException` en otro caso (A-02).
  - `Completar(DateTime ahoraUtc)`: exige `Confirmada` y `FinUtc < ahora` (RN-96).
  - La sobrecarga de `Crear` de la duda 3.
  - Sin eventos: son de la Fase 11b.
- **Hecho cuando:** `PruebasReserva` está en verde y siguen pasando las cinco pruebas de `PruebasArquitectura`.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias`.

### T4 — Contexto de usuario, repositorio del eje cliente y contacto (≈ 1 h)

**Bloqueada por las dudas 1, 2, 3 y 4.**

- **Ficheros:**
  - el contexto de usuario en `Infraestructura/Identidad/`;
  - la implementación del repositorio del eje cliente en `Infraestructura/Repositorios/`;
  - el método de contacto en `Infraestructura/Identidad/ServicioCuentas.cs`;
  - `Precio` en `ServicioDisponibilidadDto`, en `ConsultasDisponibilidad` y en el doble en memoria.
- **Implementa:**
  - RN-05 y RN-95: filtra por `UsuarioId` en la consulta.
  - Valida el usuario al agregar, como defensa en profundidad análoga a SPEC §10.3.
  - RN-78: el nombre y el teléfono se leen con `UserManager` (AGENTS §8 quater).
- **Hecho cuando:** pasan sus pruebas de integración, que son las del catálogo de repositorio.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "Identidad|Repositorios|Consultas"`.

### T5 — Traducción de 23P01 (≈ 0,5 h)

**Bloqueada por la duda 6.**

- **Ficheros:**
  - crea `Infraestructura/Errores/TraductorErroresPostgres.cs`;
  - modifica `Infraestructura/Persistencia/UnidadTrabajo.cs`.
- **Implementa:** SPEC §13 y RN-80.
  - Es el **único** sitio con el literal `"23P01"` (`arquitectura.md` §9.5).
  - Otro error de PostgreSQL no se traduce y sube tal cual.
- **Hecho cuando:**
  - pasan `PruebasTraductorErroresPostgres` y `PruebasUnidadTrabajo`;
  - `grep "23P01" src/` da un único resultado.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "TraductorErroresPostgres|UnidadTrabajo"`.

### T6 — `CrearReserva` (≈ 1 h)

**Bloqueada por las dudas 1 a 7.**

- **Ficheros:** `Aplicacion/CasosUso/Reservas/CrearReserva.cs` y su comando.
- **Implementa:** CU-07 pasos 5 a 11 (sin el evento), RN-70 a RN-80, RN-13, RN-82 y SPEC §13 "Validaciones de la creación". El orden es este:
  1. **Sin usuario:** `NoAutorizado` (RN-70).
  2. **Comprobaciones previas** (CU-07 6a, motivo concreto). Cada una da `Validacion`:
     - empresa inactiva o inexistente (RN-13);
     - servicio inexistente, inactivo o de otra empresa (RN-71);
     - profesional inexistente, inactivo, de otra empresa o que no presta el servicio (RN-72 y RN-73);
     - observaciones no válidas;
     - usuario sin teléfono (RN-82).
  3. Abre la transacción (`arquitectura.md` §9.4).
  4. **Recomprobación con el motor** (RN-75, RN-76; duda 5): si el hueco no está entre los que devuelve, da `HuecoYaNoDisponible`.
  5. **Alta de la reserva:** `Reserva.Crear` con las copias (RN-78 y RN-79). `FinUtc` lo calcula el dominio (RN-74).
  6. **Guardado:** si llega la excepción de la duda 6, `Fallo(HuecoYaNoDisponible)`, sin reintento (RN-80).
  7. Confirma la transacción y devuelve `Resultado<Guid>`.
- **Hecho cuando:** `PruebasCrearReserva` está en verde.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter PruebasCrearReserva`.

### T7 — Cancelar y completar (≈ 0,75 h)

- **Ficheros:** `CancelarReservaCliente.cs`, `CancelarReservaEmpresa.cs` y `CompletarReserva.cs`, con sus comandos, en `Aplicacion/CasosUso/Reservas/`.
- **Implementa:**
  - **CU-10:**
    - usa el repositorio del eje cliente;
    - una reserva ajena o inexistente da `NoEncontrado` (RN-95, RN-03);
    - si el estado o la hora no lo permiten, da `EstadoNoValido` (RN-91, RN-93).
  - **CU-20:**
    - usa `IRepositorioReservas` con contexto de empresa;
    - sin límite de hora (RN-94).
  - **CU-21:** solo si `FinUtc < ahora` (RN-96).
  - **Comprobación previa:** el caso de uso comprueba las precondiciones antes de llamar al dominio y las convierte en `Resultado` (`arquitectura.md` §6).
  - **Sin transacción explícita**, porque solo se toca un agregado (`arquitectura.md` §9.4).
- **Hecho cuando:** pasan sus tres ficheros de pruebas.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "CancelarReserva|CompletarReserva"`.

### T8 — Registro en DI y sabotajes (≈ 0,5 h)

- **Ficheros:**
  - modifica `Aplicacion/InyeccionDependencias.cs` (los cuatro casos de uso, scoped) y `Infraestructura/InyeccionDependencias.cs` (contexto de usuario y repositorio del eje cliente, scoped);
  - modifica `PruebasIntegracion/PruebasInyeccionDependencias.cs`;
  - los sabotajes de §6 son temporales y se deshacen.
- **Implementa:** `arquitectura.md` §10.
- **Hecho cuando:**
  - todo lo nuevo se resuelve como scoped;
  - cada sabotaje hace fallar su prueba y se restaura el código.
- **Verificación:** `dotnet test` completo y las anotaciones de los sabotajes en §11.

### T9 — Documentación (≈ 0,5 h)

- **Ficheros:**
  - `AGENTS.md` §8: tercera familia, el eje cliente, con la dirección de referencias de §4;
  - `docs/arquitectura.md`:
    - §2.3: el contexto de usuario y el repositorio del eje cliente;
    - §5.1: el ejemplo de `CrearReserva` sin `IValidator`, por la duda 7;
    - §9.5: cómo llega el 23P01 a `Aplicacion`, por la duda 6;
    - §4.2: el diagrama de la creación, alineado con el código (importante de la revisión 18:39);
    - §7: la duplicación consciente en las transiciones de estado (menor 2 de la revisión 21:59);
    - §10: `IContextoUsuario` e `IRepositorioReservasCliente` en la tabla de ciclos de vida (menor 3 de la revisión 18:39);
  - `docs/modelo-dominio.md` §4.10: la sobrecarga de `Crear`, por la duda 3;
  - este fichero: §7 con la evidencia y §11 con las decisiones de implementación.
- **Hecho cuando:** ningún documento contradice el código.
- **Verificación:** lectura cruzada y `grep` de los nombres nuevos en `docs/`.

**Suma: 7 h de 7 h presupuestadas.** No hay margen. Si una tarea se desvía más de un 30 %, se aplica la regla de `PROJECT_PLAN.md` §4: la Fase 11 no se recorta.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `CancelarPorCliente`: confirmada y futura pasa a `Cancelada` con `FechaCancelacion` y `FechaModificacion` | U | `Entidades/PruebasReserva.cs` | RN-91, RN-98 | T1 |
| `CancelarPorCliente`: lanza si `InicioUtc == ahora` o es anterior; pasa si es un instante posterior | U | ídem | RN-93 (extremos) | T1 |
| `CancelarPorCliente`: lanza sobre una cancelada o una completada, y repetirla lanza | U | ídem | RN-91, RN-92 | T1 |
| `Completar`: pasa con `FinUtc < ahora`; lanza con `FinUtc == ahora` y con `FinUtc > ahora` | U | ídem | RN-96 (extremos) | T1 |
| `Completar`: lanza sobre una cancelada, y repetirla lanza | U | ídem | RN-91, RN-92 | T1 |
| `CancelarPorEmpresa` sobre una reserva completada lanza | U | ídem | RN-91, RN-92; pendiente de la Fase 8 | T1 |
| La sobrecarga de `Crear`: `FinUtc = InicioUtc + duración`, copias de precio y duración, `Confirmada` | U | ídem | RN-74, RN-78, RN-79 | T1 |
| `ServicioDisponibilidadDto` copia `Precio` | U | `Dtos/Servicios/PruebasServicioDisponibilidadDto.cs` | RN-78 | T2 |
| Crear: caso normal persistido, con copias, `Confirmada` y `FinUtc` calculado en servidor | I | `CasosUso/Reservas/PruebasCrearReserva.cs` | RN-74, RN-78, RN-79 | T2 |
| Crear: sin usuario da `NoAutorizado` | I | ídem | RN-70 | T2 |
| Crear: empresa inactiva; servicio inexistente, inactivo o de otra empresa; profesional inactivo, de otra empresa o que no presta el servicio. Todos dan `Validacion` y no crean nada | I | ídem | RN-13, RN-71, RN-72, RN-73 | T2 |
| Crear: usuario sin teléfono, u observaciones de más de 500 caracteres, da `Validacion` | I | ídem | RN-82 | T2 |
| Crear: hueco fuera de la antelación mínima o de la ventana da `HuecoYaNoDisponible` | I | ídem | RN-75 | T2 |
| Crear: hueco ya ocupado por una reserva confirmada da `HuecoYaNoDisponible` y no deja nada | I | ídem | RN-76, RN-80 | T2 |
| Crear: una reserva contigua (`fin == inicio`) se acepta | I | ídem | RN-50, RN-77 | T2 |
| Crear: el precio copiado se conserva aunque el servicio cambie después | I | ídem | RN-78 | T2 |
| Cancelar como cliente: caso normal persistido | I | `CasosUso/Reservas/PruebasCancelarReservaCliente.cs` | RN-91, RN-98 | T2 |
| Cancelar como cliente: reserva de otro usuario o inexistente da `NoEncontrado`, y la ajena queda intacta | I | ídem | RN-95, RN-03 | T2 |
| Cancelar como cliente: ya iniciada, cancelada o completada da `EstadoNoValido` | I | ídem | RN-91, RN-93 | T2 |
| Cancelar como cliente: sin usuario da `NoAutorizado` | I | ídem | RN-70 | T2 |
| Cancelar como cliente: el hueco se puede volver a reservar y el motor lo vuelve a ofrecer | I | ídem | RN-97 ⚑ | T2 |
| Cancelar como empresa: caso normal, incluida una reserva confirmada ya pasada | I | `CasosUso/Reservas/PruebasCancelarReservaEmpresa.cs` | RN-94, RN-98 | T2 |
| Cancelar como empresa: de otra empresa o inexistente da `NoEncontrado`; cancelada o completada da `EstadoNoValido` | I | ídem | RN-03, RN-91 | T2 |
| Completar: caso normal con `FinUtc < ahora` | I | `CasosUso/Reservas/PruebasCompletarReserva.cs` | RN-96 | T2 |
| Completar: `FinUtc >= ahora` o cancelada da `EstadoNoValido`; de otra empresa o inexistente da `NoEncontrado` | I | ídem | RN-96, RN-03 | T2 |
| Repositorio del eje cliente: filtra por usuario en la consulta, no encuentra la de otro usuario, carga con seguimiento para modificar y agregar la de otro usuario lanza | I | `Repositorios/Pruebas<Repositorio del eje cliente>.cs` | RN-05, RN-95 | T2 |
| Contexto de usuario: lee el claim y, sin usuario, `TieneUsuario` es falso | I | `Identidad/Pruebas<Contexto de usuario>.cs` | RN-70 | T2 |
| Contacto del cliente: devuelve el nombre y el teléfono descifrados; un usuario inexistente no se encuentra | I | `Identidad/PruebasServicioCuentas.cs` | RN-78, AGENTS §8 quater | T2 |
| `TraductorErroresPostgres`: 23P01 se traduce y otro `SqlState` no | I | `Errores/PruebasTraductorErroresPostgres.cs` | RN-80 | T2 |
| `UnidadTrabajo`: dos reservas solapadas insertadas directamente lanzan la excepción de la duda 6 | I | `Persistencia/PruebasUnidadTrabajo.cs` | RN-77, RN-80 | T2 |
| `ConsultasDisponibilidad` devuelve el `Precio` del servicio | I | `Consultas/PruebasConsultasDisponibilidad.cs` | RN-78 | T2 |
| **Concurrencia:** 10 tareas sobre el mismo hueco, cada una con su ámbito y su `ContextoDatos`, dan exactamente 1 éxito, 9 `HuecoYaNoDisponible` y 1 fila en `reservas`. Se repite 5 iteraciones | C | `Reservas/PruebasConcurrenciaReservas.cs` | RN-76, RN-77, RN-80 (SPEC §16.3) | T2 |
| La restricción `ex_reservas_sin_solape` existe en el catálogo de PostgreSQL | I | `Persistencia/PruebasEsquema.cs` (sin cambios) | RN-77, R-3 | — |
| Lo nuevo se registra como scoped | I | `PruebasInyeccionDependencias.cs` | `arquitectura.md` §10 | T8 |
| Las cinco pruebas de forma siguen en verde, y los métodos nuevos de `Reserva` solo reciben objetos de valor, `Guid` y `DateTime` | A | `Arquitectura/PruebasArquitectura.cs` (sin cambios) | AGENTS §7 | T3 |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican, con su motivo:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `Reserva` (`CancelarPorCliente`, `Completar`) | Evento de dominio que registra | Los eventos son de la Fase 11b (SPEC §19) |
| Los cuatro casos de uso | Eventos encolados | Fase 11b |
| `CancelarReservaCliente` | Recurso de otra empresa | El eje cliente no filtra por empresa (RN-05). El caso equivalente es la reserva de otro usuario, que sí se prueba |
| `CancelarReservaCliente`, `CancelarReservaEmpresa`, `CompletarReserva` | Fallo a mitad sin dejar nada | Tocan un solo agregado, sin transacción explícita (`arquitectura.md` §9.4) |
| `CrearReserva` | Fallo a mitad sin dejar nada (aparte) | Lo cubren el hueco ocupado y la prueba de concurrencia: tras el rechazo no queda ninguna fila |
| Repositorio del eje cliente | Lectura sin seguimiento | Solo tiene `ObtenerParaModificarAsync`, que **sí** lleva seguimiento (`arquitectura.md` §9.3) |
| `CrearReserva` | Recurso de otra empresa da `NoEncontrado` | Un servicio o un profesional de otra empresa da `Validacion`, porque es una comprobación previa con su motivo concreto (CU-07 6a, T6). Lo cubren `Un_servicio_de_otra_empresa_da_validacion` y `Un_profesional_de_otra_empresa_da_validacion`. *Revisión 2026-10-12 23:11* |
| `ClaimsSesion` | Fichero de pruebas propio | Lo cubren por completo `PruebasContextoEmpresa` y `PruebasContextoUsuario`: con claim, sin claim, claim no válido, estado sin resolver y estado nunca fijado. *Revisión 2026-10-12 22:26* |
| `CancelarReservaCliente`, `CancelarReservaEmpresa`, `CompletarReserva` | Validación (`Validacion`) | El comando solo lleva `ReservaId`: no hay entrada que validar. Un identificador inexistente da `NoEncontrado`, que sí se prueba. *Revisión 2026-10-12 21:59* |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-80 (y RN-77) | `UnidadTrabajo` no traduce el 23P01 | `PruebasConcurrenciaReservas` | ✔ 2026-10-12: falló `Diez_peticiones_simultaneas_sobre_el_mismo_hueco_crean_una_sola_reserva`, y también `PruebasUnidadTrabajo.Dos_reservas_solapadas_del_mismo_profesional_lanzan_hueco_ocupado`. Código restaurado |
| RN-76 | `CrearReserva` se salta la recomprobación con el motor | `PruebasCrearReserva`, hueco fuera de la antelación mínima | ✔ 2026-10-12: falló `Un_hueco_dentro_de_la_antelacion_minima_da_hueco_ya_no_disponible`, y también las de la ventana y la fecha que no corresponde. Código restaurado |
| RN-97 | `ConsultasDisponibilidad.ObtenerReservasActivasAsync` sin el filtro de estado | `PruebasCancelarReservaCliente`, volver a reservar el hueco | ✔ 2026-10-12: falló `El_hueco_cancelado_se_vuelve_a_ofrecer_y_se_puede_volver_a_reservar`. Código restaurado |

## 7. Criterios de aceptación

- [x] Las pruebas de T1 y T2 se escribieron antes que la implementación y el desarrollador las confirmó (CLAUDE.md). *Confirmadas el 2026-10-12, con 75 pruebas en rojo contra los esqueletos.*
- [x] Los criterios de CU-07 que son de esta fase (`PruebasCrearReserva`, `PruebasConcurrenciaReservas`):
  - no se puede reservar sin autenticación;
  - no se puede reservar con un profesional que no presta el servicio;
  - `FinUtc` se calcula en servidor;
  - reservar un hueco ocupado da un error controlado;
  - N peticiones concurrentes producen exactamente una reserva;
  - el precio se conserva.

  Quedan fuera los criterios de teléfono en la interfaz y los de correo (fases 14 y 16b).
- [x] Los criterios de CU-10 que son de esta fase (`PruebasCancelarReservaCliente`):
  - no se cancela la reserva de otro;
  - no se cancela una reserva ya iniciada;
  - no se cancela dos veces;
  - el hueco vuelve a ofrecerse.

  Queda fuera el aviso por correo (Fase 16b).
- [x] CU-20 y CU-21 cumplen RN-94 y RN-96 (`PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`).
- [x] `"23P01"` aparece una sola vez en `src/`, en `TraductorErroresPostgres` (`arquitectura.md` §9.5). Comprobado con `grep`.
- [x] Ningún contrato de `Dominio/Repositorios` nombra un puerto de `Aplicacion` (§4). Comprobado con `grep IContexto`.
- [x] La conversión entre hora local y UTC sigue solo en el motor (RN-102): `CrearReserva` no usa `TimeZoneInfo`, y guarda el `InicioUtc` del hueco que devuelve el motor.
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 314 unitarias y 210 de integración.

**Esta fase cierra el Control 3**, copiado literal de `PROJECT_PLAN.md` §3:

```text
Pruebas de integracion de reservas en verde
Prueba de concurrencia en verde contra PostgreSQL real (Testcontainers)
La restriccion ex_reservas_sin_solape EXISTE en la base de datos de pruebas
```

La tercera línea se comprueba consultando el catálogo de PostgreSQL (`PruebasEsquema`), no fiándose de que la migración se aplicó (R-3).

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | Dos operaciones a la vez sobre el mismo `ContextoDatos` | Cada tarea de la prueba de concurrencia usa su propio ámbito (`arquitectura.md` §12.3). La recomprobación del motor y la inserción son secuenciales dentro del caso de uso |
| R-2 | `DateTime` sin `Kind == Utc` al crear la reserva | `FranjaHoraria` exige UTC en su constructor y `IRelojSistema` y el motor devuelven UTC. El caso normal de `PruebasCrearReserva` persiste contra PostgreSQL real, que rechazaría un `Kind` distinto |
| R-3 | La restricción no llega a la base de pruebas y la prueba de concurrencia pasa sin probar nada | Migraciones, nunca `EnsureCreated`. `PruebasEsquema` consulta `pg_constraint`. El sabotaje de RN-80 demuestra que la prueba de concurrencia depende de la restricción |
| R-11 | Las copias de `Reserva` en claro | No se toca: los conversores de cifrado son de la Fase 5. La prueba con SQL crudo es de la Fase 17 |

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Eventos `ReservaConfirmada` y `ReservaCancelada`, y su publicación después del commit (CU-07 paso 11, CU-10 paso 8) | Fase 11b |
| Correos de RN-146 a RN-149 | Fase 16b |
| Pantalla `/book/{slug}`, botón deshabilitado y testigo de idempotencia (SPEC §13, capa 1) | Fase 14 |
| Pedir el teléfono en el flujo y guardarlo en el perfil (RN-81) | Fase 14 |
| Listados y detalle (CU-08, CU-09 y CU-19) y pantallas de cancelar y completar | Fases 15 y 16 |
| Cancelación en cascada por baja de cuenta (RN-137) | Fase 15 |
| Creación manual de reservas por la empresa | Descartada (`reglas-negocio.md` §13) |
| Prueba con SQL crudo de que `reservas` no tiene datos en claro (RN-163) | Fase 17 |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-03 | T7 | `PruebasCancelarReservaEmpresa`, `PruebasCompletarReserva`: otra empresa da `NoEncontrado` |
| RN-05 | T4 | Prueba del repositorio del eje cliente |
| RN-13 | T6 | `PruebasCrearReserva`: empresa inactiva |
| RN-70 | T6, T7 | `PruebasCrearReserva`, `PruebasCancelarReservaCliente`: sin usuario |
| RN-71, RN-72, RN-73 | T6 | `PruebasCrearReserva`: validaciones |
| RN-74 ⚑ | T3, T6 | `PruebasReserva` (sobrecarga de `Crear`), `PruebasCrearReserva` (caso normal) |
| RN-75 | T6 | `PruebasCrearReserva`: antelación y ventana |
| RN-76 ⚑ | T6 | `PruebasCrearReserva`: hueco ocupado; concurrencia; sabotaje |
| RN-77 ⚑ | T5 | `PruebasUnidadTrabajo`, `PruebasConcurrenciaReservas`, `PruebasEsquema` |
| RN-78 | T4, T6 | `PruebasCrearReserva` (copias y precio conservado), `PruebasServicioCuentas`, `PruebasServicioDisponibilidadDto` |
| RN-79 | T3, T6 | `PruebasReserva`, `PruebasCrearReserva` |
| RN-80 | T5, T6 | `PruebasTraductorErroresPostgres`, `PruebasConcurrenciaReservas`; sabotaje |
| RN-81 | — | Fuera de alcance (Fase 14). Aquí solo se rechaza la falta de teléfono (RN-82) |
| RN-82 | T6 | `PruebasCrearReserva`: sin teléfono |
| RN-83 | — | Lo cubre RN-135 (`PruebasConfirmacionUsuarioActivo`, Fase 6): un usuario inactivo no tiene sesión |
| RN-90 | — | Estructural: `EstadoReserva` no cambia |
| RN-91, RN-92 | T3, T7 | `PruebasReserva`, y los tres casos de uso de cancelar y completar |
| RN-93 | T3, T7 | `PruebasReserva` (extremos), `PruebasCancelarReservaCliente` |
| RN-94 | T7 | `PruebasCancelarReservaEmpresa`: reserva pasada |
| RN-95 | T4, T7 | `PruebasCancelarReservaCliente`, prueba del repositorio del eje cliente |
| RN-96 | T3, T7 | `PruebasReserva` (extremos), `PruebasCompletarReserva` |
| RN-97 ⚑ | T7 | `PruebasCancelarReservaCliente`: volver a reservar; sabotaje |
| RN-98 | T3, T7 | `PruebasReserva`, casos de uso de cancelar |
| RN-102 | T6 | Criterio de §7: `CrearReserva` no convierte zonas |
| CU-07 | T6 | `PruebasCrearReserva`, `PruebasConcurrenciaReservas` |
| CU-10 | T7 | `PruebasCancelarReservaCliente` |
| CU-20 | T7 | `PruebasCancelarReservaEmpresa` |
| CU-21 | T7 | `PruebasCompletarReserva` |
| Pendiente de la Fase 8 | T1 | `PruebasReserva`: `CancelarPorEmpresa` sobre una completada |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-10-12 con la opción recomendada.

### Duda 1 — ¿Cómo conoce el caso de uso al usuario autenticado? *(resuelta: a)*

- **Pregunta:**
  - `CrearReserva` (RN-70) y `CancelarReservaCliente` (RN-95) necesitan el `UsuarioId` de quien actúa.
  - Son del eje cliente, que no tiene claim `empresa_id`, así que `IContextoEmpresa` no sirve.
  - Ningún documento define un puerto para el usuario actual.
- **Opciones:**
  - (a) Un puerto nuevo en `Aplicacion/Abstracciones`, con la misma forma que `IContextoEmpresa`: el identificador del usuario y si hay usuario. Lee el claim de identificador del `ClaimsPrincipal` y se implementa en `Infraestructura/Identidad`. Nombre propuesto: `IContextoUsuario`, con `UsuarioActualId` y `TieneUsuario`.
  - (b) El comando lleva el `UsuarioId`, que `Web` rellena a partir del estado de autenticación.
- **Recomendación y motivo:** (a). El identificador sale de un claim y no de la entrada, como pide AGENTS §8 para la empresa. Con (b), un componente mal escrito podría reservar o cancelar en nombre de otro.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2, T4, T6 y T7.

### Duda 2 — ¿Cómo se leen y escriben las reservas desde el eje cliente? *(resuelta: a)*

- **Pregunta:**
  - `IRepositorioReservas` filtra por `EmpresaActualId`, y el cliente no tiene empresa.
  - SPEC §10.3 y RN-05 definen el eje cliente, que filtra por `UsuarioId`, pero AGENTS §8 solo recoge dos familias de acceso a datos.
- **Opciones:**
  - (a) Un contrato nuevo en `Dominio/Repositorios` **que solo usa tipos de `Dominio`**: `ObtenerParaModificarAsync(Guid id)` y `Agregar(Reserva)`.
    - Su **implementación** en `Infraestructura/Repositorios` recibe el contexto de usuario de la duda 1 (el contrato no lo nombra).
    - Filtra por `UsuarioId` en la consulta.
    - Al agregar, valida que la reserva sea del usuario actual.
    - Supone una tercera familia en AGENTS §8.
    - Nombre propuesto: `IRepositorioReservasCliente`.
  - (b) Añadir métodos por usuario a `IRepositorioReservas`, con una implementación que, para ellos, no dependa de `IContextoEmpresa`.
- **Recomendación y motivo:** (a). Mantiene cada familia con un solo eje de filtrado, como en SPEC §10.3. Con (b), un mismo repositorio filtraría a veces por empresa y a veces por usuario, que es justo la mezcla que AGENTS §8 prohíbe.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2, T4, T6 y T7.

### Duda 3 — ¿De dónde salen los datos del servicio para `Reserva.Crear`? *(resuelta: a)*

- **Pregunta:** `Reserva.Crear` recibe hoy la entidad `Servicio` (`modelo-dominio.md` §4.10). En el eje cliente, `IRepositorioServicios` no la puede cargar, porque filtra por empresa.
- **Opciones:**
  - (a) Una sobrecarga de `Crear` que recibe `Guid servicioId`, `Duracion` y `Precio`, en la que delega la `Crear(Servicio)` actual (que sigue validando que el servicio es de la empresa).
    - `ServicioDisponibilidadDto` gana `Precio`, y `CrearReserva` lo lee de `IConsultasDisponibilidad`.
    - `FinUtc` lo sigue calculando el dominio a partir de una duración leída en servidor (RN-74).
  - (b) Un método de lectura del servicio en el contrato de la duda 2.
  - (c) Mantener solo `Crear(Servicio)` y cargar la entidad con un acceso nuevo sin contexto.
- **Recomendación y motivo:** (a). No obliga a cargar un agregado ajeno sin contexto y no cambia ninguna prueba que ya existe. (b) mezcla dos raíces en un repositorio y (c) abre una vía de lectura de entidades sin filtrar.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada. Cambia `modelo-dominio.md` §4.10 (T9).
- **Bloqueaba:** T1, T2, T3, T4 y T6.

### Duda 4 — ¿De dónde salen el nombre y el teléfono del cliente? *(resuelta: a)*

- **Pregunta:** RN-78 obliga a copiar `NombreCliente` y `TelefonoCliente`. Están en `Usuario`, cifrados (RN-162), y solo se leen con `UserManager` (AGENTS §8 quater).
- **Opciones:**
  - (a) Un método nuevo en `IServicioCuentas`, que recibe el `UsuarioId` y devuelve el nombre y el teléfono como objetos de valor (`NombrePersona`, `Telefono?`).
    - Si no hay teléfono, `CrearReserva` devuelve `Validacion` (RN-82).
    - Nombre propuesto: `ObtenerContactoAsync`.
  - (b) Que el comando lleve el nombre y el teléfono.
- **Recomendación y motivo:** (a). La copia sale del perfil en servidor, como dice RN-78, y no de la entrada. Pedir el teléfono en el flujo y guardarlo en el perfil (RN-81) es de la Fase 14.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2, T4 y T6.

### Duda 5 — ¿Cómo se recomprueba el hueco sin convertir zonas fuera del motor? *(resuelta: a)*

- **Pregunta:**
  - RN-76 exige recomprobar con el motor, y el motor recibe una `DateOnly fecha` local (SPEC §12.1).
  - Calcular esa fecha a partir de `InicioUtc` en el caso de uso rompería RN-102.
- **Opciones:**
  - (a) El comando lleva `DateOnly Fecha`, el día elegido en la interfaz, además de `InicioUtc`.
    - `CrearReserva` llama al motor con esa fecha y exige un hueco con ese profesional y ese `InicioUtc`. Si no lo hay, devuelve `HuecoYaNoDisponible`.
    - Antes, comprueba empresa, servicio y profesional para dar `Validacion` con el motivo concreto (CU-07 6a).
  - (b) Añadir al motor un método que reciba `InicioUtc`. Cambia el contrato de SPEC §12.1.
- **Recomendación y motivo:** (a). No toca el contrato del motor y la conversión sigue en un único punto. Con una fecha que no corresponda al `InicioUtc`, el hueco no se encuentra y se rechaza.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2 y T6.

### Duda 6 — ¿Cómo llega el 23P01 a `Aplicacion` sin que sepa que existe PostgreSQL? *(resuelta: a)*

- **Pregunta:**
  - `arquitectura.md` §9.5 sitúa la traducción en `TraductorErroresPostgres`.
  - La inserción ocurre en `GuardarCambiosAsync`, y los repositorios no guardan (AGENTS §8 bis).
  - Ningún documento dice cómo llega el resultado al caso de uso.
- **Opciones:**
  - (a) `UnidadTrabajo.GuardarCambiosAsync` usa `TraductorErroresPostgres`. Ante un 23P01 lanza una excepción definida en `Aplicacion/Compartido`, que `CrearReserva` captura y convierte en `Fallo(HuecoYaNoDisponible)`.
    - Otros errores suben tal cual.
    - Nombre propuesto: `HuecoOcupadoException` (sufijo `Exception`, AGENTS §5).
  - (b) `GuardarCambiosAsync` devuelve `Resultado`, lo que cambia todos los llamadores.
- **Recomendación y motivo:** (a). Es el cambio mínimo. La excepción solo atraviesa una capa y se convierte en `Resultado` en el mismo caso de uso (A-02), y `Aplicacion` no ve tipos de Npgsql.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2, T5 y T6.

### Duda 7 — ¿Se usa FluentValidation en `CrearReserva`? *(resuelta: a)*

- **Pregunta:** el ejemplo de `arquitectura.md` §5.1 inyecta `IValidator<CrearReservaComando>`, pero ningún caso de uso de las fases 8 y 9 usa FluentValidation: validan construyendo objetos de valor y devolviendo `Resultado`.
- **Opciones:**
  - (a) Seguir el patrón actual, sin validador. Las validaciones de la creación son de existencia y estado, y se resuelven con lecturas.
  - (b) Introducir el primer validador de FluentValidation.
- **Recomendación y motivo:** (a). Es coherente con el código que ya existe y no duplica comprobaciones. Se corrige el ejemplo de §5.1 (T9).
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T6 y T9.

### Duda 8 — ¿Cuántas tareas e iteraciones lleva la prueba de concurrencia? *(resuelta: a)*

- **Pregunta:** SPEC §16.3 dice "N tareas" y "varias iteraciones", sin fijar números.
- **Opciones:**
  - (a) N = 10 tareas y 5 iteraciones, con el mismo cliente. La restricción es por profesional, no por cliente (`reglas-negocio.md` §13).
  - (b) Otros valores.
- **Recomendación y motivo:** (a). Diez inserciones simultáneas bastan para provocar la carrera, y cinco repeticiones descartan que pase por azar sin alargar la suite.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T2.

### Decisiones de implementación

- **Desviación 1, el interbloqueo 40P01** (decidido por el desarrollador el 2026-10-12):
  - **Qué pasó:** la prueba de concurrencia descubrió que dos inserciones que chocan en `ex_reservas_sin_solape` pueden esperarse mutuamente. PostgreSQL aborta entonces una con `40P01` en vez de con `23P01`, y Npgsql lo entrega envuelto en `InvalidOperationException` como fallo transitorio. La reserva perdedora acababa en un error técnico.
  - **Solución elegida:** `TraductorErroresPostgres.EsHuecoOcupado(Exception, bool insertaReservas)` busca la `PostgresException` en la cadena interna. Trata `40P01` como hueco ocupado **solo** si el guardado inserta una `Reserva`; `UnidadTrabajo` lo comprueba en el `ChangeTracker` antes de guardar.
  - **Lo que se respeta:** sin reintento (RN-80) y sin bloqueos, como fija SPEC §13. Documentado en `arquitectura.md` §9.5.
  - **Pruebas añadidas por esta decisión:** tres en `PruebasTraductorErroresPostgres`: el interbloqueo directo, el interbloqueo envuelto y el interbloqueo sin insertar reservas.
- **Corrección del bloqueante de la revisión 2026-10-12 18:39.**
  - **El defecto:** tras un hueco ocupado, la reserva rechazada se quedaba en el rastreador del `ContextoDatos`, que en `InteractiveServer` dura lo que el circuito, y el siguiente guardado la reinsertaba.
  - **La corrección:** `UnidadTrabajo` desengancha las `Reserva` en estado `Added` antes de lanzar `HuecoOcupadoException`.
  - **La prueba:** `PruebasUnidadTrabajo.Tras_un_hueco_ocupado_el_mismo_ambito_puede_guardar_otra_reserva`. Fallaba antes de la corrección y pasa después.
  - **Documentación:** el diagrama de `arquitectura.md` §4.2 se actualizó al flujo real (hallazgo importante).
- **Corrección de los menores de la revisión 2026-10-12 18:39:**
  - La tabla de ciclos de vida de `arquitectura.md` §10 incluye `IContextoUsuario` e `IRepositorioReservasCliente`.
  - La lectura del identificador desde un claim pasa a `Identidad/ClaimsSesion` (`internal`), y la usan `ContextoEmpresa` y `ContextoUsuario`.
  - `ClaimsSesion` no tiene fichero de pruebas propio: la cubren por completo `PruebasContextoEmpresa` y `PruebasContextoUsuario` (con claim, sin claim, claim no válido, estado sin resolver y estado nunca fijado).
- **`IServicioCuentas.ObtenerContactoAsync`** devuelve `(NombrePersona Nombre, Telefono? Telefono)?`, para no crear un tipo nuevo; un usuario inexistente da `null`.
- **`CrearReserva` guarda el `InicioUtc` del hueco que devuelve el motor**, no el del comando. Siempre es UTC (R-2) y coincide con el pedido.
- **`Completar` actualiza `FechaModificacion`**, como las dos cancelaciones.
- **Las comprobaciones previas de `CrearReserva` usan `IConsultasDisponibilidad`**: empresa, servicio y profesionales que prestan el servicio. No se añadió ninguna lectura nueva.
- **Utilidad de pruebas `EscenarioReserva`**, en `PruebasIntegracion/Infraestructura/`: monta empresa, servicio, profesional con horario de sábado y lunes de 9:00 a 14:00, y cliente.
- **Pruebas además de las de §6:**
  - el límite exacto de la antelación mínima se acepta;
  - una fecha que no corresponde al `InicioUtc` da `HuecoYaNoDisponible`;
  - las fechas que no son UTC;
  - otro error de la base de datos sube sin traducir.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas 11` ejecutado y sus importantes anotados (`PROJECT_PLAN.md` §3)
- [ ] Commit hecho (mensaje libre)
- [ ] Control 3 superado (evidencias en §7)
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna)
