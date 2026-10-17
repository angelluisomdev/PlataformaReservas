using System.Globalization;

namespace PlataformaReservas.Aplicacion.Compartido;

public static class OrdenEspanol
{
    public static readonly StringComparer Comparador = StringComparer.Create(CultureInfo.GetCultureInfo("es-ES"), ignoreCase: false);
}
