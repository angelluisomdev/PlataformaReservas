using Npgsql;

namespace PlataformaReservas.Infraestructura.Errores;

public static class TraductorErroresPostgres
{
    private const string ViolacionExclusion = "23P01";
    private const string Interbloqueo = "40P01";

    // Dos inserciones que chocan en la restriccion EXCLUDE pueden abortar con interbloqueo en vez de
    // con violacion de exclusion (fase 11, desviacion 1): al insertar reservas, ambos son un hueco ocupado.
    public static bool EsHuecoOcupado(Exception error, bool insertaReservas)
    {
        string? codigo = BuscarErrorPostgres(error)?.SqlState;
        return codigo == ViolacionExclusion || (insertaReservas && codigo == Interbloqueo);
    }

    private static PostgresException? BuscarErrorPostgres(Exception? error)
    {
        while (error is not null and not PostgresException)
        {
            error = error.InnerException;
        }

        return error as PostgresException;
    }
}
