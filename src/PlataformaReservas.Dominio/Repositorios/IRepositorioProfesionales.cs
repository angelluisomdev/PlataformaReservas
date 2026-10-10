using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.Repositorios;

public interface IRepositorioProfesionales
{
    Task<Profesional?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Profesional?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Profesional>> ListarAsync(CancellationToken ct);

    void Agregar(Profesional profesional);
}
