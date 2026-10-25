# Fase 11b — Eventos de dominio, publicador, cola en memoria y procesador

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 6 h (SPEC §19) |
| **Semana** | 4 (`PROJECT_PLAN.md` §2), junto a las fases 12, 13 y 14 |
| **Cierra** | ninguno. Lo que construye se comprueba en el Control 4b, al final de la 16b (`PROJECT_PLAN.md` §3) |
| **Generada** | 2026-10-12 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

**Al terminar existen:**
- los seis eventos de dominio y su registro en las entidades (`EntidadBase.Eventos`, `RegistrarEvento`, `LimpiarEventos`);
- los puertos `IPublicadorEventos` e `IManejadorEvento<TEvento>`;
- la cola en memoria `ColaEventos` y el `ProcesadorEventos`, que es un `BackgroundService` con un ámbito propio por evento y que sobrevive a un manejador que lanza.

**Qué hacen los casos de uso de reservas y de empresa:** publican sus eventos **después** de confirmar la transacción (D-21, A-11, A-12).

**Qué no hay todavía:** ningún manejador real. La infraestructura queda en pie "aunque todavía sin manejadores" (`PROJECT_PLAN.md` §2, semana 4); los manejadores y el correo son de la Fase 16b.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §4, D-21 | Las entidades registran los eventos y el caso de uso los publica después del commit. Cola en memoria con `Channel<T>` y `BackgroundService`. Nunca dentro de `SaveChanges` |
| SPEC §16 bis, §19, §21bis.3 | Por qué hacen falta la cola y los eventos: la cascada de 300 correos. Las 6 h de esta fase, que no se recorta. La cola volátil como limitación declarada |
| `arquitectura.md` A-11, A-12, §2.3 | Despacho después del commit; transporte con `Channel<T>` y `BackgroundService` con un ámbito nuevo por evento. Carpetas: `Dominio/Eventos`, `Aplicacion/Abstracciones`, `Infraestructura/Eventos` |
| `arquitectura.md` §13.1, §13.2, §13.7, §13.8, §13.9 | Código de referencia del caso de uso, de `ColaEventos` y de `ProcesadorEventos`. Ciclos de vida en DI, limitación de la cola volátil y pruebas |
| `modelo-dominio.md` M-07, §10 bis, §11 | `EntidadBase` con registro de eventos, los seis eventos, `OrigenCancelacion` y las cascadas |
| AGENTS §8 ter | Reglas de eventos y notificaciones: publicar después del commit, un ámbito por evento, el `try/catch` del procesador y, en pruebas, sin levantar el servicio alojado |
| RN-140 ⚑ | Nada se envía dentro de la transacción: primero se confirma y después se encola |
| RN-141 ⚑ | Un fallo en el procesamiento de un evento nunca deshace ni impide la operación. Se registra y se sigue |
| RN-142 | Los eventos se encolan en memoria y se procesan en segundo plano |
| RN-143 | Los eventos llevan identificadores, no entidades |
| RN-144 | Si el proceso se reinicia con eventos en cola, se pierden. Limitación declarada |
| RN-150 ⚑ (encolado) | La baja de una empresa encola un `ReservaCancelada` con origen `BajaEmpresa` por cada cita anulada. El correo es de la 16b |
| CU-07 paso 11 | `ReservaConfirmada` tras confirmar la transacción |
| CU-10 paso 8 | `ReservaCancelada` con origen `Cliente` |
| CU-11 paso 8 | `EmpresaRegistrada` tras confirmar la transacción |
| CU-20 | `ReservaCancelada` con origen `Empresa`, encolada después de confirmar |
| CU-23 paso 7 | **N + 1 eventos**: `EmpresaDadaDeBaja` más un `ReservaCancelada` con origen `BajaEmpresa` por cada cita |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `CrearReserva`, `CancelarReservaCliente`, `CancelarReservaEmpresa` (Fase 11), `DarDeBajaEmpresa` y `RegistrarEmpresa` (Fase 8) | ✔ comprobado |
| `IUnidadTrabajo` e `ITransaccion`, con `ConfirmarAsync` | ✔ comprobado |
| `BackgroundService` sin paquetes nuevos: Infraestructura ya tiene `FrameworkReference Microsoft.AspNetCore.App` | ✔ comprobado |
| `System.Threading.Channels`, en la biblioteca base de net10.0 | ✔ comprobado |
| `InternalsVisibleTo PlataformaReservas.PruebasIntegracion` en Infraestructura, necesario para la duda 9 | ✔ comprobado |
| `IEventoDominio`, `Dominio/Eventos/` y `OrigenCancelacion` | ✘ ausentes: se crean en esta fase (T1) |
| El registro de eventos en `EntidadBase` (hoy solo tiene `Id`) | ✘ ausente: se crea en esta fase (T1) |
| `IPublicadorEventos`, `IManejadorEvento`, `ColaEventos` y `ProcesadorEventos` | ✘ ausentes: se crean en esta fase (T3 y T4) |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Compartido/EntidadBase.cs`. `Eventos/` (nueva): `IEventoDominio` y los seis eventos. `Enumeraciones/OrigenCancelacion.cs`. `Entidades/Reserva.cs` (`Crear`, `CancelarPorCliente` y `CancelarPorEmpresa`, que recibe el origen). `Entidades/Empresa.cs` (`Crear` y `DarDeBaja`) |
| `src/PlataformaReservas.Aplicacion` | sí | `Abstracciones/IPublicadorEventos.cs` y `Abstracciones/IManejadorEvento.cs`. En `CasosUso/Reservas/`: `CrearReserva`, `CancelarReservaCliente` y `CancelarReservaEmpresa`. En `CasosUso/Empresas/`: `DarDeBajaEmpresa` y `RegistrarEmpresa` |
| `src/PlataformaReservas.Infraestructura` | sí | `Eventos/` (nueva): `ColaEventos.cs` y `ProcesadorEventos.cs`. `Persistencia/ContextoDatos.cs` (`Ignore` de `Eventos`, duda 8). `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Web` | no | `ProcesadorEventos` se registra en `AgregarInfraestructura` (§13.7) y `Program.cs` ya lo llama |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Entidades/PruebasReserva.cs` y `Entidades/PruebasEmpresa.cs`, más las llamadas existentes a `CancelarPorEmpresa` que cambian de firma (duda 6) |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Eventos/PruebasColaEventos.cs` y `Eventos/PruebasProcesadorEventos.cs` (nuevas). Los casos de uso de `CasosUso/Reservas/` y `CasosUso/Empresas/` de §6. `PruebasInyeccionDependencias.cs`. En `Infraestructura/`: el doble `PublicadorCaptura` y su ámbito en `BaseDatosFixture`, y `EscenarioEmpresa.EntidadSeguida` (ver §11, decisiones de implementación). Las llamadas existentes a `CancelarPorEmpresa`. `Multiempresa/PruebasAislamientoEmpresas.cs`: excluye `Eventos` de una comparación de equivalencia (ver §11, decisiones de implementación) |
| `docs/` | sí | `arquitectura.md` §1 (R-6), §13.1, §13.2, §13.7 y §13.9. `modelo-dominio.md` M-07, §6 (l. 595) y §10 bis. `reglas-negocio.md` (l. 410). Este fichero (duda 10) |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, no se tocan:
- `Web`;
- las migraciones, porque los eventos no se persisten;
- `Infraestructura/Identidad`, porque `CuentaRegistrada` y `CuentaDadaDeBaja` solo se definen (duda 7);
- `CompletarReserva` y `ReactivarEmpresa`, que no publican nada (`reglas-negocio.md` §11 ter, "Lo que sigue sin notificarse");
- nada del correo.

**Dirección de referencias.** `IEventoDominio`, los eventos y `EntidadBase` viven en `Dominio` y solo usan tipos de `Dominio` y de la biblioteca base. Los puertos `IPublicadorEventos` e `IManejadorEvento<TEvento>` viven en `Aplicacion/Abstracciones`, y ningún tipo de `Dominio` los nombra.

## 5. Tareas

### T1 — Eventos en el dominio (≈ 1 h)

- **Ficheros:**
  - crea `Dominio/Eventos/IEventoDominio.cs` y un fichero por evento: `EmpresaRegistrada`, `EmpresaDadaDeBaja`, `CuentaRegistrada`, `CuentaDadaDeBaja`, `ReservaConfirmada` y `ReservaCancelada`;
  - crea `Dominio/Enumeraciones/OrigenCancelacion.cs`;
  - modifica `Dominio/Compartido/EntidadBase.cs` e `Infraestructura/Persistencia/ContextoDatos.cs`.
- **Implementa:** M-07; `modelo-dominio.md` §10bis.1 y §10bis.3, con las dudas 1, 5 y 8.
  - **`IEventoDominio`** declara `DateTime OcurridoEnUtc`. Cada evento es un `sealed record` posicional que lleva `OcurridoEnUtc` como último parámetro (duda 1).
  - **Los eventos de empresa no llevan `UsuarioId`** (duda 5): `EmpresaRegistrada(Guid EmpresaId, DateTime OcurridoEnUtc)` y `EmpresaDadaDeBaja(Guid EmpresaId, DateTime OcurridoEnUtc)`.
  - **`OrigenCancelacion`** tiene los valores `Cliente`, `Empresa`, `BajaEmpresa` y `BajaCuenta`.
  - **`EntidadBase`** tiene un campo privado `List<IEventoDominio>`, la propiedad pública `IReadOnlyCollection<IEventoDominio> Eventos`, el método `protected RegistrarEvento(IEventoDominio)` y el método `public LimpiarEventos()`.
  - **`ContextoDatos.OnModelCreating`** ignora `Eventos` en todo tipo que herede de `EntidadBase` (duda 8).
- **Hecho cuando:**
  - pasan las cinco pruebas de `PruebasArquitectura`, incluida la de colecciones mutables;
  - el modelo de EF no cambia: `dotnet ef migrations add` no genera diferencias.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias --filter PruebasArquitectura` y `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter PruebasEsquema`.

