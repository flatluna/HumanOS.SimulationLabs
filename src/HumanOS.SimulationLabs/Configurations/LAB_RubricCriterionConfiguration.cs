using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_RubricCriterionConfiguration : IEntityTypeConfiguration<LAB_RubricCriterion>
{
    public void Configure(EntityTypeBuilder<LAB_RubricCriterion> builder)
    {
        builder.ToTable("LAB_RubricCriterion", t =>
        {
            // 1. CRT_Peso debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Peso",
                "[CRT_Peso] > 0");

            // 2. CRT_ScoreMinimoEsperado debe estar entre 1.00 y 10.00 inclusive
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_ScoreMinimoEsperado",
                "[CRT_ScoreMinimoEsperado] >= 1.00 AND [CRT_ScoreMinimoEsperado] <= 10.00");

            // 3. CRT_Orden debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Orden",
                "[CRT_Orden] > 0");

            // 4. CRT_TipoEvidencia solo acepta: CONVERSATION, USER_ACTION, ARTIFACT, DECISION, SYSTEM_RESULT, MULTIPLE
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_TipoEvidencia",
                "[CRT_TipoEvidencia] IN ('CONVERSATION', 'USER_ACTION', 'ARTIFACT', 'DECISION', 'SYSTEM_RESULT', 'MULTIPLE')");

            // 5. CRT_Estatus solo acepta: DRAFT, ACTIVE, INACTIVE
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Estatus",
                "[CRT_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");

            // 6. CRT_Codigo no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Codigo",
                "LEN(TRIM([CRT_Codigo])) > 0");

            // 7. CRT_Nombre no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Nombre",
                "LEN(TRIM([CRT_Nombre])) > 0");

            // 8. CRT_Descripcion no debe estar vacía después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_Descripcion",
                "LEN(TRIM([CRT_Descripcion])) > 0");

            // 9. CRT_IndicadoresPositivos no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CRT_IndicadoresPositivos",
                "LEN(TRIM([CRT_IndicadoresPositivos])) > 0");

            // 10. CreadoPor no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_RubricCriterion_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");
        });

        // 1. CRT_IdCriterion - Primary Key
        builder.HasKey(e => e.CRT_IdCriterion);

        builder.Property(e => e.CRT_IdCriterion)
            .IsRequired()
            .ValueGeneratedNever();

        // Clave alternativa para permitir FK compuesta desde LAB_TestedSkill (SEG_IdTenant, LAB_IdVersion, CRT_IdCriterion)
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.CRT_IdCriterion })
            .HasName("AK_LAB_RubricCriterion_SEG_IdTenant_LAB_IdVersion_CRT_IdCriterion");

        // 2. RUB_IdRubric
        builder.Property(e => e.RUB_IdRubric)
            .IsRequired();

        // 3. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // 4. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // 5. OBJ_IdObjective
        builder.Property(e => e.OBJ_IdObjective)
            .IsRequired(false);

        // 6. MOM_IdExpectedMoment
        builder.Property(e => e.MOM_IdExpectedMoment)
            .IsRequired(false);

        // 7. CRT_Codigo varchar(60)
        builder.Property(e => e.CRT_Codigo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired();

        // 8. CRT_Nombre nvarchar(200)
        builder.Property(e => e.CRT_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // 9. CRT_Descripcion nvarchar(max)
        builder.Property(e => e.CRT_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // 10. CRT_TipoEvidencia varchar(30)
        builder.Property(e => e.CRT_TipoEvidencia)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 11. CRT_Peso decimal(7,4)
        builder.Property(e => e.CRT_Peso)
            .HasPrecision(7, 4)
            .IsRequired();

        // 12. CRT_ScoreMinimoEsperado decimal(5,2)
        builder.Property(e => e.CRT_ScoreMinimoEsperado)
            .HasPrecision(5, 2)
            .HasDefaultValue(7.00m)
            .IsRequired();

        // 13. CRT_EsCritico bit
        builder.Property(e => e.CRT_EsCritico)
            .IsRequired();

        // 14. CRT_IndicadoresPositivos nvarchar(max)
        builder.Property(e => e.CRT_IndicadoresPositivos)
            .IsUnicode(true)
            .IsRequired();

        // 15. CRT_IndicadoresNegativos nvarchar(max)
        builder.Property(e => e.CRT_IndicadoresNegativos)
            .IsUnicode(true)
            .IsRequired(false);

        // 16. CRT_ErrorCritico nvarchar(max)
        builder.Property(e => e.CRT_ErrorCritico)
            .IsUnicode(true)
            .IsRequired(false);

        // 17. CRT_RecomendacionBase nvarchar(max)
        builder.Property(e => e.CRT_RecomendacionBase)
            .IsUnicode(true)
            .IsRequired(false);

        // 18. CRT_Orden int
        builder.Property(e => e.CRT_Orden)
            .IsRequired();

        // 19. CRT_Estatus varchar(30)
        builder.Property(e => e.CRT_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(CriterionEstatus.Draft)
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

        // RELACIONES
        // 1. Relación OBLIGATORIA con LAB_Rubric: FK compuesta SEG_IdTenant + LAB_IdVersion + RUB_IdRubric
        builder.HasOne(c => c.Rubric)
            .WithMany(r => r.Criteria)
            .HasPrincipalKey(r => new { r.SEG_IdTenant, r.LAB_IdVersion, r.RUB_IdRubric })
            .HasForeignKey(c => new { c.SEG_IdTenant, c.LAB_IdVersion, c.RUB_IdRubric })
            .HasConstraintName("FK_LAB_RubricCriterion_LAB_Rubric_Tenant_Version_Rubric")
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Relación OPCIONAL con LAB_Objective: FK compuesta SEG_IdTenant + LAB_IdVersion + OBJ_IdObjective
        builder.HasOne(c => c.Objective)
            .WithMany(o => o.RubricCriteria)
            .HasPrincipalKey(o => new { o.SEG_IdTenant, o.LAB_IdVersion, o.OBJ_IdObjective })
            .HasForeignKey(c => new { c.SEG_IdTenant, c.LAB_IdVersion, c.OBJ_IdObjective })
            .HasConstraintName("FK_LAB_RubricCriterion_LAB_Objective_Tenant_Version_Objective")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. Relación OPCIONAL con LAB_ExpectedMoment: FK compuesta SEG_IdTenant + LAB_IdVersion + MOM_IdExpectedMoment
        builder.HasOne(c => c.ExpectedMoment)
            .WithMany(m => m.RubricCriteria)
            .HasPrincipalKey(m => new { m.SEG_IdTenant, m.LAB_IdVersion, m.MOM_IdExpectedMoment })
            .HasForeignKey(c => new { c.SEG_IdTenant, c.LAB_IdVersion, c.MOM_IdExpectedMoment })
            .HasConstraintName("FK_LAB_RubricCriterion_LAB_ExpectedMoment_Tenant_Version_Moment")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES Y RESTRICCIONES ÚNICAS
        // 1. Índice único: SEG_IdTenant + RUB_IdRubric + CRT_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_IdRubric, e.CRT_Codigo }, "UQ_LAB_RubricCriterion_Tenant_Rubric_Codigo")
            .IsUnique();

        // 2. Índice único: SEG_IdTenant + RUB_IdRubric + CRT_Orden
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_IdRubric, e.CRT_Orden }, "UQ_LAB_RubricCriterion_Tenant_Rubric_Orden")
            .IsUnique();

        // 3. Índice no único: SEG_IdTenant + LAB_IdVersion + RUB_IdRubric + CRT_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdRubric, e.CRT_Estatus }, "IX_LAB_RubricCriterion_Tenant_Version_Rubric_Estatus");

        // 4. Índice no único: SEG_IdTenant + LAB_IdVersion + RUB_IdRubric + CRT_EsCritico
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdRubric, e.CRT_EsCritico }, "IX_LAB_RubricCriterion_Tenant_Version_Rubric_EsCritico");

        // 5. Índice no único: SEG_IdTenant + LAB_IdVersion + OBJ_IdObjective
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_IdObjective }, "IX_LAB_RubricCriterion_Tenant_Version_Objective");

        // 6. Índice no único: SEG_IdTenant + LAB_IdVersion + MOM_IdExpectedMoment
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.MOM_IdExpectedMoment }, "IX_LAB_RubricCriterion_Tenant_Version_Moment");

        // 7. Índice para Foreign Key compuesta hacia LAB_Rubric
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdRubric }, "IX_LAB_RubricCriterion_Tenant_Version_Rubric");
    }
}
