using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorTelefono()
    : ValueConverter<Telefono, string>(
        valor => valor.Valor,
        guardado => new Telefono(guardado));
