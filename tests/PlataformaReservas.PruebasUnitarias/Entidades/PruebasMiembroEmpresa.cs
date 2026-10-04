using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasMiembroEmpresa
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private static MiembroEmpresa CrearMiembro(
        Guid? id = null,
        Guid? empresaId = null,
        Guid? usuarioId = null,
        DateTime? ahoraUtc = null,
        RolMiembro rol = RolMiembro.Propietario)
    {
        return MiembroEmpresa.Crear(
            id ?? Guid.CreateVersion7(), empresaId ?? Guid.CreateVersion7(), usuarioId ?? Guid.CreateVersion7(),
            rol, ahoraUtc ?? Ahora);
    }

    [Fact]
    public void Vincula_al_usuario_con_la_empresa_y_su_rol()
    {
        Guid empresaId = Guid.CreateVersion7();
        Guid usuarioId = Guid.CreateVersion7();

        MiembroEmpresa miembro = CrearMiembro(empresaId: empresaId, usuarioId: usuarioId);

        miembro.EmpresaId.Should().Be(empresaId);
        miembro.UsuarioId.Should().Be(usuarioId);
        miembro.Rol.Should().Be(RolMiembro.Propietario);
        miembro.FechaAlta.Should().Be(Ahora);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("empresa")]
    [InlineData("usuario")]
    public void Rechaza_identificadores_vacios(string cual)
    {
        Action crear = cual switch
        {
            "id" => () => CrearMiembro(id: Guid.Empty),
            "empresa" => () => CrearMiembro(empresaId: Guid.Empty),
            _ => () => CrearMiembro(usuarioId: Guid.Empty),
        };

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => CrearMiembro(ahoraUtc: DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_un_rol_que_no_existe()
    {
        Action crear = () => CrearMiembro(rol: (RolMiembro)99);

        crear.Should().Throw<DominioException>();
    }
}
