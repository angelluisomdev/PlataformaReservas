namespace PlataformaReservas.Dominio.Compartido;

// Estado imposible: una invariante que el codigo nunca deberia haber intentado romper.
// Los desenlaces de negocio previstos no usan esta excepcion, sino Resultado (arquitectura.md §6).
public sealed class DominioException : Exception
{
    public DominioException(string mensaje)
        : base(mensaje)
    {
    }
}
