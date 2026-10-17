namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record PaginaResultados<T>(IReadOnlyList<T> Elementos, int Total, int Pagina, int TamanoPagina);
