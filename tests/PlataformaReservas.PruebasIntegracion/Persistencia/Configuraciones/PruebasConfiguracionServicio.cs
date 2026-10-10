using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_servicio_guardado_se_relee_con_nombre_duracion_y_precio_exactos()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        servicio.ActualizarDatos(servicio.Nombre, new DescripcionServicio("Con lavado"), servicio.Duracion, servicio.Precio, DominioPrueba.Ahora);

        await using (AsyncServiceScope escritura = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            ContextoDatos db = escritura.ServiceProvider.GetRequiredService<ContextoDatos>();
            db.AddRange(categoria, empresa, servicio);
            await db.SaveChangesAsync();
        }

        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Servicio leido = await lectura.ServiceProvider.GetRequiredService<ContextoDatos>()
            .Servicios.SingleAsync(s => s.Id == servicio.Id);

        leido.Nombre.Should().Be(servicio.Nombre);
        leido.Duracion.Should().Be(servicio.Duracion);
        leido.Precio.Importe.Should().Be(18.50m);
        leido.Descripcion.Should().Be(new DescripcionServicio("Con lavado"));
        leido.Activo.Should().BeTrue();
    }
}
