using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorNombrePersona()
    : ValueConverter<NombrePersona, string>(
        valor => valor.Valor,
        guardado => new NombrePersona(guardado));
