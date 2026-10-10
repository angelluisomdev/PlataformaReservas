using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

internal sealed record DatosEmpresa(
    NombreEmpresa Nombre,
    DescripcionEmpresa? Descripcion,
    Email Email,
    Telefono Telefono,
    Direccion Direccion,
    CodigoPostal CodigoPostal,
    Ciudad Ciudad)
{
    public static DatosEmpresa Desde(
        string nombre,
        string? descripcion,
        string email,
        string telefono,
        string direccion,
        string codigoPostal,
        string ciudad)
    {
        return new DatosEmpresa(
            new NombreEmpresa(nombre),
            string.IsNullOrWhiteSpace(descripcion) ? null : new DescripcionEmpresa(descripcion),
            new Email(email),
            new Telefono(telefono),
            new Direccion(direccion),
            new CodigoPostal(codigoPostal),
            new Ciudad(ciudad));
    }
}
