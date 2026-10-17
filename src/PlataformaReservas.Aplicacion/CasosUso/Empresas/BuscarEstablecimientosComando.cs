namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed record BuscarEstablecimientosComando(string? Ciudad, string? Categoria, int? Pagina, int? TamanoPagina);
