using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed record EscenarioReserva(Empresa Empresa, Servicio Servicio, Profesional Profesional, Usuario Cliente)
{
    public const string TelefonoCliente = "611222333";

    public static readonly DateOnly Sabado = new(2026, 10, 3);
    public static readonly DateOnly Lunes = new(2026, 10, 5);
    public static readonly DateTime NueveLunesUtc = new(2026, 10, 5, 7, 0, 0, DateTimeKind.Utc);

    public static async Task<EscenarioReserva> CrearAsync(BaseDatosFixture baseDatos, string? telefono = TelefonoCliente)
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional);
        Usuario cliente = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Lucia Fernandez", telefono);
        return new EscenarioReserva(empresa, servicio, profesional, cliente);
    }

    public static Profesional ProfesionalQuePresta(Guid empresaId, Guid servicioId)
    {
        Profesional profesional = DominioPrueba.Profesional(empresaId);
        profesional.AsociarServicio(servicioId);
        IntervaloHorario manana = new(new TimeOnly(9, 0), new TimeOnly(14, 0));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Saturday, manana);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, manana);
        return profesional;
    }

    public CrearReservaComando Comando(DateOnly? fecha = null, DateTime? inicioUtc = null, string? observaciones = null)
    {
        return new CrearReservaComando(
            Empresa.Id, Servicio.Id, Profesional.Id, fecha ?? Lunes, inicioUtc ?? NueveLunesUtc, observaciones);
    }

    public Reserva Reserva(DateTime inicioUtc, Guid? usuarioId = null)
    {
        return Dominio.Entidades.Reserva.Crear(
            Guid.CreateVersion7(), Empresa.Id, usuarioId ?? Cliente.Id, Profesional.Id, Servicio, inicioUtc,
            new NombrePersona("Lucia Fernandez"), new Telefono(TelefonoCliente), null, DominioPrueba.Ahora);
    }
}