### T2 — Registro de eventos en las entidades (≈ 1 h)

- **Ficheros:**
  - modifica `Dominio/Entidades/Reserva.cs` y `Dominio/Entidades/Empresa.cs`;
  - modifica `PruebasUnitarias/Entidades/PruebasReserva.cs` y `PruebasUnitarias/Entidades/PruebasEmpresa.cs`;
  - ajusta las llamadas existentes a `CancelarPorEmpresa`, en `src` (`CancelarReservaEmpresa` y `DarDeBajaEmpresa`) y en las pruebas.
- **Implementa:** RN-143; CU-07, CU-10, CU-11, CU-20 y CU-23 (parte de dominio); dudas 5 y 6.
  - **`Reserva.Crear`** registra `ReservaConfirmada(Id, ahoraUtc)`. Lo hace solo la sobrecarga con `Guid servicioId`; la que recibe `Servicio` delega en ella, así que no hay registro doble.
  - **`CancelarPorCliente(ahoraUtc)`** registra `ReservaCancelada(Id, OrigenCancelacion.Cliente, ahoraUtc)`.
  - **`CancelarPorEmpresa`** pasa a recibir `CancelarPorEmpresa(DateTime ahoraUtc, OrigenCancelacion origen)` y registra `ReservaCancelada(Id, origen, ahoraUtc)`. Solo admite `Empresa` y `BajaEmpresa`; con otro origen lanza `DominioException` (duda 6).
  - **`Empresa.Crear`** registra `EmpresaRegistrada(Id, ahoraUtc)` y **`Empresa.DarDeBaja`** registra `EmpresaDadaDeBaja(Id, ahoraUtc)`.
  - **Sin evento:** `Completar` y `Reactivar`.
