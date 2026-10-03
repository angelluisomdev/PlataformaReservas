using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaReservas.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class RolesIdentidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "asp_net_roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("0199a3c0-0000-7000-8000-000000000001"), "0199a3c0-0000-7000-8000-000000000001", "Cliente", "CLIENTE" },
                    { new Guid("0199a3c0-0000-7000-8000-000000000002"), "0199a3c0-0000-7000-8000-000000000002", "Propietario", "PROPIETARIO" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "asp_net_roles",
                keyColumn: "id",
                keyValue: new Guid("0199a3c0-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "asp_net_roles",
                keyColumn: "id",
                keyValue: new Guid("0199a3c0-0000-7000-8000-000000000002"));
        }
    }
}
