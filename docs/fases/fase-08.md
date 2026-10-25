# Fase 08 — Alta y configuración de empresa, servicios, profesionales, asociación, baja y reactivación

| | |
|---|---|
| **Estado** | APROBADA |
| **Presupuesto** | 8,5 h (SPEC §19) |
| **Semana** | 2 (`PROJECT_PLAN.md` §2) |
| **Cierra** | Ninguno. Prepara el Control 4, al final de la Fase 16: "la empresa se registra, se configura y se publica sola" |
| **Generada** | 2026-10-10 con `/fase` |
| **Aprobada por** | — |

---

## 1. Objetivo

Al terminar la fase, un visitante puede dar de alta su establecimiento en `/for-business/register`, que crea en una sola operación su cuenta, el rol `Propietario`, la empresa y la pertenencia, y entra en `/app`.

Desde el área de empresa, el propietario puede:
- editar los datos y las políticas de la empresa;
- dar de alta, editar y desactivar servicios y profesionales;
- asociar profesionales a servicios;
- dar de baja la empresa, lo que cancela sus reservas futuras, y reactivarla.

Son los primeros casos de uso de `Aplicacion`, con `Resultado`, `IRelojSistema` y la transacción por caso de uso.

## 2. Origen

| Referencia | Qué aporta a esta fase |
|---|---|
| SPEC §19 | Contenido y 8,5 h |
| SPEC §5.3 | Alcance del área de empresa |
| SPEC §9.3 a §9.7 | `Empresa`, `MiembroEmpresa`, `Servicio`, `Profesional`, `ProfesionalServicio` |
| SPEC §11 | Rutas `/for-business/register` (SSR), `/app`, `/app/services`, `/app/staff`, `/app/settings` (`InteractiveServer`). "Total: 16 vistas" |
| SPEC §14 | Sin borrado físico |
| D-17 · A-01 · A-02 | Una clase por caso de uso, con `EjecutarAsync`. `Resultado` y `Resultado<T>` frente a `DominioException` |
| A-05 | `AsTracking()` solo donde se modifica |
| A-09 | Validación en tres capas, sin duplicar. Los rangos de las políticas solo en el dominio: el caso de uso traduce la `DominioException` a `CodigoError.Validacion` (`arquitectura.md` §7) |
| D-11 · `fase-07.md` duda 3 | Defensa en profundidad: `Agregar` comprueba el `EmpresaId` |
| `arquitectura.md` §2.3 | `Aplicacion/Compartido/` (`Resultado`, `CodigoError`), `Aplicacion/CasosUso/Empresas/` (`RegistrarEmpresa`, `ActualizarEmpresa`, `DarDeBajaEmpresa`, `ReactivarEmpresa`), `Servicios/` (`CrearServicio`, `ActualizarServicio`, `DesactivarServicio`), `Profesionales/` (`CrearProfesional`, …, `AsociarServicio`, `DesasociarServicio`), `Web/Components/App/` |
| `arquitectura.md` §5 y §6 | La forma del caso de uso, `IRelojSistema`, `Resultado<T>` y `CodigoError` (firmas literales) |
| `arquitectura.md` §8.3 | `[Authorize(Policy = "EsPropietario")]` en el `_Imports.razor` de `App/` |
| `arquitectura.md` §8.4 | Al dar de baja la empresa se refresca el sello del propietario |
| `arquitectura.md` §9.4 | Una transacción por caso de uso. `RegistrarEmpresa` y `DarDeBajaEmpresa` abarcan varios agregados |
| `arquitectura.md` §10 | `AgregarAplicacion`, con registro explícito de cada caso de uso. `IRelojSistema` singleton y casos de uso *scoped* |
| `modelo-dominio.md` §4.3, §4.5, §4.6 y §4.10 | El comportamiento de `Empresa`, `Servicio`, `Profesional` y `Reserva.CancelarPorEmpresa` |
| `modelo-dominio.md` §6 | RN-31 y el slug único, en el caso de uso. RN-15 y RN-132, en una sola transacción |
| AGENTS §7 | Las fábricas y los métodos de dominio reciben objetos de valor |
| RN-06 | Una sola pertenencia por usuario |
| RN-10 · RN-11 | Slug único, generado desde el nombre y con sufijo numérico si colisiona |
| RN-12 | Exactamente una categoría |
| RN-14 · RN-26 a RN-29 ⚑ | Políticas de la empresa obligatorias, por defecto 15, 60 y 60, y dentro de rango |
| RN-15 | El alta es atómica: usuario, rol, empresa y pertenencia |
| RN-20 a RN-22, RN-24, RN-25 | Servicio: duración, precio, políticas opcionales, desactivación sin borrado |
| RN-30 a RN-34 | Profesional: una sola empresa, asociación de la misma empresa y sin duplicados, desactivación sin borrado |
| RN-120 | Sin borrado físico |
| RN-130 a RN-134 (RN-132 ⚑) | Baja y reactivación de la empresa |
| RN-177 a RN-179 | Formatos de contacto, a través de los objetos de valor existentes |
| CU-11 | Registrar empresa (§3.5) |
| CU-12 | Configurar datos y políticas |
| CU-13 | Gestionar servicios |
| CU-14 | Gestionar profesionales |
| CU-15 | Asociar profesionales a servicios |
| CU-23 | Dar de baja o reactivar la empresa (§3.8) |

## 3. Prerrequisitos

| Prerrequisito | Estado |
|---|---|
| `IContextoEmpresa`, los cinco repositorios y las pruebas de aislamiento (Fase 7, commit `4f7e539`) | ✔ comprobado |
| Política `EsPropietario` y claim `empresa_id` (Fase 6) | ✔ comprobado |
| `RolesIdentidad.Propietario` y rol creado por migración | ✔ comprobado |
| Fábricas `Empresa.Crear`, `Servicio.Crear`, `Profesional.Crear`, `MiembroEmpresa.Crear`, y `Slug.Desde` y `Slug.ConSufijo` | ✔ comprobado |
| Índices únicos de `empresas.slug` y `miembros_empresa` (RN-06, RN-10) | ✔ comprobado (`PruebasEsquema`) |
| Categorías sembradas en desarrollo (RN-122) | ✔ comprobado (`PruebasDatosIniciales`) |
| `Resultado`, `CodigoError` e `IRelojSistema` | ✘ ausente, como se esperaba: `fase-07.md` duda 7 los trae a esta fase (T1) |
| Abstracción de transacción para `Aplicacion` | ✘ ausente y sin nombre en los documentos. Ver duda 2 |
| Forma de crear un `Usuario` desde `Aplicacion` | ✘ ausente y sin nombre en los documentos. Ver duda 3 |
| `Reserva.CancelarPorEmpresa` | ✘ ausente: es de la Fase 11. Ver duda 7 |
| Lectura de categorías para los formularios | ✘ ausente. Ver duda 8 |

