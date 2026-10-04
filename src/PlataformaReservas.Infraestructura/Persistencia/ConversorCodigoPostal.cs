using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorCodigoPostal()
    : ValueConverter<CodigoPostal, string>(
        valor => valor.Valor,
        guardado => new CodigoPostal(guardado));