- **Hecho cuando:** pasan `PruebasReserva`, `PruebasEmpresa` y `PruebasArquitectura`.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias`.

### T3 — Puertos de publicación y de manejo (≈ 0,5 h)

- **Ficheros:** crea `Aplicacion/Abstracciones/IPublicadorEventos.cs` y `Aplicacion/Abstracciones/IManejadorEvento.cs`.
- **Implementa:** A-11; dudas 2 y 3.
  - `IPublicadorEventos.PublicarAsync(IReadOnlyCollection<IEventoDominio> eventos, CancellationToken ct)`, que devuelve `ValueTask`.
  - `IManejadorEvento<in TEvento> where TEvento : IEventoDominio`, con `Task ManejarAsync(TEvento evento, CancellationToken ct)`.
- **Hecho cuando:** compila y ningún tipo de `Dominio` nombra estos puertos.
- **Verificación:** `dotnet build`.

### T4 — Cola en memoria y procesador (≈ 1,5 h)

- **Ficheros:**
  - crea `Infraestructura/Eventos/ColaEventos.cs` e `Infraestructura/Eventos/ProcesadorEventos.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs`;
  - crea `PruebasIntegracion/Eventos/PruebasColaEventos.cs` y `PruebasIntegracion/Eventos/PruebasProcesadorEventos.cs`.
- **Implementa:** A-12; RN-141 (la parte del procesador), RN-142 y RN-144; R-1 y R-6; `arquitectura.md` §13.2 y §13.7; dudas 3 y 9.
  - **`ColaEventos : IPublicadorEventos`:**
    - usa un `Channel.CreateUnbounded<IEventoDominio>` con `SingleReader = true`;
    - `PublicarAsync` escribe los eventos uno a uno;
    - `LeerAsync(ct)` devuelve `ReadAllAsync`.
  - **`ProcesadorEventos : BackgroundService`:**
    - `ExecuteAsync` recorre `LeerAsync` y, por cada evento, llama a `ProcesarAsync` (`internal`, duda 9);
    - `ProcesarAsync` crea un ámbito con `IServiceScopeFactory`, resuelve `IEnumerable<IManejadorEvento<T>>` cerrado con el tipo del evento y los invoca;
    - **todo va dentro de `try/catch`**: registra el fallo con `LogError` y el tipo del evento, y sigue (R-6).
  - **Registro en DI** (§13.7):
    - `ColaEventos` como singleton;
    - `IPublicadorEventos` resuelve **la misma instancia**;
    - `AddHostedService<ProcesadorEventos>()`.
- **Hecho cuando:** pasan `PruebasColaEventos` y `PruebasProcesadorEventos`.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "PruebasColaEventos|PruebasProcesadorEventos"`.

### T5 — Los casos de uso publican después del commit (≈ 1,5 h)

- **Ficheros:**
  - modifica, en `Aplicacion/CasosUso/Reservas/`, `CrearReserva.cs`, `CancelarReservaCliente.cs` y `CancelarReservaEmpresa.cs`;
  - modifica, en `Aplicacion/CasosUso/Empresas/`, `DarDeBajaEmpresa.cs` y `RegistrarEmpresa.cs`;
  - crea `PruebasIntegracion/Infraestructura/PublicadorCaptura.cs` y modifica `BaseDatosFixture.cs`;
  - modifica las pruebas de esos cinco casos de uso.
- **Implementa:** RN-140, RN-150 (encolado) y RN-132 (sin cambio de regla); CU-07 paso 11, CU-10 paso 8, CU-11 paso 8, CU-20 y CU-23 paso 7; AGENTS §8 ter; duda 4.
  - **Dónde se publica:**
    - con transacción explícita (`CrearReserva`, `DarDeBajaEmpresa` y `RegistrarEmpresa`), justo después de `ConfirmarAsync`;
    - sin ella (`CancelarReservaCliente` y `CancelarReservaEmpresa`), justo después de `GuardarCambiosAsync`, que es su commit.
  - **Después de publicar,** el caso de uso llama a `LimpiarEventos`.
  - **Origen de las cancelaciones:**
    - `CancelarReservaEmpresa` pasa `OrigenCancelacion.Empresa`;
    - `DarDeBajaEmpresa` pasa `OrigenCancelacion.BajaEmpresa` a cada reserva futura y publica, de una vez, `EmpresaDadaDeBaja` más los N `ReservaCancelada`.
  - **Cuando no se publica:** si el resultado es un fallo (validación, hueco ocupado, no encontrado, estado no válido), no se publica nada.
  - **Si el publicador lanza,** la excepción sube sin capturarla y la operación ya está confirmada (duda 4).
  - **El doble `PublicadorCaptura`:**
    - se registra con ámbito en `BaseDatosFixture` y sustituye a `IPublicadorEventos` solo en las pruebas;
    - guarda los eventos publicados y tiene un interruptor `Fallar`;
    - `ColaEventos` sigue registrada para las pruebas de T4.
