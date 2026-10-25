using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Aplicacion;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;
using PlataformaReservas.Web;
using PlataformaReservas.Web.Components;
using PlataformaReservas.Web.Cuenta;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexto, registro) => registro.ReadFrom.Configuration(contexto.Configuration));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<DestinoInicioSesion>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidadorIdentidad>();

builder.Services.AgregarLimitacionFrecuencia();
builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AgregarAplicacion();

var app = builder.Build();

app.Services.ComprobarClavesCifrado();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("BaseDatos:MigrarAlArrancar"))
{
    await app.Services.MigrarYSembrarAsync(CancellationToken.None);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UsarCabecerasSeguridad();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(opciones => opciones.ContentSecurityFrameAncestorsPolicy = null);
app.MapAdditionalIdentityEndpoints();

app.Run();
