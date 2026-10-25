# Fase 10 — Motor de disponibilidad + 13 pruebas unitarias, incluidos los invariantes de `PoliticasReserva`

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 8,5 h (SPEC §19) |
| **Semana** | 3 (`PROJECT_PLAN.md` §2), la semana crítica: no se recorta |
| **Cierra** | El Control 2 (`PROJECT_PLAN.md` §3, "fin de Fase 10"). El Control 3 cierra al final de la Fase 11 |
| **Generada** | 2026-10-12 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Existe una única implementación de `IMotorDisponibilidad` (RN-60) que calcula los huecos de un servicio en una fecha siguiendo el algoritmo de SPEC §12.2. Funciona igual en el flujo público y en el área de empresa, y convierte la hora local a UTC en un solo punto (RN-102). Las 13 pruebas de SPEC §16.1, escritas **antes** que el motor, quedan en verde.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §12.1 | Contrato: `IMotorDisponibilidad.ObtenerHuecosAsync(Guid empresaId, Guid servicioId, Guid? profesionalId, DateOnly fecha, CancellationToken ct)` y `HuecoDisponible(Guid ProfesionalId, string NombreProfesional, DateTime InicioUtc, DateTime FinUtc)` |
| SPEC §12.2 | Algoritmo en 6 pasos; la conversión de zona ocurre dentro del motor |
| SPEC §12.3 | El intervalo de huecos es la distancia entre inicios y puede ser menor que la duración |
| SPEC §12.4 | Intervalos semiabiertos; `FranjaHoraria.SeSolapaCon` |
| SPEC §4.1 | `PoliticasReserva.Resolver`: valor efectivo = `Servicio.X ?? Empresa.X`, implementado una vez en el dominio |
| SPEC §16.1 | Los 13 casos de prueba unitaria del motor |
| D-08 | Zona horaria única `Europe/Madrid` |
| `arquitectura.md` §2.3 | `Aplicacion/Disponibilidad/`: `IMotorDisponibilidad`, `MotorDisponibilidad`, `HuecoDisponible` |
| `arquitectura.md` §5.3 | `IRelojSistema`: las pruebas del motor dependen de "ahora" |
| `arquitectura.md` §10 | `IMotorDisponibilidad` scoped, registrado en `AgregarAplicacion` |
| `arquitectura.md` §12.1 | El motor se prueba sin base de datos, con dobles |
| RN-13 | Una empresa inactiva no admite nuevas reservas (duda 3) |
| RN-50 ⚑, RN-51 ⚑ | Intervalos semiabiertos; regla de solape única |
| RN-52 | El servicio cabe completo en un intervalo de horario |
| RN-53 ⚑ | No se ofrece un hueco que se solape con una reserva no cancelada del profesional |
| RN-54 | No se ofrece un hueco anterior a `ahora + antelación mínima efectiva` |
| RN-55 | No se ofrecen huecos después de `hoy + ventana máxima efectiva` |
| RN-56 | Solo profesionales activos, de la empresa y asociados al servicio |
| RN-57 | Una excepción ese día anula los huecos del profesional |
| RN-58 | El intervalo puede ser menor que la duración, con huecos solapados entre sí |
| RN-59 | Orden por inicio y, en empate, por nombre de profesional |
| RN-60 ⚑ | Una sola implementación para el flujo público y el área de empresa |
| RN-100 a RN-104 | UTC en los instantes; hora local en los horarios; conversión en un único punto; zona fija; cambio de hora con la conversión estándar |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `FranjaHoraria` (exige `Kind == Utc`) con `SeSolapaCon` | ✔ comprobado |
| `IntervaloHorario` con `SeSolapaCon` (Fase 9) | ✔ comprobado |
| `PoliticasReserva` con rangos y `Resolver(Empresa, Servicio)` | ✔ comprobado. `PruebasPoliticasReserva` solo cubre la herencia de `Resolver`, no la sobrescritura (T3) |
| `Profesional` con `Horarios`, `Excepciones`, `Presta` y `Activo`; `Servicio` con `Duracion` y `Activo`; `Empresa` con `Politicas` y `Activa` | ✔ comprobado |
| `Reserva` con `Franja`, `ProfesionalId` y `Estado` | ✔ comprobado |
| `IRelojSistema` y su implementación | ✔ comprobado |
| `Empresa.ZonaHoraria` = `"Europe/Madrid"`, que no se lee (RN-103) | ✔ comprobado |
| `Aplicacion/Disponibilidad/` | ✘ no existe: se crea en esta fase |
| Puerto de lectura del motor que no dependa de `IContextoEmpresa` | ✘ no existe: duda 1, T1 y T5 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí (ampliado el 2026-10-12) | `ValueObjects/IntervaloHuecos.cs`, `AntelacionMinimaReserva.cs` y `AntelacionMaximaReserva.cs` (fábrica `Desde(int?)`) y `ValueObjects/PoliticasReserva.cs`: sobrecarga `Resolver(PoliticasReserva, IntervaloHuecos?, AntelacionMinimaReserva?, AntelacionMaximaReserva?)` |
| `src/PlataformaReservas.Aplicacion` | sí | `Disponibilidad/` (`IMotorDisponibilidad`, `HuecoDisponible`, `MotorDisponibilidad`), `Abstracciones/IConsultasDisponibilidad.cs` e `InyeccionDependencias.cs`. Ampliado el 2026-10-12: `Abstracciones/IConsultasCatalogoPublico.cs` (solo `using`), `Dtos/` reorganizado por agregado con `ConvertirADto`, y los casos de uso que montaban DTOs a mano (`ListarServicios`, `ListarProfesionales`, `ObtenerHorarioProfesional`, `ObtenerConfiguracionEmpresa`, `RegistrarEmpresa`) y los `using` de los demás |
| `src/PlataformaReservas.Infraestructura` | sí | `Consultas/ConsultasDisponibilidad.cs` e `InyeccionDependencias.cs`. Ampliado el 2026-10-12: `ConsultasCatalogoPublico.cs` (`CategoriaDto.ConvertirADto`) |
| `src/PlataformaReservas.Web` | solo `using` (ampliado el 2026-10-12) | `Components/App/_Imports.razor` y `Cuenta/Pages/AltaEmpresa.razor`, por el nuevo namespace de los DTOs |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Disponibilidad/PruebasMotorDisponibilidad.cs`, `Infraestructura/` (los dobles `RelojFijo` y `ConsultasDisponibilidadEnMemoria`) y `ValueObjects/PruebasPoliticasReserva.cs`. Ampliado el 2026-10-12: `ValueObjects/Pruebas{IntervaloHuecos,AntelacionMinimaReserva,AntelacionMaximaReserva}.cs` (`Desde`), `Dtos/<Agregado>/Pruebas<Dto>.cs` de los DTOs nuevos y de `HorarioProfesionalDto` |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `Consultas/PruebasConsultasDisponibilidad.cs` y `PruebasInyeccionDependencias.cs`. Ampliado el 2026-10-12: los `using` de las pruebas que usan DTOs |
| `docs/` y `AGENTS.md` | sí | `arquitectura.md` (§2.3, §4.1 y §10), este fichero y `AGENTS.md` §8 y §8 bis (convención de DTOs, ampliado el 2026-10-12) |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto: ni los repositorios, ni las migraciones, ni la lógica de `Web`, ni `CrearReserva`. Las ampliaciones del 2026-10-12 las autorizó el desarrollador al pedir que el puerto del motor devolviera DTOs (§11).

## 5. Tareas

**Orden obligatorio (CLAUDE.md, AGENTS §10):** T1 y T2 solo dejan compilable el andamiaje de pruebas. T3 escribe las pruebas en rojo y **se para** hasta que el desarrollador las confirme. Solo entonces empieza T4.

### T1 — Contratos (≈ 0,5 h)

- **Ficheros:**
  - crea `Aplicacion/Disponibilidad/IMotorDisponibilidad.cs` y `HuecoDisponible.cs`, con las firmas literales de SPEC §12.1;
  - crea `Aplicacion/Abstracciones/IConsultasDisponibilidad.cs` (duda 1).
- **Implementa:** SPEC §12.1 y RN-60.
  - **`IConsultasDisponibilidad`** no recibe `IContextoEmpresa` y filtra en cada consulta por el `empresaId` que recibe. Métodos:
    - `Task<EmpresaDisponibilidadDto?> ObtenerEmpresaAsync(Guid empresaId, CancellationToken ct)`;
    - `Task<ServicioDisponibilidadDto?> ObtenerServicioAsync(Guid empresaId, Guid servicioId, CancellationToken ct)`;
    - `Task<IReadOnlyList<ProfesionalDisponibilidadDto>> ObtenerProfesionalesQuePrestanAsync(Guid empresaId, Guid servicioId, Guid? profesionalId, CancellationToken ct)`: activos, con sus horarios y excepciones;
    - `Task<IReadOnlyList<ReservaOcupadaDto>> ObtenerReservasActivasAsync(Guid empresaId, IReadOnlyCollection<Guid> profesionalIds, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)`: estado distinto de `Cancelada`, que se solapen con el rango.
- **Hecho cuando:** compila sin avisos y todavía no existe ninguna implementación.
- **Verificación:** `dotnet build`.

### T2 — Dobles para las pruebas unitarias (≈ 0,5 h)

- **Ficheros:** crea `PruebasUnitarias/Infraestructura/RelojFijo.cs` y `PruebasUnitarias/Infraestructura/ConsultasDisponibilidadEnMemoria.cs`.
- **Implementa:** `arquitectura.md` §12.1 (el motor se prueba sin base de datos).
  - Son dobles escritos a mano, sin paquetes (AGENTS §4).
  - El doble en memoria aplica los mismos filtros que promete el puerto: empresa, actividad, asociación al servicio y reservas no canceladas, para que las pruebas del motor no dependan de él para filtrar.
- **Hecho cuando:** compila y se pueden montar empresa, servicio, profesionales con horario y reservas sin base de datos.
- **Verificación:** `dotnet build`.

### T3 — Las pruebas del motor, en rojo (≈ 2 h)

- **Ficheros:** crea `PruebasUnitarias/Disponibilidad/PruebasMotorDisponibilidad.cs` y modifica `PruebasUnitarias/ValueObjects/PruebasPoliticasReserva.cs`.
- **Implementa:** los 13 casos de SPEC §16.1, el caso 14 de la duda 3 y la sobrescritura de `PoliticasReserva.Resolver` (SPEC §4.1). Ver la tabla de §6.
- **Hecho cuando:**
  - las pruebas compilan y **fallan** porque `MotorDisponibilidad` todavía no existe o no hace nada;
  - la prueba de `Resolver` pasa, porque el código ya existe;
  - **el desarrollador ha revisado y confirmado las pruebas.**
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias --filter PruebasMotorDisponibilidad` en rojo, y la confirmación por escrito del desarrollador.

