using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;

public static partial class SembradorDatos
{
    public const string CorreoClienteDemostracion = "cliente@demo.example";

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

    private static readonly EmpresaDemostracion[] _empresasDemostracion =
    [
        new("Barbería Centro", "demo-barberia-centro", "barberia", "Calle Mayor 10", "37001", "Salamanca",
            "propietario.barberia@demo.example", "Carlos Martin", [("Corte de pelo", 30, 15m), ("Corte y barba", 45, 22m)], "Ana Garcia"),
        new("Estética Norte", "demo-estetica-norte", "estetica", "Avenida de Portugal 25", "37005", "Salamanca",
            "propietaria.estetica@demo.example", "Laura Sanz", [("Limpieza facial", 60, 40m), ("Manicura", 45, 20m)], "Marta Ruiz"),
    ];

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

        foreach (EmpresaDemostracion demostracion in _empresasDemostracion)
        {
            Guid empresaId = await SembrarEmpresaAsync(db, demostracion, ahora, ct);
            if (!string.IsNullOrEmpty(clave))
            {
                await SembrarPropietarioAsync(servicios, db, demostracion, empresaId, clave, ahora, ct);
            }
        }

        if (!string.IsNullOrEmpty(clave))
        {
            await SembrarClienteAsync(servicios, clave, ahora);
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
            Guid.CreateVersion7(), new NombreEmpresa(demostracion.Nombre), slug, categoriaId, null,
            new Email($"contacto.{demostracion.Slug}@demo.example"), new Telefono("923000000"),
            new Direccion(demostracion.Direccion), new CodigoPostal(demostracion.CodigoPostal), new Ciudad(demostracion.Ciudad), ahora);
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona(demostracion.Profesional), ahora);
        db.Empresas.Add(empresa);
        foreach ((string nombre, int minutos, decimal precio) in demostracion.Servicios)
        {
            Servicio servicio = Servicio.Crear(
                Guid.CreateVersion7(), empresa.Id, new NombreServicio(nombre), new Duracion(minutos), new Precio(precio), ahora);
            db.Servicios.Add(servicio);
            profesional.AsociarServicio(servicio.Id);
        }

        foreach (DayOfWeek dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
            profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(new TimeOnly(16, 0), new TimeOnly(20, 0)));
        }

        db.Profesionales.Add(profesional);
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

    private static async Task SembrarClienteAsync(IServiceProvider servicios, string clave, DateTime ahora)
    {
        UserManager<Usuario> usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
        if (await usuarios.FindByEmailAsync(CorreoClienteDemostracion) is not null)
        {
            return;
        }

        Usuario cliente = Usuario.Crear(CorreoClienteDemostracion, new NombrePersona("Lucia Fernandez"), new Telefono("600111222"), ahora);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "DatosDemostracion:Clave no esta configurada: se siembran las empresas sin usuarios")]
    private static partial void RegistrarSinClave(ILogger registro);

    private sealed record EmpresaDemostracion(
        string Nombre,
        string Slug,
        string Categoria,
        string Direccion,
        string CodigoPostal,
        string Ciudad,
        string CorreoPropietario,
        string NombrePropietario,
        (string Nombre, int Minutos, decimal Precio)[] Servicios,
        string Profesional);
}
