using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class AddLabGenerationCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GEN_CachedInputTokens",
                table: "LAB_Lab",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GEN_ElapsedMilliseconds",
                table: "LAB_Lab",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GEN_EstimatedCostUsd",
                table: "LAB_Lab",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GEN_InputTokens",
                table: "LAB_Lab",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GEN_ModelName",
                table: "LAB_Lab",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GEN_OutputTokens",
                table: "LAB_Lab",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GEN_CachedInputTokens",
                table: "LAB_Lab");

            migrationBuilder.DropColumn(
                name: "GEN_ElapsedMilliseconds",
                table: "LAB_Lab");

            migrationBuilder.DropColumn(
                name: "GEN_EstimatedCostUsd",
                table: "LAB_Lab");

            migrationBuilder.DropColumn(
                name: "GEN_InputTokens",
                table: "LAB_Lab");

            migrationBuilder.DropColumn(
                name: "GEN_ModelName",
                table: "LAB_Lab");

            migrationBuilder.DropColumn(
                name: "GEN_OutputTokens",
                table: "LAB_Lab");
        }
    }
}