### T4 — `MotorDisponibilidad` (≈ 1,5 h)

- **Ficheros:** crea `Aplicacion/Disponibilidad/MotorDisponibilidad.cs`.
- **Implementa:** SPEC §12.2 paso a paso, RN-13 (duda 3) y RN-50 a RN-59.
  - **Antes de nada:** si la empresa no existe o no está activa, devuelve vacío (duda 3).
  - **Paso 1:** si el servicio no existe, no está activo o no es de la empresa, devuelve vacío.
  - **Paso 2:** obtiene las políticas efectivas con `PoliticasReserva.Resolver`.
  - **Paso 3:** compara la fecha pedida con `hoy` en hora local más la ventana efectiva.
  - **Paso 4:** toma los profesionales candidatos del puerto.
  - **Paso 5:**
    - salta los profesionales con una excepción esa fecha;
    - recorre los intervalos del día con `cursor += intervalo` mientras `cursor + duración <= fin`;
    - descarta los candidatos que se solapan con una reserva (`FranjaHoraria.SeSolapaCon`, sin reimplementar la regla, AGENTS §3.7);
    - descarta los que empiezan antes de `ahora + antelación`.
  - **Paso 6:** ordena por inicio y, en empate, por nombre de profesional.
  - **Conversión local → UTC (RN-102 a RN-104):** en **un único método privado**, con `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")`. Se descarta el candidato cuyo inicio o fin locales no existen y se acepta la conversión estándar en las horas ambiguas (duda 2). `ahora` sale de `IRelojSistema.AhoraUtc`.
