using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Conversores;

public sealed class ConversorMotivoExcepcion()
    : ValueConverter<MotivoExcepcion, string>(
        valor => valor.Valor,
        guardado => new MotivoExcepcion(guardado));
