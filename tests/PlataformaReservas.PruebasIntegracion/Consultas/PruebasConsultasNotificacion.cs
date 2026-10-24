using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasNotificacion(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = DominioPrueba.Ahora;
    private static readonly DateTime InstanteBaja = Ahora.AddMinutes(5);

    [Fact]
    public async Task Los_datos_de_la_reserva_llevan_establecimiento_servicio_profesional_y_cliente_descifrado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        DatosReservaNotificacionDto datos = (await ConsultarAsync(c => c.ObtenerDatosReservaAsync(reserva.Id, CancellationToken.None)))!;

        datos.Should().Be(new DatosReservaNotificacionDto(
            reserva.Id,
            escenario.Empresa.Nombre.Valor,
            $"{escenario.Empresa.Direccion.Valor}, {escenario.Empresa.CodigoPostal.Valor} {escenario.Empresa.Ciudad.Valor}",
            escenario.Empresa.Email.Valor,
            escenario.Servicio.Nombre.Valor,
            escenario.Profesional.NombreCompleto.Valor,
            reserva.Franja.InicioUtc,
            45,
            escenario.Servicio.Precio.Importe,
            "Lucia Fernandez",
            EscenarioReserva.TelefonoCliente,
            escenario.Cliente.Email!));
    }

    [Fact]
    public async Task Cada_reserva_devuelve_sus_propios_datos_sin_mezclar_otra()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva propia = escenario.Reserva(Ahora.AddDays(1));
        Reserva ajena = otra.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, propia, ajena);

        DatosReservaNotificacionDto datos = (await ConsultarAsync(c => c.ObtenerDatosReservaAsync(propia.Id, CancellationToken.None)))!;

        datos.ReservaId.Should().Be(propia.Id);
        datos.EmailCliente.Should().Be(escenario.Cliente.Email);
        datos.EmailCliente.Should().NotBe(otra.Cliente.Email);
    }

    [Fact]
    public async Task Una_reserva_inexistente_no_tiene_datos()
    {
        (await ConsultarAsync(c => c.ObtenerDatosReservaAsync(Guid.CreateVersion7(), CancellationToken.None))).Should().BeNull();
    }

    [Fact]
    public async Task Los_datos_de_la_empresa_llevan_el_propietario_los_requisitos_y_las_canceladas_en_el_instante_del_evento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario propietario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Ana Propietaria");
        MiembroEmpresa miembro = MiembroEmpresa.Crear(Guid.CreateVersion7(), escenario.Empresa.Id, propietario.Id, RolMiembro.Propietario, Ahora);
        Reserva[] enLaBaja = [escenario.Reserva(Ahora.AddDays(1)), escenario.Reserva(Ahora.AddDays(2))];
        Reserva enOtroInstante = escenario.Reserva(Ahora.AddDays(3));
        Reserva deOtraEmpresa = otra.Reserva(Ahora.AddDays(1));
        foreach (Reserva reserva in enLaBaja)
        {
            reserva.CancelarPorEmpresa(InstanteBaja, OrigenCancelacion.BajaEmpresa);
        }

        enOtroInstante.CancelarPorEmpresa(InstanteBaja.AddSeconds(1), OrigenCancelacion.Empresa);
        deOtraEmpresa.CancelarPorEmpresa(InstanteBaja, OrigenCancelacion.BajaEmpresa);
        await EscenarioEmpresa.GuardarAsync(baseDatos, [miembro, .. enLaBaja, enOtroInstante, deOtraEmpresa]);

        DatosEmpresaNotificacionDto datos = (await ConsultarAsync(
            c => c.ObtenerDatosEmpresaAsync(escenario.Empresa.Id, InstanteBaja, CancellationToken.None)))!;

        datos.Should().Be(new DatosEmpresaNotificacionDto(
            escenario.Empresa.Id, escenario.Empresa.Nombre.Valor, escenario.Empresa.Slug.Valor, propietario.Email!, "Ana Propietaria",
            true, true, true, true, 2));
    }

    [Fact]
    public async Task Una_empresa_sin_profesional_indica_los_pasos_que_faltan()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario propietario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(
            baseDatos,
            DominioPrueba.Servicio(empresa.Id),
            MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, propietario.Id, RolMiembro.Propietario, Ahora));

        DatosEmpresaNotificacionDto datos = (await ConsultarAsync(c => c.ObtenerDatosEmpresaAsync(empresa.Id, Ahora, CancellationToken.None)))!;

        datos.TieneServicioActivo.Should().BeTrue();
        datos.TieneProfesionalActivo.Should().BeFalse();
        datos.TieneAsociacion.Should().BeFalse();
        datos.EsPublicable.Should().BeFalse();
        datos.ReservasCanceladas.Should().Be(0);
    }

    [Fact]
    public async Task Una_empresa_inexistente_o_sin_propietario_no_tiene_datos()
    {
        Empresa sinPropietario = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        (await ConsultarAsync(c => c.ObtenerDatosEmpresaAsync(Guid.CreateVersion7(), Ahora, CancellationToken.None))).Should().BeNull();
        (await ConsultarAsync(c => c.ObtenerDatosEmpresaAsync(sinPropietario.Id, Ahora, CancellationToken.None))).Should().BeNull();
    }

    [Fact]
    public async Task Los_datos_de_una_cuenta_dada_de_baja_llevan_correo_nombre_y_sus_canceladas_en_el_instante_del_evento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva suya = escenario.Reserva(Ahora.AddDays(1));
        Reserva deOtro = escenario.Reserva(Ahora.AddDays(2), otro.Id);
        suya.CancelarPorCliente(InstanteBaja, OrigenCancelacion.BajaCuenta);
        deOtro.CancelarPorCliente(InstanteBaja, OrigenCancelacion.BajaCuenta);
        await EscenarioEmpresa.GuardarAsync(baseDatos, suya, deOtro);
        await UsuariosPrueba.DesactivarAsync(baseDatos, escenario.Cliente.Id);

        DatosCuentaNotificacionDto datos = (await ConsultarAsync(
            c => c.ObtenerDatosCuentaAsync(escenario.Cliente.Id, InstanteBaja, CancellationToken.None)))!;

        datos.Should().Be(new DatosCuentaNotificacionDto(escenario.Cliente.Id, escenario.Cliente.Email!, "Lucia Fernandez", 1));
    }

    [Fact]
    public async Task Una_cuenta_inexistente_no_tiene_datos()
    {
        (await ConsultarAsync(c => c.ObtenerDatosCuentaAsync(Guid.CreateVersion7(), Ahora, CancellationToken.None))).Should().BeNull();
    }

    private async Task<T> ConsultarAsync<T>(Func<IConsultasNotificacion, Task<T>> consulta)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await consulta(ambito.ServiceProvider.GetRequiredService<IConsultasNotificacion>());
    }
}
