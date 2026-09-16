using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_LabVersionConfiguration : IEntityTypeConfiguration<LAB_LabVersion>
{
    public void Configure(EntityTypeBuilder<LAB_LabVersion> builder)
    {
        builder.ToTable("LAB_LabVersion", t =>
        {
            // 1. LAB_NumeroVersion mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_LAB_NumeroVersion",
                "[LAB_NumeroVersion] > 0");

            // 2. LAB_DuracionMinutos mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_LAB_DuracionMinutos",
                "[LAB_DuracionMinutos] > 0");

            // 3. LAB_ScoreMinimo entre 1.00 y 10.00
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_LAB_ScoreMinimo",
                "[LAB_ScoreMinimo] >= 1.00 AND [LAB_ScoreMinimo] <= 10.00");

            // 4. LAB_Estatus debe pertenecer a DRAFT, APPROVED, PUBLISHED, RETIRED
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_LAB_Estatus",
                "[LAB_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");

            // 5. LAB_VigenciaHasta debe ser NULL o mayor o igual que LAB_VigenciaDesde
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_Vigencia",
                "([LAB_VigenciaHasta] IS NULL) OR ([LAB_VigenciaDesde] IS NULL) OR ([LAB_VigenciaHasta] >= [LAB_VigenciaDesde])");

            // 6. LAB_HashConfiguracion debe ser NULL o tener exactamente 64 caracteres
            t.HasCheckConstraint(
                "CK_LAB_LabVersion_LAB_HashConfiguracion",
                "([LAB_HashConfiguracion] IS NULL) OR (LEN([LAB_HashConfiguracion]) = 64)");
        });

        // 1. Primary Key
        builder.HasKey(e => e.LAB_IdVersion);

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired()
            .ValueGeneratedNever();

        // 2. LAB_IdLab
        builder.Property(e => e.LAB_IdLab)
            .IsRequired();

        // 3. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // 4. LAB_NumeroVersion
        builder.Property(e => e.LAB_NumeroVersion)
            .IsRequired();

        // 5. LAB_ObjetivoGeneral nvarchar(max)
        builder.Property(e => e.LAB_ObjetivoGeneral)
            .IsUnicode(true)
            .IsRequired();

        // 6. LAB_InstruccionesParticipante nvarchar(max)
        builder.Property(e => e.LAB_InstruccionesParticipante)
            .IsUnicode(true)
            .IsRequired();

        // 7. LAB_BriefOculto nvarchar(max)
        builder.Property(e => e.LAB_BriefOculto)
            .IsUnicode(true)
            .IsRequired(false);

        // 8. LAB_DuracionMinutos
        builder.Property(e => e.LAB_DuracionMinutos)
            .IsRequired();

        // 9. LAB_ScoreMinimo decimal(5,2)
        builder.Property(e => e.LAB_ScoreMinimo)
            .HasPrecision(5, 2)
            .IsRequired();

        // 10. LAB_Estatus varchar(30)
        builder.Property(e => e.LAB_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 11. LAB_VigenciaDesde datetime2
        builder.Property(e => e.LAB_VigenciaDesde)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 12. LAB_VigenciaHasta datetime2
        builder.Property(e => e.LAB_VigenciaHasta)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 13. LAB_HashConfiguracion char(64)
        builder.Property(e => e.LAB_HashConfiguracion)
            .HasMaxLength(64)
            .IsFixedLength(true)
            .IsUnicode(false)
            .IsRequired(false);

        // 14. FechaCreacion datetimeoffset
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // 15. CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // 16. FechaActualizacion datetimeoffset
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // 17. ActualizadoPor nvarchar(100)
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 18. RowVersion rowversion
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Clave alternativa (Alternate Key) para asegurar SEG_IdTenant al relacionar con LAB_Stage
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion })
            .HasName("AK_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion");

        // Foreign Key compuesta garantizando coincidencia de SEG_IdTenant
        builder.HasOne(v => v.Lab)
            .WithMany(l => l.Versiones)
            .HasPrincipalKey(l => new { l.SEG_IdTenant, l.LAB_IdLab })
            .HasForeignKey(v => new { v.SEG_IdTenant, v.LAB_IdLab })
            .HasConstraintName("FK_LAB_LabVersion_LAB_Lab_SEG_IdTenant_LAB_IdLab")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1:N hacia LAB_Stage
        builder.HasMany(v => v.Stages)
            .WithOne(s => s.LabVersion)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Stage_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1:N hacia LAB_Objective
        builder.HasMany(v => v.Objectives)
            .WithOne(o => o.LabVersion)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Objective_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // �NDICES
        // 1. �ndice �nico: SEG_IdTenant + LAB_IdLab + LAB_NumeroVersion
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdLab, e.LAB_NumeroVersion }, "UQ_LAB_LabVersion_SEG_IdTenant_LAB_IdLab_LAB_NumeroVersion")
            .IsUnique();

        // 2. �ndice no �nico: SEG_IdTenant + LAB_IdLab + LAB_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdLab, e.LAB_Estatus }, "IX_LAB_LabVersion_SEG_IdTenant_LAB_IdLab_LAB_Estatus");

        // 3. �ndice no �nico: SEG_IdTenant + LAB_Estatus + LAB_VigenciaDesde + LAB_VigenciaHasta
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_Estatus, e.LAB_VigenciaDesde, e.LAB_VigenciaHasta }, "IX_LAB_LabVersion_Tenant_Estatus_Vigencias");

        // 4. �ndice para la Foreign Key compuesta: SEG_IdTenant + LAB_IdLab
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdLab }, "IX_LAB_LabVersion_SEG_IdTenant_LAB_IdLab");

        // �ndice no �nico adicional en LAB_IdLab si se consulta directamente por id de lab
        builder.HasIndex(e => e.LAB_IdLab, "IX_LAB_LabVersion_LAB_IdLab");
    }
}
