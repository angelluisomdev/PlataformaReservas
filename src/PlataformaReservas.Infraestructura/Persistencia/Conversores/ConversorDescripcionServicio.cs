using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorDescripcionServicio()
    : ValueConverter<DescripcionServicio, string>(
        valor => valor.Valor,
        guardado => new DescripcionServicio(guardado));
