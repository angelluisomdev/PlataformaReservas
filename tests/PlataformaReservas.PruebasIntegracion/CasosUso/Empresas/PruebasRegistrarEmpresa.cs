using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasRegistrarEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Crea_usuario_con_rol_empresa_con_politicas_por_defecto_y_pertenencia()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string correo = CorreoUnico();

        Resultado<EmpresaRegistradaDto> resultado = await RegistrarAsync(Comando(correo, categoria.Id, $"Barberia {DominioPrueba.Sufijo()}"));

        resultado.EsExito.Should().BeTrue();
        EmpresaRegistradaDto alta = resultado.Valor!;
        Empresa empresa = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == alta.EmpresaId));
        MiembroEmpresa miembro = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.MiembrosEmpresa.SingleAsync(m => m.UsuarioId == alta.UsuarioId));
        empresa.Activa.Should().BeTrue();
        empresa.Politicas.Should().Be(new PoliticasReserva(15, 60, 60));
        empresa.Slug.Valor.Should().Be(alta.Slug);
        miembro.EmpresaId.Should().Be(empresa.Id);
        (await EsPropietarioAsync(alta.UsuarioId)).Should().BeTrue();
    }

    [Fact]
    public async Task Dos_empresas_con_el_mismo_nombre_obtienen_slugs_distintos()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string nombre = $"Barberia {DominioPrueba.Sufijo()}";

        Resultado<EmpresaRegistradaDto> primera = await RegistrarAsync(Comando(CorreoUnico(), categoria.Id, nombre));
        Resultado<EmpresaRegistradaDto> segunda = await RegistrarAsync(Comando(CorreoUnico(), categoria.Id, nombre));

        primera.Valor!.Slug.Should().Be(Slug.Desde(nombre).Valor);
        segunda.Valor!.Slug.Should().Be(Slug.Desde(nombre).ConSufijo(2).Valor);
    }

    [Fact]
    public async Task Un_correo_ya_registrado_da_conflicto_y_no_deja_empresa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);
        string nombre = $"Barberia {DominioPrueba.Sufijo()}";

        Resultado<EmpresaRegistradaDto> resultado = await RegistrarAsync(Comando(correo, categoria.Id, nombre));

        resultado.Codigo.Should().Be(CodigoError.Conflicto);
        Slug slug = Slug.Desde(nombre);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.AnyAsync(e => e.Slug == slug))).Should().BeFalse();
    }

    [Fact]
    public async Task Una_clave_invalida_no_deja_ni_usuario_ni_empresa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string correo = CorreoUnico();
        string nombre = $"Barberia {DominioPrueba.Sufijo()}";

        Resultado<EmpresaRegistradaDto> resultado = await RegistrarAsync(Comando(correo, categoria.Id, nombre) with { Clave = "corta" });

        resultado.Codigo.Should().Be(CodigoError.Validacion);
        Slug slug = Slug.Desde(nombre);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.AnyAsync(e => e.Slug == slug))).Should().BeFalse();
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        (await ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByEmailAsync(correo)).Should().BeNull();
    }

    [Fact]
    public async Task Un_fallo_al_guardar_la_empresa_deshace_la_cuenta_creada()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string nombre = $"Barberia {DominioPrueba.Sufijo()}";
        await RegistrarAsync(Comando(CorreoUnico(), categoria.Id, nombre));
        string correo = CorreoUnico();

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IServiceProvider servicios = ambito.ServiceProvider;
        RegistrarEmpresa registrar = new(
            servicios.GetRequiredService<IServicioCuentas>(),
            new EmpresasSinComprobarSlug(servicios.GetRequiredService<IRepositorioEmpresas>()),
            servicios.GetRequiredService<IRepositorioMiembros>(),
            servicios.GetRequiredService<IConsultasCatalogoPublico>(),
            servicios.GetRequiredService<IUnidadTrabajo>(),
            servicios.GetRequiredService<IRelojSistema>());
        Func<Task> registrarConSlugRepetido = () => registrar.EjecutarAsync(Comando(correo, categoria.Id, nombre), CancellationToken.None);

        await registrarConSlugRepetido.Should().ThrowAsync<DbUpdateException>();
        await using AsyncServiceScope lectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        (await lectura.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByEmailAsync(correo)).Should().BeNull();
    }

    [Fact]
    public async Task Una_categoria_inexistente_o_un_dato_mal_formado_dan_validacion()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string nombre = $"Barberia {DominioPrueba.Sufijo()}";

        Resultado<EmpresaRegistradaDto> sinCategoria = await RegistrarAsync(Comando(CorreoUnico(), Guid.CreateVersion7(), nombre));
        Resultado<EmpresaRegistradaDto> malCodigo = await RegistrarAsync(
            Comando(CorreoUnico(), categoria.Id, nombre) with { CodigoPostal = "123" });

        sinCategoria.Codigo.Should().Be(CodigoError.Validacion);
        malCodigo.Codigo.Should().Be(CodigoError.Validacion);
    }

    private static RegistrarEmpresaComando Comando(string correo, Guid categoriaId, string nombre)
    {
        return new RegistrarEmpresaComando(
            "Ana Garcia", correo, null, Clave, nombre, categoriaId, null,
            "Calle Mayor 1", "Salamanca", "37001", "600000000", "contacto@barberia.es");
    }

    private async Task<Resultado<EmpresaRegistradaDto>> RegistrarAsync(RegistrarEmpresaComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<RegistrarEmpresa>().EjecutarAsync(comando, CancellationToken.None);
    }

    private async Task<bool> EsPropietarioAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return await usuarios.IsInRoleAsync((await usuarios.FindByIdAsync(usuarioId.ToString()))!, RolesIdentidad.Propietario);
    }

    // Simula dos altas simultaneas con el mismo nombre: ninguna ve el slug de la otra antes de guardar.
    private sealed class EmpresasSinComprobarSlug(IRepositorioEmpresas real) : IRepositorioEmpresas
    {
        public Task<Empresa?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => real.ObtenerPorIdAsync(id, ct);

        public Task<Empresa?> ObtenerParaModificarAsync(Guid id, CancellationToken ct) => real.ObtenerParaModificarAsync(id, ct);

        public Task<bool> ExisteSlugAsync(Slug slug, CancellationToken ct) => Task.FromResult(false);

        public void Agregar(Empresa empresa) => real.Agregar(empresa);
    }
}
