using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorEmail()
    : ValueConverter<Email, string>(
        valor => valor.Valor,
        guardado => new Email(guardado));
