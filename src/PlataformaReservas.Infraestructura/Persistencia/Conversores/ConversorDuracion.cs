using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorDuracion()
    : ValueConverter<Duracion, int>(
        valor => valor.Minutos,
        guardado => new Duracion(guardado));