## 4. Radio de cambio

| Proyecto o carpeta | ¿Se toca? | Qué |
|---|---|---|
| `src/PlataformaReservas.Dominio` | sí | `Entidades/Empresa.cs`, `Servicio.cs`, `Profesional.cs` y `Reserva.cs` (métodos de §4 de `modelo-dominio.md`). `ValueObjects/`: solo los nuevos que aprueben las dudas 5 y 6 |
| `src/PlataformaReservas.Aplicacion` | sí | `Compartido/` (`Resultado`, `Resultado<T>`, `CodigoError`), `Abstracciones/` (`IRelojSistema`, la transacción de la duda 2, la cuenta de la duda 3, métodos nuevos en los `IRepositorio*` y las categorías de la duda 8), `CasosUso/Empresas/`, `CasosUso/Servicios/`, `CasosUso/Profesionales/`, `Dtos/` e `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Infraestructura` | sí | `Repositorios/` (métodos nuevos), la implementación de las abstracciones de las dudas 2, 3 y 8, `RelojSistema` e `InyeccionDependencias.cs` |
| `src/PlataformaReservas.Web` | sí | `Program.cs` (`AgregarAplicacion`), `Components/App/` (`_Imports.razor` con la política, `/app`, `/app/settings`, `/app/services`, `/app/staff`), `Cuenta/` (página `/for-business/register`) y `Components/Layout/` (enlace al área de empresa) |
| `tests/PlataformaReservas.PruebasUnitarias` | sí | `Entidades/` (ficheros existentes), `ValueObjects/` (los nuevos), `Compartido/PruebasResultado.cs` y `Compartido/PruebasResultadoT.cs` |
| `tests/PlataformaReservas.PruebasIntegracion` | sí | `CasosUso/Empresas/`, `CasosUso/Servicios/`, `CasosUso/Profesionales/` y `Repositorios/` (un fichero por clase), `Infraestructura/` (auxiliares). *Ampliado tras la revisión del 2026-10-10:* `Consultas/`, `Identidad/`, `Persistencia/`, `Persistencia/Configuraciones/` y `PruebasInyeccionDependencias.cs`, carpetas espejo de las clases de las dudas 2, 3, 5 y 8 (AGENTS §11 bis) |
| `docs/` | sí | `modelo-dominio.md` §4 y §5 (firmas con objetos de valor y objetos de valor nuevos, si se aprueban), `arquitectura.md` §2.3 y §5 (nombres nuevos) y este fichero |

**Todo lo que no figure como "sí" no se toca en esta fase.** En concreto, ni `/app/schedules` ni `/app/bookings`, ni el horario, ni los eventos.

## 5. Tareas

### T1 — Base de los casos de uso (≈ 0,75 h)

- **Ficheros:**
  - crea `Aplicacion/Compartido/Resultado.cs` y `CodigoError.cs`, `Aplicacion/Abstracciones/IRelojSistema.cs`, la abstracción de transacción (duda 2), `Aplicacion/InyeccionDependencias.cs` (`AgregarAplicacion`) e `Infraestructura/RelojSistema.cs`;
  - modifica `Infraestructura/InyeccionDependencias.cs` y `Web/Program.cs`;
  - pruebas: `PruebasUnitarias/Compartido/PruebasResultado.cs` y `PruebasResultadoT.cs`, una por clase (AGENTS §11 bis).
- **Implementa:** A-01, A-02, `arquitectura.md` §5.3, §6 y §10, `fase-07.md` duda 7.
- **Hecho cuando:**
  - `Resultado<T>` y `CodigoError` tienen las firmas literales de `arquitectura.md` §6.
  - `IRelojSistema` es singleton y devuelve UTC (R-2).
  - `AgregarAplicacion` registra los casos de uso uno a uno.
- **Verificación:** `PruebasResultado` (éxito, y fallo con código y mensaje) y `PruebasResultadoT` (éxito con valor, y fallo sin valor).

### T2 — Comportamiento de `Empresa` (≈ 0,5 h)

- **Ficheros:** modifica `Dominio/Entidades/Empresa.cs` y `PruebasUnitarias/Entidades/PruebasEmpresa.cs`.
- **Implementa:** `modelo-dominio.md` §4.3, RN-12, RN-14, RN-130, RN-133.
- **Hecho cuando:**
  - Existen `ActualizarDatos`, `CambiarCategoria`, `CambiarPoliticas`, `DarDeBaja` y `Reactivar`, con las firmas de la duda 4.
  - Cada cambio actualiza `FechaModificacion`.
  - La quinta prueba de arquitectura sigue en verde.
- **Verificación:** pruebas unitarias de cada método, incluidas dar de baja una empresa ya inactiva y reactivar una activa, según la duda 4.

### T3 — Comportamiento de `Servicio` (≈ 0,75 h)

- **Ficheros:**
  - modifica `Dominio/Entidades/Servicio.cs` y `PruebasUnitarias/Entidades/PruebasServicio.cs`;
  - crea los objetos de valor de las dudas 5 y 6, cada uno con su fichero de pruebas en `PruebasUnitarias/ValueObjects/`;
  - si la duda 5 se resuelve con un objeto de valor nuevo, modifica `ConfiguracionServicio.cs` y crea su conversor (sin migración: misma columna).
- **Implementa:** `modelo-dominio.md` §4.5, RN-20 a RN-22, RN-24 a RN-29.
- **Hecho cuando:** existen `ActualizarDatos`, `EstablecerPoliticas` y `Desactivar` con objetos de valor (`Activar` no se implementa: duda 10), y cada política fuera de rango se rechaza en el dominio.
- **Verificación:** pruebas unitarias. `dotnet ef migrations add` no genera cambios.

