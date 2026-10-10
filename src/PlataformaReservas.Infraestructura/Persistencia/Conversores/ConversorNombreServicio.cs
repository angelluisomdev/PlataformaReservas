using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorNombreServicio()
    : ValueConverter<NombreServicio, string>(
        valor => valor.Valor,
        guardado => new NombreServicio(guardado));
