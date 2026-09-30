using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasEsquema(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    // Control 3, tercera linea: se comprueba en el catalogo, no fiandose de que la migracion se aplico (R-3).
    [Fact]
    public async Task La_restriccion_de_no_solapamiento_existe_en_la_base_de_datos()
    {
        await using ContextoDatos db = baseDatos.CrearContexto();

        long total = await EscalarAsync(db,
            "SELECT count(*) FROM pg_constraint WHERE conname = 'ex_reservas_sin_solape' AND contype = 'x'");

        total.Should().Be(1);
    }

    // PROJECT_PLAN.md §6 y R-5: el orden de Guid v7 tal como lo guarda Npgsql en un uuid de PostgreSQL.
    [Fact]
    public async Task Los_guid_v7_se_ordenan_en_postgresql_en_el_orden_en_que_se_generaron()
    {
        List<Guid> generados = [];
        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            for (int i = 0; i < 20; i++)
            {
                Guid id = Guid.CreateVersion7();
                generados.Add(id);
                db.Categorias.Add(Categoria.Crear(id, $"Orden {i}", Slug.Desde($"orden-guid-{i}")));
                await Task.Delay(2);   // marcas de tiempo distintas: v7 solo ordena entre milisegundos
            }

            await db.SaveChangesAsync();
        }

        await using ContextoDatos lectura = baseDatos.CrearContexto();
        List<Guid> ordenadosPorPostgres = await lectura.Categorias
            .Where(c => generados.Contains(c.Id))
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync();

        ordenadosPorPostgres.Should().Equal(generados);
    }

    // RN-163 con el doble de pruebas: la columna guarda lo que devuelve el protector y la lectura
    // recupera el original. La prueba con el cifrado real y SQL crudo es de la Fase 17.
    [Fact]
    public async Task Nombre_y_telefono_de_la_reserva_pasan_por_el_protector()
    {
        Guid usuarioId = Guid.CreateVersion7();
        Guid reservaId = Guid.CreateVersion7();
        int llamadasAntes = baseDatos.Protector.Llamadas;

        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), "Cifrado", Slug.Desde("prueba-cifrado"));
            Empresa empresa = Empresa.Crear(
                Guid.CreateVersion7(), "Peluqueria Prueba", Slug.Desde("peluqueria-prueba"), categoria.Id,
                null, "contacto@prueba.es", "600000000", "Calle Mayor 1", "28001", "Madrid", Ahora);
            Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, "Corte", 30, 15m, Ahora);
            Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, "Ana", Ahora);

            db.AddRange(categoria, empresa, servicio, profesional);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO asp_net_users (id, nombre_completo, fecha_alta, activo, email_confirmed,
                    phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
                VALUES ({usuarioId}, 'Cliente de prueba', {Ahora}, true, false, false, false, true, 0)");

            Reserva reserva = Reserva.Crear(
                reservaId, empresa.Id, usuarioId, profesional.Id, servicio,
                Ahora.AddDays(1), "Lucia Perez", "611222333", null, Ahora);
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
        }

        await using ContextoDatos lectura = baseDatos.CrearContexto();
        string? nombreEnColumna = await EscalarTextoAsync(lectura,
            $"SELECT nombre_cliente FROM reservas WHERE id = '{reservaId}'");
        Reserva leida = await lectura.Reservas.SingleAsync(r => r.Id == reservaId);

        nombreEnColumna.Should().StartWith(ProtectorFalso.Prefijo).And.NotContain("Lucia");
        leida.NombreCliente.Should().Be("Lucia Perez");
        leida.TelefonoCliente.Should().Be("611222333");
        baseDatos.Protector.Llamadas.Should().BeGreaterThan(llamadasAntes);
    }

    private static async Task<long> EscalarAsync(ContextoDatos db, string sql)
    {
        object? valor = await EjecutarEscalarAsync(db, sql);
        return Convert.ToInt64(valor, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<string?> EscalarTextoAsync(ContextoDatos db, string sql)
    {
        return await EjecutarEscalarAsync(db, sql) as string;
    }

    private static async Task<object?> EjecutarEscalarAsync(ContextoDatos db, string sql)
    {
        DbConnection conexion = db.Database.GetDbConnection();
        await conexion.OpenAsync();
        await using DbCommand comando = conexion.CreateCommand();
        comando.CommandText = sql;
        return await comando.ExecuteScalarAsync();
    }
}
