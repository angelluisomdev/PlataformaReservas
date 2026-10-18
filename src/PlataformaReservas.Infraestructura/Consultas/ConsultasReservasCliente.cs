using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasReservasCliente(ContextoDatos db, IContextoUsuario contexto) : IConsultasReservasCliente
{
    public async Task<MisReservasDto> ObtenerMisReservasAsync(DateTime ahoraUtc, CancellationToken ct)
    {
        List<FilaReserva> filas = await Propias().ToListAsync(ct);
        return MisReservasDto.ConvertirADto(
            filas.Select(f => ReservaClienteDto.ConvertirADto(
                f.Id, f.Empresa.Valor, f.Servicio.Valor, f.Profesional.Valor, new FranjaHoraria(f.InicioUtc, f.FinUtc), f.Estado,
                ahoraUtc)),
            ahoraUtc);
    }

    public async Task<DetalleReservaClienteDto?> ObtenerDetalleAsync(Guid reservaId, DateTime ahoraUtc, CancellationToken ct)
    {
        FilaReserva? fila = await Propias(reservaId).FirstOrDefaultAsync(ct);
        return fila is null
            ? null
            : DetalleReservaClienteDto.ConvertirADto(
                fila.Id, fila.Empresa.Valor, fila.Servicio.Valor, fila.Profesional.Valor, new FranjaHoraria(fila.InicioUtc, fila.FinUtc),
                fila.Duracion, fila.Precio, fila.Observaciones, fila.Estado, ahoraUtc);
    }

    private IQueryable<FilaReserva> Propias(Guid? reservaId = null)
    {
        Guid usuarioId = contexto.UsuarioActualId;
        return from r in db.Reservas
               where r.UsuarioId == usuarioId && (reservaId == null || r.Id == reservaId)
               join e in db.Empresas on r.EmpresaId equals e.Id
               join s in db.Servicios on r.ServicioId equals s.Id
               join p in db.Profesionales on r.ProfesionalId equals p.Id
               select new FilaReserva(
                   r.Id, e.Nombre, s.Nombre, p.NombreCompleto, r.Franja.InicioUtc, r.Franja.FinUtc,
                   r.DuracionAplicada, r.PrecioAplicado, r.Observaciones, r.Estado);
    }

    private sealed record FilaReserva(
        Guid Id,
        NombreEmpresa Empresa,
        NombreServicio Servicio,
        NombrePersona Profesional,
        DateTime InicioUtc,
        DateTime FinUtc,
        Duracion Duracion,
        Precio Precio,
        Observaciones? Observaciones,
        EstadoReserva Estado);
}
