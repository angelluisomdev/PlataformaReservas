using System.Net.Mail;
using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Email
{
    public const int LongitudMaxima = 160;

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El correo electronico es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El correo electronico no puede superar {LongitudMaxima} caracteres.");
        }

        if (!MailAddress.TryCreate(recortado, out MailAddress? direccion) || direccion.Address != recortado)
        {
            throw new DominioException("El correo electronico no tiene un formato valido.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