### T4 — Comportamiento de `Profesional` (≈ 0,5 h)

- **Ficheros:** modifica `Dominio/Entidades/Profesional.cs` y `PruebasUnitarias/Entidades/PruebasProfesional.cs`.
- **Implementa:** `modelo-dominio.md` §4.6, RN-32, RN-33, RN-34.
- **Hecho cuando:** existen `ActualizarDatos(NombrePersona, Email?, Telefono?, …)`, `Desactivar`, `AsociarServicio`, `DesasociarServicio` y `Presta` (`Activar` no se implementa: duda 10). Asociar dos veces el mismo servicio lanza (RN-32).
- **Verificación:** pruebas unitarias.

### T5 — `Reserva.CancelarPorEmpresa` (≈ 0,25 h)

- **Ficheros:** modifica `Dominio/Entidades/Reserva.cs` y `PruebasUnitarias/Entidades/PruebasReserva.cs`.
- **Implementa:** `modelo-dominio.md` §4.10, RN-94, como requisito de RN-132 (duda 7).
- **Hecho cuando:** `void CancelarPorEmpresa(DateTime ahoraUtc)` exige `Confirmada`, no tiene límite temporal y fija `Cancelada` y `FechaCancelacion`.
- **Verificación:** pruebas unitarias: confirmada pasa a cancelada; cancelada o completada lanza.

### T6 — Métodos nuevos de los repositorios (≈ 1 h)

- **Ficheros:**
  - modifica las cinco `IRepositorio*` y sus implementaciones;
  - crea lo que decida la duda 8;
  - pruebas: `PruebasIntegracion/Repositorios/PruebasRepositorioEmpresas.cs`, `PruebasRepositorioServicios.cs`, `PruebasRepositorioProfesionales.cs` y `PruebasRepositorioReservas.cs`.
- **Implementa:** D-11, `fase-07.md` duda 3, RN-10, RN-131, RN-132.
- **Hecho cuando:**
  - `Agregar` en servicios y profesionales lanza si `EmpresaId` no es `EmpresaActualId`. Empresa y miembro, según la duda 3.
  - Existen:
    - listado de servicios y de profesionales de la empresa;
    - la comprobación de que un slug ya existe;
    - las reservas confirmadas con `InicioUtc > ahora`, para modificar;
    - el recuento de esas reservas (RN-131).
  - Todo filtra en origen y sin `SaveChanges`.
- **Verificación:** pruebas de integración de cada método nuevo, incluido el `Agregar` que lanza con otro `EmpresaId`.

### T7 — `RegistrarEmpresa` (≈ 1 h)

- **Ficheros:**
  - crea `Aplicacion/CasosUso/Empresas/RegistrarEmpresa.cs` y su comando, y la implementación de la duda 3 en `Infraestructura/Identidad/`;
  - pruebas: `PruebasIntegracion/CasosUso/Empresas/PruebasRegistrarEmpresa.cs`.
- **Implementa:** CU-11, RN-06, RN-10, RN-11, RN-12, RN-14, RN-15.
- **Hecho cuando:**
  - En una sola transacción se crean el usuario, el rol `Propietario`, la empresa con las políticas por defecto y la pertenencia, o no se crea nada.
  - El slug se deriva del nombre y, si colisiona, lleva sufijo (`Slug.ConSufijo`).
  - Un correo ya registrado da `Resultado.Fallo(CodigoError.Conflicto, …)`, y un dato con formato no válido da `Validacion`.
  - El correo duplicado se busca con `FindByEmailAsync` (R-10).
- **Verificación:** pruebas de integración:
  - alta completa;
  - dos empresas con el mismo nombre obtienen slugs distintos;
  - correo duplicado, sin dejar empresa;
  - fallo a mitad, sin dejar usuario.

### T8 — `ActualizarEmpresa`, `DarDeBajaEmpresa` y `ReactivarEmpresa` (≈ 1 h)

- **Ficheros:** crea los tres casos de uso con sus comandos en `Aplicacion/CasosUso/Empresas/`, y sus pruebas `PruebasActualizarEmpresa.cs`, `PruebasDarDeBajaEmpresa.cs` y `PruebasReactivarEmpresa.cs` en `PruebasIntegracion/CasosUso/Empresas/`.
- **Implementa:** CU-12, CU-23, RN-14, RN-26 a RN-29, RN-130 a RN-134, `arquitectura.md` §7 y §8.4.
- **Hecho cuando:**
  - `ActualizarEmpresa` traduce la `DominioException` de las políticas a `Validacion`.
  - `DarDeBajaEmpresa`, en una transacción:
    - pone `Activa = false`;
    - cancela con `CancelarPorEmpresa` las reservas confirmadas futuras;
    - deja intactas las pasadas y las completadas;
    - refresca el sello del propietario.
  - Un caso de uso de consulta, o un método del repositorio (duda 4), devuelve el recuento previo de RN-131.
  - `ReactivarEmpresa` no restaura reservas.
- **Verificación:** pruebas de integración de RN-132 (`reglas-negocio.md` §12) y de RN-133.

### T9 — Casos de uso de servicios (≈ 0,75 h)

- **Ficheros:** crea `CrearServicio`, `ActualizarServicio` y `DesactivarServicio`, con sus comandos y DTOs, en `Aplicacion/CasosUso/Servicios/`, y un caso de uso de listado si la duda 4 lo pide. Pruebas: `PruebasCrearServicio.cs`, `PruebasActualizarServicio.cs` y `PruebasDesactivarServicio.cs`.
- **Implementa:** CU-13, RN-20 a RN-29, RN-03.
- **Hecho cuando:**
  - Un servicio de otra empresa da `NoEncontrado`.
  - Las políticas vacías se guardan como `null` (heredar).
  - No hay borrado.
- **Verificación:** pruebas de integración, una por caso de uso, incluido el `NoEncontrado` entre empresas.

### T10 — Casos de uso de profesionales y asociación (≈ 0,75 h)

