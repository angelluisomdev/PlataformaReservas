using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Autorizacion;

public sealed class PruebasAutorizacionRutas
{
    private static readonly string[] RutasPublicas =
    [
        "/", "/businesses", "/businesses/{Slug}", "/book/{Slug}", "/Error", "/not-found",
        "/login", "/register", "/for-business/register",
        "/Account/Login", "/Account/Register", "/Account/AccessDenied",
        "/Account/ForgotPassword", "/Account/ForgotPasswordConfirmation",
        "/Account/ResetPassword", "/Account/ResetPasswordConfirmation", "/Account/InvalidPasswordReset",
    ];

    private static readonly IReadOnlyList<(Type Componente, string Ruta, AuthorizeAttribute? Autorizacion, bool Anonima)> Rutas =
    [
        .. typeof(PlataformaReservas.Web.Components.Publico.Landing).Assembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>().Select(r => (
                t, r.Template, t.GetCustomAttribute<AuthorizeAttribute>(), t.GetCustomAttribute<AllowAnonymousAttribute>() is not null))),
    ];

    [Fact]
    public void Hay_rutas_de_las_tres_areas()
    {
        Rutas.Should().Contain(r => r.Ruta.StartsWith("/app", StringComparison.Ordinal));
        Rutas.Should().Contain(r => r.Ruta.StartsWith("/my-account", StringComparison.Ordinal));
        Rutas.Should().Contain(r => r.Ruta == "/");
    }

    [Fact]
    public void Toda_ruta_del_area_de_empresa_exige_la_politica_de_propietario()
    {
        Rutas.Where(r => r.Ruta.StartsWith("/app", StringComparison.Ordinal))
            .Should().OnlyContain(r => r.Autorizacion != null && r.Autorizacion.Policy == RolesIdentidad.PoliticaPropietario);
    }

    [Fact]
    public void Toda_ruta_del_area_de_cliente_exige_la_politica_de_cliente()
    {
        Rutas.Where(r => r.Ruta.StartsWith("/my-account", StringComparison.Ordinal))
            .Should().OnlyContain(r => r.Autorizacion != null && r.Autorizacion.Policy == RolesIdentidad.PoliticaCliente);
    }

    [Fact]
    public void Toda_pagina_del_espacio_de_nombres_del_area_de_empresa_exige_la_politica_de_propietario()
    {
        Rutas.Where(r => r.Componente.Namespace == "PlataformaReservas.Web.Components.AreaEmpresa")
            .Should().NotBeEmpty()
            .And.OnlyContain(r => r.Autorizacion != null && r.Autorizacion.Policy == RolesIdentidad.PoliticaPropietario);
    }

    [Fact]
    public void Toda_pagina_del_espacio_de_nombres_del_area_de_cliente_exige_la_politica_de_cliente()
    {
        Rutas.Where(r => r.Componente.Namespace == "PlataformaReservas.Web.Components.MiCuenta")
            .Should().NotBeEmpty()
            .And.OnlyContain(r => r.Autorizacion != null && r.Autorizacion.Policy == RolesIdentidad.PoliticaCliente);
    }

    [Fact]
    public void Ninguna_ruta_queda_sin_proteger_salvo_las_publicas_declaradas()
    {
        IEnumerable<string> sinProteger = Rutas.Where(r => r.Autorizacion is null).Select(r => r.Ruta);

        sinProteger.Should().BeSubsetOf(RutasPublicas, "una pagina nueva sin [Authorize] tiene que declararse publica a proposito");
    }

    // [AllowAnonymous] anula en ejecucion el [Authorize] heredado de _Imports.razor: el atributo de politica no basta.
    [Fact]
    public void Ninguna_ruta_fuera_de_las_publicas_declaradas_admite_anonimos()
    {
        IEnumerable<string> anonimas = Rutas.Where(r => r.Anonima).Select(r => r.Ruta);

        anonimas.Should().BeSubsetOf(RutasPublicas, "[AllowAnonymous] en una pagina privada la abre aunque herede la politica");
    }

    [Fact]
    public void Las_rutas_publicas_declaradas_existen_y_no_piden_autorizacion()
    {
        RutasPublicas.Should().OnlyContain(publica => Rutas.Any(r => r.Ruta == publica && r.Autorizacion == null));
    }
}
