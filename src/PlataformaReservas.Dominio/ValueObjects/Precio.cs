using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Precio
{
    public const int DecimalesMaximos = 2;
    public const decimal ImporteMaximo = 99_999_999.99m;

    public Precio(decimal importe)
    {
        if (importe < 0)
        {
            throw new DominioException("El precio no puede ser negativo.");
        }

        if (importe > ImporteMaximo)
        {
            throw new DominioException("El precio no puede superar 99.999.999,99.");
        }

        if (decimal.Round(importe, DecimalesMaximos) != importe)
        {
            throw new DominioException($"El precio no puede tener mas de {DecimalesMaximos} decimales.");
        }

        Importe = importe;
    }

    public decimal Importe { get; }
}
