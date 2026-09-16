using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_ObjectiveConfiguration : IEntityTypeConfiguration<LAB_Objective>
{
    public void Configure(EntityTypeBuilder<LAB_Objective> builder)
    {
        builder.ToTable("LAB_Objective", t =>
        {
            // 1. OBJ_Peso debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Objective_OBJ_Peso",
                "[OBJ_Peso] > 0");

            // 2. OBJ_Orden debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Objective_OBJ_Orden",
                "[OBJ_Orden] > 0");

            // 3. OBJ_TipoEvidencia solo acepta: CONVERSATION, USER_ACTION, ARTIFACT, DECISION, SYSTEM_RESULT
            t.HasCheckConstraint(
                "CK_LAB_Objective_OBJ_TipoEvidencia",
                "[OBJ_TipoEvidencia] IN ('CONVERSATION', 'USER_ACTION', 'ARTIFACT', 'DECISION', 'SYSTEM_RESULT')");

            // 4. OBJ_Estatus solo acepta: DRAFT, ACTIVE, INACTIVE
            t.HasCheckConstraint(
                "CK_LAB_Objective_OBJ_Estatus",
                "[OBJ_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
        });

        // 1. OBJ_IdObjective - Primary Key
        builder.HasKey(e => e.OBJ_IdObjective);

        builder.Property(e => e.OBJ_IdObjective)
            .IsRequired()
            .ValueGeneratedNever();

        // 2. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // 3. STG_IdStage - Opcional
        builder.Property(e => e.STG_IdStage)
            .IsRequired(false);

        // 4. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // Clave alternativa para permitir FK compuesta desde LAB_ExpectedMoment (SEG_IdTenant, LAB_IdVersion, OBJ_IdObjective)
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_IdObjective })
            .HasName("AK_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective");

        // 5. OBJ_Codigo varchar(50)
        builder.Property(e => e.OBJ_Codigo)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        // 6. OBJ_Descripcion nvarchar(max)
        builder.Property(e => e.OBJ_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // 7. OBJ_TipoEvidencia varchar(30)
        builder.Property(e => e.OBJ_TipoEvidencia)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 8. OBJ_EsCritico bit
        builder.Property(e => e.OBJ_EsCritico)
            .IsRequired();

        // 9. OBJ_Peso decimal(7,4)
        builder.Property(e => e.OBJ_Peso)
            .HasPrecision(7, 4)
            .IsRequired();

        // 10. OBJ_CondicionExito nvarchar(max)
        builder.Property(e => e.OBJ_CondicionExito)
            .IsUnicode(true)
            .IsRequired();

        // 11. OBJ_Orden int
        builder.Property(e => e.OBJ_Orden)
            .IsRequired();

        // 12. OBJ_Estatus varchar(30)
        builder.Property(e => e.OBJ_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(ObjectiveEstatus.Draft)
            .IsRequired();

        // 13. FechaCreacion datetimeoffset
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // 14. CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // 15. FechaActualizacion datetimeoffset
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // 16. ActualizadoPor nvarchar(100)
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 17. RowVersion rowversion
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Relación OBLIGATORIA con LAB_LabVersion: Foreign Key compuesta SEG_IdTenant + LAB_IdVersion
        builder.HasOne(o => o.LabVersion)
            .WithMany(v => v.Objectives)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Objective_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación OPCIONAL con LAB_Stage: Foreign Key compuesta SEG_IdTenant + LAB_IdVersion + STG_IdStage
        builder.HasOne(o => o.Stage)
            .WithMany(s => s.Objectives)
            .HasPrincipalKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion, s.STG_IdStage })
            .HasForeignKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion, o.STG_IdStage })
            .HasConstraintName("FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES Y RESTRICCIONES ÚNICAS
        // 1. Índice único: SEG_IdTenant + LAB_IdVersion + OBJ_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_Codigo }, "UQ_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_Codigo")
            .IsUnique();

        // 2. Índice no único: SEG_IdTenant + LAB_IdVersion + OBJ_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_Estatus }, "IX_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_Estatus");

        // 3. Índice no único: SEG_IdTenant + LAB_IdVersion + OBJ_EsCritico
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_EsCritico }, "IX_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_EsCritico");

        // 4. Índice para consultar objetivos por etapa: SEG_IdTenant + LAB_IdVersion + STG_IdStage + OBJ_Orden
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage, e.OBJ_Orden }, "IX_LAB_Objective_Tenant_Version_Stage_Orden");

        // 5. Índice único filtrado para el orden de objetivos generales (STG_IdStage IS NULL)
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_Orden }, "UQ_LAB_Objective_General_Tenant_Version_Orden")
            .HasFilter("[STG_IdStage] IS NULL")
            .IsUnique();

        // 6. Índice único filtrado para el orden de objetivos dentro de una etapa (STG_IdStage IS NOT NULL)
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage, e.OBJ_Orden }, "UQ_LAB_Objective_Stage_Tenant_Version_Stage_Orden")
            .HasFilter("[STG_IdStage] IS NOT NULL")
            .IsUnique();
    }
}
