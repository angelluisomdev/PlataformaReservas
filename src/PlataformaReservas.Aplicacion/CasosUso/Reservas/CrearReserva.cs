using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class CrearReserva(
    IContextoUsuario contexto,
    IConsultasDisponibilidad consultas,
    IMotorDisponibilidad motor,
    IServicioCuentas cuentas,
    IRepositorioReservasCliente reservas,
    IUnidadTrabajo unidadTrabajo,
    IPublicadorEventos publicador,
    IRelojSistema reloj)
{
    public async Task<Resultado<Guid>> EjecutarAsync(CrearReservaComando comando, CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado<Guid>.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para reservar.");
        }

        EmpresaDisponibilidadDto? empresa = await consultas.ObtenerEmpresaAsync(comando.EmpresaId, ct);
        if (empresa is null || !empresa.Activa)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, "El establecimiento no admite reservas.");
        }

        ServicioDisponibilidadDto? servicio = await consultas.ObtenerServicioAsync(comando.EmpresaId, comando.ServicioId, ct);
        if (servicio is null || !servicio.Activo)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, "El servicio no esta disponible.");
        }

        IReadOnlyList<ProfesionalDisponibilidadDto> profesionales = await consultas.ObtenerProfesionalesQuePrestanAsync(
            comando.EmpresaId, comando.ServicioId, comando.ProfesionalId, ct);
        if (profesionales.Count == 0)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, "El profesional no presta ese servicio.");
        }

        Observaciones? observaciones;
        try
        {
            observaciones = string.IsNullOrWhiteSpace(comando.Observaciones) ? null : new Observaciones(comando.Observaciones);
        }
        catch (DominioException error)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        Guid usuarioId = contexto.UsuarioActualId;
        (NombrePersona Nombre, Telefono? Telefono)? contacto = await cuentas.ObtenerContactoAsync(usuarioId, ct);
        if (contacto?.Telefono is not Telefono telefono)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, "Hace falta un telefono de contacto para reservar.");
        }

        DateTime ahora = reloj.AhoraUtc;
        await using ITransaccion transaccion = await unidadTrabajo.IniciarTransaccionAsync(ct);

        IReadOnlyList<HuecoDisponible> huecos = await motor.ObtenerHuecosAsync(
            comando.EmpresaId, comando.ServicioId, comando.ProfesionalId, comando.Fecha, ct);
        HuecoDisponible? hueco = huecos.FirstOrDefault(
            h => h.ProfesionalId == comando.ProfesionalId && h.InicioUtc == comando.InicioUtc);
        if (hueco is null)
        {
            return Resultado<Guid>.Fallo(CodigoError.HuecoYaNoDisponible, "Esa hora ya no esta disponible.");
        }

        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), comando.EmpresaId, usuarioId, comando.ProfesionalId, comando.ServicioId,
            new Duracion(servicio.DuracionMinutos), new Precio(servicio.Precio), hueco.InicioUtc,
            contacto.Value.Nombre, telefono, observaciones, ahora);
        reservas.Agregar(reserva);

        try
        {
            await unidadTrabajo.GuardarCambiosAsync(ct);
        }
        catch (HuecoOcupadoException)
        {
            return Resultado<Guid>.Fallo(CodigoError.HuecoYaNoDisponible, "Esa hora ya no esta disponible.");
        }

        await transaccion.ConfirmarAsync(ct);

        await publicador.PublicarAsync(reserva.Eventos, ct);
        reserva.LimpiarEventos();
        return Resultado<Guid>.Exito(reserva.Id);
    }
}
