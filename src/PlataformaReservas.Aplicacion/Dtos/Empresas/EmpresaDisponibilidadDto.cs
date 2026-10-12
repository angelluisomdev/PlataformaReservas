using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record EmpresaDisponibilidadDto(
    bool Activa, int IntervaloHuecosMinutos, int AntelacionMinimaMinutos, int AntelacionMaximaDias)
{
    public static EmpresaDisponibilidadDto ConvertirADto(Empresa empresa)
    {
        return new EmpresaDisponibilidadDto(
            empresa.Activa,
            empresa.Politicas.IntervaloHuecosMinutos,
            empresa.Politicas.AntelacionMinimaMinutos,
            empresa.Politicas.AntelacionMaximaDias);
    }
}
