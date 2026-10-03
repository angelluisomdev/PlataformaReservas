using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ProtectorBusqueda(ILookupProtectorKeyRing anillo) : ILookupProtector
{
    public string? Protect(string keyId, string? data)
    {
        if (data is null)
        {
            return null;
        }

        byte[] clave = Convert.FromBase64String(anillo[keyId]);
        byte[] resumen = HMACSHA256.HashData(clave, Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(resumen);
    }

    public string? Unprotect(string keyId, string? data)
    {
        throw new NotSupportedException("El valor de busqueda es un HMAC: no se puede descifrar.");
    }
}
