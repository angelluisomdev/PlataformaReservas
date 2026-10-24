using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasCifradoEnReposo(BaseDatosFixture baseDatos)
{
    private const string NombreCliente = "Lucia Fernandez";
    private const string TelefonoCliente = "611222333";
    private const string NombrePropietario = "Ana Propietaria";
    private const string TelefonoPropietario = "622333444";

    [Fact]
    public async Task Ni_asp_net_users_ni_reservas_guardan_en_claro_nombres_correos_ni_telefonos()
    {
        Sistema sistema = await CrearSistemaAsync();

        List<string?> usuarios = await ConsultarAsync(db => db.Database.SqlQuery<string?>($"""
            SELECT unnest(ARRAY[email, normalized_email, user_name, normalized_user_name, nombre_completo, telefono]) AS "Value"
            FROM asp_net_users WHERE id IN ({sistema.ClienteId}, {sistema.PropietarioId})
            """).ToListAsync());
        List<string?> reservas = await ConsultarAsync(db => db.Database.SqlQuery<string?>($"""
            SELECT unnest(ARRAY[nombre_cliente, telefono_cliente]) AS "Value" FROM reservas WHERE id = {sistema.ReservaId}
            """).ToListAsync());

        usuarios.Should().HaveCount(12).And.OnlyContain(v => !string.IsNullOrEmpty(v));
        reservas.Should().HaveCount(2).And.OnlyContain(v => !string.IsNullOrEmpty(v));
        string[] personales =
        [
            sistema.CorreoCliente, sistema.CorreoPropietario, sistema.CorreoCliente.Split('@')[0], sistema.CorreoPropietario.Split('@')[0],
            "prueba.es", "Lucia", "Fernandez", "Propietaria", TelefonoCliente, TelefonoPropietario,
        ];
        foreach (string valor in usuarios.Concat(reservas).Select(v => v!))
        {
            foreach (string personal in personales)
            {
                valor.Should().NotContainEquivalentOf(personal);
            }
        }
    }

    [Fact]
    public async Task Los_datos_de_la_empresa_estan_en_claro_y_el_buscador_la_encuentra_por_su_ciudad()
    {
        Sistema sistema = await CrearSistemaAsync();

        List<string> empresa = await ConsultarAsync(db => db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre, direccion, ciudad]) AS "Value" FROM empresas WHERE id = {sistema.EmpresaId}
            """).ToListAsync());
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Resultado<PaginaResultados<ResumenEmpresaDto>> busqueda = await ambito.ServiceProvider.GetRequiredService<BuscarEstablecimientos>()
            .EjecutarAsync(new BuscarEstablecimientosComando(sistema.Ciudad, null, null, null), CancellationToken.None);

        empresa.Should().Equal("Barberia Centro", "Calle Mayor 1", sistema.Ciudad);
        busqueda.Valor!.Elementos.Should().ContainSingle(e => e.Slug == sistema.Slug);
    }

    [Fact]
    public async Task Con_el_cifrado_activo_funcionan_la_busqueda_por_correo_el_inicio_de_sesion_el_duplicado_y_el_restablecimiento()
    {
        Sistema sistema = await CrearSistemaAsync();

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IServiceProvider servicios = ambito.ServiceProvider;
        UserManager<Usuario> usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
        Usuario? cliente = await usuarios.FindByEmailAsync(sistema.CorreoCliente);
        Usuario? propietario = await usuarios.FindByEmailAsync(sistema.CorreoPropietario);
        List<Usuario> porLinq = await servicios.GetRequiredService<ContextoDatos>().Users
            .Where(u => u.Email == sistema.CorreoCliente).ToListAsync();
        SignInResult sesion = await servicios.GetRequiredService<SignInManager<Usuario>>()
            .CheckPasswordSignInAsync(cliente!, UsuariosPrueba.Clave, lockoutOnFailure: true);
        (IdentityResult duplicado, _) = await servicios.GetRequiredService<RegistroCliente>()
            .RegistrarAsync(sistema.CorreoCliente, NombreCliente, null, UsuariosPrueba.Clave, DominioPrueba.Ahora);
        await servicios.GetRequiredService<SolicitudRestablecimientoClave>().SolicitarAsync(sistema.CorreoPropietario);

        cliente!.NombreCompleto.Should().Be(NombreCliente);
        propietario!.NombreCompleto.Should().Be(NombrePropietario);
        porLinq.Should().BeEmpty();
        sesion.Succeeded.Should().BeTrue();
        duplicado.Errors.Should().Contain(e => e.Code == "DuplicateEmail", "la unicidad tiene que detectarse por el valor de busqueda (HMAC)");
        baseDatos.Remitente.EnlaceDe(sistema.CorreoPropietario).Should().NotBeNullOrEmpty();
    }

    private async Task<Sistema> CrearSistemaAsync()
    {
        string sufijo = DominioPrueba.Sufijo();
        string ciudad = $"Ciudad {sufijo[..10]}";
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id, ciudad: new Ciudad(ciudad));
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, servicio, profesional);

        string correoCliente = UsuariosPrueba.CorreoUnico();
        string correoPropietario = UsuariosPrueba.CorreoUnico();
        Guid clienteId;
        Guid propietarioId;
        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            (IdentityResult alta, Usuario? cliente) = await ambito.ServiceProvider.GetRequiredService<RegistroCliente>()
                .RegistrarAsync(correoCliente, NombreCliente, TelefonoCliente, UsuariosPrueba.Clave, DominioPrueba.Ahora);
            alta.Succeeded.Should().BeTrue();
            clienteId = cliente!.Id;
            Resultado<Guid> propietario = await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().CrearPropietarioAsync(
                new Email(correoPropietario), new NombrePersona(NombrePropietario), new Telefono(TelefonoPropietario), UsuariosPrueba.Clave,
                DominioPrueba.Ahora, CancellationToken.None);
            propietarioId = propietario.Valor;
        }

        await EscenarioEmpresa.GuardarAsync(
            baseDatos, MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, propietarioId, RolMiembro.Propietario, DominioPrueba.Ahora));

        Guid reservaId;
        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(clienteId))
        {
            Resultado<Guid> reserva = await ambito.ServiceProvider.GetRequiredService<CrearReserva>().EjecutarAsync(
                new CrearReservaComando(empresa.Id, servicio.Id, profesional.Id, EscenarioReserva.Lunes, EscenarioReserva.NueveLunesUtc, null),
                CancellationToken.None);
            reserva.EsExito.Should().BeTrue();
            reservaId = reserva.Valor;
        }

        return new Sistema(clienteId, correoCliente, propietarioId, correoPropietario, empresa.Id, empresa.Slug.Valor, ciudad, reservaId);
    }

    private async Task<T> ConsultarAsync<T>(Func<ContextoDatos, Task<T>> consulta)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await consulta(ambito.ServiceProvider.GetRequiredService<ContextoDatos>());
    }

    private sealed record Sistema(
        Guid ClienteId,
        string CorreoCliente,
        Guid PropietarioId,
        string CorreoPropietario,
        Guid EmpresaId,
        string Slug,
        string Ciudad,
        Guid ReservaId);
}
