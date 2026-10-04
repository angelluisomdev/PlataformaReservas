using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorNombreCategoria()
    : ValueConverter<NombreCategoria, string>(
        valor => valor.Valor,
        guardado => new NombreCategoria(guardado));
