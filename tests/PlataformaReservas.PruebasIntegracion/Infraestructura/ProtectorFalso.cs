using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

// Doble de pruebas del protector de datos personales. Reversible y reconocible en la columna:
// NO cifra nada. La implementacion real (AES-256-GCM) llega en la Fase 6.
public sealed class ProtectorFalso : IPersonalDataProtector
{
    public const string Prefijo = "falso:";

    private int _llamadas;

    public int Llamadas => _llamadas;

    public string? Protect(string? data)
    {
        Interlocked.Increment(ref _llamadas);
        return data is null ? null : Prefijo + Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    public string? Unprotect(string? data)
    {
        Interlocked.Increment(ref _llamadas);
        return data is null ? null : Encoding.UTF8.GetString(Convert.FromBase64String(data[Prefijo.Length..]));
    }
}