- **Hecho cuando:** pasan las 14 pruebas de `PruebasMotorDisponibilidad` y siguen pasando las de `PruebasArquitectura`.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasUnitarias`.

### T5 — `ConsultasDisponibilidad` en Infraestructura (≈ 1,25 h)

- **Ficheros:** crea `Infraestructura/Consultas/ConsultasDisponibilidad.cs` y `PruebasIntegracion/Consultas/PruebasConsultasDisponibilidad.cs`.
- **Implementa:** la duda 1, RN-53, RN-56 y AGENTS §8 (filtrar en origen, por el `empresaId` recibido).
  - Todas las consultas van sin seguimiento.
  - `ObtenerProfesionalesQuePrestanAsync` incluye horarios y excepciones.
  - `ObtenerReservasActivasAsync` filtra por estado y por solape con el rango en la propia consulta.
- **Hecho cuando:**
  - un servicio o profesional de otra empresa no aparece;
  - salen solo profesionales activos y asociados al servicio;
  - el filtro por `profesionalId` funciona;
  - las reservas canceladas o fuera del rango no aparecen.
- **Verificación:** `dotnet test tests/PlataformaReservas.PruebasIntegracion --filter PruebasConsultasDisponibilidad`.

### T6 — Registro en DI (≈ 0,5 h)

- **Ficheros:** modifica `Aplicacion/InyeccionDependencias.cs` (`IMotorDisponibilidad` → `MotorDisponibilidad`, scoped, `arquitectura.md` §10), `Infraestructura/InyeccionDependencias.cs` (`IConsultasDisponibilidad`, scoped) y `PruebasIntegracion/PruebasInyeccionDependencias.cs`.
- **Implementa:** `arquitectura.md` §10.
- **Hecho cuando:** `IMotorDisponibilidad` se resuelve desde el contenedor real.
- **Verificación:** `PruebasInyeccionDependencias`.

### T7 — Sabotajes y verificación de RN-60 (≈ 0,5 h)

- **Ficheros:** ninguno queda modificado. Los cambios son temporales y se deshacen.
- **Implementa:** comprobar que las pruebas detectan lo que dicen detectar.
  - **Cambio de hora:** calcular el UTC con un desfase fijo de +01:00 hace fallar la prueba 13.
  - **Hueco que no cabe:** cambiar `<=` por `<` en la condición del bucle hace fallar la prueba 7 o la 2.
  - **Solape de límites:** comparar con `<=` en lugar de usar `SeSolapaCon` hace fallar la prueba 8.
  - **RN-60:** un `grep` de `: IMotorDisponibilidad` en `src/` da un único resultado (duda 4).
- **Hecho cuando:** cada sabotaje hace fallar su prueba, se restaura el código y la suite vuelve a verde.
- **Verificación:** las ejecuciones anotadas en §11.

### T8 — Documentación (≈ 0,5 h)

- **Ficheros:** modifica `docs/arquitectura.md` (§2.3, añadir `IConsultasDisponibilidad` en `Abstracciones/`; §10, su ciclo de vida) y este fichero (§7 con la evidencia y §11 con las decisiones de implementación).
- **Implementa:** registrar las dudas resueltas.
- **Hecho cuando:** ningún documento contradice el código.
- **Verificación:** `grep` de `IConsultasDisponibilidad` en `docs/`.

**Suma: 7,25 h de 8,5 h presupuestadas.** Queda 1,25 h de margen para el motor, que es el núcleo del TFM.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| 1. Profesional sin horario ese día: sin huecos | U | `Disponibilidad/PruebasMotorDisponibilidad.cs` | RN-52 | T3 |
| 2. Horario simple sin reservas: huecos completos y correctos (09:00–14:00, 45 min, intervalo 45: 09:00, 09:45 … 12:45) | U | ídem | RN-52, SPEC §12.3 | T3 |
| 3. Jornada partida: dos bloques, sin huecos en el intermedio | U | ídem | RN-52 | T3 |
| 4. Excepción ese día: sin huecos | U | ídem | RN-57 | T3 |
| 5. Reserva en medio: se excluyen los huecos que se solapan. El filtro de la reserva cancelada y de la de otro profesional lo hace el puerto; en el motor, esas variantes comprueban que consume bien lo que el puerto devuelve | U | ídem | RN-53 | T3 |
| 6. Reserva al inicio y al final del horario | U | ídem | RN-53 | T3 |
| 7. Hueco insuficiente para la duración: no se emite | U | ídem | RN-52 | T3 |
| 8. Reservas consecutivas y límites exactos `[inicio, fin)`: sin falsos solapes | U | ídem | RN-50, RN-51 | T3 |
| 9. Intervalo menor que la duración: huecos solapados entre sí | U | ídem | RN-58 | T3 |
| 10. Intervalo heredado de la empresa frente a sobrescrito en el servicio | U | ídem | SPEC §4.1 | T3 |
| 11. Antelación mínima y ventana máxima, efectivas y heredadas | U | ídem | RN-54, RN-55 | T3 |
| 12. Varios profesionales: agregados y ordenados por inicio y nombre. Excluir inactivos, no asociados y de otra empresa (RN-56) es responsabilidad del puerto y lo verifica `PruebasConsultasDisponibilidad`; aquí ejercita el doble | U | ídem | RN-59 | T3 |
| 13. Cambio de hora: un día de marzo y otro de octubre; los huecos siguen alineados con el horario local (09:00 local = 08:00 UTC en invierno y 07:00 UTC en verano) | U | ídem | RN-102, RN-103, RN-104 | T3 |
| 14. Empresa inactiva o servicio inactivo, de otra empresa o inexistente: sin huecos. La inactividad la decide el motor; "de otra empresa" lo filtra el puerto (`PruebasConsultasDisponibilidad`) | U | ídem | RN-13, SPEC §12.2 paso 1 | T3 |
| `Resolver` toma del servicio lo que sobrescribe y de la empresa lo demás | U | `ValueObjects/PruebasPoliticasReserva.cs` | SPEC §4.1 | T3 |
| Filtra por `empresaId` en la consulta; solo activos y asociados; filtro por profesional; reservas canceladas o fuera de rango excluidas; ninguna consulta deja entidades en seguimiento (`Las_consultas_no_dejan_entidades_en_seguimiento`) | I | `Consultas/PruebasConsultasDisponibilidad.cs` | RN-53, RN-56, AGENTS §8 | T5 |
| `IMotorDisponibilidad` se resuelve desde el contenedor | I | `PruebasInyeccionDependencias.cs` | `arquitectura.md` §10 | T6 |
| Las cinco pruebas de forma siguen en verde | A | `Arquitectura/PruebasArquitectura.cs` (sin cambios) | AGENTS §7 | T4 |

## 7. Criterios de aceptación

- [x] Las pruebas de `PruebasMotorDisponibilidad` se escribieron antes que `MotorDisponibilidad` y el desarrollador las confirmó (CLAUDE.md). Evidencia: 40 pruebas en rojo contra un esqueleto que lanzaba `NotImplementedException`; confirmadas por el desarrollador el 2026-10-12.
- [x] Los 13 casos de SPEC §16.1, más el 14 de la duda 3, están en verde. Evidencia: las 40 pruebas de `PruebasMotorDisponibilidad` (casos 1 a 14 con sus bordes).
- [x] Existe una sola implementación de `IMotorDisponibilidad` (RN-60): `grep ": IMotorDisponibilidad" src/` da un único resultado, `MotorDisponibilidad.cs:7`.
- [x] La conversión local ↔ UTC ocurre en un único método del motor (RN-102), y en todo `src/` solo usa `"Europe/Madrid"` (RN-103), sin `TimeZoneInfo.Local` ni `DateTime.Now` (AGENTS §13). Evidencia: `MotorDisponibilidad.ALocalUtc`; `grep` sin otras apariciones (la constante de `Empresa.ZonaHoraria` no se lee).
- [x] El solape con reservas usa `FranjaHoraria.SeSolapaCon`, sin reimplementar la regla (AGENTS §3.7, RN-51). La consulta SQL de `ObtenerReservasActivasAsync` filtra el rango con la misma condición semiabierta (ver §11).
- [x] Las políticas efectivas salen de `PoliticasReserva.Resolver`, sin duplicar la resolución (SPEC §4.1). Evidencia: caso 10 y `PruebasPoliticasReserva.Resolver_toma_del_servicio_lo_que_sobrescribe_y_de_la_empresa_lo_demas`.
- [x] `IConsultasDisponibilidad` no recibe `IContextoEmpresa` y filtra por `empresaId` en cada consulta (AGENTS §8). Hay prueba de aislamiento: `PruebasConsultasDisponibilidad` (5 pruebas).
- [x] Los sabotajes de T7 hacen fallar sus pruebas (detalle en §11).
- [x] `dotnet build` sin avisos y `dotnet test` en verde: 297 unitarias y 146 de integración (tras resolver los hallazgos de las revisiones del 2026-10-12 17:14 y 17:30).

**Esta fase cierra el Control 2**, copiado literal de `PROJECT_PLAN.md` §3:

```text
Las 13 pruebas del motor de disponibilidad en verde
El motor no depende de la interfaz ni de Infraestructura
PoliticasReserva rechaza intervalo 0, negativo y mayor de 480
```

**Evidencias del Control 2:**

- [x] Las 13 pruebas del motor están en verde: los casos 1 a 13 de `Disponibilidad/PruebasMotorDisponibilidad.cs` (SPEC §16.1). El caso 14 de la duda 3 es adicional y no cuenta para el control.
- [x] El motor no depende de la interfaz ni de `Infraestructura`: `MotorDisponibilidad` vive en `Aplicacion/Disponibilidad/`, `PlataformaReservas.Aplicacion.csproj` solo referencia a `Dominio`, y el motor solo recibe `IConsultasDisponibilidad` e `IRelojSistema`. Sus pruebas corren con dobles, sin base de datos (`arquitectura.md` §12.1).
- [x] Las políticas rechazan un intervalo de 0, uno negativo y uno mayor de 480. Desde las reaperturas de M-05, el rango vive en el objeto de valor `IntervaloHuecos` (`Minimo = 1`, `Maximo = 480`), que `PoliticasReserva` recibe ya construido. Evidencia: `ValueObjects/PruebasIntervaloHuecos.Rechaza_los_valores_fuera_del_rango` (0, 481 e `int.MinValue`), que ya existe y no se modifica en esta fase.

Además prepara el Control 3: `CrearReserva` usa el motor dentro de la transacción en la Fase 11.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-2 | Un `DateTime` sin `Kind == Utc` al construir `FranjaHoraria` o comparar con reservas | La conversión devuelve `DateTimeKind.Utc`; `FranjaHoraria` lo exige en el constructor y las pruebas lo ejercitan |
| R-1 | No aplica: el motor no se usa desde un componente en esta fase | — |

**Riesgos propios de la fase, sin código R:**

- **Bucle infinito con intervalo 0 o negativo** (SPEC §4.1). Lo impiden las invariantes de `PoliticasReserva`, y `Resolver` siempre construye un valor válido. Lo cubren las pruebas de rangos existentes.
- **Conversión ingenua en el cambio de hora** (SPEC §16.1, caso 13). Lo mitigan la prueba 13 y su sabotaje en T7.
- **Zona horaria en el contenedor:** si la imagen Docker no incluye `tzdata`, `FindSystemTimeZoneById("Europe/Madrid")` fallaría. Se anota para la Fase 18 y no se actúa aquí.

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| `CrearReserva` y la recomprobación del hueco dentro de la transacción | Fase 11 |
| Restricción `EXCLUDE` y prueba de concurrencia | Fase 11 |
| Selector de huecos del flujo público | Fase 14 |
| Agenda de la empresa | Fase 16 |
| Leer `Empresa.ZonaHoraria` | No se hace: RN-103 |
| Margen de preparación entre reservas | Fuera de alcance (`reglas-negocio.md`, SPEC §6.2) |
| `tzdata` en la imagen Docker | Fase 18 |
| Prueba de `CancelarPorEmpresa` sobre una reserva completada (aplazada desde la Fase 8) | Fase 11 |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-13 | T4 | Caso 14 |
| RN-50, RN-51 | T4 | Caso 8 |
| RN-52 | T4 | Casos 1, 2, 3 y 7 |
| RN-53 | T4, T5 | Casos 5 y 6 (solape); `PruebasConsultasDisponibilidad` (excluye canceladas) |
| RN-54, RN-55 | T4 | Caso 11 |
| RN-56 | T5 | `PruebasConsultasDisponibilidad` (el motor confía en el puerto desde que devuelve DTOs) |
| RN-57 | T4 | Caso 4 |
| RN-58 | T4 | Caso 9 |
| RN-59 | T4 | Caso 12 |
| RN-60 | T1, T7 | Verificación por `grep` (duda 4) |
| RN-100, RN-101 | T4 | Casos 2 y 13 (`Kind == Utc` en los huecos) |
| RN-102 a RN-104 | T4, T7 | Caso 13 y su sabotaje |
| SPEC §4.1 | T3, T4 | Caso 10; `PruebasPoliticasReserva` (sobrescritura) |

## 11. Dudas

Todas resueltas por el desarrollador el 2026-10-12 con la opción recomendada.

### Duda 1 — ¿De dónde lee los datos el motor? *(resuelta: a)*

- **Pregunta:** el contrato de SPEC §12.1 recibe `empresaId` y lo usan el flujo público y el de cliente, que no tienen el claim `empresa_id`. Los `IRepositorio*` filtran por `IContextoEmpresa` (AGENTS §8) y lanzarían una excepción en esos flujos. `arquitectura.md` §10 dice que el motor "depende de repositorios", pero no resuelve este conflicto.
- **Opciones:**
  - (a) Un puerto de lectura propio, `IConsultasDisponibilidad`, en `Aplicacion/Abstracciones`. No recibe `IContextoEmpresa` (igual que `IConsultasCatalogoPublico`), filtra por el `empresaId` recibido en cada consulta y va sin seguimiento. Sus métodos son los de T1.
  - (b) Añadir parámetros `empresaId` a los repositorios. Rompe AGENTS §8.
  - (c) Que el caso de uso cargue los datos y se los pase al motor. Cambia el contrato de §12.1.
- **Recomendación y motivo:** (a). Mantiene el contrato de §12.1 y la separación entre repositorios y consultas de AGENTS §8, y deja el motor probable con un doble en memoria.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T1, T2, T3, T4, T5.

### Duda 2 — Horas locales inexistentes o repetidas en el cambio de hora *(resuelta: a)*

- **Pregunta:** RN-104 dice que el cambio de hora se resuelve con la conversión estándar del sistema. Pero `TimeZoneInfo.ConvertTimeToUtc` **lanza una excepción** con una hora local que no existe (de 02:00 a 03:00 el último domingo de marzo) y, con una hora ambigua (de 02:00 a 03:00 el último domingo de octubre), elige el horario estándar.
- **Opciones:**
  - (a) Descartar el candidato cuyo inicio o fin locales no existen, y aceptar la conversión estándar en las horas ambiguas.
  - (b) Desplazar el candidato a la siguiente hora válida.
  - (c) Dejar que lance.
- **Recomendación y motivo:** (a). Es lo mínimo para que el motor no falle, no inventa lógica propia de desplazamiento (RN-104) y una hora que no existe no puede ofrecerse. La prueba 13 comprueba que un horario de 09:00 a 14:00 sigue alineado con la hora local en los dos días de cambio.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T3 (caso 13), T4.

### Duda 3 — Empresa inactiva *(resuelta: a)*

- **Pregunta:** RN-13 dice que una empresa inactiva "no admite nuevas reservas", pero el paso 1 de SPEC §12.2 solo comprueba el servicio.
- **Opciones:**
  - (a) El motor devuelve vacío si la empresa no existe o no está activa, y se añade el caso 14 de prueba.
  - (b) Dejarlo a `CrearReserva` (Fase 11).
- **Recomendación y motivo:** (a). Es un único punto válido para el flujo público y el área de empresa (RN-60), y evita ofrecer huecos que después no se podrían reservar.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T3, T4.

### Duda 4 — ¿Cómo se verifica RN-60? *(resuelta: a)*

- **Pregunta:** `reglas-negocio.md` §12 pide una "verificación arquitectónica" de que existe una sola implementación de `IMotorDisponibilidad`. Una sexta prueba en `PruebasArquitectura` cambiaría la lista cerrada de cinco (CLAUDE.md).
- **Opciones:**
  - (a) Verificarlo en T7 y en la revisión con un `grep ": IMotorDisponibilidad" src/` que dé un único resultado, recogido como criterio de §7.
  - (b) Añadir una sexta prueba de arquitectura.
- **Recomendación y motivo:** (a). No toca las pruebas de arquitectura, cuyo cambio requiere una decisión propia, y basta para un único tipo.
- **Resolución:** (a). Resuelta por el desarrollador con la opción recomendada.
- **Bloqueaba:** T7, T8.

### Duda 5 — ¿Puede una consulta acotar reservas por rango en SQL? *(resuelta: a)*

- **Pregunta:** `ObtenerReservasActivasAsync` necesita traer solo las reservas que tocan el día consultado. Cualquier filtro SQL exacto por rango es, por definición, la condición de solape (`Inicio < hasta && Fin > desde`), y AGENTS §3.7 dice que esa regla existe una sola vez, en `FranjaHoraria.SeSolapaCon`, que EF no puede traducir a SQL. Surgió en la revisión del 2026-10-12 17:14 (hallazgo 1).
- **Opciones:**
  - (a) Excepción explícita en AGENTS §3.7: el prefiltro SQL de rango está permitido para traer menos filas; la decisión de dominio la toma siempre `SeSolapaCon`. La restricción `EXCLUDE` de la Fase 11 también expresará el solape en PostgreSQL.
  - (b) Filtro grueso `FinUtc > desde`, sin cota superior: trae todas las reservas futuras de los profesionales.
  - (c) Filtro por inicio con un margen de 24 h: exige un máximo en `Duracion` y reabrir M-05.
- **Recomendación y motivo:** (a). Es eficiente y exacto, y no mueve la decisión fuera del dominio.
- **Resolución:** (a), excepción añadida a AGENTS §3.7. Resuelta por el desarrollador el 2026-10-12.
- **Bloqueaba:** nada (el código no cambia).

### Decisiones de implementación (2026-10-12)

- **Esqueleto previo al motor.** Para ejecutar las pruebas en rojo en T3 se creó un `MotorDisponibilidad` que solo lanzaba `NotImplementedException`; T4 lo sustituyó.
- **`FinUtc = InicioUtc + duración`.** El fin del hueco no se obtiene convirtiendo la hora local de fin, sino sumando la duración al inicio ya convertido. Coincide con `Reserva.Crear` y evita que el 25 de octubre un hueco de 60 minutos dure dos horas. Confirmado por el desarrollador junto con las pruebas.
- **Duda 2 aplicada literalmente.** Se descarta el candidato cuyo inicio **o fin** locales no existen: el 28 de marzo, con un horario de 01:00 a 04:00, se descarta también el hueco de 01:00 a 02:00. Confirmado por el desarrollador junto con las pruebas.
- **Ventana sin conversión UTC → local.** `fecha > hoy + ventana` se evalúa como `ahoraUtc < ALocalUtc(fecha − ventana, 00:00)`, de modo que el motor solo convierte en un sentido y en un único método (`ALocalUtc`).
- **Cursor como `TimeSpan`.** El bucle de §12.2 usa `TimeSpan` en lugar de `TimeOnly`, que daría la vuelta a medianoche y podría colgar el bucle con un horario que acaba a las 23:59.
- **Una sola lectura de reservas.** El motor genera primero los candidatos de todos los profesionales y lee las reservas una vez, con el rango `[primer inicio, último fin)` de esos candidatos. Si no hay candidatos, no consulta.
- **El puerto devuelve DTOs, no entidades** (corrección del desarrollador, 2026-10-12). La primera versión de `IConsultasDisponibilidad` devolvía `Empresa`, `Servicio`, `Profesional` y `Reserva`, lo que incumplía AGENTS §6 y §8 (las consultas devuelven DTOs). Ahora devuelve `EmpresaDisponibilidadDto`, `ServicioDisponibilidadDto`, `ProfesionalDisponibilidadDto` y `ReservaOcupadaDto`.
  - `Aplicacion/Dtos/` se reorganiza por agregado (`Categorias/`, `Empresas/`, `Servicios/`, `Profesionales/`, `Reservas/`), y **cada DTO se construye con su método estático `ConvertirADto(entidad, ...)`**. Se descartaron un mapeador genérico `ConvertirADto<T>` (se resolvería en ejecución, como AutoMapper, que AGENTS §4 descarta) y unas clases mapeadoras por agregado. Recogido en AGENTS §8 bis.
  - Las conversiones que hacían a mano `ListarServicios`, `ListarProfesionales`, `ObtenerHorarioProfesional`, `ObtenerConfiguracionEmpresa`, `RegistrarEmpresa` y `ConsultasCatalogoPublico` pasan al `ConvertirADto` de su DTO, con el mismo resultado.
  - Para resolver las políticas sin entidades, `PoliticasReserva` gana la sobrecarga `Resolver(PoliticasReserva, IntervaloHuecos?, AntelacionMinimaReserva?, AntelacionMaximaReserva?)` (objetos de valor y no `int?`, AGENTS §7; hallazgo 2 de la revisión del 2026-10-12 17:14), y `Resolver(Empresa, Servicio)` delega en ella: la regla `Servicio.X ?? Empresa.X` sigue en un único punto (SPEC §4.1).
  - El motor ya no repite el filtro de profesionales (`EmpresaId`, `Activo`, `Presta`) ni la comprobación de `servicio.EmpresaId`: el DTO no lleva esos datos y el puerto filtra en origen (T4, paso 4).
  - El solape se sigue comprobando con `FranjaHoraria.SeSolapaCon`, construyendo la franja de cada `ReservaOcupadaDto`.
- **Rango de reservas en SQL.** `ObtenerReservasActivasAsync` filtra en la consulta con `InicioUtc < hasta && FinUtc > desde`. Ver la duda 5.
- **Hallazgos menores de la revisión del 2026-10-12 17:14.**
  - `ObtenerReservasActivasAsync` proyecta en la consulta con `ReservaOcupadaDto.ConvertirADto(Guid, FranjaHoraria)`, una sobrecarga en la que delega `ConvertirADto(Reserva)`. El SQL solo lee `profesional_id`, `inicio_utc` y `fin_utc`, así que ya no se cargan ni se descifran los datos personales del cliente (D-22). Pasar `r.Franja` entero hacía que EF leyera la fila completa, comprobado con `ToQueryString`.
  - Se quita el comentario de `SuperaLaVentana` (AGENTS §3.10).
  - El desempate por nombre (RN-59) usa `StringComparer.CurrentCulture`, como `ListarProfesionales`. Lo verifica `Caso12_en_empate_el_nombre_con_tilde_se_ordena_por_su_letra_y_no_por_su_codigo`, que falla si se vuelve a `Ordinal`.
- **Hallazgos de la revisión del 2026-10-12 17:30.**
  - Importante 1: `PruebasConsultasDisponibilidad.Las_consultas_no_dejan_entidades_en_seguimiento` llama a los cuatro métodos del puerto y comprueba que el `ChangeTracker` del ámbito queda vacío. Un `AsTracking()` temporal en `ObtenerProfesionalesQuePrestanAsync` la hace fallar.
  - Menor 2: `IntervaloHuecos`, `AntelacionMinimaReserva` y `AntelacionMaximaReserva` ganan `Desde(int?)`, que devuelve `null` si llega `null` (patrón de `Slug.Desde`). `PoliticasReserva.Resolver(Empresa, Servicio)` y el motor ya no repiten el ternario. Decisión del desarrollador.
  - Menor 3: el comentario de `ALocalUtc` se mantiene como punto crítico (AGENTS §3.10), porque marca la conversión única de RN-102 ⚑ y la decisión de la duda 2, que no se deducen del código. Decisión del desarrollador.
  - Menor 4: la sobrecarga `ReservaOcupadaDto.ConvertirADto(Guid, FranjaHoraria)` queda recogida como excepción en AGENTS §8 bis.
- **Asociación al servicio en la consulta.** Se filtra con `EF.Property<List<ProfesionalServicio>>(p, "_servicios")`, el campo privado que mapea `ConfiguracionProfesional`.
- **Cabecera.** "Cierra: Ninguno" contradecía §7 y `PROJECT_PLAN.md` §3 ("Control 2 — fin de Fase 10"); se corrige.

### Sabotajes de T7 (2026-10-12)

| Sabotaje | Pruebas que fallan | Resultado |
|---|---|---|
| `ALocalUtc` con desfase fijo de +01:00 | 19 de 40, entre ellas el caso 13 de marzo (día 28), de octubre (día 24) y las dos de horas nocturnas | Detectado |
| `cursor + duración < fin` en lugar de `<=` | 11 de 40, entre ellas `Caso07_un_servicio_que_cabe_justo_emite_un_hueco` y los cuatro días del caso 13 | Detectado |
| `<=` propio en lugar de `FranjaHoraria.SeSolapaCon` | `Caso08_reservas_consecutivas_…` y `Caso06_…` | Detectado |
| Los tres anteriores, repetidos tras pasar el puerto a DTOs | Desfase fijo: casos 2 y 13; `<`: casos 7 y 13; `<=` propio: casos 6 y 8 | Detectado |
| `grep ": IMotorDisponibilidad" src/` | Un único resultado | Cumple RN-60 |

Tras cada sabotaje se restauró el fichero (comprobado con `diff`) y la suite volvió a verde.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [ ] `/revisar` sin hallazgos bloqueantes
- [ ] `/auditar-pruebas 10` ejecutado y sus importantes anotados (`AGENTS.md` §11 bis)
- [ ] Commit hecho (mensaje libre)
- [ ] Control 2 superado (evidencias en §7)
- [ ] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna)
