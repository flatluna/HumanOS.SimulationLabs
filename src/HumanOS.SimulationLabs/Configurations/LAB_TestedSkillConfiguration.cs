using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_TestedSkillConfiguration : IEntityTypeConfiguration<LAB_TestedSkill>
{
    public void Configure(EntityTypeBuilder<LAB_TestedSkill> builder)
    {
        builder.ToTable("LAB_TestedSkill", t =>
        {
            t.HasCheckConstraint("CK_LAB_TestedSkill_SKL_Tipo", "[SKL_Tipo] IN ('TECHNICAL', 'SOFT')");
        });

        builder.HasKey(e => e.SKL_IdSkill);

        builder.Property(e => e.SKL_IdSkill)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.RUB_IdCriterion)
            .IsRequired(false);

        builder.Property(e => e.SKL_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.SKL_Tipo)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.SKL_RelevanceToRole)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.SKL_DemonstrationStandard)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.SKL_FeedbackGuidance)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Relación OBLIGATORIA con LAB_LabVersion: FK compuesta SEG_IdTenant + LAB_IdVersion
        builder.HasOne(e => e.LabVersion)
            .WithMany()
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion })
            .HasConstraintName("FK_LAB_TestedSkill_LAB_LabVersion_Tenant_Version")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación OPCIONAL con LAB_RubricCriterion: FK compuesta SEG_IdTenant + LAB_IdVersion + RUB_IdCriterion
        builder.HasOne(e => e.Criterion)
            .WithMany()
            .HasPrincipalKey(c => new { c.SEG_IdTenant, c.LAB_IdVersion, c.CRT_IdCriterion })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdCriterion })
            .HasConstraintName("FK_LAB_TestedSkill_LAB_RubricCriterion_Tenant_Version_Criterion")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion }, "IX_LAB_TestedSkill_Tenant_Version");
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdCriterion }, "IX_LAB_TestedSkill_Tenant_Version_Criterion");
    }
}
