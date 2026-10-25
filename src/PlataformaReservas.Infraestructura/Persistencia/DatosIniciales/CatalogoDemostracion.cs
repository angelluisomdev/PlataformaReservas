using System.Globalization;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;

internal static class CatalogoDemostracion
{
    public const string CorreoClientePrincipal = "cliente@demo.example";

    public static IReadOnlyList<ClienteDemostracion> Clientes { get; } =
    [
        new(CorreoClientePrincipal, "Lucia Fernandez", "600111222"),
        new("cliente01@demo.example", "Pablo Ortega", "600111223"),
        new("cliente02@demo.example", "Elena Castro", "600111224"),
        new("cliente03@demo.example", "Javier Molina", "600111225"),
        new("cliente04@demo.example", "Sara Delgado", "600111226"),
        new("cliente05@demo.example", "Diego Herrera", "600111227"),
        new("cliente06@demo.example", "Nuria Campos", "600111228"),
        new("cliente07@demo.example", "Adrian Vega", "600111229"),
        new("cliente08@demo.example", "Irene Prieto", "600111230"),
        new("cliente09@demo.example", "Hugo Navarro", "600111231"),
        new("cliente10@demo.example", "Claudia Rubio", "600111232"),
    ];

    private static readonly CiudadDemostracion[] _ciudades =
    [
        new("Madrid", "28013", "91", ["Calle de Alcalá", "Gran Vía", "Calle de Serrano", "Calle de Fuencarral"], ["peluqueria", "barberia", "estetica", "fisioterapia", "psicologia", "taller", "academia", "entrenamiento-personal"]),
        new("Barcelona", "08007", "93", ["Passeig de Gracia", "Carrer de Balmes", "Carrer d'Aragó", "Avinguda Diagonal"], ["peluqueria", "barberia", "estetica", "fisioterapia", "psicologia", "taller", "academia", "entrenamiento-personal"]),
        new("Valencia", "46002", "96", ["Calle de Colón", "Avenida del Puerto", "Calle de la Paz", "Gran Vía Marques del Turia"], ["peluqueria", "barberia", "estetica", "fisioterapia", "academia", "entrenamiento-personal"]),
        new("Sevilla", "41004", "95", ["Calle Sierpes", "Avenida de la Constitución", "Calle Feria", "Calle Asunción"], ["peluqueria", "estetica", "fisioterapia", "psicologia", "taller", "academia"]),
        new("Málaga", "29005", "952", ["Calle Larios", "Alameda Principal", "Calle Carretería", "Paseo de Reding"], ["peluqueria", "barberia", "estetica", "fisioterapia", "taller", "entrenamiento-personal"]),
        new("Zaragoza", "50004", "976", ["Paseo de la Independencia", "Calle Alfonso I", "Avenida de Goya", "Calle del Coso"], ["peluqueria", "barberia", "fisioterapia", "taller", "academia"]),
        new("Bilbao", "48008", "94", ["Gran Vía de Don Diego López de Haro", "Calle Ercilla", "Alameda de Recalde", "Calle Licenciado Poza"], ["peluqueria", "estetica", "psicologia", "academia", "entrenamiento-personal"]),
        new("Salamanca", "37002", "923", ["Calle Toro", "Gran Vía", "Calle Zamora", "Paseo de Canalejas"], ["peluqueria", "fisioterapia", "psicologia", "taller", "academia", "entrenamiento-personal"]),
        new("Valladolid", "47001", "983", ["Calle Santiago", "Paseo de Zorrilla", "Calle Miguel Íscar", "Calle Gamazo"], ["peluqueria", "barberia", "estetica", "fisioterapia", "taller"]),
        new("Granada", "18009", "958", ["Calle Recogidas", "Gran Vía de Colón", "Calle Reyes Católicos", "Camino de Ronda"], ["barberia", "estetica", "psicologia", "academia", "entrenamiento-personal"]),
    ];

