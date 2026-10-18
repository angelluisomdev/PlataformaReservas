using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaReservas.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class RolClienteParaPropietarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO asp_net_user_roles (user_id, role_id)
                SELECT DISTINCT m.usuario_id, r.id
                FROM miembros_empresa m
                CROSS JOIN asp_net_roles r
                WHERE r.normalized_name = 'CLIENTE'
                ON CONFLICT DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atras: no se sabe que propietarios tenian ya el rol Cliente antes de esta migracion.
        }
    }
}
