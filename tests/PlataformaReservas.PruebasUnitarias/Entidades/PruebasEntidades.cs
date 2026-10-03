using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasEntidades
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid EmpresaId = Guid.CreateVersion7();

    private static Servicio ServicioDe(Guid empresaId)
    {
        return Servicio.Crear(Guid.CreateVersion7(), empresaId, "Corte", 45, 18.50m, Ahora);
    }

    private static Reserva CrearReserva(
        Servicio servicio,
        string nombreCliente = "Lucia Perez",
        string telefonoCliente = "611222333",
        string? observaciones = null)
    {
        return Reserva.Crear(
            Guid.CreateVersion7(), EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), servicio,
            Ahora.AddDays(1), nombreCliente, telefonoCliente, observaciones, Ahora);
    }

    private static Empresa CrearEmpresa(
        string? descripcion = null,
        string email = "contacto@prueba.es",
        string telefono = "600000000",
        string direccion = "Calle Mayor 1",
        string codigoPostal = "28001",
        string ciudad = "Madrid")
    {
        return Empresa.Crear(
            Guid.CreateVersion7(), "Peluqueria Prueba", Slug.Desde("peluqueria-prueba"), Guid.CreateVersion7(),
            descripcion, email, telefono, direccion, codigoPostal, ciudad, Ahora);
    }

    [Fact]
    public void Reserva_nace_confirmada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));

        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.FechaCancelacion.Should().BeNull();
    }

    [Fact]
    public void Reserva_calcula_el_fin_y_copia_precio_y_duracion_del_servicio()
    {
        Servicio servicio = ServicioDe(EmpresaId);

        Reserva reserva = CrearReserva(servicio);

        reserva.Franja.FinUtc.Should().Be(reserva.Franja.InicioUtc.AddMinutes(45));
        reserva.DuracionAplicadaMinutos.Should().Be(45);
        reserva.PrecioAplicado.Should().Be(18.50m);
        reserva.ServicioId.Should().Be(servicio.Id);
    }

    [Theory]
    [InlineData("", "611222333")]
    [InlineData("   ", "611222333")]
    [InlineData("Lucia Perez", "")]
    [InlineData("Lucia Perez", "   ")]
    public void Reserva_rechaza_nombre_o_telefono_vacios(string nombre, string telefono)
    {
        Action crear = () => CrearReserva(ServicioDe(EmpresaId), nombre, telefono);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Reserva_rechaza_un_servicio_de_otra_empresa()
    {
        Action crear = () => CrearReserva(ServicioDe(Guid.CreateVersion7()));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Reserva_rechaza_observaciones_de_mas_de_500_caracteres()
    {
        Action crear = () => CrearReserva(ServicioDe(EmpresaId), observaciones: new string('x', 501));

        crear.Should().Throw<DominioException>();
    }

    [Theory]
    [InlineData("email", "")]
    [InlineData("email", 161)]
    [InlineData("telefono", "")]
    [InlineData("telefono", 21)]
    [InlineData("direccion", "")]
    [InlineData("direccion", 201)]
    [InlineData("codigoPostal", "")]
    [InlineData("codigoPostal", 11)]
    [InlineData("ciudad", "")]
    [InlineData("ciudad", 81)]
    public void Empresa_rechaza_datos_de_contacto_vacios_o_demasiado_largos(string campo, object valor)
    {
        string texto = valor is int longitud ? new string('x', longitud) : (string)valor;

        Action crear = campo switch
        {
            "email" => () => CrearEmpresa(email: texto),
            "telefono" => () => CrearEmpresa(telefono: texto),
            "direccion" => () => CrearEmpresa(direccion: texto),
            "codigoPostal" => () => CrearEmpresa(codigoPostal: texto),
            _ => () => CrearEmpresa(ciudad: texto),
        };

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Empresa_admite_descripcion_nula_y_rechaza_la_de_mas_de_1000_caracteres()
    {
        CrearEmpresa(descripcion: null).Descripcion.Should().BeNull();

        Action crear = () => CrearEmpresa(descripcion: new string('x', 1001));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Categoria_rechaza_un_slug_de_mas_de_80_caracteres()
    {
        Slug largo = Slug.Desde(new string('a', 81));

        Action crear = () => Categoria.Crear(Guid.CreateVersion7(), "Categoria", largo);

        crear.Should().Throw<DominioException>();
    }
}
