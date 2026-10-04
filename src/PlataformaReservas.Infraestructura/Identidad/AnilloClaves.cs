using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class AnilloClaves : ILookupProtectorKeyRing
{
    public const string IdentificadorClaveActual = "1";

    private const int BytesPorClave = 32;

    private readonly byte[] _claveDatos;

    private readonly string _claveBusqueda;

    public AnilloClaves(IConfiguration configuracion)
    {
        byte[] claveDatos = Leer(configuracion, "Cifrado:ClaveDatos");
        byte[] claveBusqueda = Leer(configuracion, "Cifrado:ClaveBusqueda");

        if (claveDatos.AsSpan().SequenceEqual(claveBusqueda))
        {
            throw new InvalidOperationException("Cifrado:ClaveDatos y Cifrado:ClaveBusqueda deben ser claves distintas.");
        }

        _claveDatos = claveDatos;
        _claveBusqueda = Convert.ToBase64String(claveBusqueda);
    }

    public string CurrentKeyId => IdentificadorClaveActual;

    internal ReadOnlySpan<byte> ClaveDatos => _claveDatos;

    public string this[string keyId] => keyId == IdentificadorClaveActual
        ? _claveBusqueda
        : throw new KeyNotFoundException($"No existe la clave de busqueda '{keyId}'.");

    public IEnumerable<string> GetAllKeyIds() => [IdentificadorClaveActual];

    private static byte[] Leer(IConfiguration configuracion, string nombre)
    {
        string? valor = configuracion[nombre];
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException($"Falta la clave de configuracion {nombre}.");
        }

        byte[] clave = new byte[BytesPorClave];
        if (!Convert.TryFromBase64String(valor, clave, out int escritos) || escritos != BytesPorClave)
        {
            throw new InvalidOperationException($"{nombre} debe ser una clave de 256 bits en base64.");
        }

        return clave;
    }
}
