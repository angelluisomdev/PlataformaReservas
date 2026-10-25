# Reglas de negocio

**Fase 1 — Modelado funcional.** Ubicación final: `docs/reglas-negocio.md`.

Catálogo de las reglas que gobiernan el sistema. Cada regla se deriva de una sección concreta de `TFM_PlataformaReservas_SPEC.md` y se verifica con al menos una prueba.

Ninguna regla de este documento amplía el alcance del SPEC. Si una regla parece necesaria y no aparece aquí, es que hay un hueco en el SPEC y hay que resolverlo allí primero.

## Cómo leer este documento

- **RN-xx** identifica la regla de forma estable. Los identificadores no se reutilizan aunque una regla se elimine.
- **Origen** indica la sección del SPEC de la que se deriva.
- **Verificación** indica el tipo de prueba que la cubre: `U` unitaria, `I` integración, `C` concurrencia, `V` validación de entrada.
- Las reglas marcadas con **⚑** son las que sostienen el valor técnico del TFM. No se relajan bajo ninguna circunstancia.

---

## 1. Aislamiento entre empresas

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-01 | Todo recurso de negocio (`Servicio`, `Profesional`, `Horario`, `ExcepcionHorario`, `Reserva`, `MiembroEmpresa`) pertenece a **exactamente una** empresa, identificada por `EmpresaId`. | §10 | I |
| RN-02 ⚑ | Un usuario solo puede leer, crear, modificar o eliminar recursos de la empresa a la que pertenece según `MiembroEmpresa`. | §10.1 | I |
| RN-03 ⚑ | El intento de acceder a un recurso de otra empresa por identificador directo responde **"no encontrado"**, nunca "prohibido". No se revela la existencia del recurso ajeno. | §10.3 | I |
| RN-04 | Las consultas del catálogo público **nunca aplican contexto de empresa**: no reciben `IContextoEmpresa` y no pueden filtrar por él. Qué empresas son visibles lo define RN-109. | §10.3 | I |
| RN-05 | El área de cliente no filtra por empresa: filtra por `UsuarioId`. Un cliente ve sus reservas en todas las empresas donde haya reservado. | §10.3 | I |
| RN-06 | En el MVP un usuario tiene como máximo una pertenencia (`MiembroEmpresa`). Un segundo alta de empresa para el mismo usuario se rechaza. | §9.4 | V |

---

## 2. Empresa

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-10 | El `Slug` de una empresa es único en toda la plataforma y es la clave de su dirección pública `/businesses/{slug}`. | §9.3 | V, I |
| RN-11 | El `Slug` se genera a partir del nombre en el alta y, si colisiona, se le añade un sufijo numérico hasta que sea único. | §9.3 | V |
| RN-12 | Una empresa tiene exactamente una categoría. No hay categoría múltiple en el MVP. | §9.3 | V |
| RN-13 | Una empresa inactiva no aparece en el buscador, su ficha pública no es accesible y no admite nuevas reservas. Sus reservas existentes se conservan. | §9.3 | I |
| RN-14 | Las tres políticas de reserva de la empresa (`IntervaloHuecosMinutos`, `AntelacionMinimaMinutos`, `AntelacionMaximaDias`) son obligatorias y siempre tienen valor. En el alta toman los valores por defecto 15, 60 y 60. | §9.3 | V |
| RN-15 | El alta de empresa crea, en la misma operación atómica: el `Usuario`, la asignación del rol `Propietario`, la `Empresa` y el `MiembroEmpresa` que los vincula. Si algo falla, no se crea nada. | §5.3 | I |
| RN-16 ⚑ | Una empresa es **publicable** si y solo si existe al menos una combinación completa de servicio activo, profesional activo asociado a ese servicio, y ese profesional con al menos un intervalo de horario definido. | §9.3 | I |
| RN-17 | Una empresa no publicable no aparece en la búsqueda y su ficha pública devuelve "no encontrado", aunque esté activa y aunque se conozca su slug. | RN-16 | I |
| RN-18 | La condición de publicable **se calcula en cada consulta**, no se almacena. No existe campo `Publicada` ni proceso que lo mantenga. | RN-16 | I |
| RN-19 | Mientras la empresa no sea publicable, su panel indica de forma explícita que el establecimiento aún no es visible y qué requisitos faltan. | RN-16 | — |

> **Por qué RN-16 exige la combinación completa y no tres condiciones sueltas.** "Tiene servicios y tiene profesionales y tiene horarios" es una condición más débil y deja pasar el caso malo: una empresa con un servicio sin ningún profesional asignado, o con un profesional que sí tiene horario pero no presta ese servicio. En ambos casos la ficha se publica y el motor de disponibilidad no devuelve ni un hueco. La condición correcta es que exista **al menos un camino completo** servicio → profesional → horario, que es exactamente lo que el motor necesita para producir un hueco.
>
> **Por qué RN-18 calcula en vez de almacenar.** Un campo `Publicada` habría que recalcularlo al desactivar un servicio, al desactivar un profesional, al romper una asociación y al borrar un horario: cuatro puntos donde puede quedar desincronizado, y un estado incorrecto significa una ficha publicada que no permite reservar, o una empresa correcta que nadie encuentra. La búsqueda ya acota por ciudad y categoría antes de evaluar esta condición (RN-110), así que se evalúa sobre un conjunto pequeño. Si el rendimiento llegara a molestar, la optimización es un campo desnormalizado recalculado en esos cuatro puntos; queda anotada como trabajo futuro, no se implementa ahora.

