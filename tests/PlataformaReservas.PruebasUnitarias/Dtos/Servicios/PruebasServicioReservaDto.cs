using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Servicios;

public sealed class PruebasServicioReservaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_los_datos_la_ventana_recibida_y_solo_los_profesionales_que_lo_prestan_ordenados()
    {
        Guid empresaId = Guid.CreateVersion7();
        Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresaId, new NombreServicio("Corte"), new Duracion(30), new Precio(12.5m), Ahora);
        Profesional zoe = QuePresta("Zoe Ruiz", empresaId, servicio.Id);
        Profesional angel = QuePresta("Ángel Lopez", empresaId, servicio.Id);
        Profesional noLoPresta = Profesional.Crear(Guid.CreateVersion7(), empresaId, new NombrePersona("Bea Diaz"), Ahora);

        ServicioReservaDto dto = ServicioReservaDto.ConvertirADto(servicio, new PoliticasReserva(15, 60, 21), [zoe, noLoPresta, angel]);

        dto.Id.Should().Be(servicio.Id);
        dto.Nombre.Should().Be("Corte");
        dto.DuracionMinutos.Should().Be(30);
        dto.Precio.Should().Be(12.5m);
        dto.AntelacionMaximaDias.Should().Be(21);
        dto.Profesionales.Select(p => p.Nombre).Should().Equal("Ángel Lopez", "Zoe Ruiz");
    }

    private static Profesional QuePresta(string nombre, Guid empresaId, Guid servicioId)
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresaId, new NombrePersona(nombre), Ahora);
        profesional.AsociarServicio(servicioId);
        return profesional;
    }
}
