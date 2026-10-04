using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;
using PlataformaReservas.Web.Components;
using PlataformaReservas.Web.Cuenta;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddSingleton<IEmailSender<Usuario>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidadorIdentidad>();

builder.Services.AgregarInfraestructura(builder.Configuration);

var app = builder.Build();

app.Services.ComprobarClavesCifrado();

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrarYSembrarAsync(CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();

app.Run();
