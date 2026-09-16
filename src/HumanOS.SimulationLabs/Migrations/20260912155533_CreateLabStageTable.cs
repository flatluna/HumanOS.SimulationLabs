using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabStageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_LabVersion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateTable(
                name: "LAB_Stage",
                columns: table => new
                {
                    STG_IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STG_Codigo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    STG_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    STG_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STG_Orden = table.Column<int>(type: "int", nullable: false),
                    STG_TipoInteraccion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    STG_EsObligatorio = table.Column<bool>(type: "bit", nullable: false),
                    STG_CondicionCompletitud = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    STG_TiempoSugeridoMinutos = table.Column<int>(type: "int", nullable: true),
                    STG_PermiteOrdenFlexible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    STG_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Stage", x => x.STG_IdStage);
                    table.CheckConstraint("CK_LAB_Stage_STG_Estatus", "[STG_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("CK_LAB_Stage_STG_Orden", "[STG_Orden] > 0");
                    table.CheckConstraint("CK_LAB_Stage_STG_TiempoSugeridoMinutos", "([STG_TiempoSugeridoMinutos] IS NULL) OR ([STG_TiempoSugeridoMinutos] > 0)");
                    table.CheckConstraint("CK_LAB_Stage_STG_TipoInteraccion", "[STG_TipoInteraccion] IN ('CONVERSATION', 'DESKTOP', 'HYBRID', 'ARTIFACT')");
                    table.ForeignKey(
                        name: "FK_LAB_Stage_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_EsObligatorio",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_EsObligatorio" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Estatus",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Codigo",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Orden",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Stage");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_LabVersion");
        }
    }
}