---

## 2 bis. Datos de contacto

Se aplican allí donde aparece el dato: empresa, profesional, usuario y la copia del teléfono en la reserva. Se comprueban al construir los objetos de valor `Email`, `Telefono` y `CodigoPostal` (`modelo-dominio.md` §5.5). *Añadidas el 2026-10-04.*

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-177 | Un correo electrónico es obligatorio donde se pide, tiene como mucho 160 caracteres y es una dirección válida sin nombre visible (`ana@ejemplo.es`, no `Ana <ana@ejemplo.es>`). | M-05 | U |
| RN-178 | Un teléfono tiene como mucho 20 caracteres, solo admite dígitos, espacios y guiones, con un `+` opcional al principio, y contiene entre 6 y 15 dígitos. | M-05 | U, I |
| RN-179 | El código postal de una empresa tiene exactamente 5 dígitos. | M-05 | U |

---

## 3. Servicio

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-20 | `DuracionMinutos` debe ser mayor que cero. | §9.5, §14 | V |
| RN-21 | `Precio` debe ser mayor o igual que cero. Un servicio gratuito es válido. | §9.5, §14 | V |
| RN-22 | Las tres políticas del servicio son opcionales. Un valor `null` significa "heredar de la empresa", no "sin límite". | §4.1 | U |
| RN-23 ⚑ | El valor efectivo de cada política es `Servicio.X ?? Empresa.X`. Esta resolución se implementa una sola vez en el dominio y se usa tanto en el motor de disponibilidad como en la validación de la reserva. | §4.1 | U |
| RN-24 | Un servicio inactivo no genera huecos, no aparece en la ficha pública y no admite nuevas reservas. Sus reservas existentes se conservan intactas. | §14 | U, I |
| RN-25 | Un servicio no se borra físicamente nunca. Se desactiva. | §14 | I |

---

## 3 bis. Rangos válidos de las políticas de reserva

Se aplican **igual en los dos niveles**: al valor obligatorio de la empresa y al valor opcional del servicio cuando no es `null`.

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-26 ⚑ | `IntervaloHuecosMinutos` debe estar entre **1 y 480** minutos, ambos incluidos. | RN-14, RN-22 | V, U |
| RN-27 | `AntelacionMinimaMinutos` debe estar entre **0 y 43 200** minutos (30 días), ambos incluidos. Cero significa "se puede reservar hasta el último momento". | RN-14, RN-22 | V, U |
| RN-28 | `AntelacionMaximaDias` debe estar entre **1 y 365**, ambos incluidos. | RN-14, RN-22 | V, U |
| RN-29 ⚑ | Los tres rangos son **invariantes de dominio**, no validaciones de formulario. Se comprueban al construir `PoliticasReserva` (SPEC §4.1) y es imposible obtener una instancia fuera de rango. La validación de interfaz es una comodidad adicional, nunca la única defensa. | §4.1 | U |

> **Por qué RN-26 lleva marca de crítica.** El paso 5.4 del algoritmo (SPEC §12.2) avanza con `cursor = cursor + intervalo`. Un intervalo de **cero produce un bucle infinito**: no un resultado extraño, sino un hilo colgado y la petición muerta. Y el valor lo teclea el propietario desde la pantalla de configuración. Un valor negativo haría retroceder el cursor, con el mismo efecto.
>
> El techo de 480 minutos (una jornada) evita el extremo contrario, que es inútil pero no peligroso. El suelo de 1 minuto sí produce hasta 480 huecos en una jornada de ocho horas: es legítimo, y es responsabilidad de la interfaz paginarlos o agruparlos, no del motor rechazarlos.
>
> **Por qué RN-28 empieza en 1.** Con `AntelacionMaximaDias = 0` la condición `fecha > hoy + 0` descarta cualquier fecha futura y solo dejaría reservar hoy mismo — casi siempre incompatible con la antelación mínima, de modo que la empresa se quedaría sin poder recibir ni una reserva sin entender por qué.

---

## 4. Profesional

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-30 | Un profesional pertenece a una única empresa. No se comparte entre empresas. | §9.6 | V |
| RN-31 | Un profesional y un servicio solo pueden asociarse si pertenecen a la misma empresa. | §9.7 | V, I |
| RN-32 | La asociación profesional-servicio es única: no puede repetirse el mismo par. | §9.7 | V |
| RN-33 | Un profesional inactivo no genera huecos ni aparece en la ficha pública. Sus reservas existentes se conservan. | §14 | U, I |
| RN-34 | Un profesional no se borra físicamente nunca. Se desactiva. | §14 | I |

---

