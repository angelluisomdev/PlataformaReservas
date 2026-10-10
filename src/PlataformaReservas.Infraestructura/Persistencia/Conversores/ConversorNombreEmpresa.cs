using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorNombreEmpresa()
    : ValueConverter<NombreEmpresa, string>(
        valor => valor.Valor,
        guardado => new NombreEmpresa(guardado));
