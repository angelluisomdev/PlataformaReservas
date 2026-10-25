using System.Net;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlataformaReservas.Web;

namespace PlataformaReservas.PruebasIntegracion;

public sealed class PruebasLimitacionFrecuencia
{
    private const string Ip = "203.0.113.7";

    [Fact]
    public void El_sexto_envio_a_login_desde_la_misma_ip_se_rechaza()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();

        Admitidas(limitador, "POST", "/login", Ip, 5).Should().Be(5);
        Admitida(limitador, "POST", "/login", Ip).Should().BeFalse();
    }

    [Fact]
    public void Otra_ip_tiene_su_propio_cupo()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();
        Admitidas(limitador, "POST", "/login", Ip, 5);

        Admitida(limitador, "POST", "/login", "198.51.100.9").Should().BeTrue();
    }

    [Fact]
    public void Las_visitas_al_formulario_no_cuentan()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();

        Admitidas(limitador, "GET", "/login", Ip, 20).Should().Be(20);
        Admitidas(limitador, "POST", "/login", Ip, 5).Should().Be(5);
    }

    [Fact]
    public void Las_rutas_de_cuenta_comparten_el_cupo_de_autenticacion()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();

        Admitida(limitador, "POST", "/login", Ip).Should().BeTrue();
        Admitida(limitador, "POST", "/Account/Login", Ip).Should().BeTrue();
        Admitida(limitador, "POST", "/register", Ip).Should().BeTrue();
        Admitida(limitador, "POST", "/Account/Register", Ip).Should().BeTrue();
        Admitida(limitador, "POST", "/Account/ForgotPassword", Ip).Should().BeTrue();
        Admitida(limitador, "POST", "/account/forgotpassword", Ip).Should().BeFalse();
        Admitida(limitador, "POST", "/login/", Ip).Should().BeFalse();
        Admitida(limitador, "POST", "/for-business/register", Ip).Should().BeFalse();
    }

    [Theory]
    [InlineData("/businesses")]
    [InlineData("/businesses/barberia-centro")]
    public void La_peticion_61_en_un_minuto_al_catalogo_se_rechaza(string ruta)
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();

        Admitidas(limitador, "GET", ruta, Ip, 60).Should().Be(60);
        Admitida(limitador, "GET", ruta, Ip).Should().BeFalse();
    }

    [Fact]
    public void La_ficha_y_el_buscador_comparten_el_cupo_del_catalogo()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();
        Admitidas(limitador, "GET", "/businesses", Ip, 60);

        Admitida(limitador, "GET", "/businesses/barberia-centro", Ip).Should().BeFalse();
    }

    [Fact]
    public void El_cupo_de_autenticacion_no_consume_el_del_catalogo()
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();
        Admitidas(limitador, "POST", "/login", Ip, 6);

        Admitida(limitador, "GET", "/businesses", Ip).Should().BeTrue();
    }

    [Theory]
    [InlineData("GET", "/")]
    [InlineData("GET", "/app")]
    [InlineData("POST", "/app")]
    [InlineData("GET", "/my-account/bookings")]
    [InlineData("POST", "/Account/Logout")]
    [InlineData("GET", "/businessesx")]
    public void Una_ruta_sin_grupo_no_se_limita(string metodo, string ruta)
    {
        using PartitionedRateLimiter<HttpContext> limitador = LimitacionFrecuencia.CrearLimitador();

        Admitidas(limitador, metodo, ruta, Ip, 200).Should().Be(200);
    }

    [Theory]
    [InlineData("POST", "/login", LimitacionFrecuencia.GrupoAutenticacion)]
    [InlineData("POST", "/Account/ForgotPassword", LimitacionFrecuencia.GrupoAutenticacion)]
    [InlineData("GET", "/login", null)]
    [InlineData("POST", "/login/", LimitacionFrecuencia.GrupoAutenticacion)]
    [InlineData("POST", "/Account/Login//", LimitacionFrecuencia.GrupoAutenticacion)]
    [InlineData("POST", "/for-business/register", LimitacionFrecuencia.GrupoAutenticacion)]
    [InlineData("GET", "/for-business/register", null)]
    [InlineData("POST", "/login/otra", null)]
    [InlineData("GET", "/businesses", LimitacionFrecuencia.GrupoCatalogo)]
    [InlineData("POST", "/BUSINESSES/x", LimitacionFrecuencia.GrupoCatalogo)]
    [InlineData("GET", "/businessesx", null)]
    [InlineData("GET", "/", null)]
    public void Asigna_cada_peticion_a_su_grupo(string metodo, string ruta, string? grupo)
    {
        LimitacionFrecuencia.GrupoDe(Peticion(metodo, ruta, Ip).Request).Should().Be(grupo);
    }

    [Fact]
    public void El_registro_rechaza_con_429_y_con_su_propia_respuesta()
    {
        ServiceCollection servicios = new();
        servicios.AgregarLimitacionFrecuencia();
        using ServiceProvider proveedor = servicios.BuildServiceProvider();

        RateLimiterOptions opciones = proveedor.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        opciones.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        opciones.GlobalLimiter.Should().NotBeNull();
        opciones.OnRejected.Should().NotBeNull();
    }

    [Fact]
    public async Task El_rechazo_escribe_su_mensaje_y_no_deja_reejecutar_la_pagina_de_estado()
    {
        DefaultHttpContext contexto = new();
        contexto.Response.Body = new MemoryStream();
        StatusCodePagesFeature paginasEstado = new();
        contexto.Features.Set<IStatusCodePagesFeature>(paginasEstado);

        await LimitacionFrecuencia.ResponderRechazoAsync(contexto, CancellationToken.None);

        paginasEstado.Enabled.Should().BeFalse();
        contexto.Response.ContentType.Should().StartWith("text/plain");
        contexto.Response.Body.Position = 0;
        (await new StreamReader(contexto.Response.Body).ReadToEndAsync()).Should().Contain("Demasiadas peticiones");
    }

    private static int Admitidas(PartitionedRateLimiter<HttpContext> limitador, string metodo, string ruta, string ip, int veces)
    {
        return Enumerable.Range(0, veces).Count(_ => Admitida(limitador, metodo, ruta, ip));
    }

    private static bool Admitida(PartitionedRateLimiter<HttpContext> limitador, string metodo, string ruta, string ip)
    {
        using RateLimitLease permiso = limitador.AttemptAcquire(Peticion(metodo, ruta, ip));
        return permiso.IsAcquired;
    }

    private static DefaultHttpContext Peticion(string metodo, string ruta, string ip)
    {
        DefaultHttpContext contexto = new();
        contexto.Request.Method = metodo;
        contexto.Request.Path = ruta;
        contexto.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return contexto;
    }
}
