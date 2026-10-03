using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ProtectorDatosPersonales(AnilloClaves anillo) : IPersonalDataProtector
{
    private const int BytesNonce = 12;
    private const int BytesEtiqueta = 16;

    public string? Protect(string? data)
    {
        if (data is null)
        {
            return null;
        }

        byte[] claro = Encoding.UTF8.GetBytes(data);
        byte[] resultado = new byte[BytesNonce + BytesEtiqueta + claro.Length];
        Span<byte> nonce = resultado.AsSpan(0, BytesNonce);
        Span<byte> etiqueta = resultado.AsSpan(BytesNonce, BytesEtiqueta);
        Span<byte> cifrado = resultado.AsSpan(BytesNonce + BytesEtiqueta);

        RandomNumberGenerator.Fill(nonce);
        using AesGcm aes = new(anillo.ClaveDatos, BytesEtiqueta);
        aes.Encrypt(nonce, claro, cifrado, etiqueta);

        return Convert.ToBase64String(resultado);
    }

    public string? Unprotect(string? data)
    {
        if (data is null)
        {
            return null;
        }

        byte[] entrada = Convert.FromBase64String(data);
        if (entrada.Length < BytesNonce + BytesEtiqueta)
        {
            throw new CryptographicException("El valor cifrado no tiene el formato esperado.");
        }

        ReadOnlySpan<byte> nonce = entrada.AsSpan(0, BytesNonce);
        ReadOnlySpan<byte> etiqueta = entrada.AsSpan(BytesNonce, BytesEtiqueta);
        ReadOnlySpan<byte> cifrado = entrada.AsSpan(BytesNonce + BytesEtiqueta);
        byte[] claro = new byte[cifrado.Length];

        using AesGcm aes = new(anillo.ClaveDatos, BytesEtiqueta);
        aes.Decrypt(nonce, cifrado, etiqueta, claro);

        return Encoding.UTF8.GetString(claro);
    }
}
