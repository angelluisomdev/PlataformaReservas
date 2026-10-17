using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaReservas.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CiudadSinAcentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE COLLATION IF NOT EXISTS ciudad_sin_acentos (provider = icu, locale = 'und-u-ks-level1', deterministic = false);");

            migrationBuilder.AlterColumn<string>(
                name: "ciudad",
                table: "empresas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                collation: "ciudad_sin_acentos",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ciudad",
                table: "empresas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldCollation: "ciudad_sin_acentos");

            migrationBuilder.Sql("DROP COLLATION IF EXISTS ciudad_sin_acentos;");
        }
    }
}
