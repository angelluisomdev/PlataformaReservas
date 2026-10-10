using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorDireccion()
    : ValueConverter<Direccion, string>(
        valor => valor.Valor,
        guardado => new Direccion(guardado));
