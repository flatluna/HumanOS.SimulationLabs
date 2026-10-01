using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class LoosenLabDefinitionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Attempt_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Attempt");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Attempt_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_Attempt");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_UserAction_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                table: "LAB_UserAction");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_MOM_IdExpectedMoment",
                table: "LAB_UserAction",
                column: "MOM_IdExpectedMoment");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_ACT_IdActor",
                table: "LAB_ConversationTurn",
                column: "ACT_IdActor");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn",
                column: "TRN_IdExpectedMoment");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_LAB_IdVersion",
                table: "LAB_Attempt",
                column: "LAB_IdVersion");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_SCN_IdScenario",
                table: "LAB_Attempt",
                column: "SCN_IdScenario");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission",
                column: "OBJ_IdObjective");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_STG_IdStage",
                table: "LAB_ArtifactSubmission",
                column: "STG_IdStage");

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Objective_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission",
                column: "OBJ_IdObjective",
                principalTable: "LAB_Objective",
                principalColumn: "OBJ_IdObjective",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Stage_STG_IdStage",
                table: "LAB_ArtifactSubmission",
                column: "STG_IdStage",
                principalTable: "LAB_Stage",
                principalColumn: "STG_IdStage",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Attempt_LAB_LabVersion_LAB_IdVersion",
                table: "LAB_Attempt",
                column: "LAB_IdVersion",
                principalTable: "LAB_LabVersion",
                principalColumn: "LAB_IdVersion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Attempt_LAB_Scenario_SCN_IdScenario",
                table: "LAB_Attempt",
                column: "SCN_IdScenario",
                principalTable: "LAB_Scenario",
                principalColumn: "SCN_IdScenario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_ExpectedMoment_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn",
                column: "TRN_IdExpectedMoment",
                principalTable: "LAB_ExpectedMoment",
                principalColumn: "MOM_IdExpectedMoment",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_SimulatedActor_ACT_IdActor",
                table: "LAB_ConversationTurn",
                column: "ACT_IdActor",
                principalTable: "LAB_SimulatedActor",
                principalColumn: "ACT_IdActor",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_UserAction_LAB_ExpectedMoment_MOM_IdExpectedMoment",
                table: "LAB_UserAction",
                column: "MOM_IdExpectedMoment",
                principalTable: "LAB_ExpectedMoment",
                principalColumn: "MOM_IdExpectedMoment",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Objective_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Stage_STG_IdStage",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Attempt_LAB_LabVersion_LAB_IdVersion",
                table: "LAB_Attempt");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Attempt_LAB_Scenario_SCN_IdScenario",
                table: "LAB_Attempt");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_ExpectedMoment_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_SimulatedActor_ACT_IdActor",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropForeignKey(
                name: "FK_LAB_UserAction_LAB_ExpectedMoment_MOM_IdExpectedMoment",
                table: "LAB_UserAction");

            migrationBuilder.DropIndex(
                name: "IX_LAB_UserAction_MOM_IdExpectedMoment",
                table: "LAB_UserAction");

            migrationBuilder.DropIndex(
                name: "IX_LAB_ConversationTurn_ACT_IdActor",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropIndex(
                name: "IX_LAB_ConversationTurn_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn");

            migrationBuilder.DropIndex(
                name: "IX_LAB_Attempt_LAB_IdVersion",
                table: "LAB_Attempt");

            migrationBuilder.DropIndex(
                name: "IX_LAB_Attempt_SCN_IdScenario",
                table: "LAB_Attempt");

            migrationBuilder.DropIndex(
                name: "IX_LAB_ArtifactSubmission_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.DropIndex(
                name: "IX_LAB_ArtifactSubmission_STG_IdStage",
                table: "LAB_ArtifactSubmission");

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" },
                principalTable: "LAB_Objective",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ArtifactSubmission_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" },
                principalTable: "LAB_Stage",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Attempt_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                principalTable: "LAB_LabVersion",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Attempt_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" },
                principalTable: "LAB_Scenario",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "TRN_IdExpectedMoment" },
                principalTable: "LAB_ExpectedMoment",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_ConversationTurn_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ACT_IdActor" },
                principalTable: "LAB_SimulatedActor",
                principalColumns: new[] { "SEG_IdTenant", "ACT_IdActor" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_UserAction_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                principalTable: "LAB_ExpectedMoment",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
