using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Notificaciones;

public sealed record DatosEmpresaNotificacionDto(
    Guid EmpresaId,
    string Nombre,
    string Slug,
    string EmailPropietario,
    string NombrePropietario,
    bool TieneServicioActivo,
    bool TieneProfesionalActivo,
    bool TieneAsociacion,
    bool EsPublicable,
    int ReservasCanceladas)
{
    public static DatosEmpresaNotificacionDto ConvertirADto(
        Guid empresaId,
        NombreEmpresa nombre,
        Slug slug,
        Email emailPropietario,
        NombrePersona nombrePropietario,
        bool tieneServicioActivo,
        bool tieneProfesionalActivo,
        bool tieneAsociacion,
        bool esPublicable,
        int reservasCanceladas)
    {
        return new DatosEmpresaNotificacionDto(
            empresaId,
            nombre.Valor,
            slug.Valor,
            emailPropietario.Valor,
            nombrePropietario.Valor,
            tieneServicioActivo,
            tieneProfesionalActivo,
            tieneAsociacion,
            esPublicable,
            reservasCanceladas);
    }
}