- **Ficheros:** crea `CrearProfesional`, `ActualizarProfesional`, `DesactivarProfesional` (nombres de la duda 4), `AsociarServicio` y `DesasociarServicio` en `Aplicacion/CasosUso/Profesionales/`, con sus pruebas `Pruebas<CasoDeUso>.cs`.
- **Implementa:** CU-14, CU-15, RN-30 a RN-34, RN-03.
- **Hecho cuando:** `AsociarServicio` carga el servicio con el repositorio de la empresa. Un servicio de otra empresa da `NoEncontrado` (RN-31 sin revelar que existe). La asociación duplicada da `Conflicto`.
- **Verificación:** pruebas de integración.

### T11 — `/for-business/register` y entrada a `/app` (≈ 0,75 h)

- **Ficheros:** crea la página SSR de alta en `Web/Cuenta/Pages/`, `Web/Components/App/_Imports.razor` con `[Authorize(Policy = "EsPropietario")]` y la página `/app` mínima (duda 9). Modifica `Components/Layout/`.
- **Implementa:** CU-11 (pasos 1, 2 y 6), `arquitectura.md` §8.3, R-4.
- **Hecho cuando:** el formulario único envía, crea la empresa con `RegistrarEmpresa`, inicia sesión con el claim `empresa_id` ya emitido y redirige a `/app`. Un cliente sin empresa no entra en `/app`.
- **Verificación:** a mano: alta, aterrizaje en `/app` y cierre de sesión. Una prueba de integración de `RegistrarEmpresa` ya cubre la lógica.

### T12 — `/app/settings` (≈ 0,5 h)

- **Ficheros:** crea `Web/Components/App/Configuracion.razor` (nombre en la duda 4).
- **Implementa:** CU-12, CU-23 (RN-131: muestra el recuento antes de confirmar).
- **Hecho cuando:**
  - Edita los datos y las políticas.
  - La baja está separada visualmente del resto y pide una confirmación explícita con el recuento.
  - Con la empresa inactiva, ofrece reactivarla.
  - No hay dos casos de uso en paralelo (R-1).
- **Verificación:** a mano, en las dos direcciones (baja y reactivación).

### T13 — `/app/services` y `/app/staff` (≈ 0,75 h)

- **Ficheros:** crea `Web/Components/App/Servicios.razor` y `Web/Components/App/Profesionales.razor` (nombres en la duda 4).
- **Implementa:** CU-13, CU-14, CU-15.
- **Hecho cuando:**
  - Listan, dan de alta, editan y desactivan.
  - La interfaz deja claro que una política vacía significa "heredar" (CU-13).
  - La ficha del profesional marca los servicios que presta.
  - Solo hablan con casos de uso y reciben DTOs (`arquitectura.md` §2.1).
- **Verificación:** a mano.

**Suma: 9,25 h de 8,5 h presupuestadas.** Se pasa en 0,75 h, un 9 %. Ver duda 1.

## 6. Pruebas

| Prueba | Tipo | Fichero | Regla que verifica | Tarea |
|---|---|---|---|---|
| `Resultado` de éxito y de fallo | U | `Compartido/PruebasResultado.cs` | A-02 | T1 |
| `Resultado<T>` de éxito con valor y de fallo | U | `Compartido/PruebasResultadoT.cs` | A-02 | T1 |
| Métodos de `Empresa` | U | `Entidades/PruebasEmpresa.cs` | RN-12, RN-14, RN-130, RN-133 | T2 |
| Métodos de `Servicio` y políticas fuera de rango | U | `Entidades/PruebasServicio.cs` | RN-22, RN-24, RN-26 a RN-29 | T3 |
| Objetos de valor nuevos | U | `ValueObjects/Pruebas<Objeto>.cs`, uno por objeto | RN-26 a RN-28 | T3 |
| Métodos de `Profesional` y asociación duplicada | U | `Entidades/PruebasProfesional.cs` | RN-32, RN-33 | T4 |
| `CancelarPorEmpresa` | U | `Entidades/PruebasReserva.cs` | RN-94 | T5 |
| `Agregar` con otro `EmpresaId` lanza; los listados filtran en origen | I | `Repositorios/PruebasRepositorioServicios.cs` y `PruebasRepositorioProfesionales.cs` | D-11, RN-02 | T6 |
| Slug existente | I | `Repositorios/PruebasRepositorioEmpresas.cs` | RN-10 | T6 |
| Reservas futuras confirmadas y su recuento | I | `Repositorios/PruebasRepositorioReservas.cs` | RN-131, RN-132 | T6 |
| El alta crea las cuatro cosas o ninguna; slugs distintos; correo duplicado | I | `CasosUso/Empresas/PruebasRegistrarEmpresa.cs` | RN-11, RN-15 | T7 |
| Políticas fuera de rango dan `Validacion` | I | `CasosUso/Empresas/PruebasActualizarEmpresa.cs` | RN-29 | T8 |
| La baja cancela solo las futuras confirmadas | I | `CasosUso/Empresas/PruebasDarDeBajaEmpresa.cs` | RN-132 ⚑ | T8 |
| Reactivar no restaura las reservas | I | `CasosUso/Empresas/PruebasReactivarEmpresa.cs` | RN-133 | T8 |
| Servicios: alta, edición, desactivación y `NoEncontrado` de otra empresa | I | `CasosUso/Servicios/Pruebas<CasoDeUso>.cs` | RN-03, RN-24, RN-25 | T9 |
| Profesionales: alta, edición, desactivación; asociación de otra empresa y duplicada | I | `CasosUso/Profesionales/Pruebas<CasoDeUso>.cs` | RN-31, RN-32, RN-34 | T10 |
| Las 5 de arquitectura y las 5 de aislamiento siguen en verde | A · I | `Arquitectura/PruebasArquitectura.cs`, `Multiempresa/PruebasAislamientoEmpresas.cs` | D-14, RN-02 | todas |

Tipos: `U` unitaria, `I` integración, `C` concurrencia, `A` arquitectura.

## 7. Criterios de aceptación

