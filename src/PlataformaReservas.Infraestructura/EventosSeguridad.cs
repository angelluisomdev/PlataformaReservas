using Microsoft.Extensions.Logging;

namespace PlataformaReservas.Infraestructura;

internal static partial class EventosSeguridad
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Acceso rechazado entre empresas: la sesion de {EmpresaSesion} intento operar sobre {EmpresaRecurso}")]
    public static partial void AccesoEntreEmpresas(ILogger registro, Guid empresaSesion, Guid empresaRecurso);
}
