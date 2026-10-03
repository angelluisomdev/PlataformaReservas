using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

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