## 5. Horarios y excepciones

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-40 | El horario se define **siempre por profesional**. No existe horario de empresa. | §4 (D-04) | — |
| RN-41 | En un intervalo de horario, `HoraInicio` debe ser estrictamente menor que `HoraFin`. | §9.8 | V |
| RN-42 | Dos intervalos de horario del mismo profesional y el mismo día de la semana no pueden solaparse. | §9.8 | V, U |
| RN-43 | Un profesional puede tener varios intervalos el mismo día, lo que permite jornada partida. | §9.8 | U |
| RN-44 | Una excepción de horario inhabilita el **día completo** para ese profesional. No existe horario personalizado en el MVP. | §9.9 | U |
| RN-45 | Solo puede existir una excepción por profesional y fecha. | §9.9 | V |
| RN-46 | Crear una excepción **no cancela** las reservas ya existentes de ese día. Solo impide generar huecos nuevos. La empresa debe cancelarlas manualmente si procede. | §9.9, §9.11 | I |
| RN-47 | Modificar o eliminar un horario **no afecta** a las reservas ya creadas. | §9.8 | I |

> RN-46 y RN-47 son decisiones conscientes. Una excepción de horario es un cambio de agenda que la empresa puede querer resolver caso por caso, no la desaparición de una de las partes. Si decide cancelar alguna de esas citas, lo hace a mano y entonces sí sale el aviso de RN-149.

---

## 6. Disponibilidad

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-50 ⚑ | Los intervalos de tiempo son **semiabiertos**: `[inicio, fin)`. 09:00–10:00 y 10:00–11:00 no se solapan. | §12.4 | U |
| RN-51 ⚑ | Dos intervalos se solapan si y solo si `inicioA < finB && finA > inicioB`. Esta regla vive en un único punto del dominio y no se reimplementa en ninguna capa. | §12.4 | U |
| RN-52 | Un hueco solo se ofrece si el servicio cabe **completo** dentro de un intervalo de horario del profesional. | §12.2 | U |
| RN-53 ⚑ | Un hueco no se ofrece si se solapa con una reserva existente del mismo profesional en estado distinto de `Cancelada`. | §12.2 | U |
| RN-54 | Un hueco no se ofrece si su inicio es anterior a `ahora + antelación mínima efectiva`. | §12.2 | U |
| RN-55 | No se ofrecen huecos para fechas posteriores a `hoy + ventana máxima efectiva`. | §12.2 | U |
| RN-56 | Solo generan huecos los profesionales activos, de la empresa, y asociados al servicio solicitado. | §12.2 | U |
| RN-57 | Si el profesional tiene una excepción ese día, no genera ningún hueco. | §12.2 | U |
| RN-58 | El intervalo de generación de huecos es configurable y **puede ser menor que la duración del servicio**, produciendo huecos solapados entre sí. Es el comportamiento deseado, no un defecto. | §12.3 | U |
| RN-59 | Los huecos se devuelven ordenados por instante de inicio y, en caso de empate, por nombre de profesional. | §12.2 | U |
| RN-60 ⚑ | El cálculo de disponibilidad es **idéntico** para el flujo público y para el área de empresa. Existe una sola implementación. | §12 | — |

---

## 7. Creación de reserva

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-70 | Crear una reserva exige usuario autenticado. No existe reserva como invitado. | §4 (D-02) | I |
| RN-71 | El servicio debe existir, estar activo y pertenecer a la empresa indicada. | §13 | V, I |
| RN-72 | El profesional debe existir, estar activo y pertenecer a la empresa indicada. | §13 | V, I |
| RN-73 | El profesional debe prestar el servicio solicitado. | §13 | V, I |
| RN-74 ⚑ | `FinUtc` se calcula **en servidor** como `InicioUtc + DuracionMinutos`. Nunca se acepta del cliente. | §13 | I |
| RN-75 | El inicio debe respetar la antelación mínima y la ventana máxima efectivas del servicio. | §13 | I |
| RN-76 ⚑ | La disponibilidad se recomprueba dentro de la transacción, inmediatamente antes de insertar. | §13 | I |
| RN-77 ⚑ | **Nunca pueden existir dos reservas no canceladas del mismo profesional cuyos intervalos se solapen.** Esta garantía la impone la base de datos mediante una restricción `EXCLUDE`, no el código de aplicación. | §13 | C |
| RN-78 | Al crear la reserva se copian `PrecioAplicado`, `DuracionAplicadaMinutos`, `NombreCliente` y `TelefonoCliente`. Cambios posteriores en el servicio o en el perfil del usuario no alteran el histórico. | §9.10 | I |
| RN-79 | Una reserva nace en estado `Confirmada`. No existe estado intermedio de espera. | §9.11 | I |
| RN-80 | Si la restricción de solapamiento rechaza la inserción, el sistema devuelve el error `HuecoYaNoDisponible` y la interfaz recarga los huecos del día. La operación no se reintenta de forma automática. | §13 | C |
| RN-81 | El teléfono es **opcional al registrarse** pero **obligatorio para reservar**. Si el usuario no lo tiene, el flujo de reserva se lo pide antes de confirmar y lo guarda también en su perfil, de modo que no vuelva a pedírselo. | §9.1, §9.10 | V, I |
| RN-82 | `Reserva.TelefonoCliente` y `Reserva.NombreCliente` son obligatorios y no admiten nulo. Son la única vía de contacto de la empresa con el cliente mientras no existan notificaciones. | §9.10 | V |
| RN-83 | Un usuario inactivo no puede crear reservas, porque no puede iniciar sesión (RN-135). | RN-135 | I |

---

