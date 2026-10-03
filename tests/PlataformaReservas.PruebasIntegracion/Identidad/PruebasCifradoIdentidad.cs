using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasCifradoIdentidad(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);
    private const string Clave = "ClaveSegura1!";
    private const string Nombre = "Lucia Fernandez";
    private const string Telefono = "699887766";

    [Fact]
    public async Task Las_columnas_personales_de_asp_net_users_no_contienen_nada_en_claro()
    {
        string correo = $"lucia.{Guid.CreateVersion7():N}@prueba.es";
        Usuario usuario = await CrearUsuarioAsync(correo);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        List<string> columnas = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[email, normalized_email, user_name, normalized_user_name, nombre_completo, telefono]) AS "Value"
            FROM asp_net_users WHERE id = {usuario.Id}
            """).ToListAsync();

        columnas.Should().HaveCount(6).And.OnlyContain(valor => !string.IsNullOrEmpty(valor));
        foreach (string valor in columnas)
        {
            valor.Should().NotContainEquivalentOf("lucia")
                .And.NotContainEquivalentOf("prueba.es")
                .And.NotContain(Telefono);
        }
    }

    [Fact]
    public async Task Las_copias_de_la_reserva_van_cifradas_y_la_empresa_en_claro()
    {
        Usuario usuario = await CrearUsuarioAsync($"{Guid.CreateVersion7():N}@prueba.es");
        string sufijo = Guid.CreateVersion7().ToString("N");

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), $"Categoria {sufijo}", Slug.Desde($"cat-{sufijo}"));
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), "Barberia Centro", Slug.Desde($"empresa-{sufijo}"), categoria.Id,
            null, "contacto@barberia.es", "600000000", "Calle Mayor 1", "28001", "Salamanca", Ahora);
        Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, "Corte", 30, 15m, Ahora);
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, "Ana", Ahora);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio,
            Ahora.AddDays(1), Nombre, Telefono, null, Ahora);

        db.AddRange(categoria, empresa, servicio, profesional, reserva);
        await db.SaveChangesAsync();

        List<string> copias = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre_cliente, telefono_cliente]) AS "Value"
            FROM reservas WHERE id = {reserva.Id}
            """).ToListAsync();
        List<string> datosEmpresa = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre, ciudad]) AS "Value"
            FROM empresas WHERE id = {empresa.Id}
            """).ToListAsync();

        copias.Should().HaveCount(2);
        copias.Should().NotContain(valor => valor.Contains("Lucia") || valor.Contains(Telefono));
        datosEmpresa.Should().BeEquivalentTo("Barberia Centro", "Salamanca");
    }

    [Fact]
    public async Task Con_el_cifrado_activo_se_busca_por_correo_y_se_inicia_sesion_pero_no_por_linq()
    {
        string correo = $"{Guid.CreateVersion7():N}@prueba.es";
        await CrearUsuarioAsync(correo);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        SignInManager<Usuario> inicioSesion = ambito.ServiceProvider.GetRequiredService<SignInManager<Usuario>>();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        Usuario? encontrado = await usuarios.FindByEmailAsync(correo);
        SignInResult sesion = await inicioSesion.CheckPasswordSignInAsync(encontrado!, Clave, lockoutOnFailure: true);
        List<Usuario> porLinq = await db.Users.Where(u => u.Email == correo).ToListAsync();

        encontrado.Should().NotBeNull();
        encontrado!.NombreCompleto.Should().Be(Nombre);
        sesion.Succeeded.Should().BeTrue();
        porLinq.Should().BeEmpty();
    }

    private async Task<Usuario> CrearUsuarioAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = Usuario.Crear(correo, Nombre, Telefono, Ahora);

        (await usuarios.CreateAsync(usuario, Clave)).Succeeded.Should().BeTrue();
        return usuario;
    }
}
