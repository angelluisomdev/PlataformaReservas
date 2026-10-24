namespace PlataformaReservas.Infraestructura.Correo;

public sealed class CorreoNoEnviadoException : Exception
{
    public CorreoNoEnviadoException(string tipoFallo)
        : base($"No se pudo enviar el correo ({tipoFallo}).")
    {
    }
}