- [x] El alta crea usuario, rol, empresa y pertenencia, o nada (RN-15). Evidencia: `PruebasRegistrarEmpresa` (alta completa; con la cuenta fallida no queda ni usuario ni empresa) y `PruebasUnidadTrabajo`.
- [x] Dos empresas con el mismo nombre obtienen slugs distintos (RN-11). Evidencia: `PruebasRegistrarEmpresa`.
- [x] El propietario aterriza en `/app` con la sesión iniciada (CU-11). Evidencia: prueba manual del 2026-10-10 contra la base local; el formulario envía, redirige a `/app` y `/app/settings` carga los datos. Un anónimo que pide `/app` va a `/login`.
- [x] Un usuario que ya tiene empresa no puede crear otra (RN-06): el formulario crea siempre una cuenta nueva, así que lo cubren el correo duplicado y el índice único. Evidencia: `PruebasRegistrarEmpresa` (correo duplicado → `Conflicto`, sin empresa) y `PruebasEsquema`.
- [x] Una política fuera de rango se rechaza en el dominio, no solo en el formulario (CU-12, RN-29). Evidencia: `PruebasActualizarEmpresa`, `PruebasCrearServicio` y las pruebas unitarias de los tres objetos de valor de políticas.
- [x] Servicios y profesionales se desactivan, nunca se borran (RN-25, RN-34, RN-120). Evidencia: `PruebasDesactivarServicio`, `PruebasDesactivarProfesional`.
- [x] No se puede asociar un servicio de otra empresa (RN-31). Evidencia: `PruebasAsociarServicio` (`NoEncontrado`, sin asociar).
- [x] La baja cancela solo las reservas futuras confirmadas (RN-132), y reactivar no las restaura (RN-133). Evidencia: `PruebasDarDeBajaEmpresa`, `PruebasReactivarEmpresa`. Sabotaje: sin el filtro `InicioUtc > ahora` fallan esa prueba y `PruebasRepositorioReservas`; restaurado.
- [x] Tras la baja, el propietario sigue entrando en `/app` (RN-134). La baja no toca `MiembroEmpresa`, así que el claim `empresa_id` se sigue emitiendo. Comprobado a mano por el desarrollador el 2026-10-10 en `/app/settings`: guardar, rechazo de una política fuera de rango, baja con recuento, nuevo inicio de sesión tras la baja y reactivación. El aviso de baja se ve sin estilo porque el proyecto no carga Bootstrap.
- [x] La empresa nunca se borra físicamente (RN-120). Ningún caso de uso ni repositorio borra.
- [x] Ningún componente de `App/` usa tipos de `Infraestructura` ni entidades (`arquitectura.md` §2.1, §5.2). Solo inyectan casos de uso e `IConsultasCatalogoPublico` y reciben DTOs.
- [x] `dotnet build` sin avisos y `dotnet test` en verde (2026-10-10: 211 unitarias y 113 de integración).

**Esta fase no cierra ningún punto de control.** Los criterios de CU-11 y CU-23 que dependen del catálogo público ("no aparece en la búsqueda ni por slug directo", "aparece sin más pasos") se comprueban en la Fase 13. Los que dependen del correo (RN-150, RN-152 y RN-153), en la 16b.

## 8. Riesgos que aplican

| Riesgo | Cómo se manifestaría en esta fase | Mitigación aquí |
|---|---|---|
| R-1 | "A second operation was started on this context" en las páginas de `App/` | Nunca dos casos de uso en paralelo ni `async void` (AGENTS §13). Un caso de uso por acción del usuario (T12, T13) |
| R-2 | Npgsql rechaza un `DateTime` no UTC en `FechaModificacion` o `FechaCancelacion` | Toda hora sale de `IRelojSistema`, que devuelve UTC (T1). Los métodos de dominio validan `Kind` |
| R-4 | El formulario SSR de alta no envía, falla el antiforgery o la redirección entra en bucle | `/for-business/register` sigue el patrón de `Register.razor` de la plantilla, con `SignInManager` en la página (T11) |
| R-10 | La comprobación de correo duplicado con LINQ devuelve vacío y deja registrar dos veces | Siempre `FindByEmailAsync` o `CreateAsync` de Identity (T7) |

Los demás no aplican: R-3 (no hay concurrencia ni `EXCLUDE` nuevos), R-5 a R-9, R-11 a R-14 (eventos, correo, claves, copias de `Reserva`, CSP, bloqueo y limitación de frecuencia).

## 9. Fuera de alcance de esta fase

| Qué | Dónde se hace |
|---|---|
| Publicable (RN-16 a RN-19): aviso de "aún no visible" con los requisitos que faltan | Fase 13 (`IConsultasCatalogoPublico`) y 16 (panel, CU-18) |
| Panel `/app` con reservas de hoy y próximas (CU-18) | Fase 16. Aquí, solo la página mínima de la duda 9 |
| Eventos `EmpresaRegistrada`, `EmpresaDadaDeBaja` y `ReservaCancelada` (origen `BajaEmpresa`) y su publicación tras el commit | Fase 11b |
| Correos de alta, de baja y de aviso a los clientes (RN-150, RN-152, RN-153) | Fase 16b |
| Horario y excepciones (`/app/schedules`, CU-16, CU-17) | Fase 9 |
| `CancelarPorCliente` y `Completar` | Fase 11 |
| Filtrar los hijos de `Profesional` por `EmpresaId` en el `Include` (`fase-07-revision.md`, menor 2) | Fase 9, con la escritura de horarios |
| Bloqueo por intentos en la página de alta | Fase 19 |

## 10. Trazabilidad

| RN · CU | Tarea | Prueba |
|---|---|---|
| RN-06 | T7 | Correo duplicado; índice único (`PruebasEsquema`) |
| RN-10 · RN-11 | T6, T7 | Slug existente; slugs distintos con el mismo nombre |
| RN-12 | T2, T7 | `PruebasEmpresa` (`CambiarCategoria`) |
| RN-14 · RN-26 a RN-29 ⚑ | T2, T3, T8 | `PruebasEmpresa`, `PruebasServicio`, `PruebasActualizarEmpresa` |
| RN-15 | T7 | Todo o nada en `PruebasRegistrarEmpresa` |
| RN-20 · RN-21 | T3, T9 | Ya cubiertas por `PruebasDuracion` y `PruebasPrecio`; `PruebasCrearServicio` |
| RN-22 | T3, T9 | Políticas `null` (heredar) |
| RN-24 · RN-25 | T3, T9 | `PruebasDesactivarServicio` |
| RN-30 · RN-31 | T10 | Asociación con un servicio de otra empresa |
| RN-32 | T4, T10 | Asociación duplicada |
| RN-33 · RN-34 | T4, T10 | `PruebasDesactivarProfesional` |
| RN-120 | T8, T9, T10 | Ningún caso de uso borra |
| RN-130 · RN-134 | T8, T12 | `PruebasDarDeBajaEmpresa`; acceso a `/app` comprobado a mano |
| RN-131 | T6, T12 | Recuento en `PruebasRepositorioReservas` |
| RN-132 ⚑ | T5, T6, T8 | `PruebasDarDeBajaEmpresa` |
| RN-133 | T8 | `PruebasReactivarEmpresa` |
| CU-11 | T7, T11 | `PruebasRegistrarEmpresa`; a mano |
| CU-12 | T8, T12 | `PruebasActualizarEmpresa` |
| CU-13 | T9, T13 | Pruebas de `CasosUso/Servicios/` |
| CU-14 · CU-15 | T10, T13 | Pruebas de `CasosUso/Profesionales/` |
| CU-23 | T8, T12 | `PruebasDarDeBajaEmpresa`, `PruebasReactivarEmpresa` |

