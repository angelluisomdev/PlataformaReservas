using System.Globalization;
using PlataformaReservas.Aplicacion.Disponibilidad;

namespace PlataformaReservas.Infraestructura.Correo.Plantillas;

internal static class FormatoCorreo
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-ES");

    public static string Fecha(DateTime instanteUtc)
    {
        return MotorDisponibilidad.ALocal(instanteUtc).ToString("dddd d 'de' MMMM 'de' yyyy", Espanol);
    }

    public static string Hora(DateTime instanteUtc)
    {
        return MotorDisponibilidad.ALocal(instanteUtc).ToString("HH:mm", Espanol);
    }

    public static string Precio(decimal importe)
    {
        return importe.ToString("C", Espanol);
    }
}
