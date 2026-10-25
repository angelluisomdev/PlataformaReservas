# Base de datos

**Fase 5.** Esquema de PostgreSQL de la plataforma: qué tablas hay, qué las protege y por qué.

La fuente del esquema es la migración `Infraestructura/Persistencia/Migraciones/*_Inicial.cs`, generada con EF Core y completada a mano con lo que EF no sabe modelar. Todo el SQL de este documento sale de `dotnet ef migrations script`.

---

## 1. Convenciones

| Qué | Cómo | Origen |
|---|---|---|
| Nombres de tablas y columnas | `snake_case`, con `UseSnakeCaseNamingConvention()` | D-13 |
| Identificadores | `uuid`, generados en la aplicación con `Guid.CreateVersion7()` | D-16 |
| Instantes | `timestamp with time zone`, siempre en UTC. El nombre no lleva sufijo obligatorio: `fecha_alta`, `inicio_utc` | D-08, RN-100 |
| Horas y fechas locales | `time` y `date`, sin conversión | RN-101 |
| Enumeraciones | Entero, con valores estables | `modelo-dominio.md` §7 |
| Importes | `numeric(10,2)` | SPEC §9 |
| Objetos de valor | Aplanados en columnas de la propia tabla, no en tablas aparte | `modelo-dominio.md` §8 |
| Borrado | Sin borrado físico de `empresas`, `servicios`, `profesionales` ni `reservas`: se desactivan | RN-120 |

`IdentityDbContext` fija los nombres `AspNet*` de sus tablas, y la convención `snake_case` no los reescribe. Por eso se fijan explícitamente como `asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens` y `asp_net_role_claims`. Sus columnas e índices propios (`EmailIndex`, `UserNameIndex`, `RoleNameIndex`) se dejan como los genera Identity.

## 2. Tablas

| Tabla | Entidad | Notas |
|---|---|---|
| `categorias` | `Categoria` | Datos maestros, sembrados en desarrollo (§7) |
| `empresas` | `Empresa` | `Politicas` en `intervalo_huecos_minutos`, `antelacion_minima_minutos`, `antelacion_maxima_dias` |
| `miembros_empresa` | `MiembroEmpresa` | Pertenencia de un usuario a una empresa |
| `servicios` | `Servicio` | Tres columnas de política anulables: `null` = hereda de la empresa |
| `profesionales` | `Profesional` | Raíz del agregado |
| `horarios` | `Horario` | Hijo de `Profesional`. `Intervalo` en `hora_inicio` y `hora_fin` |
| `excepciones_horario` | `ExcepcionHorario` | Hijo de `Profesional`. Día completo |
| `profesional_servicio` | `Profesional.ServiciosQuePresta` | Tabla de unión. En el dominio es un conjunto de `Guid`, respaldado por el tipo interno `ProfesionalServicio` |
| `reservas` | `Reserva` | `Franja` en `inicio_utc` y `fin_utc`, que son columnas propias porque las necesita la restricción `EXCLUDE` |
| `asp_net_users` | `Usuario` | Identity, más `nombre_completo`, `telefono`, `fecha_alta` y `activo` |

`horarios.empresa_id` y `excepciones_horario.empresa_id` son el discriminador desnormalizado del profesional (`modelo-dominio.md` §2.5). **No son clave ajena**: sirven para filtrar sin *join*.

## 3. Índices

### 3.1. Los de SPEC §14

| Índice | Tipo | Para qué |
|---|---|---|
| `ix_empresas_slug` | único | URL pública `/businesses/{slug}` (RN-10) |
| `ix_categorias_nombre`, `ix_categorias_slug` | únicos | |
| `ix_miembros_empresa_empresa_id_usuario_id` | único | Una pertenencia por usuario y empresa (RN-06) |
| `ix_excepciones_horario_profesional_id_fecha` | único | Una excepción por profesional y fecha (RN-45) |
| `pk_profesional_servicio (profesional_id, servicio_id)` | clave primaria | Garantiza la unicidad del par (RN-32) sin índice adicional |
| `ix_empresas_ciudad_categoria_id` | | Consulta del buscador. `ciudad` lleva la colación `ciudad_sin_acentos` |
| `ix_empresas_activa` | | |
| `ix_servicios_empresa_id`, `ix_profesionales_empresa_id` | | Filtrado por empresa |
| `ix_horarios_profesional_id_dia_semana` | | Motor de disponibilidad |
| `ix_reservas_empresa_id_inicio_utc` | | Panel y listado de reservas de la empresa |
| `ix_reservas_usuario_id_inicio_utc` | | Área de cliente |

Los dos últimos están **escritos a mano en la migración**: EF Core no permite declarar índices sobre las columnas de un tipo complejo (`Franja`).

**No se crea** un B-tree sobre `(profesional_id, inicio_utc)`: esas consultas las sirve el índice GiST de la restricción `EXCLUDE` (SPEC §13).

### 3.2. Índices de clave ajena que añade EF Core

