namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IContextoUsuario
{
    Guid UsuarioActualId { get; }

    bool TieneUsuario { get; }
}
