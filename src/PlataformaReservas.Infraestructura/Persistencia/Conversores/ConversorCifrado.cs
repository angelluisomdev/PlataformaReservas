using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ConversorCifrado(IPersonalDataProtector protector)
    : ValueConverter<string, string>(
        valor => protector.Protect(valor),
        cifrado => protector.Unprotect(cifrado));
