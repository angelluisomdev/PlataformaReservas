namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record PanelEmpresaDto(
    bool Activa,
    bool TieneServicioActivo,
    bool TieneProfesionalActivo,
    bool TieneAsociacion,
    bool EsPublicable,
    IReadOnlyList<ReservaEmpresaDto> Hoy,
    IReadOnlyList<ReservaEmpresaDto> Proximas)
{
    public static PanelEmpresaDto ConvertirADto(
        bool activa,
        bool tieneServicioActivo,
        bool tieneProfesionalActivo,
        bool tieneAsociacion,
        bool esPublicable,
        IEnumerable<ReservaEmpresaDto> hoy,
        IEnumerable<ReservaEmpresaDto> proximas)
    {
        return new PanelEmpresaDto(
            activa,
            tieneServicioActivo,
            tieneProfesionalActivo,
            tieneAsociacion,
            esPublicable,
            hoy.OrderBy(r => r.InicioUtc).ToList(),
            proximas.OrderBy(r => r.InicioUtc).ToList());
    }
}
