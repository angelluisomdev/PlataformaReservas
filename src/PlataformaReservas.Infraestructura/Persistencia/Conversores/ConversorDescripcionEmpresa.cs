using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorDescripcionEmpresa()
    : ValueConverter<DescripcionEmpresa, string>(
        valor => valor.Valor,
        guardado => new DescripcionEmpresa(guardado));
