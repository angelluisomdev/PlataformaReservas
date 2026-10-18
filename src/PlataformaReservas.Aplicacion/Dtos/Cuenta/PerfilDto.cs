using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Cuenta;

public sealed record PerfilDto(string Nombre, string? Telefono)
{
    public static PerfilDto ConvertirADto(NombrePersona nombre, Telefono? telefono)
    {
        return new PerfilDto(nombre.Valor, telefono?.Valor);
    }
}
