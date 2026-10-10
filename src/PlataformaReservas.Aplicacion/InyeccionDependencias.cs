using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.CasosUso.Profesionales;
using PlataformaReservas.Aplicacion.CasosUso.Servicios;

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

        return servicios;
    }
}