## 11. Dudas

Todas resueltas el 2026-10-10 con la opción recomendada, por indicación del desarrollador. Se conservan para dejar rastro de cada decisión.

### Duda 1 — Presupuesto

- **Pregunta:** las tareas suman 9,25 h para 8,5 h. La fase arrastra de la 7 `Resultado`, `CodigoError` e `IRelojSistema` (0,75 h, `fase-07.md` duda 7), y es la primera con casos de uso y pantallas interactivas.
- **Opciones:**
  - (a) Aceptar la desviación de 0,75 h (9 %). Está por debajo del umbral del 30 % de `PROJECT_PLAN.md` §4, y se anota en las métricas.
  - (b) Llevar T13 (`/app/services` y `/app/staff`) a la Fase 9, que ya tiene pantalla (`/app/schedules`).
  - (c) Recortar `/app/settings` a datos y baja, y dejar las políticas para más adelante.
- **Recomendación y motivo:** (a). El sobrecoste viene de piezas comunes que solo se hacen una vez. Mover pantallas a la Fase 9 la deja con 4,75 h para 4 h, y (c) deja CU-12 a medias.
- **Bloquea:** ninguna tarea. Afecta al resumen de §5.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 2 — Cómo abre la transacción un caso de uso

- **Pregunta:** AGENTS §8 bis dice "un caso de uso, una transacción; la abre y la cierra el caso de uso". Pero `Aplicacion` no puede ver `ContextoDatos` (`arquitectura.md` §2.1), y el ejemplo de §9.4 usa `_db.Database` directamente. No hay ninguna abstracción con nombre.
- **Opciones:**
  - (a) `IUnidadTrabajo` en `Aplicacion/Abstracciones/`, con `IniciarTransaccionAsync`, `GuardarCambiosAsync` y `ConfirmarAsync` (o un objeto de transacción *disposable*), implementada en `Infraestructura/Persistencia/` sobre el `ContextoDatos` del ámbito. Así Identity y los repositorios comparten la transacción.
  - (b) Poner los casos de uso en `Infraestructura`. Contradice `arquitectura.md` §2.3.
- **Recomendación y motivo:** (a). Los repositorios siguen sin guardar, el caso de uso decide cuándo se confirma, y la abstracción no expone EF. Hay que anotar el nombre en `arquitectura.md` §2.3 y §9.4.
- **Bloquea:** T1, T7 a T10.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 3 — Cómo crea `RegistrarEmpresa` al usuario y qué pasa con la defensa en profundidad del alta

- **Pregunta:** `RegistrarEmpresa` vive en `Aplicacion` (`arquitectura.md` §2.3), pero `Usuario`, `UserManager` y los roles viven en `Infraestructura`. Además, en el alta todavía no hay `EmpresaActualId`, así que `Agregar` no puede comparar `EmpresaId` para la empresa ni para el miembro.
- **Opciones:**
  - (a) Una abstracción en `Aplicacion/Abstracciones/`, por ejemplo `IServicioCuentas` con `CrearPropietarioAsync(email, nombre, telefono, clave)`, que devuelve el `Guid` del usuario o un fallo. Se implementa en `Infraestructura/Identidad/` con `UserManager` (crea el usuario y asigna `Propietario`) dentro de la transacción de la duda 2. `RepositorioEmpresas.Agregar` y `RepositorioMiembros.Agregar` no comparan con el contexto, que es la misma excepción del alta que la duda 2 de la Fase 7.
  - (b) Poner `RegistrarEmpresa` en `Infraestructura/Identidad/`, como `RegistroCliente` (Fase 6).
- **Recomendación y motivo:** (a). Respeta la ubicación de `arquitectura.md` §2.3 y deja el caso de uso probable con su forma (A-01). Hay que fijar el nombre.
- **Bloquea:** T6, T7, T11.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 4 — Firmas y nombres que los documentos no fijan

- **Pregunta:** `modelo-dominio.md` §4.3, §4.5 y §4.6 dan firmas con primitivos (`ActualizarDatos(string nombre, …)`, `EstablecerPoliticas(int?, …)`, `Profesional.Crear(Guid, string)`), que contradicen AGENTS §7 y la quinta prueba de arquitectura. `Empresa.ActualizarDatos(...)` no lista parámetros. Faltan además los nombres de:
  - los casos de uso de profesionales elididos con "…";
  - las páginas Razor;
  - las consultas de listado y de recuento;
  - el comportamiento de `DarDeBaja` o `Reactivar` sobre el estado que ya tienen.
- **Opciones:**
  - (a) Las firmas reciben objetos de valor y `DateTime ahoraUtc` cuando tocan `FechaModificacion`. Por ejemplo, `Empresa.ActualizarDatos(NombreEmpresa, DescripcionEmpresa?, Email, Telefono, Direccion, CodigoPostal, Ciudad, DateTime)`. El slug no cambia al cambiar el nombre (RN-11 lo genera en el alta).
    - Casos de uso: `ActualizarProfesional` y `DesactivarProfesional`, por analogía con `Servicios/`, más `ObtenerConfiguracionEmpresa`, `ListarServicios` y `ListarProfesionales` para las pantallas.
    - Páginas: `App/Inicio.razor`, `Configuracion.razor`, `Servicios.razor` y `Profesionales.razor`.
    - `DarDeBaja` sobre una empresa inactiva, o `Reactivar` sobre una activa, lanzan `DominioException`, y el caso de uso devuelve antes `EstadoNoValido`.
    - Se corrige `modelo-dominio.md` §4.
  - (b) Otros nombres que indique el desarrollador.
