using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class DarDeBajaEmpresa(
    IRepositorioEmpresas empresas,
    IRepositorioReservas reservas,
    IServicioCuentas cuentas,
    IContextoEmpresa contexto,
    IUnidadTrabajo unidadTrabajo,
    IPublicadorEventos publicador,
    IRelojSistema reloj)
{
    public async Task<Resultado<int>> EjecutarAsync(CancellationToken ct)
    {
        Empresa? empresa = await empresas.ObtenerParaModificarAsync(contexto.EmpresaActualId, ct);
        if (empresa is null)
        {
            return Resultado<int>.Fallo(CodigoError.NoEncontrado, "No se encuentra la empresa.");
        }

        if (!empresa.Activa)
        {
            return Resultado<int>.Fallo(CodigoError.EstadoNoValido, "La empresa ya esta dada de baja.");
        }

        DateTime ahora = reloj.AhoraUtc;
        await using ITransaccion transaccion = await unidadTrabajo.IniciarTransaccionAsync(ct);

        empresa.DarDeBaja(ahora);
        IReadOnlyList<Reserva> futuras = await reservas.ObtenerFuturasConfirmadasParaModificarAsync(ahora, ct);
        foreach (Reserva reserva in futuras)
        {
            reserva.CancelarPorEmpresa(ahora, OrigenCancelacion.BajaEmpresa);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);
        await cuentas.RenovarSelloMiembrosAsync(empresa.Id, ct);
        await transaccion.ConfirmarAsync(ct);

        await publicador.PublicarAsync([.. empresa.Eventos, .. futuras.SelectMany(r => r.Eventos)], ct);
        empresa.LimpiarEventos();
        foreach (Reserva reserva in futuras)
        {
            reserva.LimpiarEventos();
        }

        return Resultado<int>.Exito(futuras.Count);
    }
}