EF Core crea, por convención, un índice por cada clave ajena que no esté ya cubierta por la columna inicial de otro índice que EF conozca. PostgreSQL no indexa las claves ajenas por su cuenta. **Se aceptan** (decisión de la Fase 5):

| Índice | Nota |
|---|---|
| `ix_empresas_categoria_id` | |
| `ix_miembros_empresa_usuario_id` | |
| `ix_profesional_servicio_servicio_id` | |
| `ix_reservas_profesional_id` | Coexiste con el GiST del `EXCLUDE`, que EF no conoce |
| `ix_reservas_servicio_id` | |
| `ix_reservas_empresa_id` | **Redundante** con `ix_reservas_empresa_id_inicio_utc`, que empieza por la misma columna. EF no ve el índice manual |
| `ix_reservas_usuario_id` | **Redundante** con `ix_reservas_usuario_id_inicio_utc`, por el mismo motivo |

Los dos redundantes son el precio de declarar a mano los índices compuestos. Quitarlos obligaría a manipular el modelo de EF por convención. No afectan a la corrección, solo a un coste de escritura menor.

## 4. Restricciones `CHECK`

| Nombre | Condición | Regla |
|---|---|---|
| `ck_servicios_duracion` | `duracion_minutos > 0` | RN-20 |
| `ck_servicios_precio` | `precio >= 0` | RN-21 |
| `ck_reservas_franja` | `fin_utc > inicio_utc` | RN-50 |

Repiten invariantes que el dominio ya protege. Son la última defensa frente a una escritura que no pase por él.

