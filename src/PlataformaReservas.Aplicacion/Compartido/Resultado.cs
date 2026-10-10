namespace PlataformaReservas.Aplicacion.Compartido;

public sealed class Resultado
{
    private Resultado(bool esExito, CodigoError? codigo, string? mensaje)
    {
        EsExito = esExito;
        Codigo = codigo;
        Mensaje = mensaje;
    }

    public bool EsExito { get; }

    public CodigoError? Codigo { get; }

    public string? Mensaje { get; }

    public static Resultado Exito() => new(true, null, null);

    public static Resultado Fallo(CodigoError codigo, string mensaje) => new(false, codigo, mensaje);
}