## 8. Ciclo de vida de la reserva

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-90 | Los únicos estados son `Confirmada`, `Cancelada` y `Completada`. | §9.11 | — |
| RN-91 | Las únicas transiciones legales son `Confirmada → Cancelada` y `Confirmada → Completada`. Cualquier otra se rechaza. | §9.11 | U, I |
| RN-92 | `Cancelada` y `Completada` son estados terminales. | §9.11 | U |
| RN-93 | El cliente puede cancelar su reserva mientras `InicioUtc > ahora`. Pasada la hora de inicio, no. | §9.11 | I |
| RN-94 | La empresa puede cancelar cualquiera de sus reservas en estado `Confirmada`, sin restricción temporal. | §9.11 | I |
| RN-95 | Un usuario solo puede cancelar reservas cuyo `UsuarioId` coincida con el suyo. | §10.3 | I |
| RN-96 | Marcar como `Completada` es una acción manual de la empresa y solo es posible si `FinUtc < ahora`. No existe proceso automático. | §9.11 | I |
| RN-97 ⚑ | Una reserva `Cancelada` libera el hueco de inmediato, tanto para el motor de disponibilidad como para la restricción de base de datos. | §9.11, §13 | U, C |
| RN-98 | Al cancelar se registra `FechaCancelacion`. La reserva no se borra. | §9.10 | I |

---

## 9. Tiempo y zona horaria

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-100 | Todo instante absoluto se persiste en UTC (`timestamptz`). El nombre de la propiedad no necesita sufijo `Utc` (decidido en la Fase 5). | §9 | U |
| RN-101 | Los horarios (`TimeOnly`) y las fechas de excepción (`DateOnly`) son hora local de la empresa, sin conversión en base de datos. | §9.8, §9.9 | U |
| RN-102 | La conversión entre hora local y UTC ocurre **en un único punto**, dentro del motor de disponibilidad. | §12.2 | U |
| RN-103 | En el MVP la zona horaria es única y fija: `Europe/Madrid`. `Empresa.ZonaHoraria` existe pero no se lee. | §4 (D-08) | — |
| RN-104 | El cambio de hora estacional se resuelve por la conversión de zona horaria estándar del sistema. No se implementa lógica propia. | §12.2 | U |

> **RN-104 tiene prueba obligatoria**: es el caso 13 de §16.1 del SPEC. El motor combina una hora local con una fecha y convierte a UTC; en el día del cambio estacional una hora local no existe o existe dos veces, y una conversión ingenua desplaza **toda la agenda de esa jornada**. Ocurre dos veces al año y afecta a todas las reservas del día.

---

## 10. Búsqueda y catálogo público

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-109 | La búsqueda devuelve únicamente empresas que estén **activas** (RN-13) **y publicables** (RN-16). Ambas condiciones se aplican siempre, en toda consulta del catálogo público. | RN-16 | I |
| RN-110 ⚑ | **La ciudad es un filtro obligatorio.** Se considera indicada cuando tiene al menos **2 caracteres tras recortar espacios**; por debajo de eso se trata como ausente (RN-112). La comparación es por texto libre, sin distinguir mayúsculas ni acentos. | §5.1 | I, V |
| RN-111 | La categoría es un filtro opcional que refina el resultado dentro de la ciudad. | §5.1 | I |
| RN-112 | Una petición sin ciudad, o con una ciudad vacía o en blanco, devuelve **lista vacía** y el formulario de búsqueda. No es un error: es el estado inicial de la pantalla. | RN-110 | I, V |
| RN-113 ⚑ | Los resultados se paginan con un tamaño de página máximo fijado en servidor (20). Un tamaño mayor solicitado por el cliente se ignora y se aplica el máximo. | RN-110 | I, V |
| RN-114 | La ficha pública muestra únicamente servicios y profesionales activos. | §5.1 | I |
| RN-115 | El horario que muestra la ficha pública es la unión de los horarios de sus profesionales activos. No existe horario de empresa (RN-40). | §5.1 | I |
| RN-116 | La ficha de una empresa inexistente, inactiva o no publicable devuelve "no encontrado". | RN-13, RN-17 | I |

> **Por qué RN-110 y RN-113.** Sin filtro obligatorio, `/businesses` es un volcado completo del catálogo: una sola petición devuelve todas las empresas de la plataforma, con su nombre, dirección y teléfono. Eso es un problema de coste (la consulta crece con el negocio) y de exposición (extracción masiva de datos de contacto de los clientes de la plataforma).
>
> Obligar a indicar ciudad **encarece** la extracción, no la impide: quien quiera enumerar puede recorrer una lista de municipios. La pareja que de verdad acota es **filtro obligatorio + tamaño de página en servidor**, y por eso van juntas. La limitación de frecuencia de peticiones por IP, que en la Fase 1 quedó fuera de alcance, entró después con D-23 (E-5, RN-173): 60 peticiones por minuto e IP en `/businesses` y en la ficha (`arquitectura.md` §14.5).
>
> **Por qué el mínimo son 2 caracteres y no 3 o 4.** Porque hay municipios españoles más cortos que eso. **Ea** (Vizcaya) tiene dos letras y es el municipio con el nombre más corto de España. Con tres letras hay alrededor de treinta: Oia, Mos y Tui en Pontevedra; Ibi y Sax en Alicante; Vic en Barcelona; Alp, Das, Ger, Pau, Les y Bot en Cataluña; Cee, Teo, Zas, Foz y Pol en Galicia; Gor, Jun y Rus en Andalucía. Un mínimo de 3 dejaría a Ea fuera del buscador y uno de 4 excluiría a treinta y un municipios.
>
> Además, `Empresa.Ciudad` es texto libre que teclea el propietario: puede contener una pedanía o una localidad que no sea municipio, y algunas son aún más cortas. El mínimo de 2 solo existe para evitar la consulta de un único carácter; quien acota de verdad el volumen es RN-113.

