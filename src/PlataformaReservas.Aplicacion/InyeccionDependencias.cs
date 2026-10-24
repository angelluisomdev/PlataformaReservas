using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.CasosUso.Profesionales;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.CasosUso.Servicios;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Aplicacion;

public static class InyeccionDependencias
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<RegistrarEmpresa>();
        servicios.AddScoped<ObtenerConfiguracionEmpresa>();
        servicios.AddScoped<ActualizarEmpresa>();
        servicios.AddScoped<DarDeBajaEmpresa>();
        servicios.AddScoped<ReactivarEmpresa>();
        servicios.AddScoped<BuscarEstablecimientos>();

        servicios.AddScoped<ListarServicios>();
        servicios.AddScoped<CrearServicio>();
        servicios.AddScoped<ActualizarServicio>();
        servicios.AddScoped<DesactivarServicio>();

        servicios.AddScoped<ListarProfesionales>();
        servicios.AddScoped<CrearProfesional>();
        servicios.AddScoped<ActualizarProfesional>();
        servicios.AddScoped<DesactivarProfesional>();
        servicios.AddScoped<AsociarServicio>();
        servicios.AddScoped<DesasociarServicio>();

        servicios.AddScoped<ObtenerHorarioProfesional>();
        servicios.AddScoped<AgregarIntervalo>();
        servicios.AddScoped<EliminarIntervalo>();
        servicios.AddScoped<MarcarNoDisponible>();
        servicios.AddScoped<QuitarExcepcion>();

        servicios.AddScoped<CrearReserva>();
        servicios.AddScoped<CancelarReservaCliente>();
        servicios.AddScoped<CancelarReservaEmpresa>();
        servicios.AddScoped<CompletarReserva>();
        servicios.AddScoped<ConsultarDisponibilidad>();
        servicios.AddScoped<ConsultarMisReservas>();
        servicios.AddScoped<ConsultarDetalleReserva>();
        servicios.AddScoped<ConsultarReservasEmpresa>();
        servicios.AddScoped<ConsultarPanelEmpresa>();
        servicios.AddScoped<IManejadorEvento<ReservaConfirmada>, EnviarCorreosReservaConfirmada>();
        servicios.AddScoped<IManejadorEvento<ReservaCancelada>, EnviarCorreoReservaCancelada>();
        servicios.AddScoped<IManejadorEvento<EmpresaRegistrada>, EnviarCorreoEmpresaRegistrada>();
        servicios.AddScoped<IManejadorEvento<EmpresaDadaDeBaja>, EnviarCorreoEmpresaDadaDeBaja>();
        servicios.AddScoped<IManejadorEvento<CuentaRegistrada>, EnviarCorreoCuentaRegistrada>();
        servicios.AddScoped<IManejadorEvento<CuentaDadaDeBaja>, EnviarCorreoCuentaDadaDeBaja>();

        servicios.AddScoped<EstablecerTelefono>();
        servicios.AddScoped<ConsultarPerfil>();
        servicios.AddScoped<ActualizarPerfil>();
        servicios.AddScoped<DarDeBajaCuenta>();

        servicios.AddScoped<IMotorDisponibilidad, MotorDisponibilidad>();

        return servicios;
    }
}
