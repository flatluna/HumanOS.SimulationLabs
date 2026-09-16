using HumanOS.SimulationLabs.Configurations;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Data;

public class SimulationLabsDbContext : DbContext
{
    public SimulationLabsDbContext(DbContextOptions<SimulationLabsDbContext> options)
        : base(options)
    {
    }

    public DbSet<LAB_Lab> Labs => Set<LAB_Lab>();
    public DbSet<LAB_LabVersion> LabVersions => Set<LAB_LabVersion>();
    public DbSet<LAB_Stage> Stages => Set<LAB_Stage>();
    public DbSet<LAB_Objective> Objectives => Set<LAB_Objective>();
    public DbSet<LAB_ExpectedMoment> ExpectedMoments => Set<LAB_ExpectedMoment>();
    public DbSet<LAB_Rubric> Rubrics => Set<LAB_Rubric>();
    public DbSet<LAB_RubricCriterion> RubricCriteria => Set<LAB_RubricCriterion>();

    public DbSet<LAB_TestedSkill> TestedSkills => Set<LAB_TestedSkill>();
    public DbSet<LAB_Scenario> Scenarios => Set<LAB_Scenario>();
    public DbSet<LAB_SimulatedActor> SimulatedActors => Set<LAB_SimulatedActor>();
    public DbSet<LAB_Attempt> Attempts => Set<LAB_Attempt>();
    public DbSet<LAB_ConversationTurn> ConversationTurns => Set<LAB_ConversationTurn>();
    public DbSet<LAB_UserAction> UserActions => Set<LAB_UserAction>();
    public DbSet<LAB_ArtifactSubmission> ArtifactSubmissions => Set<LAB_ArtifactSubmission>();
    public DbSet<LAB_Enrollment> Enrollments => Set<LAB_Enrollment>();
    public DbSet<LAB_AttemptEvaluation> AttemptEvaluations => Set<LAB_AttemptEvaluation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new LAB_LabConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_LabVersionConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_StageConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_ObjectiveConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_ExpectedMomentConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_RubricConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_RubricCriterionConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_TestedSkillConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_ScenarioConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_SimulatedActorConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_AttemptConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_ConversationTurnConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_UserActionConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_ArtifactSubmissionConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new LAB_AttemptEvaluationConfiguration());
    }
}
