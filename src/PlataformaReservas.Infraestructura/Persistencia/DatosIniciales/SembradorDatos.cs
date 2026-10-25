using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;

public static partial class SembradorDatos
{
    public const string CorreoClienteDemostracion = CatalogoDemostracion.CorreoClientePrincipal;

    private static readonly string[] _categorias =
    [
        "Peluquería",
        "Barbería",
        "Estética",
        "Fisioterapia",
        "Psicología",
        "Taller",
        "Academia",
        "Entrenamiento personal",
    ];

    private static readonly TimeSpan[] _horasReserva =
        [new(9, 30, 0), new(11, 30, 0), new(16, 30, 0), new(18, 30, 0)];

    public static async Task MigrarYSembrarAsync(this IServiceProvider servicios, CancellationToken ct)
    {
        using IServiceScope ambito = servicios.CreateScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        await db.Database.MigrateAsync(ct);
        await SembrarAsync(db, ct);
        await SembrarDemostracionAsync(ambito.ServiceProvider, ambito.ServiceProvider.GetRequiredService<IConfiguration>(), ct);
    }

    public static async Task SembrarAsync(ContextoDatos db, CancellationToken ct)
    {
        List<Slug> existentes = await db.Categorias
            .Select(c => c.Slug)
            .ToListAsync(ct);

        foreach (string nombre in _categorias)
        {
            Slug slug = Slug.Desde(nombre);
            if (!existentes.Contains(slug))
            {
                db.Categorias.Add(Categoria.Crear(Guid.CreateVersion7(), new NombreCategoria(nombre), slug));
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task SembrarDemostracionAsync(IServiceProvider servicios, IConfiguration configuracion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);
        if (!configuracion.GetValue<bool>("DatosDemostracion:Habilitado"))
        {
            return;
        }

        ContextoDatos db = servicios.GetRequiredService<ContextoDatos>();
        DateTime ahora = servicios.GetRequiredService<IRelojSistema>().AhoraUtc;
        ILogger registro = servicios.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SembradorDatos));
        string? clave = configuracion["DatosDemostracion:Clave"];
        if (string.IsNullOrEmpty(clave))
        {
            RegistrarSinClave(registro);
        }

        List<ClienteSembrado> clientes = string.IsNullOrEmpty(clave) ? [] : await SembrarClientesAsync(servicios, clave, ahora);
        int turno = 0;
        foreach (EmpresaDemostracion demostracion in CatalogoDemostracion.Empresas)
        {
            Guid empresaId = await SembrarEmpresaAsync(db, demostracion, ahora, ct);
            if (!string.IsNullOrEmpty(clave))
            {
                await SembrarPropietarioAsync(servicios, db, demostracion, empresaId, clave, ahora, ct);
                turno = await SembrarReservasAsync(db, empresaId, clientes, turno, ahora, ct);
            }
        }
    }

    private static async Task<Guid> SembrarEmpresaAsync(ContextoDatos db, EmpresaDemostracion demostracion, DateTime ahora, CancellationToken ct)
    {
        Slug slug = Slug.Desde(demostracion.Slug);
        Guid? existente = await db.Empresas.Where(e => e.Slug == slug).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
        if (existente is not null)
        {
            return existente.Value;
        }

        Slug categoria = Slug.Desde(demostracion.Categoria);
        Guid categoriaId = await db.Categorias.Where(c => c.Slug == categoria).Select(c => c.Id).SingleAsync(ct);
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa(demostracion.Nombre), slug, categoriaId,
            demostracion.Descripcion is null ? null : new DescripcionEmpresa(demostracion.Descripcion),
            new Email($"contacto.{demostracion.Slug}@demo.example"), new Telefono(demostracion.Telefono),
            new Direccion(demostracion.Direccion), new CodigoPostal(demostracion.CodigoPostal), new Ciudad(demostracion.Ciudad), ahora);
        db.Empresas.Add(empresa);

        List<Guid> servicios = [];
        foreach ((string nombre, int minutos, decimal precio) in demostracion.Servicios)
        {
            Servicio servicio = Servicio.Crear(
                Guid.CreateVersion7(), empresa.Id, new NombreServicio(nombre), new Duracion(minutos), new Precio(precio), ahora);
            db.Servicios.Add(servicio);
            servicios.Add(servicio.Id);
        }

        for (int p = 0; p < demostracion.Profesionales.Count; p++)
        {
            Profesional profesional = Profesional.Crear(
                Guid.CreateVersion7(), empresa.Id, new NombrePersona(demostracion.Profesionales[p]), ahora);
            for (int s = 0; s < servicios.Count; s++)
            {
                if (servicios.Count == 1 || s != p % servicios.Count)
                {
                    profesional.AsociarServicio(servicios[s]);
                }
            }

            foreach (DayOfWeek dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            {
                profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
                profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(new TimeOnly(16, 0), new TimeOnly(20, 0)));
            }

            db.Profesionales.Add(profesional);
        }

