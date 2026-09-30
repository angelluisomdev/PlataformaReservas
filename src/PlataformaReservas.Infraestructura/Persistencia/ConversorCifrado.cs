using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PlataformaReservas.Infraestructura.Persistencia;

// Cifra en reposo las copias de datos personales de Reserva (RN-163, D-22). Identity solo protege
// la entidad de usuario: sin esto, nombre y telefono quedarian en claro en la tabla reservas (R-11).
// La implementacion real de IPersonalDataProtector (AES-256-GCM) llega en la Fase 6.
public sealed class ConversorCifrado(IPersonalDataProtector protector)
    : ValueConverter<string, string>(
        valor => protector.Protect(valor),
        cifrado => protector.Unprotect(cifrado));
