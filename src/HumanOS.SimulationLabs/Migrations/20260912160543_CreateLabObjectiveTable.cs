using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabObjectiveTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                table: "LAB_Stage",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" });

            migrationBuilder.CreateTable(
                name: "LAB_Objective",
                columns: table => new
                {
                    OBJ_IdObjective = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STG_IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OBJ_Codigo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    OBJ_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OBJ_TipoEvidencia = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    OBJ_EsCritico = table.Column<bool>(type: "bit", nullable: false),
                    OBJ_Peso = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    OBJ_CondicionExito = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OBJ_Orden = table.Column<int>(type: "int", nullable: false),
                    OBJ_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Objective", x => x.OBJ_IdObjective);
                    table.CheckConstraint("CK_LAB_Objective_OBJ_Estatus", "[OBJ_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("CK_LAB_Objective_OBJ_Orden", "[OBJ_Orden] > 0");
                    table.CheckConstraint("CK_LAB_Objective_OBJ_Peso", "[OBJ_Peso] > 0");
                    table.CheckConstraint("CK_LAB_Objective_OBJ_TipoEvidencia", "[OBJ_TipoEvidencia] IN ('CONVERSATION', 'USER_ACTION', 'ARTIFACT', 'DECISION', 'SYSTEM_RESULT')");
                    table.ForeignKey(
                        name: "FK_LAB_Objective_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.STG_IdStage },
                        principalTable: "LAB_Stage",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_EsCritico",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_EsCritico" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_Estatus",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Objective_Tenant_Version_Stage_Orden",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage", "OBJ_Orden" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Objective_General_Tenant_Version_Orden",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_Orden" },
                unique: true,
                filter: "[STG_IdStage] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_Codigo",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Objective_Stage_Tenant_Version_Stage_Orden",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage", "OBJ_Orden" },
                unique: true,
                filter: "[STG_IdStage] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Objective");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                table: "LAB_Stage");
        }
    }
}
