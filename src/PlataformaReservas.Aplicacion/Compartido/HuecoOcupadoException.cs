namespace PlataformaReservas.Aplicacion.Compartido;

public sealed class HuecoOcupadoException : Exception
{
    public HuecoOcupadoException(Exception interna)
        : base("El hueco ya esta ocupado por otra reserva.", interna)
    {
    }
}
