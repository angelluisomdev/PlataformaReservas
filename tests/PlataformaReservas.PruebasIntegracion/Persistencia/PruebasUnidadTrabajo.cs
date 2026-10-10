using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasUnidadTrabajo(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Una_transaccion_sin_confirmar_no_deja_nada()
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            IUnidadTrabajo unidad = ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>();
            await using ITransaccion transaccion = await unidad.IniciarTransaccionAsync(CancellationToken.None);
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Add(categoria);
            await unidad.GuardarCambiosAsync(CancellationToken.None);
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Categorias.AnyAsync(c => c.Id == categoria.Id))).Should().BeFalse();
    }

    [Fact]
    public async Task Una_transaccion_confirmada_guarda_los_cambios()
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            IUnidadTrabajo unidad = ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>();
            await using ITransaccion transaccion = await unidad.IniciarTransaccionAsync(CancellationToken.None);
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Add(categoria);
            await unidad.GuardarCambiosAsync(CancellationToken.None);
            await transaccion.ConfirmarAsync(CancellationToken.None);
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Categorias.AnyAsync(c => c.Id == categoria.Id))).Should().BeTrue();
    }
}
