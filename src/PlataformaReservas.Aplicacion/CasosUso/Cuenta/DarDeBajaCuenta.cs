using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Cuenta;

public sealed class DarDeBajaCuenta(
    IContextoUsuario contexto,
    IServicioCuentas cuentas,
    IRepositorioMiembros miembros,
    IConsultasDisponibilidad consultas,
    IRepositorioReservasCliente reservas,
    IUnidadTrabajo unidadTrabajo,
    IPublicadorEventos publicador,
    IRelojSistema reloj)
{
    public async Task<Resultado<int>> EjecutarAsync(CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado<int>.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para dar de baja la cuenta.");
        }

        Guid usuarioId = contexto.UsuarioActualId;
        MiembroEmpresa? miembro = await miembros.ObtenerPorUsuarioAsync(usuarioId, ct);
        if (miembro is not null)
        {
            EmpresaDisponibilidadDto? empresa = await consultas.ObtenerEmpresaAsync(miembro.EmpresaId, ct);
            if (empresa?.Activa == true)
            {
                return Resultado<int>.Fallo(
                    CodigoError.Conflicto, "Tienes una empresa activa. Dala de baja antes en la configuracion de tu empresa.");
            }
        }

        if (!await cuentas.EstaActivaAsync(usuarioId, ct))
        {
            return Resultado<int>.Fallo(CodigoError.EstadoNoValido, "La cuenta ya esta dada de baja.");
        }

        DateTime ahora = reloj.AhoraUtc;
        await using ITransaccion transaccion = await unidadTrabajo.IniciarTransaccionAsync(ct);

        IReadOnlyList<Reserva> futuras = await reservas.ObtenerFuturasConfirmadasParaModificarAsync(ahora, ct);
        foreach (Reserva reserva in futuras)
        {
            reserva.CancelarPorCliente(ahora, OrigenCancelacion.BajaCuenta);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);
        await cuentas.DarDeBajaAsync(usuarioId, ct);
        await transaccion.ConfirmarAsync(ct);

        await publicador.PublicarAsync([new CuentaDadaDeBaja(usuarioId, ahora), .. futuras.SelectMany(r => r.Eventos)], ct);
        foreach (Reserva reserva in futuras)
        {
            reserva.LimpiarEventos();
        }

        return Resultado<int>.Exito(futuras.Count);
    }
}
