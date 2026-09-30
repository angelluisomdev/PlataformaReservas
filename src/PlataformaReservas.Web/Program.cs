using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;
using PlataformaReservas.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AgregarInfraestructura(builder.Configuration);   // BD (arquitectura.md §10.1)

var app = builder.Build();

// Migraciones y datos iniciales solo en desarrollo (RN-123).
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