## 5. La restricción `EXCLUDE`

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE reservas
ADD CONSTRAINT ex_reservas_sin_solape
EXCLUDE USING gist (
    profesional_id WITH =,
    tstzrange(inicio_utc, fin_utc, '[)') WITH &&
)
WHERE (estado <> 1);   -- 1 = Cancelada
```

Es **la garantía** de RN-77: nunca pueden existir dos reservas no canceladas del mismo profesional que se solapen.

- **Por qué en la base de datos y no en el código.** La invariante abarca a varias instancias del agregado `Reserva`, y ningún agregado puede protegerla por sí solo (`modelo-dominio.md` §2.4). Con `READ COMMITTED`, dos transacciones simultáneas superarían a la vez cualquier comprobación previa (SPEC §13). La restricción es declarativa: no hay forma de eludirla.
- **`'[)'`** reproduce literalmente la semántica semiabierta de `FranjaHoraria.SeSolapaCon` (RN-50, RN-51), así que aplicación y base de datos no pueden discrepar.
- **`WHERE (estado <> 1)`** hace que una reserva cancelada libere el hueco de inmediato (RN-97).
- **`btree_gist`** es necesaria para usar `=` sobre `profesional_id` dentro de un índice GiST.
- EF Core no la modela: está escrita a mano en la migración, y `Down()` la elimina antes que las tablas.

> **Nunca `EnsureCreated`.** No ejecuta el SQL manual de las migraciones. La restricción no existiría y la prueba de concurrencia pasaría sin probar nada (R-3). Las pruebas de integración aplican las migraciones, y `La_restriccion_de_no_solapamiento_existe_en_la_base_de_datos` lo comprueba en `pg_constraint`.

La violación (`SqlState 23P01`) se traduce a `HuecoYaNoDisponible` en la Fase 11.

## 6. Claves ajenas y borrado

| Clave ajena | Al borrar el principal |
|---|---|
| Entre agregados: `empresas → categorias`, `miembros_empresa → empresas / asp_net_users`, `servicios → empresas`, `profesionales → empresas`, `profesional_servicio → servicios` | `RESTRICT`: no hay borrado físico de entidades de negocio |
| Todas las de `reservas` (`empresas`, `asp_net_users`, `profesionales`, `servicios`) | `RESTRICT` (SPEC §14) |
| Dentro del agregado `Profesional`: `horarios`, `excepciones_horario`, `profesional_servicio → profesionales` | `CASCADE`: son hijos del agregado. `Horario` y `ExcepcionHorario` sí se borran físicamente (RN-121) |

## 7. Datos personales cifrados

| Columna | Mecanismo | Estado |
|---|---|---|
| `reservas.nombre_cliente`, `reservas.telefono_cliente` | Conversor de EF Core `ConversorCifrado`, sobre `IPersonalDataProtector` (RN-163) | Activo desde la Fase 6 |
| `asp_net_users`: correo, nombre de acceso, `nombre_completo`, `telefono` | `ProtectPersonalData` de Identity (RN-162) | Activo desde la Fase 6 |
| `asp_net_users`: `normalized_email`, `normalized_user_name` | `ILookupProtector`: HMAC-SHA256 determinista, para que `FindByEmailAsync` funcione (RN-166) | Activo desde la Fase 6 |

Las columnas cifradas son `text`, sin longitud máxima, porque el texto cifrado ocupa más que el original. Las longitudes de negocio (120 y 20) las garantizan `Reserva.Crear` y `Usuario.Crear`.

Los valores cifrados son AES-256-GCM (`ProtectorDatosPersonales`), en base64 de `nonce ‖ tag ‖ cifrado`, con un nonce aleatorio por valor. Las dos claves (`Cifrado:ClaveDatos` y `Cifrado:ClaveBusqueda`) salen de configuración, nunca del repositorio, y `AnilloClaves` exige que sean distintas y de 256 bits (RN-168, `arquitectura.md` §8.5).

**El protector es obligatorio.** `ContextoDatos.ObtenerProtector()` lanza si no hay un `IPersonalDataProtector` registrado: el mecanismo falla en cerrado y nunca guarda en claro sin avisar (R-11). La única excepción es el tiempo de diseño de `dotnet ef`, que construye el modelo sin protector; el esquema es idéntico.

Consecuencia (RN-167, R-10): una consulta LINQ sobre una columna protegida, como `Users.Where(u => u.Email == correo)`, **devuelve vacío sin error**. Se busca siempre con los métodos de `UserManager`.

Lo comprueba `PruebasIntegracion/Identidad/PruebasCifradoIdentidad.cs` con SQL crudo y los protectores reales con claves aleatorias: nada en claro en `asp_net_users` ni en `reservas`, `empresas` en claro, `FindByEmailAsync` e inicio de sesión funcionando, y el LINQ directo vacío. La prueba final con todo el sistema es de la Fase 17.

Los datos de `empresas` **no se cifran**: son públicos y `ciudad` tiene que poder buscarse (RN-164).

## 7 bis. Objetos de valor en columnas simples

`Email`, `Telefono`, `CodigoPostal`, `Precio`, `Duracion` y los ocho textos (`NombreCategoria`, `NombreEmpresa`, `DescripcionEmpresa`, `Direccion`, `Ciudad`, `NombreServicio`, `NombrePersona`, `Observaciones`) se guardan con un conversor de EF Core por tipo (`Persistencia/Conversor*.cs`) en la misma columna que tenía el primitivo: `text`/`varchar` para los tres primeros, `decimal(10,2)` para el precio e `integer` para la duración (`duracion_minutos`, `duracion_aplicada_minutos`). El esquema no cambió al introducirlos (2026-10-04). En `reservas.nombre_cliente` y `reservas.telefono_cliente`, el conversor del objeto de valor se encadena con el de cifrado.

> **Búsqueda por ciudad (resuelto en la Fase 13).** La columna `empresas.ciudad` usa la colación ICU no determinista `ciudad_sin_acentos` (`und-u-ks-level1`), creada a mano en la migración `CiudadSinAcentos`. La igualdad ignora mayúsculas y acentos, funciona en LINQ sobre la propiedad con conversor y mantiene el índice `(ciudad, categoria_id)`. La coincidencia es completa, no parcial: las colaciones no deterministas no admiten `LIKE`, y una búsqueda parcial haría que "Ea" encontrase "Leganés" (RN-110). *Fase 13, dudas 4 y 5.*

## 8. Orden de `Guid` v7 en PostgreSQL (R-5)

`PROJECT_PLAN.md` §6 pedía comprobar si la monotonía de `Guid.CreateVersion7()` se conserva tal y como Npgsql guarda el `uuid`, porque el orden de bytes de `System.Guid` difiere del de un `uuid` en los tres primeros grupos.

**Resultado (2026-09-30, PostgreSQL 16, Npgsql 10.0.3): sí se conserva.** Se generaron 20 identificadores separados por 2 ms, y `ORDER BY id` los devolvió exactamente en el orden de generación (`Los_guid_v7_se_ordenan_en_postgresql_en_el_orden_en_que_se_generaron`, en verde). Npgsql escribe el `uuid` en el orden de bytes estándar (RFC 9562), así que la marca de tiempo queda al principio y las inserciones caen al final del índice B-tree.

Desde la revisión de la Fase 5 las pruebas usan `postgres:latest` en lugar de `postgres:16-alpine`, así que esta prueba se repite con cada versión mayor nueva. *Pendiente de anotar aquí la primera ejecución con `latest`.*

Matiz que no invalida el resultado: dentro de un mismo milisegundo, `Guid.CreateVersion7()` de .NET no garantiza el orden, porque el resto de bits es aleatorio. El beneficio de localidad es por milisegundo, no absoluto.

## 9. Datos iniciales

Solo en `Development` (RN-123). Al arrancar, `MigrarYSembrarAsync` aplica las migraciones y siembra **solo las 8 categorías** de SPEC §9.2 (RN-122), de forma idempotente: compara por slug y no duplica. Las empresas y los usuarios de demostración quedan para cuando existan Identity con cifrado y los horarios.

## 10. Base de datos local

`docker compose up -d postgres` (imagen `postgres:latest`), con las credenciales en `.env`, que crea el desarrollador sin plantilla versionada, y la cadena de conexión en User Secrets. Procedimiento completo en `arquitectura.md` §11.4.

Las pruebas de integración **no** usan esta base: levantan su propio PostgreSQL con Testcontainers.
