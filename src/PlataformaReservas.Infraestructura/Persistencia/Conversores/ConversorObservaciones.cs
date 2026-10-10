using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorObservaciones()
    : ValueConverter<Observaciones, string>(
        valor => valor.Valor,
        guardado => new Observaciones(guardado));