- **Recomendación y motivo:** (a). Es lo que exige AGENTS §7 y no rompe la quinta prueba de arquitectura. Los casos de uso de lectura hacen falta porque las pantallas solo reciben DTOs (`arquitectura.md` §5.2).
- **Bloquea:** T2 a T4, T8 a T13.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 5 — `Servicio.Descripcion` es un `string` validado

- **Pregunta:** `Servicio.Descripcion` es `string?` con 500 caracteres de máximo en base de datos. AGENTS §7 dice que todo campo con regla de validación es un objeto de valor, pero no existe `DescripcionServicio` y AGENTS prohíbe crear objetos de valor sin reabrir M-05.
- **Opciones:**
  - (a) Reabrir M-05 y crear `DescripcionServicio` (máx. 500, opcional), con su conversor en la misma columna.
  - (b) Dejar `string?` y validar solo en el formulario. Contradice AGENTS §7.
- **Recomendación y motivo:** (a). Es la misma regla que ya se aplicó a `DescripcionEmpresa` el 2026-10-04.
- **Bloquea:** T3, T9.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 6 — Las políticas opcionales del servicio frente a la quinta prueba de arquitectura

- **Pregunta:** `EstablecerPoliticas(int? intervalo, int? antelacionMinima, int? antelacionMaxima)` (`modelo-dominio.md` §4.5) recibe `int?`, que la quinta prueba de arquitectura prohíbe. `modelo-dominio.md` §4.5 descarta agrupar las tres en un objeto de valor anulable.
- **Opciones:**
  - (a) Reabrir M-05 con tres objetos de valor, uno por política, con los rangos de RN-26 a RN-28: por ejemplo `IntervaloHuecos`, `AntelacionMinimaReserva` y `AntelacionMaximaReserva`. `EstablecerPoliticas` los recibe anulables, y `PoliticasReserva` delega en ellos los rangos, que siguen en un único sitio (RN-29). Las propiedades de `Servicio` siguen siendo `int?` en base de datos.
  - (b) `EstablecerPoliticas(PoliticasReserva? politicas)`: todo o nada. Cambia RN-22, que permite heredar cada política por separado.
  - (c) Una excepción en la prueba de arquitectura. Prohibido (CLAUDE.md: "si alguna falla, el código está mal").
- **Recomendación y motivo:** (a). Mantiene RN-22 y RN-29 y la prueba de arquitectura. Los nombres los fija el desarrollador.
- **Bloquea:** T3, T9.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 7 — Adelantar `Reserva.CancelarPorEmpresa`

- **Pregunta:** RN-132 ⚑ exige cancelar las reservas futuras en la baja de empresa, que es de esta fase. Pero los métodos de cancelación de `Reserva` son de la Fase 11.
- **Opciones:**
  - (a) Adelantar solo `CancelarPorEmpresa(DateTime ahoraUtc)` (`modelo-dominio.md` §4.10), sin evento: los eventos son de la 11b. La 11b añadirá el `ReservaCancelada` con origen `BajaEmpresa`, que distingue la baja de una cancelación normal.
  - (b) Llevar la baja de empresa a la Fase 11. Contradice SPEC §19.
- **Recomendación y motivo:** (a). Es un método de dominio de 0,25 h, y la Fase 11 lo reutiliza para CU-20.
- **Bloquea:** T5, T8.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 8 — De dónde salen las categorías de los formularios

- **Pregunta:** el alta (CU-11) y la configuración (CU-12) piden la categoría. No hay ninguna lectura de categorías: no son de ninguna empresa, así que no encajan en los repositorios de empresa (RN-122: no se mantienen desde la aplicación).
- **Opciones:**
  - (a) Crear ya `IConsultasCatalogoPublico` con un único método, `ObtenerCategoriasAsync`, que devuelve DTOs y no recibe `IContextoEmpresa` (RN-04). La Fase 13 le añade la búsqueda y la ficha.
  - (b) Un `IRepositorioCategorias` nuevo, que no figura en `modelo-dominio.md` §8.
- **Recomendación y motivo:** (a). Las categorías son datos públicos (la landing también las muestra), y no inventa una interfaz nueva.
- **Bloquea:** T6, T11, T12.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 9 — `/app` antes del panel

- **Pregunta:** CU-11 manda al propietario a `/app`, pero el panel de `/app` es CU-18 (Fase 16), y el aviso de "aún no visible" (RN-19) necesita la condición de publicable (Fase 13).
- **Opciones:**
  - (a) Una página `/app` mínima: bienvenida y enlaces a configuración, servicios y profesionales. La Fase 16 la convierte en el panel. No añade vistas: `/app` ya está en las 16.
  - (b) Aterrizar en `/app/settings` hasta la Fase 16. Contradice CU-11.
- **Recomendación y motivo:** (a). Cumple CU-11 y no adelanta el panel.
- **Bloquea:** T11.
- **Resolución:** opción (a), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Duda 10 — `Activar` de servicio y profesional sin caso de uso

- **Pregunta:** `modelo-dominio.md` define `Servicio.Activar()` y `Profesional.Activar()`, pero CU-13 y CU-14 solo hablan de "alta, edición y desactivación", y `arquitectura.md` §2.3 no tiene ningún caso de uso de reactivación.
- **Opciones:**
  - (a) Implementar los métodos de dominio con sus pruebas, sin caso de uso ni botón. Quedan para cuando un CU los pida.
  - (b) No implementarlos hasta que exista un CU.
  - (c) Añadir `ActivarServicio` y `ActivarProfesional` con botón. Amplía CU-13 y CU-14.
- **Recomendación y motivo:** (b). Sin caso de uso es código sin uso (AGENTS §3.1). Se anota en `modelo-dominio.md` que no tienen caso de uso en el MVP.
- **Bloquea:** T3, T4.
- **Resolución:** opción (b), la recomendada, el 2026-10-10 por indicación del desarrollador.

### Decisiones tomadas durante la implementación (2026-10-10)

