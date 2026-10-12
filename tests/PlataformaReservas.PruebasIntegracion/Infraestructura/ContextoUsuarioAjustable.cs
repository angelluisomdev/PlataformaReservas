using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class ContextoUsuarioAjustable : IContextoUsuario
{
    public Guid? UsuarioId { get; set; }

    public Guid UsuarioActualId => UsuarioId ?? throw new InvalidOperationException("La prueba no ha fijado el usuario.");

    public bool TieneUsuario => UsuarioId is not null;
}
