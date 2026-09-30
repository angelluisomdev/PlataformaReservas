namespace PlataformaReservas.Dominio.Enumeraciones;

// Se persiste como entero: la restriccion EXCLUDE filtra por estado <> 1 (SPEC §13).
// Los valores son estables y no se reordenan.
public enum EstadoReserva
{
    Confirmada = 0,
    Cancelada = 1,
    Completada = 2,
}
