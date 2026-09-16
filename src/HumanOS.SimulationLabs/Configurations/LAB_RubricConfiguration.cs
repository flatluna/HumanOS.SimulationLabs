using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_RubricConfiguration : IEntityTypeConfiguration<LAB_Rubric>
{
    public void Configure(EntityTypeBuilder<LAB_Rubric> builder)
    {
        builder.ToTable("LAB_Rubric", t =>
        {
            // 1. RUB_EscalaMinima debe ser igual a 1.00
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_EscalaMinima",
                "[RUB_EscalaMinima] = 1.00");

            // 2. RUB_EscalaMaxima debe ser igual a 10.00
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_EscalaMaxima",
                "[RUB_EscalaMaxima] = 10.00");

            // 3. RUB_EscalaMaxima debe ser mayor que RUB_EscalaMinima
            t.HasCheckConstraint(
                "CK_LAB_Rubric_Escala",
                "[RUB_EscalaMaxima] > [RUB_EscalaMinima]");

            // 4. RUB_ScoreMinimoAprobacion debe estar entre RUB_EscalaMinima y RUB_EscalaMaxima
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_ScoreMinimoAprobacion",
                "[RUB_ScoreMinimoAprobacion] >= [RUB_EscalaMinima] AND [RUB_ScoreMinimoAprobacion] <= [RUB_EscalaMaxima]");

            // 5. RUB_TipoEvaluacion solo acepta: CONVERSATION, DESKTOP, HYBRID, ARTIFACT, MULTI_SOURCE
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_TipoEvaluacion",
                "[RUB_TipoEvaluacion] IN ('CONVERSATION', 'DESKTOP', 'HYBRID', 'ARTIFACT', 'MULTI_SOURCE')");

            // 6. RUB_MetodoCalculo solo acepta: WEIGHTED_AVERAGE, SIMPLE_AVERAGE, CRITICAL_GATE, WEIGHTED_WITH_CRITICAL_GATE
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_MetodoCalculo",
                "[RUB_MetodoCalculo] IN ('WEIGHTED_AVERAGE', 'SIMPLE_AVERAGE', 'CRITICAL_GATE', 'WEIGHTED_WITH_CRITICAL_GATE')");

            // 7. RUB_Estatus solo acepta: DRAFT, APPROVED, PUBLISHED, RETIRED
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_Estatus",
                "[RUB_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");

            // 8. RUB_VigenciaHasta debe ser NULL o mayor o igual que RUB_VigenciaDesde
            t.HasCheckConstraint(
                "CK_LAB_Rubric_Vigencia",
                "([RUB_VigenciaHasta] IS NULL) OR ([RUB_VigenciaDesde] IS NULL) OR ([RUB_VigenciaHasta] >= [RUB_VigenciaDesde])");

            // 9. RUB_Codigo no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_Codigo",
                "LEN(TRIM([RUB_Codigo])) > 0");

            // 10. RUB_Nombre no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_Nombre",
                "LEN(TRIM([RUB_Nombre])) > 0");

            // 11. RUB_Descripcion no debe estar vacía después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_Descripcion",
                "LEN(TRIM([RUB_Descripcion])) > 0");

            // 12. RUB_InstruccionesEvaluador no debe estar vacía después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_Rubric_RUB_InstruccionesEvaluador",
                "LEN(TRIM([RUB_InstruccionesEvaluador])) > 0");

            // 13. CreadoPor no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_Rubric_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");
        });

        // 1. RUB_IdRubric - Primary Key
        builder.HasKey(e => e.RUB_IdRubric);

        builder.Property(e => e.RUB_IdRubric)
            .IsRequired()
            .ValueGeneratedNever();

        // 2. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // 3. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // Clave alternativa (Alternate Key) para asegurar SEG_IdTenant + LAB_IdVersion al relacionar con LAB_RubricCriterion
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.RUB_IdRubric })
            .HasName("AK_LAB_Rubric_SEG_IdTenant_LAB_IdVersion_RUB_IdRubric");

        // 4. RUB_Codigo varchar(60)
        builder.Property(e => e.RUB_Codigo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired();

        // 5. RUB_Nombre nvarchar(200)
        builder.Property(e => e.RUB_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // 6. RUB_Descripcion nvarchar(max)
        builder.Property(e => e.RUB_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // 7. RUB_TipoEvaluacion varchar(30)
        builder.Property(e => e.RUB_TipoEvaluacion)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 8. RUB_EscalaMinima decimal(5,2)
        builder.Property(e => e.RUB_EscalaMinima)
            .HasPrecision(5, 2)
            .IsRequired();

        // 9. RUB_EscalaMaxima decimal(5,2)
        builder.Property(e => e.RUB_EscalaMaxima)
            .HasPrecision(5, 2)
            .IsRequired();

        // 10. RUB_ScoreMinimoAprobacion decimal(5,2)
        builder.Property(e => e.RUB_ScoreMinimoAprobacion)
            .HasPrecision(5, 2)
            .IsRequired();

        // 11. RUB_MetodoCalculo varchar(40)
        builder.Property(e => e.RUB_MetodoCalculo)
            .HasMaxLength(40)
            .IsUnicode(false)
            .IsRequired();

        // 12. RUB_RequiereEvidencia bit
        builder.Property(e => e.RUB_RequiereEvidencia)
            .HasDefaultValue(true)
            .IsRequired();

        // 13. RUB_PermiteFallaCritica bit
        builder.Property(e => e.RUB_PermiteFallaCritica)
            .HasDefaultValue(true)
            .IsRequired();

        // 14. RUB_InstruccionesEvaluador nvarchar(max)
        builder.Property(e => e.RUB_InstruccionesEvaluador)
            .IsUnicode(true)
            .IsRequired();

        // 15. RUB_Estatus varchar(30)
        builder.Property(e => e.RUB_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(RubricEstatus.Draft)
            .IsRequired();

        // 16. RUB_VigenciaDesde datetime2
        builder.Property(e => e.RUB_VigenciaDesde)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 17. RUB_VigenciaHasta datetime2
        builder.Property(e => e.RUB_VigenciaHasta)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 18. FechaCreacion datetimeoffset
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // 19. CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // 20. FechaActualizacion datetimeoffset
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // 21. ActualizadoPor nvarchar(100)
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 22. RowVersion rowversion
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Relación 1 a 0..1 con LAB_LabVersion: FK compuesta (SEG_IdTenant, LAB_IdVersion)
        builder.HasOne(r => r.LabVersion)
            .WithOne(v => v.Rubric)
            .HasPrincipalKey<LAB_LabVersion>(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey<LAB_Rubric>(r => new { r.SEG_IdTenant, r.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Rubric_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES Y RESTRICCIONES ÚNICAS
        // 1. Índice único: SEG_IdTenant + LAB_IdVersion (Una sola rúbrica por versión)
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion }, "UQ_LAB_Rubric_SEG_IdTenant_LAB_IdVersion")
            .IsUnique();

        // 2. Índice único: SEG_IdTenant + RUB_Codigo (Código único dentro del tenant)
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_Codigo }, "UQ_LAB_Rubric_SEG_IdTenant_RUB_Codigo")
            .IsUnique();

        // 3. Índice no único: SEG_IdTenant + RUB_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_Estatus }, "IX_LAB_Rubric_SEG_IdTenant_RUB_Estatus");

        // 4. Índice no único: SEG_IdTenant + RUB_TipoEvaluacion + RUB_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_TipoEvaluacion, e.RUB_Estatus }, "IX_LAB_Rubric_Tenant_Tipo_Estatus");

        // 5. Índice no único: SEG_IdTenant + RUB_VigenciaDesde + RUB_VigenciaHasta
        builder.HasIndex(e => new { e.SEG_IdTenant, e.RUB_VigenciaDesde, e.RUB_VigenciaHasta }, "IX_LAB_Rubric_Tenant_Vigencias");
    }
}
