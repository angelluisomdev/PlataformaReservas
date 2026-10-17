using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class BuscarEstablecimientos(IConsultasCatalogoPublico catalogo)
{
    public const int LongitudMinimaCiudad = 2;
    public const int TamanoPaginaMaximo = 20;

    public static bool EsCiudadValida(string? ciudad)
    {
        int longitud = ciudad?.Trim().Length ?? 0;
        return longitud >= LongitudMinimaCiudad && longitud <= Ciudad.LongitudMaxima;
    }

    public async Task<Resultado<PaginaResultados<ResumenEmpresaDto>>> EjecutarAsync(
        BuscarEstablecimientosComando comando, CancellationToken ct)
    {
        int pagina = comando.Pagina is > 0 ? comando.Pagina.Value : 1;
        int tamano = comando.TamanoPagina is > 0 ? Math.Min(comando.TamanoPagina.Value, TamanoPaginaMaximo) : TamanoPaginaMaximo;

        if (!EsCiudadValida(comando.Ciudad))
        {
            return Resultado<PaginaResultados<ResumenEmpresaDto>>.Exito(new PaginaResultados<ResumenEmpresaDto>([], 0, pagina, tamano));
        }

        string? categoria = string.IsNullOrWhiteSpace(comando.Categoria) ? null : comando.Categoria.Trim();
        PaginaResultados<ResumenEmpresaDto> resultados =
            await catalogo.BuscarAsync(comando.Ciudad!.Trim(), categoria, pagina, tamano, ct);
        return Resultado<PaginaResultados<ResumenEmpresaDto>>.Exito(resultados);
    }
}