---

## 11. Datos y borrado

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-120 | No existe borrado físico de `Empresa`, `Servicio`, `Profesional` ni `Reserva`. | §14 | I |
| RN-121 | `Horario` y `ExcepcionHorario` sí se borran físicamente: no tienen histórico asociado. | §14 | I |
| RN-122 | Las categorías se cargan por datos iniciales y no se mantienen desde la aplicación. | §5.4 | — |
| RN-123 | Los datos iniciales solo se cargan en entorno de desarrollo. | §14 | — |

---

## 11 bis. Baja de empresa y baja de cuenta

Resuelve el hueco detectado en la revisión: `Empresa.Activa` y `Usuario.Activo` existían en el modelo pero **ninguna funcionalidad podía cambiarlos**.

### Baja de empresa

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-130 | El propietario puede dar de baja su empresa desde `/app/settings`. La baja pone `Activa = false`; nunca borra la empresa (RN-120). | CU-23 | I |
| RN-131 | La baja exige confirmación explícita y el sistema indica antes **cuántas reservas futuras confirmadas** se van a cancelar. | CU-23 | — |
| RN-132 ⚑ | Al dar de baja la empresa, **todas sus reservas confirmadas con `InicioUtc > ahora` pasan a `Cancelada`** en la misma transacción. Las pasadas y las ya completadas no se tocan. | RN-13 | I |
| RN-133 | La baja es reversible: el propietario puede reactivar su empresa. Las reservas canceladas por la baja **no se restauran**. | RN-130 | I |
| RN-134 | Una empresa inactiva desaparece del catálogo público (RN-13, RN-109) pero su propietario conserva el acceso a `/app` para consultar el histórico y reactivarla. | RN-13 | I |

### Baja de cuenta

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-135 ⚑ | **Un usuario inactivo no puede iniciar sesión.** La comprobación ocurre en la autenticación, no en la interfaz. Una sesión abierta previamente deja de ser válida en la siguiente petición. | §9.1 | I |
| RN-136 | El usuario puede darse de baja desde `/my-account/profile`. La baja pone `Activo = false`; no borra el usuario ni sus reservas. | CU-22 | I |
| RN-137 ⚑ | Al darse de baja, **todas sus reservas confirmadas con `InicioUtc > ahora` pasan a `Cancelada`** en la misma transacción, liberando los huecos para las empresas afectadas. | CU-22 | I |
| RN-138 | Un propietario con empresa activa **no puede darse de baja** sin dar de baja antes su empresa. El sistema lo indica y enlaza a `/app/settings`. Evita empresas sin propietario accesible. | RN-130 | V, I |
| RN-139 | La baja de cuenta es una desactivación, no un borrado: el correo sigue ocupado y el histórico se conserva. Reactivar una cuenta dada de baja **no está contemplado en el MVP**: requiere intervención sobre la base de datos. | §14 | — |

> **Aviso de cumplimiento normativo.** RN-139 es una limitación consciente y hay que declararla como tal en la memoria. El derecho de supresión del RGPD exige poder eliminar realmente los datos personales, y un `Activo = false` no lo satisface. Un borrado real obligaría a decidir qué ocurre con el histórico de reservas de las empresas, que tienen su propio interés legítimo en conservarlo, y a anonimizar en vez de borrar. Eso es un trabajo de diseño que excede el MVP y se documenta como trabajo futuro, no como descuido.
>
> **Por qué RN-132 y RN-137 cancelan en cascada, y RN-46 no.** Aquí la cascada sí es correcta: si la empresa cierra o el cliente se va, esas reservas **no se van a atender**. Dejarlas confirmadas sería mentir a la otra parte. En cambio una excepción de horario (RN-46) es un cambio de agenda que la empresa puede querer resolver caso por caso. La diferencia es si desaparece una de las dos partes o solo cambia la disponibilidad.
>
> La cancelación en cascada **sí se notifica** por correo: RN-150 al cliente cuando cierra el establecimiento, RN-151 al negocio cuando el cliente se da de baja. Es precisamente el caso que justifica la cola de eventos, porque una baja con 300 reservas futuras son 300 correos.

---

## 11 ter. Notificaciones por correo

El correo entra en el alcance del MVP. SMS, mensajería y recordatorios siguen fuera (SPEC §6.2).

