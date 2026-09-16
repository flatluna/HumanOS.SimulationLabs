using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_Lab",
                columns: table => new
                {
                    LAB_IdLab = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_Codigo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    LAB_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LAB_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LAB_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    LAB_Dominio = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    LAB_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    LAB_OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Lab", x => x.LAB_IdLab);
                    table.CheckConstraint("CK_LAB_Lab_LAB_Estatus", "[LAB_Estatus] IN ('DRAFT', 'PUBLISHED', 'RETIRED')");
                    table.CheckConstraint("CK_LAB_Lab_LAB_Tipo", "[LAB_Tipo] IN ('CONVERSATIONAL', 'DESKTOP', 'HYBRID')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Lab_SEG_IdTenant_LAB_Estatus",
                table: "LAB_Lab",
                columns: new[] { "SEG_IdTenant", "LAB_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Lab_SEG_IdTenant_LAB_Codigo",
                table: "LAB_Lab",
                columns: new[] { "SEG_IdTenant", "LAB_Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Lab");
        }
    }
}
