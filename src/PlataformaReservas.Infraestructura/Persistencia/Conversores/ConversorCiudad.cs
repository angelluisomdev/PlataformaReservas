using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorCiudad()
    : ValueConverter<Ciudad, string>(
        valor => valor.Valor,
        guardado => new Ciudad(guardado));