    private static readonly Dictionary<string, CategoriaDemostracion> _categorias = new()
    {
        ["peluqueria"] = new(
            ["Peluquería Tijeras de Oro", "Salón Melena", "Peluquería Estilo Libre", "Corte y Color", "Peluquería Mechas", "Studio Cabello", "Peluquería La Onda", "Salón Brillo", "Peluquería Raíz", "Taller del Cabello"],
            "Salón de peluquería con cortes, color y tratamientos capilares para toda la familia.",
            [("Corte de mujer", 45, 22m), ("Corte de hombre", 30, 14m), ("Tinte completo", 60, 38m), ("Mechas", 60, 55m), ("Peinado", 30, 18m), ("Tratamiento de hidratación", 30, 20m)]),
        ["barberia"] = new(
            ["Barbería El Bigote", "Barbería Navaja", "The Barber Club", "Barbería Clásica", "Barbería Old School", "Barbería La Brocha", "Barbería del Puerto", "Barbería Patillas", "Barbería Fade", "Barbería Gentleman"],
            "Barbería tradicional: cortes clásicos, afeitado a navaja y arreglo de barba.",
            [("Corte de pelo", 30, 15m), ("Corte y barba", 45, 22m), ("Arreglo de barba", 20, 10m), ("Afeitado a navaja", 30, 16m), ("Corte infantil", 20, 11m), ("Degradado", 40, 18m)]),
        ["estetica"] = new(
            ["Centro Estético Armonía", "Estética Piel Sana", "Belleza Natural", "Estética Luna", "Centro Bienestar Aura", "Estética Esencia", "Espacio Belleza", "Estética Seda", "Centro Lumière", "Estética Flor de Loto"],
            "Centro de estética con tratamientos faciales, manicura, pedicura y depilación.",
            [("Limpieza facial", 60, 40m), ("Manicura", 45, 20m), ("Pedicura", 45, 25m), ("Depilación de piernas", 30, 22m), ("Masaje relajante", 60, 45m), ("Diseño de cejas", 15, 10m)]),
        ["fisioterapia"] = new(
            ["Fisio Activa", "Clínica Movimiento", "Fisioterapia Integral", "Centro Fisio Salud", "Fisio Deporte", "Clínica Espalda Sana", "Fisioterapia Equilibrio", "Centro Recupera", "Fisio Plus", "Clínica Articula"],
            "Clínica de fisioterapia: lesiones deportivas, dolor de espalda y rehabilitación.",
            [("Primera valoración", 45, 35m), ("Sesión de fisioterapia", 50, 40m), ("Masaje deportivo", 45, 38m), ("Punción seca", 30, 35m), ("Rehabilitación postoperatoria", 60, 50m), ("Drenaje linfatico", 50, 42m)]),
        ["psicologia"] = new(
            ["Centro de Psicología Mente", "Psicología Sentir", "Consulta Bienestar Emocional", "Espacio Calma", "Psicología Avanza", "Centro Equilibra", "Psicología Contigo", "Consulta Raices", "Psicología Despierta", "Centro Psique"],
            "Consulta de psicología para adultos, parejas y adolescentes.",
            [("Primera consulta", 50, 60m), ("Sesión individual", 50, 55m), ("Terapia de pareja", 60, 75m), ("Sesión online", 50, 50m), ("Orientación para adolescentes", 50, 55m)]),
        ["taller"] = new(
            ["Taller Mecánico Rueda", "Autoservicio Motor", "Taller El Pistón", "Taller Neumáticos Express", "Taller Revisión Total", "Taller Garaje Central", "Taller Mecánica Rápida", "Taller Motor Sur", "Taller El Embrague", "Taller Auto Norte"],
            "Taller mecánico: revisiones, cambio de aceite, neumáticos y diagnosis.",
            [("Cambio de aceite", 60, 45m), ("Revisión pre-ITV", 60, 35m), ("Cambio de neumáticos", 45, 30m), ("Diagnosis electrónica", 30, 40m), ("Cambio de pastillas de freno", 60, 70m), ("Revisión del aire acondicionado", 45, 50m)]),
        ["academia"] = new(
            ["Academia Sigma", "Academia Saber", "Centro de Estudios Delta", "Academia Logos", "Academia Idiomas Babel", "Academia Pitágoras", "Centro Formativo Atenea", "Academia Clave", "Academia Aprende", "Academia Horizonte"],
            "Academia de refuerzo escolar, idiomas y preparación de exámenes.",
            [("Clase de matemáticas", 60, 20m), ("Clase de ingles", 60, 22m), ("Preparación de selectividad", 60, 28m), ("Clase de física y química", 60, 22m), ("Conversacion en francés", 45, 18m), ("Clase de programación", 60, 30m)]),
        ["entrenamiento-personal"] = new(
            ["Entrena Fit", "Personal Trainer Pro", "Estudio Fuerza", "Entrenamiento Vital", "Box Funcional", "Estudio Pilates Core", "Entrena Más", "Estudio Movimiento", "Energía Training", "Estudio Cuerpo y Mente"],
            "Entrenamiento personal y en grupos reducidos: fuerza, pilates y pérdida de peso.",
            [("Entrenamiento personal", 60, 40m), ("Valoración inicial", 30, 20m), ("Pilates individual", 50, 35m), ("Sesión de fuerza", 45, 30m), ("Entrenamiento en pareja", 60, 55m), ("Estiramientos y movilidad", 30, 20m)]),
    };

