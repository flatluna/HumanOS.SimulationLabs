using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabExpectedMomentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                table: "LAB_Objective",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" });

            migrationBuilder.CreateTable(
                name: "LAB_ExpectedMoment",
                columns: table => new
                {
                    MOM_IdExpectedMoment = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STG_IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OBJ_IdObjective = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MOM_Codigo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    MOM_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MOM_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MOM_Trigger = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MOM_IntencionEsperada = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MOM_RespuestaEjemplar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MOM_InformacionDescubrible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MOM_ErrorFrecuente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MOM_Recomendacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MOM_EsCritico = table.Column<bool>(type: "bit", nullable: false),
                    MOM_OrdenSugerido = table.Column<int>(type: "int", nullable: false),
                    MOM_PermiteOrdenFlexible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MOM_RequiereRespuesta = table.Column<bool>(type: "bit", nullable: false),
                    MOM_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_ExpectedMoment", x => x.MOM_IdExpectedMoment);
                    table.CheckConstraint("CK_LAB_ExpectedMoment_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_Codigo", "LEN(TRIM([MOM_Codigo])) > 0");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_Estatus", "[MOM_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_IntencionEsperada", "LEN(TRIM([MOM_IntencionEsperada])) > 0");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_Nombre", "LEN(TRIM([MOM_Nombre])) > 0");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_OrdenSugerido", "[MOM_OrdenSugerido] > 0");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_Tipo", "[MOM_Tipo] IN ('DIALOGUE', 'USER_ACTION', 'SYSTEM_EVENT', 'DECISION_POINT', 'ARTIFACT_REVIEW')");
                    table.CheckConstraint("CK_LAB_ExpectedMoment_MOM_Trigger", "LEN(TRIM([MOM_Trigger])) > 0");
                    table.ForeignKey(
                        name: "FK_LAB_ExpectedMoment_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ExpectedMoment_LAB_Objective_Tenant_Version_Objective",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.OBJ_IdObjective },
                        principalTable: "LAB_Objective",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ExpectedMoment_LAB_Stage_Tenant_Version_Stage",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.STG_IdStage },
                        principalTable: "LAB_Stage",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ExpectedMoment_Tenant_Version_Objective",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ExpectedMoment_Tenant_Version_Stage",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ExpectedMoment_Tenant_Version_Stage_EsCritico",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage", "MOM_EsCritico" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ExpectedMoment_Tenant_Version_Stage_Estatus",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage", "MOM_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_Codigo",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_ExpectedMoment_Tenant_Version_Stage_Orden",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage", "MOM_OrdenSugerido" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_ExpectedMoment");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                table: "LAB_Objective");
        }
    }
}
