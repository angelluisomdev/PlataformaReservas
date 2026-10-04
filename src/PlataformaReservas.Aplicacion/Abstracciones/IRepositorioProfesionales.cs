using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IRepositorioProfesionales
{
    Task<Profesional?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Profesional?> ObtenerParaModificarAsync(Guid id, CancellationToken ct);
}
