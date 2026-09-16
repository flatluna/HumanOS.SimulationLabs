using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                table: "LAB_Lab",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab" });

            migrationBuilder.CreateTable(
                name: "LAB_LabVersion",
                columns: table => new
                {
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdLab = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_NumeroVersion = table.Column<int>(type: "int", nullable: false),
                    LAB_ObjetivoGeneral = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LAB_InstruccionesParticipante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LAB_BriefOculto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LAB_DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    LAB_ScoreMinimo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LAB_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    LAB_VigenciaDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LAB_VigenciaHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LAB_HashConfiguracion = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_LabVersion", x => x.LAB_IdVersion);
                    table.CheckConstraint("CK_LAB_LabVersion_LAB_DuracionMinutos", "[LAB_DuracionMinutos] > 0");
                    table.CheckConstraint("CK_LAB_LabVersion_LAB_Estatus", "[LAB_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");
                    table.CheckConstraint("CK_LAB_LabVersion_LAB_HashConfiguracion", "([LAB_HashConfiguracion] IS NULL) OR (LEN([LAB_HashConfiguracion]) = 64)");
                    table.CheckConstraint("CK_LAB_LabVersion_LAB_NumeroVersion", "[LAB_NumeroVersion] > 0");
                    table.CheckConstraint("CK_LAB_LabVersion_LAB_ScoreMinimo", "[LAB_ScoreMinimo] >= 1.00 AND [LAB_ScoreMinimo] <= 10.00");
                    table.CheckConstraint("CK_LAB_LabVersion_Vigencia", "([LAB_VigenciaHasta] IS NULL) OR ([LAB_VigenciaDesde] IS NULL) OR ([LAB_VigenciaHasta] >= [LAB_VigenciaDesde])");
                    table.ForeignKey(
                        name: "FK_LAB_LabVersion_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdLab },
                        principalTable: "LAB_Lab",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdLab" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_LabVersion_LAB_IdLab",
                table: "LAB_LabVersion",
                column: "LAB_IdLab");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_LabVersion_SEG_IdTenant_LAB_IdLab",
                table: "LAB_LabVersion",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_LabVersion_SEG_IdTenant_LAB_IdLab_LAB_Estatus",
                table: "LAB_LabVersion",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab", "LAB_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_LabVersion_Tenant_Estatus_Vigencias",
                table: "LAB_LabVersion",
                columns: new[] { "SEG_IdTenant", "LAB_Estatus", "LAB_VigenciaDesde", "LAB_VigenciaHasta" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_LabVersion_SEG_IdTenant_LAB_IdLab_LAB_NumeroVersion",
                table: "LAB_LabVersion",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab", "LAB_NumeroVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_LabVersion");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                table: "LAB_Lab");
        }
    }
}