- **Hecho cuando:** pasan las pruebas de los cinco casos de uso de §6.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter "CasosUso"`.

### T6 — DI, sabotajes y documentación (≈ 0,5 h)

- **Ficheros:**
  - modifica `PruebasIntegracion/PruebasInyeccionDependencias.cs`;
  - en `docs/arquitectura.md`, modifica §13.1, §13.2 (firmas de las dudas 2, 3 y 9), §13.7 (`IManejadorEvento<T>` resuelto por tipo cerrado), §13.9 (el doble y la prueba con el publicador que falla) y la fila R-6 de §15 (Control 4b);
  - en `docs/modelo-dominio.md`, modifica M-07 (referencia a §10 bis), §6 l. 595 (la frase "descartada por coste" se refiere a la consistencia eventual entre agregados), §10bis.3 (records con `OcurridoEnUtc` y eventos de empresa sin `UsuarioId`) y el rango de reglas de §10 bis;
  - en `docs/reglas-negocio.md`, la l. 410: la fila "Notificación … en cascada" sale de las descartadas, porque la contradicen RN-150 y RN-151;
  - este fichero: §6 (sabotajes), §7 y §11.
- **Implementa:** `arquitectura.md` §13.7; duda 10.
- **Hecho cuando:**
  - `ColaEventos` es singleton e `IPublicadorEventos` resuelve la misma instancia;
  - `ProcesadorEventos` está registrado como servicio alojado;
  - los sabotajes hacen fallar su prueba;
  - ningún documento contradice el código.
- **Verificación:** `dotnet test` completo y lectura cruzada.

**Suma: 6 h de 6 h presupuestadas.** No hay margen. La fase no se recorta (SPEC §19, `PROJECT_PLAN.md` §4).

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `Crear` registra un único `ReservaConfirmada` con el `Id` y `OcurridoEnUtc`, también por la sobrecarga con `Servicio` | U | `Entidades/PruebasReserva.cs` | CU-07, RN-143 | T2 |
| `CancelarPorCliente` registra `ReservaCancelada` con origen `Cliente`; si lanza no registra nada, y repetirla no añade un segundo evento | U | ídem | CU-10 | T2 |
| `CancelarPorEmpresa` registra el origen recibido (`Empresa` o `BajaEmpresa`) y lanza con `Cliente` o `BajaCuenta` | U | ídem | CU-20, CU-23, duda 6 | T2 |
| `Completar` no registra ningún evento | U | ídem | reglas §11 ter | T2 |
| `LimpiarEventos` vacía `Eventos` | U | ídem | AGENTS §8 ter | T2 |
| `Empresa.Crear` registra `EmpresaRegistrada` y `DarDeBaja` registra `EmpresaDadaDeBaja`; `Reactivar` no registra nada, y una baja repetida que lanza no registra | U | `Entidades/PruebasEmpresa.cs` | CU-11, CU-23 | T2 |
| `PublicarAsync` sin lector termina enseguida y los eventos se leen después, en el mismo orden | I | `Eventos/PruebasColaEventos.cs` | RN-142 | T4 |
| `ProcesarAsync` invoca los manejadores del tipo del evento y no los de otros tipos | I | `Eventos/PruebasProcesadorEventos.cs` | A-12 | T4 |
| Cada evento se procesa en un ámbito nuevo: dos eventos reciben instancias scoped distintas | I | ídem | R-1, AGENTS §8 ter | T4 |
| Un manejador que lanza no propaga la excepción, se registra con nivel `Error` y el siguiente evento se procesa | I | ídem | RN-141, R-6 (Control 4b, línea 3) | T4 |
| Un evento sin manejadores no falla | I | ídem | A-12 | T4 |
| Humo: con `StartAsync`, un evento publicado en una `ColaEventos` propia llega a su manejador antes de un plazo acotado, y `StopAsync` termina limpio | I | ídem | A-12, duda 9 | T4 |
| `CrearReserva`: tras el éxito se publica exactamente un `ReservaConfirmada` y la reserva queda sin eventos | I | `CasosUso/Reservas/PruebasCrearReserva.cs` | RN-140, CU-07 | T5 |
| `CrearReserva`: validación y hueco ocupado no publican nada | I | ídem | RN-140 | T5 |
| `CrearReserva`: con un publicador que falla, la reserva queda confirmada en la base de datos | I | ídem | RN-140 ⚑, `arquitectura.md` §13.9 | T5 |
| `CancelarReservaCliente`: publica `ReservaCancelada` con origen `Cliente`; un fallo no publica nada | I | `CasosUso/Reservas/PruebasCancelarReservaCliente.cs` | CU-10 | T5 |
| `CancelarReservaEmpresa`: publica `ReservaCancelada` con origen `Empresa`; un fallo no publica nada | I | `CasosUso/Reservas/PruebasCancelarReservaEmpresa.cs` | CU-20 | T5 |
| `DarDeBajaEmpresa` con N reservas futuras publica N + 1 eventos: `EmpresaDadaDeBaja` y N `ReservaCancelada` con origen `BajaEmpresa`. Las pasadas y las completadas no generan evento | I | `CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs` | RN-150 ⚑, RN-132, CU-23 | T5 |
| `DarDeBajaEmpresa`: con un publicador que falla, la baja y las cancelaciones quedan confirmadas | I | ídem | RN-140 ⚑ | T5 |
| `RegistrarEmpresa`: publica `EmpresaRegistrada`; un correo duplicado o una validación no publican nada | I | `CasosUso/Empresas/PruebasRegistrarEmpresa.cs` | CU-11 | T5 |
| `ColaEventos` es singleton e `IPublicadorEventos` resuelve la misma instancia; `ProcesadorEventos` está registrado como `IHostedService` | I | `PruebasInyeccionDependencias.cs` | `arquitectura.md` §13.7 | T6 |
| Las cinco pruebas de forma siguen en verde con `Eventos` en `EntidadBase` | A | `Arquitectura/PruebasArquitectura.cs` (sin cambios) | AGENTS §7 | T1 |
| El esquema no cambia | I | `Persistencia/PruebasEsquema.cs` (sin cambios) | duda 8 | T1 |

Casos del catálogo de `AGENTS.md` §11 bis que no aplican, con su motivo:

| Clase | Caso omitido | Motivo |
|---|---|---|
| `EntidadBase` | Fichero de pruebas propio | Es abstracta: su registro de eventos se prueba a través de `Reserva` y `Empresa` |
| Los seis eventos y `OrigenCancelacion` | Fichero de pruebas propio | Son records y un enum sin lógica. Lo que contienen se comprueba en las pruebas de las entidades |
| `IPublicadorEventos`, `IManejadorEvento` | Pruebas | Son interfaces |
| `ColaEventos` | Rangos y extremos | No tiene reglas con números ni fechas. Es un canal sin límite |
| Los cinco casos de uso | Recurso de otra empresa, validación, fallo a mitad | Ya los cubren sus ficheros de la Fase 8 o de la 11. Esta fase solo añade lo relativo a la publicación |
| `ProcesadorEventos` | Repositorio y filtrado por empresa | No accede a datos. Los manejadores que lo harán son de la 16b |

### Sabotajes

| Regla ⚑ | Cambio temporal en el código | Prueba que debe fallar | Resultado |
|---|---|---|---|
| RN-140 | En `CrearReserva`, publicar **antes** de `ConfirmarAsync` | `PruebasCrearReserva`: la del publicador que falla, porque la reserva no llega a confirmarse | ✔ 2026-10-12: falló `Con_un_publicador_que_falla_la_reserva_queda_confirmada`. Código restaurado |
| RN-141 (R-6) | Quitar el `try/catch` de `ProcesadorEventos.ProcesarAsync` | `PruebasProcesadorEventos`: la del manejador que lanza | ✔ 2026-10-12: con un filtro `when` que nunca se cumple, falló `Un_manejador_que_lanza_no_propaga_se_registra_como_error_y_el_siguiente_evento_se_procesa`. Código restaurado |
| RN-150 | En `DarDeBajaEmpresa`, publicar solo los eventos de la empresa | `PruebasDarDeBajaEmpresa`: la de los N + 1 eventos | ✔ 2026-10-12: falló `Con_N_reservas_futuras_publica_la_baja_y_N_cancelaciones_por_baja_de_la_empresa`. Código restaurado |
| AGENTS §8 ter (limpiar tras publicar) | En `CrearReserva`, quitar `reserva.LimpiarEventos()` | `PruebasCrearReserva`: la de "publica un único evento", que comprueba la entidad seguida | ✔ 2026-10-12 (tras `/revisar 11b`, hallazgo 1): falló `Tras_confirmar_publica_un_unico_evento_de_reserva_confirmada`. Código restaurado |

## 7. Criterios de aceptación

- [x] Las entidades registran los eventos y los casos de uso los publican **después** del commit; nunca dentro de `SaveChanges` (D-21, A-11). `grep` de `PublicarAsync` en `Infraestructura/Persistencia` no da resultados.
- [x] `CrearReserva`, `CancelarReservaCliente`, `CancelarReservaEmpresa`, `DarDeBajaEmpresa` y `RegistrarEmpresa` publican sus eventos y los limpian. Un fallo no publica nada (pruebas de T5).
- [x] CU-23 paso 7: la baja de una empresa con N reservas futuras encola N + 1 eventos, con origen `BajaEmpresa` (RN-150). Lo cubre `PruebasDarDeBajaEmpresa`.
- [x] El procesador sobrevive a un manejador que lanza (R-6), que es la línea 3 del Control 4b, probada por adelantado (`PruebasProcesadorEventos`).
- [x] `ColaEventos` es el único singleton con estado, y `ProcesadorEventos` crea un ámbito por evento (§13.7, AGENTS §8 ter). Lo cubren `PruebasInyeccionDependencias` y `PruebasProcesadorEventos`.
- [x] Sin paquetes nuevos (`Directory.Packages.props` sin cambios) y sin migración: `dotnet ef migrations has-pending-model-changes` responde "No changes".
- [x] Los sabotajes de §6 hacen fallar sus pruebas.
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 327 pruebas unitarias y 231 de integración.

Esta fase no cierra ningún punto de control.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | El procesador, que es singleton, usaría un `ContextoDatos` scoped sin ámbito, o compartido entre eventos | `IServiceScopeFactory` y un ámbito nuevo por evento. Lo comprueba la prueba de "instancias scoped distintas" |
| R-6 | Una excepción escapa de `ExecuteAsync` y el `BackgroundService` muere en silencio | El `try/catch` de `ProcesarAsync` registra con `LogError` y sigue. Hay prueba y sabotaje |

R-7 y R-8 son del correo (Fase 16b) y no aplican aquí.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `IServicioEmail`, `MensajeCorreo`, MailKit, `Correo:Habilitado`, plantillas, `IRenderizadorPlantillas`, `IConsultasNotificacion` y los manejadores reales (RN-145 a RN-156) | Fase 16b |
| La prueba con un `IServicioEmail` que siempre falla (RN-141, `arquitectura.md` §13.9) | Fase 16b |
| "Con 300 reservas el botón responde igual que con 3" | Control 4b, Fase 16b |
| La baja de cuenta, su cascada y `CuentaDadaDeBaja` publicado (CU-22, RN-137 y RN-151) | Fase 15 |
| Publicar `CuentaRegistrada` desde el registro de cliente (CU-04, RN-154) | Fase 16b (duda 7) |
| El patrón *outbox* | Descartado (RN-144, `arquitectura.md` §13.8) |
| Corregir el intercambio CU-22/CU-23 en `reglas-negocio.md` (RN-130, RN-131, RN-136 y RN-137) | Fases 15 y 16, que tocan esos casos de uso (duda 10) |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-140 ⚑ | T5 | Pruebas del publicador que falla en `PruebasCrearReserva` y `PruebasDarDeBajaEmpresa`; sabotaje |
| RN-141 ⚑ | T4 | `PruebasProcesadorEventos`: el manejador que lanza; sabotaje. El correo es de la 16b |
| RN-142 | T4 | `PruebasColaEventos`: publicar sin lector |
| RN-143 | T1, T2 | `PruebasReserva` y `PruebasEmpresa`: los eventos llevan identificadores |
| RN-144 | — | Limitación declarada (`arquitectura.md` §13.8); no se prueba |
| RN-150 ⚑ | T2, T5 | `PruebasDarDeBajaEmpresa`: N + 1 eventos con origen `BajaEmpresa`; sabotaje |
| RN-132 | T5 | `PruebasDarDeBajaEmpresa` (ya existía): las pasadas y las completadas no se tocan ni generan evento |
| CU-07 | T2, T5 | `PruebasReserva`, `PruebasCrearReserva` |
| CU-10 | T2, T5 | `PruebasReserva`, `PruebasCancelarReservaCliente` |
| CU-11 | T2, T5 | `PruebasEmpresa`, `PruebasRegistrarEmpresa` |
| CU-20 | T2, T5 | `PruebasReserva`, `PruebasCancelarReservaEmpresa` |
| CU-23 | T2, T5 | `PruebasEmpresa`, `PruebasDarDeBajaEmpresa` |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-10-12 con la opción recomendada.

### Duda 1 — `OcurridoEnUtc` en los eventos *(resuelta: a)*

- **Pregunta:** `IEventoDominio` exige `DateTime OcurridoEnUtc` (`modelo-dominio.md` l. 832), pero los seis records de las líneas 834-839 no lo declaran, así que no compilarían.
- **Opciones:**
  - (a) Añadirlo como último parámetro posicional de cada record, rellenado con el `ahoraUtc` del método de dominio.
  - (b) Quitarlo de la interfaz.
- **Recomendación y motivo:** (a). Conserva el contrato documentado. Los métodos que registran eventos ya reciben `ahoraUtc`, así que no cambia ninguna firma, y el instante sirve a la 16b para el texto del correo.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T1, T2.

### Duda 2 — Firma de `PublicarAsync` *(resuelta: a)*

- **Pregunta:** `ColaEventos.PublicarAsync` recibe un solo `IEventoDominio` (`arquitectura.md` §13.2), pero los casos de uso le pasan la colección `reserva.Eventos` (§13.1, AGENTS §8 ter, `modelo-dominio.md` §10bis.2).
- **Opciones:**
  - (a) El puerto recibe `IReadOnlyCollection<IEventoDominio>` y la cola escribe los eventos uno a uno.
  - (b) Recibe un solo evento y el caso de uso hace el bucle.
- **Recomendación y motivo:** (a). Coincide con AGENTS, que es la norma, y con dos de los tres documentos. La cascada de `DarDeBajaEmpresa` se publica en una sola llamada.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada. Se corrige §13.2 (T6).
- **Bloqueaba:** T3, T4, T5.

### Duda 3 — Forma de `IManejadorEvento` y despacho *(resuelta: a)*

- **Pregunta:** `ProcesadorEventos` resuelve `GetKeyedServices<IManejadorEvento>(evento.GetType())`, sin genérico y por clave (§13.2). En cambio, el manejador de ejemplo implementa `IManejadorEvento<ReservaConfirmada>` (§13.5), y la tabla de DI registra `IManejadorEvento<T>` (§13.7).
- **Opciones:**
  - (a) Solo la interfaz genérica `IManejadorEvento<TEvento>`: el procesador construye el tipo cerrado con el tipo del evento y resuelve todos sus manejadores.
  - (b) Solo la interfaz no genérica, con servicios con clave.
  - (c) Las dos, con un adaptador.
- **Recomendación y motivo:** (a). Coincide con §13.5 y §13.7, donde trabajará la 16b, y deja el manejador tipado sin conversiones. La reflexión queda en un único sitio, el procesador.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada. Se corrige §13.2 (T6).
- **Bloqueaba:** T3, T4.

### Duda 4 — ¿Puede fallar el publicador? *(resuelta: a)*

- **Pregunta:** §13.1 dice que la escritura en memoria "no puede fallar", pero §13.9 pide una prueba "con un publicador que falla: la reserva se crea igualmente".
- **Opciones:**
  - (a) Mantener la prueba: un doble que lanza en `PublicarAsync`. La excepción sube y la reserva queda confirmada en la base de datos.
  - (b) Que el caso de uso capture y registre el fallo.
  - (c) Quitar la prueba.
- **Recomendación y motivo:** (a). Demuestra el orden commit → publicar sin añadir código defensivo a un camino que en producción no falla. Es la prueba que hace útil el sabotaje de RN-140.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T5.

### Duda 5 — `UsuarioId` en los eventos de empresa *(resuelta: b)*

- **Pregunta:** `EmpresaRegistrada(EmpresaId, UsuarioId)` y `EmpresaDadaDeBaja(EmpresaId, UsuarioId)` (`modelo-dominio.md` l. 834-835) no se pueden registrar desde `Empresa`, porque la entidad no conoce a su propietario.
- **Opciones:**
  - (a) `Empresa.Crear` y `DarDeBaja` reciben un `Guid usuarioId` adicional.
  - (b) Quitar `UsuarioId` de los dos eventos: el manejador de la 16b lee el propietario con su propia consulta (RN-143).
  - (c) Que registre el evento el caso de uso, lo que rompe "la entidad los registra" (D-21).
- **Recomendación y motivo:** (b). Es coherente con RN-143, que dice que el manejador vuelve a leer, y no cambia firmas de entidad que ya usan cinco casos de uso y sus pruebas.
- **Resolución:** (b). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada. Se corrige `modelo-dominio.md` §10bis.3 (T6).
- **Bloqueaba:** T1, T2.

### Duda 6 — Origen `BajaEmpresa` en la cascada *(resuelta: b)*

- **Pregunta:** `DarDeBajaEmpresa` cancela con `CancelarPorEmpresa`, que registraría el origen `Empresa`. RN-150 y CU-23 exigen el origen `BajaEmpresa`.
- **Opciones:**
  - (a) Un método nuevo, `CancelarPorBajaEmpresa`.
  - (b) Un parámetro `OrigenCancelacion` en `CancelarPorEmpresa`, que solo admite `Empresa` o `BajaEmpresa`.
- **Recomendación y motivo:** (b). La transición es la misma (sin límite de hora, RN-94); solo cambia el motivo. Un enum no es un primitivo validable (AGENTS §7), así que pasa la quinta prueba de arquitectura.
- **Resolución:** (b). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada. Cambia `modelo-dominio.md` §4.10 (T6).
- **Bloqueaba:** T2, T5.

### Duda 7 — `CuentaRegistrada` y `CuentaDadaDeBaja` *(resuelta: a)*

- **Pregunta:**
  - `Usuario` es de Identity, vive en `Infraestructura` y no hereda de `EntidadBase`.
  - El alta la hace `RegistroCliente`, en `Infraestructura/Identidad`.
  - La baja de cuenta (CU-22) es de la Fase 15.
- **Opciones:**
  - (a) En la 11b solo se definen los dos records. Quién los registra y publica lo deciden la 15 (la baja) y la 16b (el alta).
  - (b) Publicar ya `CuentaRegistrada` desde `RegistroCliente`.
- **Recomendación y motivo:** (a). (b) obliga a ampliar el radio a `Identidad` y a decidir cómo publica una clase que no es un caso de uso, algo que ningún documento resuelve.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T1, T5.

### Duda 8 — Excluir `Eventos` del modelo de EF *(resuelta: a)*

- **Pregunta:** si EF no ignora la propiedad pública `Eventos` de `EntidadBase`, intentará mapearla o fallará al construir el modelo.
- **Opciones:**
  - (a) Un `Ignore` genérico en `ContextoDatos.OnModelCreating`, para todo tipo que herede de `EntidadBase`.
  - (b) Un `builder.Ignore(e => e.Eventos)` en cada una de las configuraciones.
- **Recomendación y motivo:** (a). Una entidad nueva no puede olvidarlo, y hay un solo sitio que cambiar.
- **Resolución:** (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T1.

### Duda 9 — Probar el procesador sin levantar el host *(resuelta: b, más una prueba de humo con a)*

- **Pregunta:** AGENTS §8 ter dice que "en pruebas, el manejador se invoca directamente; no se levanta el servicio alojado". Pero hay que probar el ámbito por evento y el `try/catch` del procesador (R-6).
- **Opciones:**
  - (a) Arrancar `ProcesadorEventos` con `StartAsync` sobre una `ColaEventos` propia de la prueba, publicar, esperar con un plazo acotado y llamar a `StopAsync`.
  - (b) Extraer `internal Task ProcesarAsync(IEventoDominio, CancellationToken)`, al que llama `ExecuteAsync` por cada evento, y probarlo directamente gracias a `InternalsVisibleTo`.
- **Recomendación y motivo:** (b) para el ámbito, el despacho y el `try/catch`, porque es determinista y rápido. Se añade una única prueba de humo con (a) para comprobar que `ExecuteAsync` está conectado a la cola.
- **Resolución:** (b), más una prueba de humo con (a). Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T4.

### Duda 10 — Incoherencias entre documentos *(resuelta: a, parcial)*

- **Pregunta:** la lectura encontró estas incoherencias:
  - `reglas-negocio.md` intercambia los casos de uso de las bajas: RN-130 y RN-131 dicen CU-22, y RN-136 y RN-137 dicen CU-23, al revés que `casos-uso.md`;
  - la l. 410 de reglas descartadas contradice RN-150 y RN-151;
  - M-07 remite a "§11" en lugar de a §10 bis;
  - la l. 595 de `modelo-dominio.md` choca en apariencia con M-07;
  - el rango de reglas no coincide (RN-152 frente a RN-161);
  - R-6 sitúa la comprobación en el Control 5, y `PROJECT_PLAN.md` en el 4b.
- **Opciones:**
  - (a) Corregirlas en T6.
  - (b) Dejarlas para la Fase 20.
- **Recomendación y motivo:** (a) solo para las que tocan eventos: l. 410, M-07, l. 595, el rango y R-6. El intercambio CU-22/CU-23 es de las fases 15 y 16, que tocan esos casos de uso; se anota en §9 y no se corrige aquí, para no salir del radio.
- **Resolución:** (a), parcial. Resuelta por el desarrollador el 2026-10-12 con la opción recomendada.
- **Bloqueaba:** T6.

### Decisiones de implementación

- **Desviación 1: `PruebasAislamientoEmpresas`, fuera del radio** (decidida con la opción recomendada, por indicación del desarrollador de no preguntar).
  - **Qué pasó:** la prueba `Una_reserva_de_otra_empresa_no_se_encuentra_y_queda_intacta` compara con `BeEquivalentTo` la reserva leída de la base con la creada en memoria. Desde T1, la creada lleva `ReservaConfirmada` en `Eventos` y la leída no.
  - **Qué se hizo:** la comparación excluye `Eventos`. Es el cambio mínimo y no altera lo que la prueba verifica: el aislamiento entre empresas.
- **Despacho por reflexión, en un único sitio.** `ProcesadorEventos.ProcesarAsync` construye `IManejadorEvento<>` con el tipo del evento, lo resuelve con `GetServices` e invoca `ManejarAsync` por reflexión (duda 3). El registro del fallo usa `LoggerMessage`, como exige el analizador de rendimiento con `TreatWarningsAsErrors`.
- **El doble `PublicadorCaptura`** se registra con ámbito en `BaseDatosFixture` y sustituye a `IPublicadorEventos` solo en esa suite. Cada prueba lee lo publicado en su propio ámbito y puede activar `Fallar`. `PruebasInyeccionDependencias` comprueba el registro real sobre una colección de servicios nueva.
- **`CancelarPorEmpresa` con un origen no válido** lanza `DominioException` antes de cambiar el estado, así que no registra ningún evento.
- **`DarDeBajaEmpresa`** publica en una sola llamada `EmpresaDadaDeBaja` seguida de las N `ReservaCancelada`, y después limpia los eventos de la empresa y de cada reserva.
- **Sabotaje 2:** quitar el `catch` sin más no compilaba, porque el análisis de nulos lo rechaza. Se hizo con un filtro `when` que nunca se cumple, de efecto equivalente.
- **Limpieza de eventos comprobada** (`/revisar 11b`, hallazgo 1). La prueba de éxito de cada caso de uso lee, en su mismo ámbito, la entidad que sigue el `ContextoDatos` (`EscenarioEmpresa.EntidadSeguida`) y comprueba que su `Eventos` queda vacío. Si la entidad no estuviera seguida, `Single` lanzaría, así que la prueba no puede pasar en vacío. El método vive en `EscenarioEmpresa`, la utilidad compartida de escenarios de las pruebas de integración, y se añade al radio de §4 (`/revisar 11b`, segunda ronda, hallazgo 1).
- **Parada del host** (`/revisar 11b`, segunda ronda, hallazgo 4). `ProcesarAsync` filtra el `catch`: deja pasar una `OperationCanceledException` cuando el token del host está cancelado, porque es una parada ordenada y no un fallo, y así no la registra como `Error`. Cualquier otra excepción, o una cancelación sin parada, se sigue registrando y no se propaga (R-6). Lo cubre `La_cancelacion_por_la_parada_del_host_se_propaga_sin_registrar_error`. `arquitectura.md` §13.2 recoge el mismo filtro, y §13.9 la excepción de la prueba de humo (duda 9).

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [x] Sabotajes de §6 ejecutados, con la prueba que falló anotada, y código restaurado
- [ ] `/auditar-pruebas`: no aplica, porque solo va en las fases 10, 11, 16 y 17
- [ ] Commit hecho (mensaje libre)
- [ ] Punto de control superado: no aplica, porque esta fase no cierra ninguno
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna)
