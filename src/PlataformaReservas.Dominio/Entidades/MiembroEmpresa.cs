using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.Dominio.Entidades;

// Vincula un usuario con una empresa. Unico por (EmpresaId, UsuarioId), garantizado por indice (RN-06).
public sealed class MiembroEmpresa : EntidadBase
{
    private MiembroEmpresa()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid UsuarioId { get; private set; }

    public RolMiembro Rol { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public static MiembroEmpresa Crear(Guid id, Guid empresaId, Guid usuarioId, RolMiembro rol, DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "La pertenencia");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa de la pertenencia");
        Validacion.IdentificadorObligatorio(usuarioId, "El usuario de la pertenencia");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la pertenencia");

        return new MiembroEmpresa
        {
            Id = id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Rol = rol,
            FechaAlta = ahoraUtc,
        };
    }
}