        empresa.LimpiarEventos();
        await db.SaveChangesAsync(ct);
        return empresa.Id;
    }

    private static async Task SembrarPropietarioAsync(
        IServiceProvider servicios, ContextoDatos db, EmpresaDemostracion demostracion, Guid empresaId, string clave, DateTime ahora,
        CancellationToken ct)
    {
        UserManager<Usuario> usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
        Usuario? existente = await usuarios.FindByEmailAsync(demostracion.CorreoPropietario);
        Guid propietarioId;
        if (existente is not null)
        {
            propietarioId = existente.Id;
            if (await db.MiembrosEmpresa.AnyAsync(m => m.UsuarioId == propietarioId && m.EmpresaId == empresaId, ct))
            {
                return;
            }
        }
        else
        {
            Resultado<Guid> propietario = await servicios.GetRequiredService<IServicioCuentas>().CrearPropietarioAsync(
                new Email(demostracion.CorreoPropietario), new NombrePersona(demostracion.NombrePropietario), null, clave, ahora, ct);
            if (!propietario.EsExito)
            {
                throw new InvalidOperationException($"No se pudo sembrar el propietario de demostracion: {propietario.Mensaje}");
            }

            propietarioId = propietario.Valor;
        }

        db.MiembrosEmpresa.Add(MiembroEmpresa.Crear(Guid.CreateVersion7(), empresaId, propietarioId, RolMiembro.Propietario, ahora));
        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<ClienteSembrado>> SembrarClientesAsync(IServiceProvider servicios, string clave, DateTime ahora)
    {
        UserManager<Usuario> usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
        List<ClienteSembrado> sembrados = [];
        foreach (ClienteDemostracion demostracion in CatalogoDemostracion.Clientes)
        {
            NombrePersona nombre = new(demostracion.Nombre);
            Telefono telefono = new(demostracion.Telefono);
            Usuario? cliente = await usuarios.FindByEmailAsync(demostracion.Correo);
            if (cliente is null)
            {
                cliente = Usuario.Crear(demostracion.Correo, nombre, telefono, ahora);
                IdentityResult resultado = await usuarios.CreateAsync(cliente, clave);
                if (resultado.Succeeded)
                {
                    resultado = await usuarios.AddToRoleAsync(cliente, RolesIdentidad.Cliente);
                }

                if (!resultado.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo sembrar el cliente de demostracion: {string.Join(", ", resultado.Errors.Select(e => e.Code))}");
                }
            }

            sembrados.Add(new ClienteSembrado(cliente.Id, nombre, telefono));
        }

        return sembrados;
    }

    // Solo para empresas sin reservas: asi es idempotente aunque la empresa naciera en un arranque sin clave.
    // Cada profesional usa dias distintos y horas separadas 2 h, y ningun servicio pasa de 60 min: no hay solapes.
    private static async Task<int> SembrarReservasAsync(
        ContextoDatos db, Guid empresaId, List<ClienteSembrado> clientes, int turno, DateTime ahora, CancellationToken ct)
    {
        if (clientes.Count == 0 || await db.Reservas.AnyAsync(r => r.EmpresaId == empresaId, ct))
        {
            return turno;
        }

        List<Servicio> servicios = await db.Servicios.Where(s => s.EmpresaId == empresaId).ToListAsync(ct);
        List<Profesional> profesionales = await db.Profesionales.Where(p => p.EmpresaId == empresaId).ToListAsync(ct);
        DateOnly hoy = DateOnly.FromDateTime(MotorDisponibilidad.ALocal(ahora));
        List<DateOnly> pasados = DiasLaborables(hoy, -1);
        List<DateOnly> futuros = DiasLaborables(hoy, 1);

        for (int p = 0; p < profesionales.Count; p++)
        {
            Profesional profesional = profesionales[p];
            List<Servicio> presta = servicios.Where(s => profesional.ServiciosQuePresta.Contains(s.Id)).ToList();
            for (int r = 0; r < 3; r++)
            {
                Reserva reserva = CrearReserva(empresaId, profesional, presta[(p + r) % presta.Count], clientes[turno++ % clientes.Count],
                    pasados[(p + (3 * r)) % pasados.Count], _horasReserva[r % _horasReserva.Length], ahora);
                if (r < 2)
                {
                    reserva.Completar(ahora);
                }
                else
                {
                    reserva.CancelarPorEmpresa(ahora, OrigenCancelacion.Empresa);
                }

                Agregar(db, reserva);
            }

            for (int r = 0; r < 4; r++)
            {
                Reserva reserva = CrearReserva(empresaId, profesional, presta[(p + r + 1) % presta.Count], clientes[turno++ % clientes.Count],
                    futuros[(p + (2 * r)) % futuros.Count], _horasReserva[(p + r) % _horasReserva.Length], ahora);
                if (r == 3)
                {
                    reserva.CancelarPorCliente(ahora, OrigenCancelacion.Cliente);
                }

                Agregar(db, reserva);
            }
        }

        await db.SaveChangesAsync(ct);
        return turno;
    }

    private static Reserva CrearReserva(
        Guid empresaId, Profesional profesional, Servicio servicio, ClienteSembrado cliente, DateOnly dia, TimeSpan hora, DateTime ahora)
    {
        DateTime inicioUtc = MotorDisponibilidad.DiaLocalEnUtc(dia).DesdeUtc + hora;
        return Reserva.Crear(
            Guid.CreateVersion7(), empresaId, cliente.Id, profesional.Id, servicio, inicioUtc, cliente.Nombre, cliente.Telefono, null, ahora);
    }

    private static void Agregar(ContextoDatos db, Reserva reserva)
    {
        reserva.LimpiarEventos();
        db.Reservas.Add(reserva);
    }

    // Se saltan los dias del cambio de hora: alli "inicio del dia + hora local" no da la hora local correcta.
    private static List<DateOnly> DiasLaborables(DateOnly hoy, int sentido)
    {
        List<DateOnly> dias = [];
        for (DateOnly dia = hoy.AddDays(sentido); dias.Count < 10; dia = dia.AddDays(sentido))
        {
            (DateTime desde, DateTime hasta) = MotorDisponibilidad.DiaLocalEnUtc(dia);
            if (dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && hasta - desde == TimeSpan.FromDays(1))
            {
                dias.Add(dia);
            }
        }

        return dias;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "DatosDemostracion:Clave no esta configurada: se siembran las empresas sin usuarios")]
    private static partial void RegistrarSinClave(ILogger registro);

    private sealed record ClienteSembrado(Guid Id, NombrePersona Nombre, Telefono Telefono);
}
