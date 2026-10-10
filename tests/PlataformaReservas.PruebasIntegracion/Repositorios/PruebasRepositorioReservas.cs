using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Repositorios;

[Collection("BaseDatos")]
public sealed class PruebasRepositorioReservas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Las_futuras_confirmadas_excluyen_pasadas_canceladas_y_ajenas()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(propia.Id);
        Profesional profesional = DominioPrueba.Profesional(propia.Id);
        Servicio servicioAjeno = DominioPrueba.Servicio(ajena.Id);
        Profesional profesionalAjeno = DominioPrueba.Profesional(ajena.Id);

        Reserva futura = Nueva(propia.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1));
        Reserva pasada = Nueva(propia.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-1));
        Reserva cancelada = Nueva(propia.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(2));
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora);
        Reserva deOtra = Nueva(ajena.Id, usuario.Id, profesionalAjeno.Id, servicioAjeno, DominioPrueba.Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, servicio, profesional, servicioAjeno, profesionalAjeno, futura, pasada, cancelada, deOtra);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IRepositorioReservas repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioReservas>();

        IReadOnlyList<Reserva> futuras = await repositorio.ObtenerFuturasConfirmadasParaModificarAsync(
            DominioPrueba.Ahora, CancellationToken.None);
        int recuento = await repositorio.ContarFuturasConfirmadasAsync(DominioPrueba.Ahora, CancellationToken.None);

        futuras.Should().ContainSingle().Which.Id.Should().Be(futura.Id);
        futuras[0].Estado.Should().Be(EstadoReserva.Confirmada);
        recuento.Should().Be(1);
    }

    private static Reserva Nueva(Guid empresaId, Guid usuarioId, Guid profesionalId, Servicio servicio, DateTime inicioUtc)
    {
        return Reserva.Crear(
            Guid.CreateVersion7(), empresaId, usuarioId, profesionalId, servicio, inicioUtc,
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
    }
}
