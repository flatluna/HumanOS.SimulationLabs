using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_EnrollmentConfiguration : IEntityTypeConfiguration<LAB_Enrollment>
{
    public void Configure(EntityTypeBuilder<LAB_Enrollment> builder)
    {
        builder.ToTable("LAB_Enrollment", t =>
        {
            t.HasCheckConstraint(
                "CK_LAB_Enrollment_ENR_Estatus",
                "[ENR_Estatus] IN ('ENROLLED', 'CANCELLED')");
        });

        builder.HasKey(e => e.ENR_IdEnrollment);

        builder.Property(e => e.ENR_IdEnrollment)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdLab)
            .IsRequired();

        builder.Property(e => e.USR_IdParticipant)
            .IsRequired();

        builder.Property(e => e.ENR_Estatus)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasDefaultValue(EnrollmentEstatus.Enrolled)
            .IsRequired();

        builder.Property(e => e.ENR_FechaInscripcion)
            .IsRequired();

        builder.Property(e => e.ENR_FechaCancelacion)
            .IsRequired(false);

        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // One enrollment row per (tenant, lab, participant) — re-enrolling after
        // cancelling just flips ENR_Estatus back to ENROLLED on the same row.
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdLab, e.USR_IdParticipant })
            .IsUnique()
            .HasDatabaseName("UQ_LAB_Enrollment_Tenant_Lab_Participant");

        builder.HasOne(e => e.Lab)
            .WithMany()
            .HasForeignKey(e => e.LAB_IdLab)
            .HasPrincipalKey(l => l.LAB_IdLab)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