### Mecanismo

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-140 ⚑ | Ningún envío de correo ocurre dentro de una transacción de negocio. La operación **confirma primero** y el evento se encola después. | §13 | I |
| RN-141 ⚑ | Un fallo al enviar un correo **nunca deshace ni impide** la operación que lo originó. Se registra y se sigue. | §13 | I |
| RN-142 | Los eventos se encolan en memoria y se procesan en segundo plano. El usuario no espera al envío. | §13 | I |
| RN-143 | Cada manejador vuelve a leer los datos que necesita: los eventos llevan identificadores, no entidades. | §13 | U |
| RN-144 | Si el proceso se reinicia con eventos en cola, **esos correos se pierden**. Limitación declarada. | RN-142 | — |
| RN-145 | En entorno de desarrollo y en las pruebas, el correo **no se envía de verdad**: se registra o se captura. | §14 | I |

### Qué se notifica

| ID | Evento | Destinatario | Contenido mínimo | Verif. |
|---|---|---|---|---|
| RN-146 | Reserva confirmada (CU-07) | **Cliente** | establecimiento, dirección, servicio, profesional, fecha, hora, duración, precio | I |
| RN-147 | Reserva confirmada (CU-07) | **Negocio** | servicio, profesional, fecha, hora, nombre y teléfono del cliente | I |
| RN-148 | Reserva cancelada por el cliente (CU-10) | **Negocio** | los mismos datos, indicando que la cancela el cliente | I |
| RN-149 | Reserva cancelada por la empresa (CU-20) | **Cliente** | los mismos datos, indicando que la cancela el establecimiento | I |
| RN-150 ⚑ | Reserva cancelada **por baja de la empresa** (RN-132) | **Cliente** | cita anulada porque el establecimiento ha cerrado | I |
| RN-151 | Reserva cancelada **por baja de la cuenta del cliente** (RN-137) | **Negocio** | cita anulada porque el cliente ha dado de baja su cuenta | I |
| RN-152 | Empresa registrada (CU-11) | Propietario | enlace a su ficha pública y pasos que faltan para publicarla | I |
| RN-153 | Empresa dada de baja (CU-23) | Propietario | confirmación, número de reservas canceladas y cómo reactivar | I |
| RN-154 | Cuenta registrada (CU-04) | Usuario | bienvenida | I |
| RN-155 | Cuenta dada de baja (CU-22) | Usuario | confirmación y número de reservas canceladas | I |
| RN-156 ⚑ | Recuperación de contraseña (CU-24) | Usuario | enlace con token de un solo uso | I |

> **El texto cambia según por qué se canceló.** No es lo mismo "has cancelado tu cita" que "el establecimiento ha cerrado y tu cita del jueves queda anulada". Por eso el evento `ReservaCancelada` lleva un `OrigenCancelacion` y no solo el identificador: sin ese dato, el manejador tendría que adivinar el motivo.
>
> **RN-150 y RN-151 son la razón de peso para montar la cola.** Dar de baja una empresa con 300 reservas futuras son 300 correos. En línea, eso es un botón colgado varios minutos; en cola, el botón responde igual con 3 que con 300.

### Recuperación de contraseña

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-157 | Cualquier usuario puede solicitar el restablecimiento desde el formulario público, **sea cliente o propietario**: es el mismo flujo y la misma entidad `Usuario`. | CU-24 | I |
| RN-158 ⚑ | La solicitud responde **siempre lo mismo**, exista o no la cuenta. No se revela qué correos están registrados. | CU-24 | I |
| RN-159 | El token caduca y es de un solo uso. Lo gestiona Identity; no se implementa a mano. | CU-24 | I |
| RN-160 | Una cuenta **inactiva** no recibe correo de restablecimiento, y la respuesta es igualmente indistinguible (RN-158). | RN-135 | I |
| RN-161 | Restablecer la contraseña **renueva el sello de seguridad**, de modo que las sesiones abiertas se invalidan. | RN-135 | I |

### Lo que sigue sin notificarse

| Situación | Motivo |
|---|---|
| Verificación de la dirección de correo al registrarse | `RequireConfirmedAccount` sigue en `false`. Obligar a confirmar antes de reservar rompería el flujo de CU-07 alt. 1a, que retoma la reserva donde estaba. Limitación de seguridad declarada en `arquitectura.md` §8.1 |
| Recordatorio antes de la cita | Fuera de alcance: necesita un planificador, no una cola de eventos |
| Excepción de horario creada sobre un día con reservas (RN-46) | La excepción no cancela nada, así que no hay nada que comunicar. La empresa decide y cancela a mano si procede, y entonces sí sale RN-149 |
| Reserva completada (CU-21) | No aporta nada al cliente, que acaba de estar allí |

---

## 11 quater. Protección de datos personales

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-162 ⚑ | Los datos personales del usuario —nombre, correo, nombre de acceso y teléfono— se **cifran en reposo** con `ProtectPersonalData` de Identity. | §15.1 | I |
| RN-163 ⚑ | `Reserva.NombreCliente` y `Reserva.TelefonoCliente` se cifran con conversores de EF Core. **Sin esto, cifrar el usuario no sirve de nada**: los mismos datos quedarían en claro en la tabla de reservas. | §15.1 | I |
| RN-164 | **Los datos de la empresa no se cifran.** Son públicos: se muestran en su ficha. Además, `Ciudad` debe poder buscarse (RN-110). | §15.1 | I |
| RN-165 | Las contraseñas siguen con **hash irreversible** de Identity, no con cifrado. No se descifran nunca. | §15 | — |
| RN-166 | La búsqueda por correo usa un valor **determinista** (HMAC) almacenado en `NormalizedEmail`. Iniciar sesión, la unicidad y CU-24 siguen funcionando. | §15.1 | I |
| RN-167 | Una consulta LINQ directa sobre un campo protegido **devuelve vacío sin error**. Siempre se usa `FindByEmailAsync`. | RN-166 | I |
| RN-168 | Las claves de cifrado y de búsqueda son **distintas entre sí**, de 256 bits, y nunca están en el repositorio. | §15.1 | — |

