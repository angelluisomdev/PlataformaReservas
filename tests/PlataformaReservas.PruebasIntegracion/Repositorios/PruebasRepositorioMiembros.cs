using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Repositorios;

[Collection("BaseDatos")]
public sealed class PruebasRepositorioMiembros(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Agregar_guarda_la_pertenencia_y_se_encuentra_por_usuario()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        MiembroEmpresa miembro = MiembroEmpresa.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, RolMiembro.Propietario, DominioPrueba.Ahora);

        await using (AsyncServiceScope escritura = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            escritura.ServiceProvider.GetRequiredService<IRepositorioMiembros>().Agregar(miembro);
            await escritura.ServiceProvider.GetRequiredService<ContextoDatos>().SaveChangesAsync();
        }

        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        MiembroEmpresa? encontrado = await lectura.ServiceProvider.GetRequiredService<IRepositorioMiembros>()
            .ObtenerPorUsuarioAsync(usuario.Id, CancellationToken.None);

        encontrado!.EmpresaId.Should().Be(empresa.Id);
    }
}
