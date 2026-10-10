using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class RegistrarEmpresa(
    IServicioCuentas cuentas,
    IRepositorioEmpresas empresas,
    IRepositorioMiembros miembros,
    IConsultasCatalogoPublico catalogo,
    IUnidadTrabajo unidadTrabajo,
    IRelojSistema reloj)
{
    public async Task<Resultado<EmpresaRegistradaDto>> EjecutarAsync(RegistrarEmpresaComando comando, CancellationToken ct)
    {
        Email emailCuenta;
        NombrePersona nombrePropietario;
        Telefono? telefonoPropietario;
        DatosEmpresa datos;
        Slug slugBase;
        try
        {
            emailCuenta = new Email(comando.Email);
            nombrePropietario = new NombrePersona(comando.NombreCompleto);
            telefonoPropietario = string.IsNullOrWhiteSpace(comando.Telefono) ? null : new Telefono(comando.Telefono);
            datos = DatosEmpresa.Desde(
                comando.NombreEmpresa, comando.Descripcion, comando.EmailEmpresa, comando.TelefonoEmpresa,
                comando.Direccion, comando.CodigoPostal, comando.Ciudad);
            slugBase = Slug.Desde(comando.NombreEmpresa);
        }
        catch (DominioException error)
        {
            return Resultado<EmpresaRegistradaDto>.Fallo(CodigoError.Validacion, error.Message);
        }

        IReadOnlyList<CategoriaDto> categorias = await catalogo.ObtenerCategoriasAsync(ct);
        if (!categorias.Any(c => c.Id == comando.CategoriaId))
        {
            return Resultado<EmpresaRegistradaDto>.Fallo(CodigoError.Validacion, "La categoria elegida no existe.");
        }

        Slug slug = slugBase;
        for (int sufijo = 2; await empresas.ExisteSlugAsync(slug, ct); sufijo++)
        {
            slug = slugBase.ConSufijo(sufijo);
        }

        DateTime ahora = reloj.AhoraUtc;
        await using ITransaccion transaccion = await unidadTrabajo.IniciarTransaccionAsync(ct);

        Resultado<Guid> cuenta = await cuentas.CrearPropietarioAsync(
            emailCuenta, nombrePropietario, telefonoPropietario, comando.Clave, ahora, ct);
        if (!cuenta.EsExito)
        {
            return Resultado<EmpresaRegistradaDto>.Fallo(cuenta.Codigo!.Value, cuenta.Mensaje!);
        }

        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), datos.Nombre, slug, comando.CategoriaId, datos.Descripcion, datos.Email,
            datos.Telefono, datos.Direccion, datos.CodigoPostal, datos.Ciudad, ahora);
        empresas.Agregar(empresa);
        miembros.Agregar(MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, cuenta.Valor, RolMiembro.Propietario, ahora));

        await unidadTrabajo.GuardarCambiosAsync(ct);
        await transaccion.ConfirmarAsync(ct);

        return Resultado<EmpresaRegistradaDto>.Exito(new EmpresaRegistradaDto(cuenta.Valor, empresa.Id, slug.Valor));
    }
}
