using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public static class CondicionPublicable
{
    public static Expression<Func<Empresa, bool>> Aplica(ContextoDatos db)
    {
        return e => db.Servicios.Any(s =>
            s.EmpresaId == e.Id && s.Activo && db.Profesionales.Any(p =>
                p.EmpresaId == e.Id && p.Activo && p.Horarios.Any()
                && EF.Property<List<ProfesionalServicio>>(p, "_servicios").Any(ps => ps.ServicioId == s.Id)));
    }
}