    private static readonly string[] _profesionales =
    [
        "Ana Garcia", "Marta Ruiz", "Carlos Martin", "Laura Sanz", "Jorge Lopez", "Lucia Romero", "Daniel Torres", "Paula Gil",
        "Alvaro Moreno", "Cristina Serrano", "Sergio Blanco", "Patricia Ramos", "Raul Iglesias", "Beatriz Medina", "Ivan Cortes", "Silvia Garrido",
        "Ruben Castillo", "Rocio Santos", "Oscar Lozano", "Eva Guerrero", "Mario Cano", "Andrea Prieto", "Victor Mendez", "Alicia Cruz",
        "Pedro Calvo", "Natalia Vidal", "Fernando Leon", "Teresa Marquez", "Alberto Herrero", "Monica Pena", "Enrique Fuentes", "Pilar Carmona",
        "Guillermo Soto", "Raquel Pascual", "Hector Vargas", "Lorena Diez", "Ignacio Benitez", "Sonia Caballero", "Marcos Arias", "Veronica Rojas",
        "Joaquin Nieto", "Carmen Reyes", "Rafael Bravo", "Isabel Sanchez", "Tomas Aguilar", "Julia Montero", "Emilio Pardo", "Rosa Ibanez",
        "Gonzalo Vicente", "Noelia Crespo", "Ramon Duran", "Ines Ferrer", "Samuel Rios", "Marina Santana", "Agustin Lorenzo", "Celia Gallego",
        "Felipe Cabrera", "Aurora Mora", "Lorenzo Hidalgo", "Miriam Velasco", "Esteban Pastor", "Lidia Soler", "Rodrigo Esteban", "Olga Bermudez",
        "Martin Campos", "Ainhoa Echeverria", "Xavier Puig", "Montse Riera", "Unai Etxeberria", "Nerea Arregui", "Jordi Vila", "Nuria Soler",
        "Borja Salinas", "Carla Ortiz", "Pau Roca", "Elsa Quintana", "Iker Zubiri", "Alba Pons", "Nicolas Ferrero", "Valeria Cuesta",
    ];

    public static IReadOnlyList<EmpresaDemostracion> Empresas => _empresas ??= Generar();

    private static List<EmpresaDemostracion>? _empresas;

    private static List<EmpresaDemostracion> Generar()
    {
        List<EmpresaDemostracion> empresas =
        [
            new("Barbería Centro", "demo-barberia-centro", "barberia", null, "Calle Mayor 10", "37001", "Salamanca", "923000000",
                "propietario.barberia@demo.example", "Carlos Martin", [("Corte de pelo", 30, 15m), ("Corte y barba", 45, 22m)], ["Ana Garcia"]),
            new("Estética Norte", "demo-estetica-norte", "estetica", null, "Avenida de Portugal 25", "37005", "Salamanca", "923000001",
                "propietaria.estetica@demo.example", "Laura Sanz", [("Limpieza facial", 60, 40m), ("Manicura", 45, 20m)], ["Marta Ruiz"]),
        ];

        Dictionary<string, int> apariciones = [];
        int indice = 0;
        int siguienteProfesional = 4;
        foreach (CiudadDemostracion ciudad in _ciudades)
        {
            for (int c = 0; c < ciudad.Categorias.Length; c++)
            {
                string categoria = ciudad.Categorias[c];
                CategoriaDemostracion datos = _categorias[categoria];
                int aparicion = apariciones.GetValueOrDefault(categoria);
                apariciones[categoria] = aparicion + 1;

                int numeroServicios = 4 + (indice % 3);
                List<(string, int, decimal)> servicios = [];
                for (int s = 0; s < Math.Min(numeroServicios, datos.Servicios.Length); s++)
                {
                    servicios.Add(datos.Servicios[(aparicion + s) % datos.Servicios.Length]);
                }

                int numeroProfesionales = 3 + (indice % 3);
                List<string> profesionales = [];
                for (int p = 0; p < numeroProfesionales; p++)
                {
                    profesionales.Add(_profesionales[siguienteProfesional % _profesionales.Length]);
                    siguienteProfesional++;
                }

                string sufijo = $"{categoria}-{Slug.Desde(ciudad.Nombre).Valor}";
                empresas.Add(new(
                    datos.Nombres[aparicion % datos.Nombres.Length],
                    $"demo-{sufijo}",
                    categoria,
                    datos.Descripcion,
                    $"{ciudad.Calles[c % ciudad.Calles.Length]} {3 + (7 * aparicion) + c}",
                    ciudad.CodigoPostal,
                    ciudad.Nombre,
                    $"{ciudad.Prefijo}{(100000 + (indice * 7919)).ToString("D7", CultureInfo.InvariantCulture)[..(9 - ciudad.Prefijo.Length)]}",
                    $"propietario.{sufijo}@demo.example",
                    profesionales[0],
                    servicios,
                    profesionales));
                indice++;
            }
        }

        return empresas;
    }

    private sealed record CiudadDemostracion(string Nombre, string CodigoPostal, string Prefijo, string[] Calles, string[] Categorias);

    private sealed record CategoriaDemostracion(string[] Nombres, string Descripcion, (string Nombre, int Minutos, decimal Precio)[] Servicios);
}

internal sealed record EmpresaDemostracion(
    string Nombre,
    string Slug,
    string Categoria,
    string? Descripcion,
    string Direccion,
    string CodigoPostal,
    string Ciudad,
    string Telefono,
    string CorreoPropietario,
    string NombrePropietario,
    IReadOnlyList<(string Nombre, int Minutos, decimal Precio)> Servicios,
    IReadOnlyList<string> Profesionales);

internal sealed record ClienteDemostracion(string Correo, string Nombre, string Telefono);
