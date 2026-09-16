using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_AttemptEvaluationConfiguration : IEntityTypeConfiguration<LAB_AttemptEvaluation>
{
    public void Configure(EntityTypeBuilder<LAB_AttemptEvaluation> builder)
    {
        builder.ToTable("LAB_AttemptEvaluation", t =>
        {
            t.HasCheckConstraint("CK_LAB_AttemptEvaluation_ScoreFinal", "[EVL_ScoreFinal] >= 1.00 AND [EVL_ScoreFinal] <= 10.00");
            t.HasCheckConstraint(
                "CK_LAB_AttemptEvaluation_Resultado",
                "[EVL_Resultado] IN ('PASSED', 'PARTIAL', 'REPEAT_RECOMMENDED', 'NOT_COMPLETED', 'CRITICAL_FAILURE')");
        });

        builder.HasKey(e => e.EVL_IdEvaluation);
        builder.Property(e => e.EVL_IdEvaluation).IsRequired().ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant).IsRequired();
        builder.Property(e => e.ATT_IdAttempt).IsRequired();

        // One evaluation per attempt — re-evaluating replaces the existing row.
        builder.HasIndex(e => new { e.SEG_IdTenant, e.ATT_IdAttempt })
            .IsUnique()
            .HasDatabaseName("UQ_LAB_AttemptEvaluation_Tenant_Attempt");

        builder.Property(e => e.EVL_ScoreFinal).HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.EVL_Resultado).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(e => e.EVL_Feedback).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_StrengthsJson).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_GapsJson).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_RecommendedSkillsJson).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_CriteriaScoresJson).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_TurnEvaluationsJson).IsUnicode(true).IsRequired();
        builder.Property(e => e.EVL_GeneratedModel).HasMaxLength(100).IsUnicode(true).IsRequired(false);

        builder.Property(e => e.FechaCreacion).IsRequired();
        builder.Property(e => e.CreadoPor).HasMaxLength(100).IsUnicode(true).IsRequired();
    }
}