> **Qué protege y qué no.** Cubre la filtración de la base de datos —copia robada, volcado, puerto expuesto, acceso de solo lectura mal concedido—, que es como ocurren la mayoría de los incidentes reales. **No cubre el compromiso del servidor de aplicación**, porque la clave vive ahí: la aplicación necesita descifrar para mostrar.
>
> Hay que declararlo exactamente así en la memoria. El artículo 32 del RGPD menciona el cifrado como medida apropiada, pero una medida sin su modelo de amenaza es una casilla marcada, no una decisión.
>
> El valor determinista de RN-166 **filtra la igualdad**: quien vea la base de datos no sabe qué correo es, pero sí sabe que dos filas comparten el mismo. Es inevitable si se quiere poder iniciar sesión.

---

## 11 quinquies. Endurecimiento

Las seis medidas de SPEC §15.2. Todas dentro del alcance (D-23).

| ID | Regla | Origen | Verif. |
|---|---|---|---|
| RN-169 ⚑ | Cinco intentos fallidos de inicio de sesión **bloquean la cuenta 15 minutos**. El inicio de sesión se hace con `lockoutOnFailure: true`; con `false` la configuración no surte efecto. | E-1 | I |
| RN-170 | Una cuenta bloqueada da **el mismo mensaje genérico** que una contraseña incorrecta. No se revela que existe ni que está bloqueada. | RN-169 | I |
| RN-171 ⚑ | **`MarkupString` está prohibido** en las plantillas de correo y en la ficha pública. Todo texto de entrada de usuario se renderiza codificado. | E-3 | I |
| RN-172 | Los intentos fallidos de inicio de sesión se registran con nivel `Warning`, con el **correo enmascarado** y la IP. | E-4 | I |
| RN-173 ⚑ | Limitación de frecuencia: **5 intentos cada 5 minutos** por IP en autenticación, **60 peticiones por minuto** en el buscador. Al superarlo se responde `429`. | E-5 | I |
| RN-174 | La limitación de frecuencia se guarda **en memoria** y por tanto es por instancia. Limitación declarada: con varias instancias el límite real se multiplica. | RN-173 | — |
| RN-175 | La aplicación envía las cabeceras `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy` y, fuera de desarrollo, HSTS. | E-6 | I |
| RN-176 | El análisis de dependencias vulnerables **hace fallar la compilación**, incluyendo las transitivas. | E-2 | — |

> **RN-173 cierra el hueco que RN-110 y RN-113 dejaban abierto a propósito.** Allí se documentó que el filtro obligatorio de ciudad y el tamaño de página acotan el volumen *por petición* pero no el *número* de peticiones, y que limitar la frecuencia quedaba fuera de alcance. Con D-23 ya no lo está, y la tabla de §13 se actualiza en consecuencia.
>
> **RN-171 ataca el único vector de inyección real de este diseño**: `Empresa.Descripcion` es texto libre que escribe el propietario y acaba dentro de correos HTML que recibe el cliente. Blazor codifica por defecto, así que el riesgo está en `MarkupString` y en la concatenación manual de cadenas.

---

## 12. Trazabilidad a las pruebas

Correspondencia entre las reglas marcadas ⚑ y las pruebas obligatorias del SPEC §16.

