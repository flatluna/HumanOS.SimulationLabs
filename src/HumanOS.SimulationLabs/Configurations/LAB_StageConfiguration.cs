using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_StageConfiguration : IEntityTypeConfiguration<LAB_Stage>
{
    public void Configure(EntityTypeBuilder<LAB_Stage> builder)
    {
        builder.ToTable("LAB_Stage", t =>
        {
            // 1. STG_Orden debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Stage_STG_Orden",
                "[STG_Orden] > 0");

            // 2. STG_TiempoSugeridoMinutos debe ser NULL o mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Stage_STG_TiempoSugeridoMinutos",
                "([STG_TiempoSugeridoMinutos] IS NULL) OR ([STG_TiempoSugeridoMinutos] > 0)");

            // 3. STG_TipoInteraccion solo acepta: CONVERSATION, DESKTOP, HYBRID, ARTIFACT
            t.HasCheckConstraint(
                "CK_LAB_Stage_STG_TipoInteraccion",
                "[STG_TipoInteraccion] IN ('CONVERSATION', 'DESKTOP', 'HYBRID', 'ARTIFACT')");

            // 4. STG_Estatus solo acepta: DRAFT, ACTIVE, INACTIVE
            t.HasCheckConstraint(
                "CK_LAB_Stage_STG_Estatus",
                "[STG_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
        });

        // 1. STG_IdStage - Primary Key
        builder.HasKey(e => e.STG_IdStage);

        builder.Property(e => e.STG_IdStage)
            .IsRequired()
            .ValueGeneratedNever();

        // 2. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // 3. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // 4. STG_Codigo varchar(50)
        builder.Property(e => e.STG_Codigo)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        // 5. STG_Nombre nvarchar(200)
        builder.Property(e => e.STG_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // 6. STG_Descripcion nvarchar(max)
        builder.Property(e => e.STG_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // 7. STG_Orden int
        builder.Property(e => e.STG_Orden)
            .IsRequired();

        // 8. STG_TipoInteraccion varchar(30)
        builder.Property(e => e.STG_TipoInteraccion)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 9. STG_EsObligatorio bit
        builder.Property(e => e.STG_EsObligatorio)
            .IsRequired();

        // 10. STG_CondicionCompletitud nvarchar(max)
        builder.Property(e => e.STG_CondicionCompletitud)
            .IsUnicode(true)
            .IsRequired(false);

        // 11. STG_TiempoSugeridoMinutos int
        builder.Property(e => e.STG_TiempoSugeridoMinutos)
            .IsRequired(false);

        // 12. STG_PermiteOrdenFlexible bit
        builder.Property(e => e.STG_PermiteOrdenFlexible)
            .HasDefaultValue(false)
            .IsRequired();

        // 13. STG_Estatus varchar(30)
        builder.Property(e => e.STG_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(StageEstatus.Draft)
            .IsRequired();

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

        // Clave alternativa (Alternate Key) para asegurar SEG_IdTenant + LAB_IdVersion al relacionar con LAB_Objective
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage })
            .HasName("AK_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage");

        // Relación con LAB_LabVersion: Foreign Key compuesta SEG_IdTenant + LAB_IdVersion
        builder.HasOne(s => s.LabVersion)
            .WithMany(v => v.Stages)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Stage_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1:N hacia LAB_Objective (opcional desde Objective)
        builder.HasMany(s => s.Objectives)
            .WithOne(o => o.Stage)
            .HasPrincipalKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion, s.STG_IdStage })
            .HasForeignKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion, o.STG_IdStage })
            .HasConstraintName("FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage")
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + LAB_IdVersion + STG_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_Codigo }, "UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Codigo")
            .IsUnique();

        // 2. Índice único: SEG_IdTenant + LAB_IdVersion + STG_Orden
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_Orden }, "UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Orden")
            .IsUnique();

        // 3. Índice no único: SEG_IdTenant + LAB_IdVersion + STG_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_Estatus }, "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Estatus");

        // 4. Índice para la Foreign Key compuesta: SEG_IdTenant + LAB_IdVersion
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion }, "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion");

        // 5. Índice no único para consultar etapas obligatorias: SEG_IdTenant + LAB_IdVersion + STG_EsObligatorio
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_EsObligatorio }, "IX_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_EsObligatorio");
    }
}
