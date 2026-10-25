using Microsoft.AspNetCore.Components;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class NavegacionPrueba : NavigationManager
{
    public const string Base = "https://reservas.example/";

    public NavegacionPrueba(string rutaActual = "")
    {
        Initialize(Base, Base + rutaActual);
    }

    public string? Destino { get; private set; }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        Destino = uri;
    }
}
