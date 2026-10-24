using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlataformaReservas.Aplicacion;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Correo;
using PlataformaReservas.Infraestructura.Eventos;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion;

[Collection("BaseDatos")]
public sealed class PruebasInyeccionDependencias(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task EsPropietario_exige_el_rol_y_el_claim_de_empresa()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        IAuthorizationService autorizacion = servicios.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal sinEmpresa = Principal(new Claim(ClaimTypes.Role, RolesIdentidad.Propietario));
        ClaimsPrincipal conEmpresa = Principal(
            new Claim(ClaimTypes.Role, RolesIdentidad.Propietario),
            new Claim(FabricaClaimsUsuario.ClaimEmpresa, Guid.CreateVersion7().ToString()));
        ClaimsPrincipal cliente = Principal(new Claim(ClaimTypes.Role, RolesIdentidad.Cliente));

        (await autorizacion.AuthorizeAsync(sinEmpresa, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(conEmpresa, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, RolesIdentidad.PoliticaCliente)).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task La_matriz_de_politicas_completa_separa_anonimo_cliente_y_propietario()
    {
        IAuthorizationService autorizacion = baseDatos.ServiciosIdentidad.GetRequiredService<IAuthorizationService>();
        string empresa = Guid.CreateVersion7().ToString();
        ClaimsPrincipal anonimo = new(new ClaimsIdentity());
        ClaimsPrincipal clienteConClaimDeEmpresa = Principal(
            new Claim(ClaimTypes.Role, RolesIdentidad.Cliente), new Claim(FabricaClaimsUsuario.ClaimEmpresa, empresa));
        ClaimsPrincipal propietario = Principal(
            new Claim(ClaimTypes.Role, RolesIdentidad.Propietario),
            new Claim(ClaimTypes.Role, RolesIdentidad.Cliente),
            new Claim(FabricaClaimsUsuario.ClaimEmpresa, empresa));
        ClaimsPrincipal propietarioSinRolCliente = Principal(
            new Claim(ClaimTypes.Role, RolesIdentidad.Propietario), new Claim(FabricaClaimsUsuario.ClaimEmpresa, empresa));

        (await autorizacion.AuthorizeAsync(anonimo, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(anonimo, RolesIdentidad.PoliticaCliente)).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(clienteConClaimDeEmpresa, RolesIdentidad.PoliticaPropietario)).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(propietario, RolesIdentidad.PoliticaCliente)).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(propietarioSinRolCliente, RolesIdentidad.PoliticaCliente)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public void La_cookie_revalida_el_sello_cada_cinco_minutos()
    {
        SecurityStampValidatorOptions opciones = baseDatos.ServiciosIdentidad
            .GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value;

        opciones.ValidationInterval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Sin_claves_configuradas_la_comprobacion_de_arranque_falla()
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlataformaReservas"] = "Host=localhost",
            })
            .Build();
        ServiceCollection servicios = new();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        using ServiceProvider proveedor = servicios.BuildServiceProvider();

        Action comprobar = proveedor.ComprobarClavesCifrado;

        comprobar.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_contexto_de_empresa_y_los_repositorios_son_scoped()
    {
        Type[] tipos =
        [
            typeof(IContextoEmpresa), typeof(IRepositorioEmpresas), typeof(IRepositorioServicios),
            typeof(IRepositorioProfesionales), typeof(IRepositorioReservas), typeof(IRepositorioMiembros),
            typeof(IConsultasReservasEmpresa),
        ];
        ServiceCollection servicios = new();
        servicios.AgregarInfraestructura(new ConfigurationBuilder().Build());

        servicios.Where(d => tipos.Contains(d.ServiceType))
            .Should().HaveCount(tipos.Length)
            .And.OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Los_casos_de_uso_del_panel_y_del_listado_de_la_empresa_son_scoped()
    {
        ServiceCollection servicios = new();
        servicios.AgregarAplicacion();

        servicios.Where(d => d.ServiceType == typeof(ConsultarReservasEmpresa) || d.ServiceType == typeof(ConsultarPanelEmpresa))
            .Should().HaveCount(2)
            .And.OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task El_motor_de_disponibilidad_y_su_puerto_de_lectura_se_resuelven_como_scoped()
    {
        ServiceCollection servicios = new();
        servicios.AgregarInfraestructura(new ConfigurationBuilder().Build());
        servicios.AgregarAplicacion();

        servicios.Where(d => d.ServiceType == typeof(IMotorDisponibilidad) || d.ServiceType == typeof(IConsultasDisponibilidad))
            .Should().HaveCount(2)
            .And.OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<IMotorDisponibilidad>().Should().BeOfType<MotorDisponibilidad>();
    }

    [Fact]
    public async Task El_eje_cliente_y_los_casos_de_uso_de_reservas_se_resuelven_como_scoped()
    {
        Type[] tipos =
        [
            typeof(IContextoUsuario), typeof(IRepositorioReservasCliente), typeof(CrearReserva),
            typeof(CancelarReservaCliente), typeof(CancelarReservaEmpresa), typeof(CompletarReserva),
            typeof(IConsultasReservaPublica), typeof(IConsultasReservasCliente), typeof(ConsultarDisponibilidad),
            typeof(EstablecerTelefono),
        ];
        ServiceCollection servicios = new();
        servicios.AgregarInfraestructura(new ConfigurationBuilder().Build());
        servicios.AgregarAplicacion();

        servicios.Where(d => tipos.Contains(d.ServiceType))
            .Should().HaveCount(tipos.Length)
            .And.OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        foreach (Type tipo in tipos)
        {
            ambito.ServiceProvider.GetRequiredService(tipo).Should().NotBeNull();
        }
    }

    [Fact]
    public void La_cola_de_eventos_es_un_singleton_compartido_con_el_publicador_y_el_procesador_es_un_servicio_alojado()
    {
        ServiceCollection servicios = new();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(new ConfigurationBuilder().Build());
        using ServiceProvider proveedor = servicios.BuildServiceProvider();

        servicios.Should().ContainSingle(d => d.ServiceType == typeof(ColaEventos))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        servicios.Should().ContainSingle(d => d.ServiceType == typeof(IPublicadorEventos))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        proveedor.GetRequiredService<IPublicadorEventos>().Should().BeSameAs(proveedor.GetRequiredService<ColaEventos>());
        servicios.Should().Contain(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(ProcesadorEventos));
    }

    [Theory]
    [InlineData(null, typeof(ServicioEmailRegistro))]
    [InlineData("false", typeof(ServicioEmailRegistro))]
    [InlineData("true", typeof(ServicioEmailSmtp))]
    public void El_servicio_de_correo_solo_envia_de_verdad_si_esta_habilitado(string? habilitado, Type esperado)
    {
        ServiceCollection servicios = new();
        servicios.AgregarInfraestructura(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:Habilitado"] = habilitado })
            .Build());

        ServiceDescriptor correo = servicios.Should().ContainSingle(d => d.ServiceType == typeof(IServicioEmail)).Subject;
        correo.ImplementationType.Should().Be(esperado);
        correo.Lifetime.Should().Be(ServiceLifetime.Scoped);
        servicios.Should().ContainSingle(d => d.ServiceType == typeof(IRenderizadorPlantillas))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        servicios.Should().ContainSingle(d => d.ServiceType == typeof(IConsultasNotificacion))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        ServiceDescriptor remitente = servicios.Should().ContainSingle(d => d.ServiceType == typeof(IEmailSender<Usuario>)).Subject;
        remitente.ImplementationType.Should().Be<RemitenteIdentity>();
        remitente.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Cada_evento_de_dominio_tiene_su_manejador_de_correo_scoped()
    {
        ServiceCollection servicios = new();
        servicios.AgregarAplicacion();

        Type[] eventos =
        [
            typeof(ReservaConfirmada), typeof(ReservaCancelada), typeof(EmpresaRegistrada), typeof(EmpresaDadaDeBaja),
            typeof(CuentaRegistrada), typeof(CuentaDadaDeBaja),
        ];

        eventos.Should().AllSatisfy(evento => servicios
            .Should().ContainSingle(d => d.ServiceType == typeof(IManejadorEvento<>).MakeGenericType(evento))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void La_configuracion_del_repositorio_no_habilita_el_envio_de_correo(string fichero)
    {
        string ruta = Path.Combine(RaizRepositorio(), "src", "PlataformaReservas.Web", fichero);

        IConfiguration configuracion = new ConfigurationBuilder().AddJsonFile(ruta, optional: false).Build();

        configuracion.GetSection("Correo").Exists().Should().BeTrue();
        configuracion.GetValue<bool>("Correo:Habilitado").Should().BeFalse();
        configuracion["Correo:Contrasena"].Should().BeNullOrEmpty();
    }

    private static string RaizRepositorio()
    {
        DirectoryInfo? directorio = new(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Directory.Packages.props")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encuentra la raiz del repositorio.");
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
    }
}