| Regla | Prueba que la demuestra |
|---|---|
| RN-02, RN-03 | Las 5 pruebas de aislamiento entre empresas (§16.2) |
| RN-16, RN-17 | Prueba de integración de publicación: empresa recién dada de alta → no aparece ni por búsqueda ni por slug directo; se completan servicio, profesional, asociación y horario → aparece; se desactiva el único profesional → vuelve a desaparecer |
| RN-23 | Prueba unitaria 10: intervalo heredado frente a sobrescrito |
| RN-177, RN-178, RN-179 | Pruebas unitarias de `Email`, `Telefono` y `CodigoPostal`; prueba de integración: registrarse con un teléfono mal formado falla sin crear el usuario |
| RN-26, RN-29 | Prueba unitaria: `PoliticasReserva` rechaza intervalo 0, negativo y mayor de 480, y antelación máxima 0 o mayor de 365. **Ningún valor fuera de rango puede llegar al motor** |
| RN-50, RN-51 | Prueba unitaria 8: reservas consecutivas y límites exactos |
| RN-53 | Pruebas unitarias 5, 6 y 7 |
| RN-60 | Verificación arquitectónica: una sola implementación de `IMotorDisponibilidad` |
| RN-74 | Prueba de integración de creación de reserva |
| RN-76, RN-77, RN-80 | **Prueba de concurrencia** (§16.3) |
| RN-97 | Prueba de integración: cancelar y volver a reservar el mismo hueco |
| RN-110, RN-113 | Prueba de integración de búsqueda: sin ciudad → vacío; ciudad de 1 carácter → vacío; `"Ea"` → encuentra; tamaño de página forzado desde cliente → se aplica el máximo |
| RN-132 | Prueba de integración: dar de baja una empresa cancela sus reservas futuras y deja intactas las pasadas y las completadas |
| RN-135 | Prueba de integración: un usuario inactivo no puede iniciar sesión |
| RN-137 | Prueba de integración: darse de baja cancela las reservas futuras y libera los huecos para la empresa |
| RN-140, RN-141 | Prueba de integración con un servicio de correo que **siempre falla**: la reserva se crea igualmente y el fallo queda registrado |
| RN-150 | Prueba de integración: dar de baja una empresa con N reservas futuras encola N correos con origen `BajaEmpresa` |
| RN-156, RN-158 | Prueba de integración: solicitar restablecimiento con un correo inexistente devuelve **la misma respuesta** que con uno válido |
| RN-162, RN-163 | Prueba de integración que lee **con SQL crudo** las tablas `asp_net_users` y `reservas`: ningún nombre, correo ni teléfono aparece en claro |
| RN-164 | La misma prueba comprueba que los datos de `empresas` **sí** están en claro y que el buscador por ciudad funciona |
| RN-166, RN-167 | Prueba de integración: iniciar sesión, registrar un correo duplicado y recuperar la contraseña funcionan con el cifrado activo |
| RN-169 | Prueba de integración: cinco contraseñas incorrectas bloquean, y **la sexta con la contraseña correcta también falla**. Es la que detecta un `lockoutOnFailure` mal puesto |
| RN-171 | Prueba de integración: una descripción con `<script>alert(1)</script>` aparece **escapada** en la ficha pública y en el HTML del correo |
| RN-173 | Prueba de integración: superar el umbral en `/login` y en `/businesses` devuelve `429` |

---

## 13. Reglas descartadas explícitamente

Se documentan para dejar constancia de que la ausencia es deliberada y no un olvido.

| Regla que **no** existe | Motivo |
|---|---|
| Margen de preparación o limpieza entre reservas | Fuera de alcance (§6.2). Se resolvería añadiendo un campo al servicio y ampliando el intervalo comparado en RN-53 |
| Límite de reservas simultáneas por cliente | No pedido. Un cliente puede tener reservas solapadas en empresas distintas |
| Prohibición de que un cliente reserve dos veces a la misma hora | Consecuencia de la anterior. La restricción `EXCLUDE` es por profesional, no por cliente |
| Cancelación con antelación mínima | El cliente puede cancelar hasta el instante de inicio (RN-93). Una política de penalización exigiría pagos |
| Confirmación manual de la reserva por la empresa | No existe estado de espera (RN-79). Añadirlo implicaría notificaciones |
| Aforo o plazas por servicio | Las reservas son individuales. Las reservas de grupo están fuera de alcance |
| Cancelación en cascada al crear una excepción | Ver RN-46 |
| ~~Limitación de frecuencia de peticiones por IP~~ | **Ya no está descartada.** Entra con D-23: ver RN-173 |
| Límite de reservas por cliente y periodo | Un usuario podría ocupar toda la agenda de un establecimiento (OWASP A04). Se descarta porque exige decidir umbral y periodo, y plantea qué hacer con un cliente legítimo que reserva mucho. **Declarado como parcial en la tabla OWASP del SPEC §15.3**, no omitido |
| Segundo factor de autenticación | Fuera del alcance de un MVP. Identity lo soporta; añadirlo sería una fase propia |
| Firma de artefactos de compilación | Exige infraestructura de claves de firma que no aporta al objeto de estudio del TFM (OWASP A08) |
| **Creación manual de reservas por la empresa** | Fuera de alcance por coste. Es la primera funcionalidad que pide cualquier negocio real —el cliente que llama por teléfono— y su ausencia obliga al establecimiento a llevar esas citas por otro medio. Reutilizaría el mismo caso de uso de creación saltándose la exigencia de usuario autenticado (RN-70), lo que a su vez obliga a decidir cómo se identifica a ese cliente sin cuenta. Documentar como la limitación funcional más relevante del MVP |
| Borrado real de datos personales (derecho de supresión) | La baja de cuenta desactiva, no borra (RN-139). Ver el aviso de cumplimiento normativo de §11 bis |
| Reactivación de una cuenta dada de baja | Fuera de alcance (RN-139). Requiere intervención sobre la base de datos |
| Patrón *outbox* transaccional | La cola es volátil (RN-144). El *outbox* —persistir el evento en la misma transacción y marcarlo como enviado— es la respuesta de producción y queda como trabajo futuro con su diseño esbozado |
| Reintento automático de correos fallidos | Un fallo se registra y se abandona (RN-141). Reintentar exige manejadores idempotentes y una política de reintentos, que sin *outbox* no tiene dónde apoyarse |
| Verificación de la dirección de correo | Ver §11 ter |

---

## 14. Decisiones que este documento deja abiertas

Ninguna. Todas las reglas derivan de secciones ya cerradas del SPEC.

Si durante la implementación aparece una situación no cubierta, el procedimiento es: **parar, resolverlo en el SPEC, propagarlo aquí y después programar.** No se improvisa en el código.
