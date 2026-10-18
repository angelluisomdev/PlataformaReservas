namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record MisReservasDto(
    IReadOnlyList<ReservaClienteDto> Proximas, IReadOnlyList<ReservaClienteDto> Historicas, int Cancelables)
{
    public static MisReservasDto ConvertirADto(IEnumerable<ReservaClienteDto> reservas, DateTime ahoraUtc)
    {
        List<ReservaClienteDto> lista = reservas.ToList();
        return new MisReservasDto(
            lista.Where(r => r.InicioUtc >= ahoraUtc).OrderBy(r => r.InicioUtc).ToList(),
            lista.Where(r => r.InicioUtc < ahoraUtc).OrderByDescending(r => r.InicioUtc).ToList(),
            lista.Count(r => r.Cancelable));
    }
}
