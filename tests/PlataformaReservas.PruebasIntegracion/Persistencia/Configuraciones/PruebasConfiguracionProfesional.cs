using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionProfesional(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_profesional_guardado_se_relee_con_su_nombre_y_sin_contacto()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);

        await using (AsyncServiceScope escritura = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            ContextoDatos db = escritura.ServiceProvider.GetRequiredService<ContextoDatos>();
            db.AddRange(categoria, empresa, profesional);
            await db.SaveChangesAsync();
        }

        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Profesional leido = await lectura.ServiceProvider.GetRequiredService<ContextoDatos>()
            .Profesionales.SingleAsync(p => p.Id == profesional.Id);

        leido.NombreCompleto.Should().Be(profesional.NombreCompleto);
        leido.Email.Should().BeNull();
        leido.Telefono.Should().BeNull();
        leido.Activo.Should().BeTrue();
    }
}
