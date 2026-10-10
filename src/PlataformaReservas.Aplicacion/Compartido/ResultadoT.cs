namespace PlataformaReservas.Aplicacion.Compartido;

public sealed class Resultado<T>
{
    private Resultado(bool esExito, T? valor, CodigoError? codigo, string? mensaje)
    {
        EsExito = esExito;
        Valor = valor;
        Codigo = codigo;
        Mensaje = mensaje;
    }

    public bool EsExito { get; }

    public T? Valor { get; }

    public CodigoError? Codigo { get; }

    public string? Mensaje { get; }

    public static Resultado<T> Exito(T valor) => new(true, valor, null, null);

    public static Resultado<T> Fallo(CodigoError codigo, string mensaje) => new(false, default, codigo, mensaje);
}
