using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasNotificacion(ContextoDatos db, UserManager<Usuario> usuarios) : IConsultasNotificacion
{
    public async Task<DatosReservaNotificacionDto?> ObtenerDatosReservaAsync(Guid reservaId, CancellationToken ct)
    {
        FilaReserva? fila = await (
            from r in db.Reservas
            where r.Id == reservaId
            join e in db.Empresas on r.EmpresaId equals e.Id
            join s in db.Servicios on r.ServicioId equals s.Id
            join p in db.Profesionales on r.ProfesionalId equals p.Id
            select new FilaReserva(
                r.Id, r.UsuarioId, e.Nombre, e.Direccion, e.CodigoPostal, e.Ciudad, e.Email, s.Nombre, p.NombreCompleto, r.Franja,
                r.DuracionAplicada, r.PrecioAplicado, r.NombreCliente, r.TelefonoCliente))
            .FirstOrDefaultAsync(ct);
        if (fila is null)
        {
            return null;
        }

        Usuario? cliente = await usuarios.FindByIdAsync(fila.UsuarioId.ToString());
        if (cliente?.Email is null)
        {
            return null;
        }

        return DatosReservaNotificacionDto.ConvertirADto(
            fila.Id, fila.Establecimiento, fila.Direccion, fila.CodigoPostal, fila.Ciudad, fila.EmailEstablecimiento, fila.Servicio,
            fila.Profesional, fila.Franja, fila.Duracion, fila.Precio, fila.NombreCliente, fila.TelefonoCliente, new Email(cliente.Email));
    }

    public async Task<DatosEmpresaNotificacionDto?> ObtenerDatosEmpresaAsync(Guid empresaId, DateTime instanteEvento, CancellationToken ct)
    {
        var empresa = await db.Empresas
            .Where(e => e.Id == empresaId)
            .Select(e => new { e.Nombre, e.Slug })
            .FirstOrDefaultAsync(ct);
        Guid? propietarioId = await db.MiembrosEmpresa
            .Where(m => m.EmpresaId == empresaId && m.Rol == RolMiembro.Propietario)
            .Select(m => (Guid?)m.UsuarioId)
            .FirstOrDefaultAsync(ct);
        if (empresa is null || propietarioId is null)
        {
            return null;
        }

        Usuario? propietario = await usuarios.FindByIdAsync(propietarioId.Value.ToString());
        if (propietario?.Email is null)
        {
            return null;
        }

        CondicionPublicable.Requisitos requisitos = await CondicionPublicable.RequisitosAsync(db, empresaId, ct);
        int canceladas = await db.Reservas.CountAsync(
            r => r.EmpresaId == empresaId && r.Estado == EstadoReserva.Cancelada && r.FechaCancelacion == instanteEvento, ct);

        return DatosEmpresaNotificacionDto.ConvertirADto(
            empresaId, empresa.Nombre, empresa.Slug, new Email(propietario.Email), new NombrePersona(propietario.NombreCompleto),
            requisitos.TieneServicioActivo, requisitos.TieneProfesionalActivo, requisitos.TieneAsociacion, requisitos.EsPublicable,
            canceladas);
    }

    public async Task<DatosCuentaNotificacionDto?> ObtenerDatosCuentaAsync(Guid usuarioId, DateTime instanteEvento, CancellationToken ct)
    {
        Usuario? usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario?.Email is null)
        {
            return null;
        }

        int canceladas = await db.Reservas.CountAsync(
            r => r.UsuarioId == usuarioId && r.Estado == EstadoReserva.Cancelada && r.FechaCancelacion == instanteEvento, ct);

        return DatosCuentaNotificacionDto.ConvertirADto(
            usuarioId, new Email(usuario.Email), new NombrePersona(usuario.NombreCompleto), canceladas);
    }

    private sealed record FilaReserva(
        Guid Id,
        Guid UsuarioId,
        NombreEmpresa Establecimiento,
        Direccion Direccion,
        CodigoPostal CodigoPostal,
        Ciudad Ciudad,
        Email EmailEstablecimiento,
        NombreServicio Servicio,
        NombrePersona Profesional,
        FranjaHoraria Franja,
        Duracion Duracion,
        Precio Precio,
        NombrePersona NombreCliente,
        Telefono TelefonoCliente);
}
