using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorPrecio()
    : ValueConverter<Precio, decimal>(
        valor => valor.Importe,
        guardado => new Precio(guardado));