- **T13 se hizo en una segunda tanda** (`/app/services` y `/app/staff`, en `Components/App/Servicios.razor` y `Profesionales.razor`). Las dos páginas solo usan casos de uso, ejecutan uno por acción y dejan claro que una política vacía significa heredar. La asociación se marca con casillas desde la ficha de cada profesional (CU-15).
- **Los `IRepositorio*` pasan de `Aplicacion/Abstracciones` a `Dominio/Repositorios`** (`namespace PlataformaReservas.Dominio.Repositorios`), a propuesta del desarrollador: el repositorio es un contrato del modelo (DDD). Solo cambian la ubicación, el espacio de nombres y los `using`; ninguna firma. Se actualizan `AGENTS.md` §6, `arquitectura.md` §2.3 y `modelo-dominio.md`. `AGENTS.md` y `Dominio/Repositorios` quedan así fuera del radio de §4, con autorización.
- **Los conversores de EF pasan de `Persistencia/` a `Persistencia/Conversores/`** (`namespace PlataformaReservas.Infraestructura.Persistencia.Conversores`), a petición del desarrollador. Solo cambian la ubicación, el espacio de nombres y un `using` en cinco `Configuracion*`; ninguna lógica ni el modelo de EF.
- **Correcciones tras la revisión del 2026-10-10 19:29** (`fase-08-revision.md`), decididas por el desarrollador:
  - D-09 y SPEC §8.1/§8.2 alineados con el traslado de los `IRepositorio*` a `Dominio`.
  - Los identificadores en inglés de `AltaEmpresa.razor` (`Input`, `InputModel`, `Password`, `ConfirmPassword`) quedan autorizados como excepción de plantilla en `AGENTS.md` §5.
  - `PruebasRegistrarEmpresa` prueba ahora un fallo a mitad (choque del índice único del slug después de crear la cuenta) y comprueba que no queda el usuario (RN-15). Sabotaje: sin la transacción, la prueba falla.
  - `ServicioCuentas.RenovarSelloMiembrosAsync` lanza si `empresaId` no es `EmpresaActualId`; las tres excepciones al filtro por empresa quedan en `arquitectura.md` §8.2.
  - El radio de §4 se amplía con las carpetas espejo de pruebas.
  - Hallazgo 3 (analizadores en `AltaEmpresa.razor`): cerrado como falso positivo (opción a del desarrollador). Los analizadores no se ejecutan en ningún `.razor` del proyecto, esté o no en `Cuenta/`: lo comprobaron CA1305 y CA1861 introducidos a propósito. Queda anotado en `arquitectura.md` §3.3; `.editorconfig` no cambia.
- **Cierre de la segunda revisión (2026-10-10 20:04)**, con las opciones elegidas por el desarrollador:
  - T8 crea comando solo para `ActualizarEmpresa`. `DarDeBajaEmpresa` y `ReactivarEmpresa` no tienen datos de entrada y siguen la regla, ahora escrita en AGENTS §8 bis y `arquitectura.md` §5.2: sin datos de entrada, no hay comando.
  - `/app` (`Inicio.razor`) declara `@rendermode InteractiveServer` (D-01).
  - Las pruebas de `Resultado<T>` pasan a `Compartido/PruebasResultadoT.cs` (AGENTS §11 bis).
  - La prueba "una reserva completada no se cancela por la empresa" se hará en la Fase 11, con `Reserva.Completar`; hoy el código ya la cubre con `Estado != Confirmada`. Queda anotada en `fase-09.md` §9.
  - Las etiquetas de políticas de `Configuracion.razor` y `Servicios.razor` toman los rangos de las constantes `Minimo` y `Maximo` de los objetos de valor.
  - `AgregarPuertosAplicacion` registra reloj, unidad de trabajo, catálogo y contexto de empresa por separado de `AgregarRepositorios`.
  - C08 de `comprobaciones.sh` ignora los comentarios.
- **CA1000 suprimido solo en `Aplicacion/Compartido/ResultadoT.cs`**, aprobado por el desarrollador: `Resultado<T>.Exito` y `Fallo` son la firma literal de `arquitectura.md` §6. Anotado en `.editorconfig` y en `arquitectura.md` §3.3 (A-18). `.editorconfig` queda así fuera del radio de §4, con autorización.
- **`@namespace PlataformaReservas.Web.Components.AreaEmpresa`** en `Components/App/_Imports.razor`: la carpeta `App` generaría el espacio de nombres `...Components.App`, que choca con el componente raíz `App`.
- **`IServicioCuentas.RenovarSelloMiembrosAsync(empresaId)`** renueva el sello del propietario al dar de baja (`arquitectura.md` §8.4) sin dar `IContextoEmpresa` a `RepositorioMiembros`, que lo usa `FabricaClaimsUsuario` al iniciar sesión.
- **`IRepositorioEmpresas.ExisteSlugAsync` no filtra por empresa**: el slug es único en toda la plataforma (RN-10) y solo devuelve si existe.
- **`CrearServicio` y `CrearProfesional`** llaman a `Crear` y después a `ActualizarDatos` o `EstablecerPoliticas`, para no cambiar las firmas de `Crear`.
- **Pruebas de integración**: `BaseDatosFixture` registra `AgregarAplicacion`, un `RelojFijo` y un `ContextoEmpresaAjustable`; `AmbitoDeEmpresa(id)` abre un ámbito con esa empresa. `EscenarioEmpresa` crea y relee datos.

## 12. Cierre de la fase

- [x] `dotnet build` sin avisos
- [x] `dotnet test` en verde
- [x] `/revisar` sin hallazgos bloqueantes *(2026-10-10 20:22: `APTO` en la tercera ronda, con 211 unitarias y 115 de integración. Queda 1 menor abierto, y el importante 4 (rechazo de `CancelarPorEmpresa` sobre una reserva completada) está aplazado a la Fase 11; ver el seguimiento de 2026-10-12 en `fase-08-revision.md`)*
- [x] Commit hecho *(`547f191`, `bd71111` y `cdbac62`. Desde el 2026-10-12 el mensaje es libre (`AGENTS.md` §12))*
- [x] Punto de control superado, si esta fase cierra alguno (no cierra ninguno) *(no aplica)*
- [x] Tareas de verificación de `PROJECT_PLAN.md` §6 resueltas, si alguna es de esta fase (ninguna) *(no aplica)*

