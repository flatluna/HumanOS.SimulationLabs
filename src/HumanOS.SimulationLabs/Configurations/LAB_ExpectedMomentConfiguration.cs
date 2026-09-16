using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_ExpectedMomentConfiguration : IEntityTypeConfiguration<LAB_ExpectedMoment>
{
    public void Configure(EntityTypeBuilder<LAB_ExpectedMoment> builder)
    {
        builder.ToTable("LAB_ExpectedMoment", t =>
        {
            // 1. MOM_OrdenSugerido debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_OrdenSugerido",
                "[MOM_OrdenSugerido] > 0");

            // 2. MOM_Tipo solo acepta: DIALOGUE, USER_ACTION, SYSTEM_EVENT, DECISION_POINT, ARTIFACT_REVIEW
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_Tipo",
                "[MOM_Tipo] IN ('DIALOGUE', 'USER_ACTION', 'SYSTEM_EVENT', 'DECISION_POINT', 'ARTIFACT_REVIEW')");

            // 3. MOM_Estatus solo acepta: DRAFT, ACTIVE, INACTIVE
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_Estatus",
                "[MOM_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");

            // 4. MOM_Codigo no debe estar vacío después de eliminar espacios
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_Codigo",
                "LEN(TRIM([MOM_Codigo])) > 0");

            // 5. MOM_Nombre no debe estar vacío después de eliminar espacios
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_Nombre",
                "LEN(TRIM([MOM_Nombre])) > 0");

            // 6. MOM_Trigger no debe estar vacío después de eliminar espacios
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_Trigger",
                "LEN(TRIM([MOM_Trigger])) > 0");

            // 7. MOM_IntencionEsperada no debe estar vacía después de eliminar espacios
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_MOM_IntencionEsperada",
                "LEN(TRIM([MOM_IntencionEsperada])) > 0");

            // 8. CreadoPor no debe estar vacío después de eliminar espacios
            t.HasCheckConstraint(
                "CK_LAB_ExpectedMoment_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");
        });

        // 1. MOM_IdExpectedMoment - Primary Key
        builder.HasKey(e => e.MOM_IdExpectedMoment);

        builder.Property(e => e.MOM_IdExpectedMoment)
            .IsRequired()
            .ValueGeneratedNever();

        // 2. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // 3. STG_IdStage
        builder.Property(e => e.STG_IdStage)
            .IsRequired();

        // 4. OBJ_IdObjective
        builder.Property(e => e.OBJ_IdObjective)
            .IsRequired(false);

        // 5. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // Clave alternativa (Alternate Key) para asegurar SEG_IdTenant + LAB_IdVersion al relacionar con LAB_RubricCriterion
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.MOM_IdExpectedMoment })
            .HasName("AK_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment");

        // 6. MOM_Codigo varchar(60)
        builder.Property(e => e.MOM_Codigo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired();

        // 7. MOM_Nombre nvarchar(200)
        builder.Property(e => e.MOM_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // 8. MOM_Tipo varchar(30)
        builder.Property(e => e.MOM_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 9. MOM_Trigger nvarchar(max)
        builder.Property(e => e.MOM_Trigger)
            .IsUnicode(true)
            .IsRequired();

        // 10. MOM_IntencionEsperada nvarchar(max)
        builder.Property(e => e.MOM_IntencionEsperada)
            .IsUnicode(true)
            .IsRequired();

        // 11. MOM_RespuestaEjemplar nvarchar(max)
        builder.Property(e => e.MOM_RespuestaEjemplar)
            .IsUnicode(true)
            .IsRequired(false);

        // 12. MOM_InformacionDescubrible nvarchar(max)
        builder.Property(e => e.MOM_InformacionDescubrible)
            .IsUnicode(true)
            .IsRequired(false);

        // 13. MOM_ErrorFrecuente nvarchar(max)
        builder.Property(e => e.MOM_ErrorFrecuente)
            .IsUnicode(true)
            .IsRequired(false);

        // 14. MOM_Recomendacion nvarchar(max)
        builder.Property(e => e.MOM_Recomendacion)
            .IsUnicode(true)
            .IsRequired(false);

        // 15. MOM_EsCritico bit
        builder.Property(e => e.MOM_EsCritico)
            .IsRequired();

        // 16. MOM_OrdenSugerido int
        builder.Property(e => e.MOM_OrdenSugerido)
            .IsRequired();

        // 17. MOM_PermiteOrdenFlexible bit
        builder.Property(e => e.MOM_PermiteOrdenFlexible)
            .HasDefaultValue(true)
            .IsRequired();

        // 18. MOM_RequiereRespuesta bit
        builder.Property(e => e.MOM_RequiereRespuesta)
            .IsRequired();

        // 19. MOM_Estatus varchar(30)
        builder.Property(e => e.MOM_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(MomentEstatus.Draft)
            .IsRequired();

        // 20. FechaCreacion datetimeoffset
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // 21. CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // 22. FechaActualizacion datetimeoffset
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // 23. ActualizadoPor nvarchar(100)
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 24. RowVersion rowversion
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Relación OBLIGATORIA con LAB_LabVersion: FK compuesta SEG_IdTenant + LAB_IdVersion
        builder.HasOne(m => m.LabVersion)
            .WithMany(v => v.ExpectedMoments)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(m => new { m.SEG_IdTenant, m.LAB_IdVersion })
            .HasConstraintName("FK_LAB_ExpectedMoment_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación OBLIGATORIA con LAB_Stage: FK compuesta SEG_IdTenant + LAB_IdVersion + STG_IdStage
        builder.HasOne(m => m.Stage)
            .WithMany(s => s.ExpectedMoments)
            .HasPrincipalKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion, s.STG_IdStage })
            .HasForeignKey(m => new { m.SEG_IdTenant, m.LAB_IdVersion, m.STG_IdStage })
            .HasConstraintName("FK_LAB_ExpectedMoment_LAB_Stage_Tenant_Version_Stage")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación OPCIONAL con LAB_Objective: FK compuesta SEG_IdTenant + LAB_IdVersion + OBJ_IdObjective
        builder.HasOne(m => m.Objective)
            .WithMany(o => o.ExpectedMoments)
            .HasPrincipalKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion, o.OBJ_IdObjective })
            .HasForeignKey(m => new { m.SEG_IdTenant, m.LAB_IdVersion, m.OBJ_IdObjective })
            .HasConstraintName("FK_LAB_ExpectedMoment_LAB_Objective_Tenant_Version_Objective")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES Y RESTRICCIONES ÚNICAS
        // 1. Índice único: SEG_IdTenant + LAB_IdVersion + MOM_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.MOM_Codigo }, "UQ_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_Codigo")
            .IsUnique();

        // 2. Índice único: SEG_IdTenant + LAB_IdVersion + STG_IdStage + MOM_OrdenSugerido
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage, e.MOM_OrdenSugerido }, "UQ_LAB_ExpectedMoment_Tenant_Version_Stage_Orden")
            .IsUnique();

        // 3. Índice no único: SEG_IdTenant + LAB_IdVersion + STG_IdStage + MOM_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage, e.MOM_Estatus }, "IX_LAB_ExpectedMoment_Tenant_Version_Stage_Estatus");

        // 4. Índice no único: SEG_IdTenant + LAB_IdVersion + STG_IdStage + MOM_EsCritico
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage, e.MOM_EsCritico }, "IX_LAB_ExpectedMoment_Tenant_Version_Stage_EsCritico");

        // 5. Índice no único: SEG_IdTenant + LAB_IdVersion + OBJ_IdObjective
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_IdObjective }, "IX_LAB_ExpectedMoment_Tenant_Version_Objective");

        // 6. Índices para Foreign Keys compuestas
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion }, "IX_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion");
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage }, "IX_LAB_ExpectedMoment_Tenant_Version_Stage");
    }
}
