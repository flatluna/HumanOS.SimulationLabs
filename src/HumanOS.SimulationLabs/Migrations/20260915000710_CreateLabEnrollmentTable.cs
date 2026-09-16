using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabEnrollmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_Enrollment",
                columns: table => new
                {
                    ENR_IdEnrollment = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdLab = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USR_IdParticipant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ENR_Estatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ENROLLED"),
                    ENR_FechaInscripcion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ENR_FechaCancelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Enrollment", x => x.ENR_IdEnrollment);
                    table.CheckConstraint("CK_LAB_Enrollment_ENR_Estatus", "[ENR_Estatus] IN ('ENROLLED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_LAB_Enrollment_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdLab },
                        principalTable: "LAB_Lab",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdLab" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Enrollment_Tenant_Lab_Participant",
                table: "LAB_Enrollment",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab", "USR_IdParticipant" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Enrollment");
        }
    }
}
