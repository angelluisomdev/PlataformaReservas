using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionReserva(BaseDatosFixture baseDatos)
{
    private const string Nombre = "Lucia Fernandez";
    private const string Telefono = "699887766";

    [Fact]
    public async Task Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro()
    {
        Usuario usuario = await CrearAsync(baseDatos, $"{Guid.CreateVersion7():N}@prueba.es", Nombre, Telefono);
        string sufijo = Guid.CreateVersion7().ToString("N");

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), new NombreCategoria($"Categoria {sufijo}"), Slug.Desde($"cat-{sufijo}"));
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Barberia Centro"), Slug.Desde($"empresa-{sufijo}"), categoria.Id,
            null, new Email("contacto@barberia.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"), new CodigoPostal("28001"), new Ciudad("Salamanca"), Ahora);
        Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Corte"), new Duracion(30), new Precio(15m), Ahora);
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona("Ana"), Ahora);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio,
            Ahora.AddDays(1), new NombrePersona(Nombre), new Telefono(Telefono), null, Ahora);

        db.AddRange(categoria, empresa, servicio, profesional, reserva);
        await db.SaveChangesAsync();

        List<string> copias = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre_cliente, telefono_cliente]) AS "Value"
            FROM reservas WHERE id = {reserva.Id}
            """).ToListAsync();
        List<string> datosEmpresa = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre, ciudad]) AS "Value"
            FROM empresas WHERE id = {empresa.Id}
            """).ToListAsync();

        copias.Should().HaveCount(2);
        copias.Should().NotContain(valor => valor.Contains("Lucia") || valor.Contains(Telefono));
        datosEmpresa.Should().BeEquivalentTo("Barberia Centro", "Salamanca");
    }

    [Fact]
    public async Task Una_reserva_guardada_se_relee_descifrada_y_con_sus_copias()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio, Ahora.AddDays(2),
            new NombrePersona(Nombre), new Telefono(Telefono), new Observaciones("Primera visita"), Ahora);

        await using (AsyncServiceScope escritura = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            ContextoDatos db = escritura.ServiceProvider.GetRequiredService<ContextoDatos>();
            db.AddRange(categoria, empresa, servicio, profesional, reserva);
            await db.SaveChangesAsync();
        }

        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Reserva leida = await lectura.ServiceProvider.GetRequiredService<ContextoDatos>()
            .Reservas.SingleAsync(r => r.Id == reserva.Id);

        leida.NombreCliente.Valor.Should().Be(Nombre);
        leida.TelefonoCliente.Valor.Should().Be(Telefono);
        leida.Observaciones.Should().Be(new Observaciones("Primera visita"));
        leida.PrecioAplicado.Importe.Should().Be(18.50m);
        leida.DuracionAplicada.Minutos.Should().Be(45);
        leida.Franja.Should().Be(reserva.Franja);
        leida.Estado.Should().Be(reserva.Estado);
    }
}
