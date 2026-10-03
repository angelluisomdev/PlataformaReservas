namespace PlataformaReservas.Dominio.Compartido;

public sealed class DominioException : Exception
{
    public DominioException(string mensaje)
        : base(mensaje)
    {
    }
}
