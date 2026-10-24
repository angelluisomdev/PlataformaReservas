using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Cuenta;

[Collection("BaseDatos")]
public sealed class PruebasEstablecerTelefono(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Guarda_el_telefono_en_el_perfil_del_usuario_de_la_sesion()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());

        Resultado resultado = await EstablecerAsync(usuario.Id, "  +34 644-555-666 ");

        resultado.EsExito.Should().BeTrue();
        (await TelefonoAsync(usuario.Id)).Should().Be(new Telefono("+34 644-555-666"));
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        Resultado resultado = await EstablecerAsync(null, "644555666");

        resultado.Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("seis cuatro cuatro")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    public async Task Un_telefono_invalido_da_validacion_y_el_perfil_sigue_sin_telefono(string? telefono)
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());

        Resultado resultado = await EstablecerAsync(usuario.Id, telefono);

        resultado.Codigo.Should().Be(CodigoError.Validacion);
        (await TelefonoAsync(usuario.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Si_ya_tiene_telefono_da_conflicto_sin_llegar_a_escribir()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), telefono: "611222333");
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuario.Id);
        CuentasQueCuentanEscrituras cuentas = new(ambito.ServiceProvider.GetRequiredService<IServicioCuentas>());

        Resultado resultado = await new EstablecerTelefono(ambito.ServiceProvider.GetRequiredService<IContextoUsuario>(), cuentas)
            .EjecutarAsync(new EstablecerTelefonoComando("644555666"), CancellationToken.None);

        resultado.Codigo.Should().Be(CodigoError.Conflicto);
        cuentas.Escrituras.Should().Be(0);
        (await TelefonoAsync(usuario.Id)).Should().Be(new Telefono("611222333"));
    }

    [Fact]
    public async Task Si_otra_peticion_guarda_el_telefono_entre_la_lectura_y_la_escritura_da_conflicto()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuario.Id);
        IServicioCuentas real = ambito.ServiceProvider.GetRequiredService<IServicioCuentas>();
        CuentasQueCuentanEscrituras cuentas = new(real)
        {
            AntesDeEscribir = () => real.EstablecerTelefonoAsync(usuario.Id, new Telefono("611222333"), CancellationToken.None),
        };

        Resultado resultado = await new EstablecerTelefono(ambito.ServiceProvider.GetRequiredService<IContextoUsuario>(), cuentas)
            .EjecutarAsync(new EstablecerTelefonoComando("644555666"), CancellationToken.None);

        resultado.Codigo.Should().Be(CodigoError.Conflicto);
        (await TelefonoAsync(usuario.Id)).Should().Be(new Telefono("611222333"));
    }

    [Fact]
    public async Task Si_el_usuario_de_la_sesion_no_existe_lanza_porque_es_un_estado_imposible()
    {
        Func<Task> establecer = () => EstablecerAsync(Guid.CreateVersion7(), "644555666");

        await establecer.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Tras_establecerlo_la_reserva_copia_ese_telefono()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos, telefono: null);

        await EstablecerAsync(escenario.Cliente.Id, "644555666");
        Resultado<Guid> reserva;
        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id))
        {
            reserva = await ambito.ServiceProvider.GetRequiredService<CrearReserva>()
                .EjecutarAsync(escenario.Comando(), CancellationToken.None);
        }

        reserva.EsExito.Should().BeTrue();
        Reserva guardada = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == reserva.Valor));
        guardada.TelefonoCliente.Should().Be(new Telefono("644555666"));
    }

    private async Task<Resultado> EstablecerAsync(Guid? usuarioId, string? telefono)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<EstablecerTelefono>()
            .EjecutarAsync(new EstablecerTelefonoComando(telefono), CancellationToken.None);
    }

    private sealed class CuentasQueCuentanEscrituras(IServicioCuentas real) : IServicioCuentas
    {
        public int Escrituras { get; private set; }

        public Func<Task>? AntesDeEscribir { get; init; }

        public Task<Resultado<Guid>> CrearPropietarioAsync(
            Email email, NombrePersona nombreCompleto, Telefono? telefono, string clave, DateTime ahoraUtc, CancellationToken ct)
            => real.CrearPropietarioAsync(email, nombreCompleto, telefono, clave, ahoraUtc, ct);

        public Task RenovarSelloMiembrosAsync(Guid empresaId, CancellationToken ct) => real.RenovarSelloMiembrosAsync(empresaId, ct);

        public Task<(NombrePersona Nombre, Telefono? Telefono)?> ObtenerContactoAsync(Guid usuarioId, CancellationToken ct)
            => real.ObtenerContactoAsync(usuarioId, ct);

        public async Task EstablecerTelefonoAsync(Guid usuarioId, Telefono telefono, CancellationToken ct)
        {
            Escrituras++;
            if (AntesDeEscribir is not null)
            {
                await AntesDeEscribir();
            }

            await real.EstablecerTelefonoAsync(usuarioId, telefono, ct);
        }

        public Task ActualizarPerfilAsync(Guid usuarioId, NombrePersona nombre, Telefono? telefono, CancellationToken ct)
            => real.ActualizarPerfilAsync(usuarioId, nombre, telefono, ct);

        public Task<bool> EstaActivaAsync(Guid usuarioId, CancellationToken ct) => real.EstaActivaAsync(usuarioId, ct);

        public Task DarDeBajaAsync(Guid usuarioId, CancellationToken ct) => real.DarDeBajaAsync(usuarioId, ct);
    }

    private async Task<Telefono?> TelefonoAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return (await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
            .ObtenerContactoAsync(usuarioId, CancellationToken.None))!.Value.Telefono;
    }
}
