using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasCatalogoPublico(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_las_categorias_activas_ordenadas_por_nombre_sin_contexto_de_empresa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);

        IReadOnlyList<CategoriaDto> categorias = await ObtenerCategoriasAsync();

        categorias.Should().Contain(new CategoriaDto(categoria.Id, categoria.Nombre.Valor, categoria.Slug.Valor));
        categorias.Select(c => c.Nombre).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task Una_categoria_inactiva_no_se_devuelve()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        await EjecutarSqlAsync($"UPDATE categorias SET activa = false WHERE id = {categoria.Id}");

        IReadOnlyList<CategoriaDto> categorias = await ObtenerCategoriasAsync();

        categorias.Should().NotContain(c => c.Id == categoria.Id);
    }

    [Fact]
    public async Task Solo_devuelve_empresas_publicables_de_esa_ciudad()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa buscada = await CrearPublicableAsync(categoria.Id, ciudad);
        await CrearPublicableAsync(categoria.Id, CiudadUnica());

        PaginaResultados<ResumenEmpresaDto> pagina = await BuscarAsync(ciudad);

        pagina.Elementos.Should().ContainSingle().Which.Slug.Should().Be(buscada.Slug.Valor);
        pagina.Total.Should().Be(1);
    }

    [Fact]
    public async Task Una_ciudad_de_dos_caracteres_encuentra_sus_empresas()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa enEa = await CrearPublicableAsync(categoria.Id, "Ea");

        PaginaResultados<ResumenEmpresaDto> pagina = await BuscarAsync("Ea", categoria.Slug.Valor);

        pagina.Elementos.Should().ContainSingle().Which.Slug.Should().Be(enEa.Slug.Valor);
    }

    [Fact]
    public async Task La_ciudad_se_compara_sin_mayusculas_ni_acentos_y_completa()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string sufijo = DominioPrueba.Sufijo();
        Empresa enMalaga = await CrearPublicableAsync(categoria.Id, $"Málaga {sufijo}");

        (await BuscarAsync($"malaga {sufijo}")).Elementos.Should().ContainSingle().Which.Slug.Should().Be(enMalaga.Slug.Valor);
        (await BuscarAsync($"MÁLAGA {sufijo.ToUpperInvariant()}")).Elementos.Should().ContainSingle();
        (await BuscarAsync($"Mala {sufijo}")).Elementos.Should().BeEmpty();
    }

    [Fact]
    public async Task La_categoria_refina_el_resultado_dentro_de_la_ciudad()
    {
        Categoria barberia = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Categoria estetica = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa deBarberia = await CrearPublicableAsync(barberia.Id, ciudad);
        await CrearPublicableAsync(estetica.Id, ciudad);

        (await BuscarAsync(ciudad)).Total.Should().Be(2);
        PaginaResultados<ResumenEmpresaDto> refinada = await BuscarAsync(ciudad, barberia.Slug.Valor);
        refinada.Elementos.Should().ContainSingle().Which.Should().Be(
            new ResumenEmpresaDto("Barberia Centro", deBarberia.Slug.Valor, barberia.Nombre.Valor, ciudad, null));
        (await BuscarAsync(ciudad, "categoria-que-no-existe")).Elementos.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_categoria_inactiva_no_filtra_resultados_y_devuelve_vacio()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        await CrearPublicableAsync(categoria.Id, ciudad);
        await EjecutarSqlAsync($"UPDATE categorias SET activa = false WHERE id = {categoria.Id}");

        (await BuscarAsync(ciudad, categoria.Slug.Valor)).Elementos.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_empresa_inactiva_no_aparece_ni_por_slug_y_reactivada_vuelve()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa empresa = await CrearPublicableAsync(categoria.Id, ciudad);

        await EjecutarSqlAsync($"UPDATE empresas SET activa = false WHERE id = {empresa.Id}");
        (await BuscarAsync(ciudad)).Elementos.Should().BeEmpty();
        (await FichaAsync(empresa.Slug.Valor)).Should().BeNull();

        await EjecutarSqlAsync($"UPDATE empresas SET activa = true WHERE id = {empresa.Id}");
        (await BuscarAsync(ciudad)).Elementos.Should().ContainSingle();
        (await FichaAsync(empresa.Slug.Valor)).Should().NotBeNull();
    }

    [Fact]
    public async Task Una_empresa_solo_es_visible_mientras_es_publicable()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id, ciudad: new Ciudad(ciudad));
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa);

        (await BuscarAsync(ciudad)).Elementos.Should().BeEmpty();
        (await FichaAsync(empresa.Slug.Valor)).Should().BeNull();

        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional);
        (await BuscarAsync(ciudad)).Elementos.Should().ContainSingle();
        (await FichaAsync(empresa.Slug.Valor)).Should().NotBeNull();

        await EjecutarSqlAsync($"UPDATE profesionales SET activo = false WHERE id = {profesional.Id}");
        (await BuscarAsync(ciudad)).Elementos.Should().BeEmpty();
        (await FichaAsync(empresa.Slug.Valor)).Should().BeNull();
    }

    [Fact]
    public async Task Sin_un_camino_completo_servicio_profesional_horario_no_es_publicable()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id, ciudad: new Ciudad(ciudad));
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional conHorarioSinServicio = DominioPrueba.Profesional(empresa.Id);
        conHorarioSinServicio.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        Profesional conServicioSinHorario = DominioPrueba.Profesional(empresa.Id);
        conServicioSinHorario.AsociarServicio(servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, servicio, conHorarioSinServicio, conServicioSinHorario);

        (await BuscarAsync(ciudad)).Elementos.Should().BeEmpty();
        (await FichaAsync(empresa.Slug.Valor)).Should().BeNull();
    }

    [Fact]
    public async Task Pagina_de_veinte_en_veinte_con_el_total_y_nunca_mas_de_veinte()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        for (int i = 1; i <= 25; i++)
        {
            await CrearPublicableAsync(categoria.Id, ciudad, $"Empresa {i:00}");
        }

        PaginaResultados<ResumenEmpresaDto> primera = await BuscarAsync(ciudad);
        PaginaResultados<ResumenEmpresaDto> segunda = await BuscarAsync(ciudad, pagina: 2);
        PaginaResultados<ResumenEmpresaDto> forzada = await BuscarAsync(ciudad, tamano: 100);

        primera.Elementos.Should().HaveCount(20);
        primera.Total.Should().Be(25);
        segunda.Elementos.Should().HaveCount(5);
        segunda.Elementos.Select(e => e.Nombre).Should().Equal("Empresa 21", "Empresa 22", "Empresa 23", "Empresa 24", "Empresa 25");
        forzada.Elementos.Should().HaveCount(20);
        forzada.TamanoPagina.Should().Be(20);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public async Task Una_pagina_fuera_de_rango_devuelve_vacio_con_el_total_real(int pagina)
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        for (int i = 1; i <= 25; i++)
        {
            await CrearPublicableAsync(categoria.Id, ciudad, $"Empresa {i:00}");
        }

        PaginaResultados<ResumenEmpresaDto> fuera = await BuscarAsync(ciudad, pagina: pagina);

        fuera.Elementos.Should().BeEmpty();
        fuera.Total.Should().Be(25);
        fuera.Pagina.Should().Be(pagina);
    }

    [Fact]
    public async Task Ordena_por_nombre_y_a_igual_nombre_por_slug()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        await CrearPublicableAsync(categoria.Id, ciudad, "Zeta");
        Empresa alfaUno = await CrearPublicableAsync(categoria.Id, ciudad, "Alfa");
        Empresa alfaDos = await CrearPublicableAsync(categoria.Id, ciudad, "Alfa");

        IReadOnlyList<ResumenEmpresaDto> elementos = (await BuscarAsync(ciudad)).Elementos;

        elementos.Select(e => e.Nombre).Should().Equal("Alfa", "Alfa", "Zeta");
        elementos.Take(2).Select(e => e.Slug).Should().BeInAscendingOrder(StringComparer.Ordinal)
            .And.BeEquivalentTo([alfaUno.Slug.Valor, alfaDos.Slug.Valor]);
    }

    [Fact]
    public async Task La_ficha_solo_muestra_lo_activo_sin_datos_personales_y_une_el_horario()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = DominioPrueba.Empresa(
            DominioPrueba.Sufijo(), categoria.Id, new DescripcionEmpresa("<script>alert(1)</script>"), ciudad: new Ciudad(CiudadUnica()));
        Servicio corte = DominioPrueba.Servicio(empresa.Id);
        Servicio retirado = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Retirado"), new Duracion(30), new Precio(10m), DominioPrueba.Ahora);
        retirado.Desactivar(DominioPrueba.Ahora);
        Profesional ana = EscenarioReserva.ProfesionalQuePresta(empresa.Id, corte.Id);
        Profesional luis = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona("Luis Perez"), DominioPrueba.Ahora);
        luis.ActualizarDatos(new NombrePersona("Luis Perez"), new Email("luis@prueba.es"), new Telefono("600111222"));
        luis.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(14, 0), new TimeOnly(18, 0)));
        Profesional baja = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona("Baja Ruiz"), DominioPrueba.Ahora);
        baja.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Tuesday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        baja.Desactivar();
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, corte, retirado, ana, luis, baja);

        FichaEmpresaDto? ficha = await FichaAsync(empresa.Slug.Valor);

        ficha.Should().NotBeNull();
        ficha!.Categoria.Should().Be(categoria.Nombre.Valor);
        ficha.Descripcion.Should().Be("<script>alert(1)</script>");
        ficha.Servicios.Select(s => s.Nombre).Should().Equal("Corte y barba");
        ficha.Profesionales.Should().Equal(new ProfesionalFichaDto("Ana Garcia"), new ProfesionalFichaDto("Luis Perez"));
        ficha.Horario.Single(d => d.Dia == DayOfWeek.Monday).Intervalos
            .Should().Equal(new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(18, 0)));
        ficha.Horario.Single(d => d.Dia == DayOfWeek.Saturday).Intervalos
            .Should().Equal(new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        ficha.Horario.Single(d => d.Dia == DayOfWeek.Tuesday).Cerrado.Should().BeTrue();
    }

    [Theory]
    [InlineData("no-existe-este-slug")]
    [InlineData("x")]
    [InlineData("Con Mayusculas")]
    public async Task La_ficha_de_un_slug_inexistente_o_mal_formado_es_nula(string slug)
    {
        (await FichaAsync(slug)).Should().BeNull();
    }

    [Fact]
    public async Task La_ciudad_se_guarda_en_claro_para_poder_buscarla()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        string ciudad = CiudadUnica();
        Empresa empresa = await CrearPublicableAsync(categoria.Id, ciudad);

        string guardada = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Database
            .SqlQuery<string>($"SELECT ciudad AS \"Value\" FROM empresas WHERE id = {empresa.Id}")
            .SingleAsync());

        guardada.Should().Be(ciudad);
        (await BuscarAsync(ciudad)).Elementos.Should().ContainSingle();
    }

    [Fact]
    public async Task Los_datos_para_reservar_solo_llevan_servicios_y_profesionales_activos_y_asociados()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = await CrearPublicableAsync(categoria.Id, CiudadUnica());
        Servicio activo = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Servicios.SingleAsync(s => s.EmpresaId == empresa.Id && s.Activo));
        Profesional publicado = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Profesionales.SingleAsync(p => p.EmpresaId == empresa.Id && p.Activo));
        Servicio inactivo = DominioPrueba.Servicio(empresa.Id);
        inactivo.Desactivar(DominioPrueba.Ahora);
        Profesional profesionalInactivo = EscenarioReserva.ProfesionalQuePresta(empresa.Id, inactivo.Id);
        profesionalInactivo.AsociarServicio(activo.Id);
        profesionalInactivo.Desactivar();
        Profesional sinAsociar = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, inactivo, profesionalInactivo, sinAsociar);

        DatosReservaDto? datos = await DatosReservaAsync(empresa.Slug.Valor);

        datos.Should().NotBeNull();
        datos!.EmpresaId.Should().Be(empresa.Id);
        datos.Slug.Should().Be(empresa.Slug.Valor);
        datos.Servicios.Should().ContainSingle().Which.Id.Should().Be(activo.Id);
        datos.Servicios[0].Profesionales.Should().ContainSingle().Which.Id.Should().Be(publicado.Id);
    }

    [Fact]
    public async Task Los_datos_para_reservar_de_una_empresa_inactiva_o_no_publicable_son_nulos()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa inactiva = await CrearPublicableAsync(categoria.Id, CiudadUnica());
        await EjecutarSqlAsync($"UPDATE empresas SET activa = false WHERE id = {inactiva.Id}");
        Empresa noPublicable = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id);
        Servicio sinProfesional = DominioPrueba.Servicio(noPublicable.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, noPublicable, sinProfesional);

        (await DatosReservaAsync(inactiva.Slug.Valor)).Should().BeNull();
        (await DatosReservaAsync(noPublicable.Slug.Valor)).Should().BeNull();
    }

    [Theory]
    [InlineData("no-existe-este-slug")]
    [InlineData("Mal Formado")]
    [InlineData("")]
    public async Task Los_datos_para_reservar_de_un_slug_inexistente_o_mal_formado_son_nulos(string slug)
    {
        (await DatosReservaAsync(slug)).Should().BeNull();
    }

    [Fact]
    public async Task La_ventana_del_servicio_sobrescribe_la_de_la_empresa_y_sin_ella_se_hereda()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id);
        empresa.CambiarPoliticas(new PoliticasReserva(15, 60, 30), DominioPrueba.Ahora);
        Servicio heredado = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Afeitado"), new Duracion(30), new Precio(10m), DominioPrueba.Ahora);
        Servicio propio = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Barba"), new Duracion(30), new Precio(10m), DominioPrueba.Ahora);
        propio.EstablecerPoliticas(null, null, new AntelacionMaximaReserva(7), DominioPrueba.Ahora);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, heredado.Id);
        profesional.AsociarServicio(propio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, heredado, propio, profesional);

        DatosReservaDto datos = (await DatosReservaAsync(empresa.Slug.Valor))!;

        datos.Servicios.Single(s => s.Id == heredado.Id).AntelacionMaximaDias.Should().Be(30);
        datos.Servicios.Single(s => s.Id == propio.Id).AntelacionMaximaDias.Should().Be(7);
    }

    private static string CiudadUnica() => $"Ciudad {DominioPrueba.Sufijo()}";

    private async Task<DatosReservaDto?> DatosReservaAsync(string slug)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IConsultasReservaPublica>()
            .ObtenerDatosReservaAsync(slug, CancellationToken.None);
    }

    private async Task<Empresa> CrearPublicableAsync(Guid categoriaId, string ciudad, string nombre = "Barberia Centro")
    {
        Empresa empresa = DominioPrueba.Empresa(
            DominioPrueba.Sufijo(), categoriaId, nombre: new NombreEmpresa(nombre), ciudad: new Ciudad(ciudad));
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = EscenarioReserva.ProfesionalQuePresta(empresa.Id, servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, empresa, servicio, profesional);
        return empresa;
    }

    private async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync()
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>()
            .ObtenerCategoriasAsync(CancellationToken.None);
    }

    private async Task<PaginaResultados<ResumenEmpresaDto>> BuscarAsync(
        string ciudad, string? categoria = null, int pagina = 1, int tamano = 20)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>()
            .BuscarAsync(ciudad, categoria, pagina, tamano, CancellationToken.None);
    }

    private async Task<FichaEmpresaDto?> FichaAsync(string slug)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>()
            .ObtenerFichaAsync(slug, CancellationToken.None);
    }

    private async Task EjecutarSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(sql);
    }
}
