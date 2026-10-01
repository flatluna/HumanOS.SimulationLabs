using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_ArtifactSubmissionConfiguration : IEntityTypeConfiguration<LAB_ArtifactSubmission>
{
    public void Configure(EntityTypeBuilder<LAB_ArtifactSubmission> builder)
    {
        builder.ToTable("LAB_ArtifactSubmission", t =>
        {
            // SUB_Version debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_Version",
                "[SUB_Version] > 0");

            // SUB_Tipo solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_Tipo",
                "[SUB_Tipo] IN ('FORM', 'WORKSHEET', 'MATRIX', 'DOCUMENT', 'DIAGRAM', 'DECISION_RECORD', 'CHECKLIST', 'JSON_ARTIFACT', 'FILE_REFERENCE')");

            // SUB_Formato solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_Formato",
                "[SUB_Formato] IN ('TEXT', 'MARKDOWN', 'HTML', 'JSON', 'STRUCTURED_FORM', 'FILE', 'DIAGRAM_DATA')");

            // SUB_Estatus solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_Estatus",
                "[SUB_Estatus] IN ('DRAFT', 'SUBMITTED', 'FINAL', 'REPLACED', 'EXCLUDED', 'ERROR')");

            // SUB_FechaEnvio NULL o igual o posterior a SUB_FechaInicio
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_Fechas",
                "([SUB_FechaEnvio] IS NULL) OR ([SUB_FechaInicio] IS NULL) OR ([SUB_FechaEnvio] >= [SUB_FechaInicio])");

            // SUB_HashSHA256 NULL o exactamente 64 caracteres
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_HashSHA256",
                "([SUB_HashSHA256] IS NULL) OR (LEN([SUB_HashSHA256]) = 64)");

            // SUB_ContenidoJson NULL o JSON válido
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_ContenidoJson",
                "([SUB_ContenidoJson] IS NULL) OR (ISJSON([SUB_ContenidoJson]) = 1)");

            // SUB_TipoAsistencia NULL o uno de los valores permitidos
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_TipoAsistencia",
                "([SUB_TipoAsistencia] IS NULL) OR ([SUB_TipoAsistencia] IN ('NONE', 'HINT', 'TEMPLATE', 'AI_ASSISTANCE', 'HUMAN_ASSISTANCE', 'SYSTEM_SUGGESTION'))");

            // SUB_CodigoArtefacto no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_CodigoArtefacto",
                "LEN(TRIM([SUB_CodigoArtefacto])) > 0");

            // SUB_Nombre no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_SUB_Nombre",
                "LEN(TRIM([SUB_Nombre])) > 0");

            // CreadoPor no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_ArtifactSubmission_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");
        });

        // Primary Key
        builder.HasKey(e => e.SUB_IdSubmission);

        builder.Property(e => e.SUB_IdSubmission)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.ATT_IdAttempt)
            .IsRequired();

        builder.Property(e => e.STG_IdStage)
            .IsRequired(false);

        builder.Property(e => e.OBJ_IdObjective)
            .IsRequired(false);

        builder.Property(e => e.SUB_CodigoArtefacto)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.SUB_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.SUB_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.SUB_Formato)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.SUB_Version)
            .IsRequired();

        builder.Property(e => e.SUB_ContenidoTexto)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.SUB_ContenidoJson)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.SUB_BlobPath)
            .HasMaxLength(500)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.SUB_NombreArchivo)
            .HasMaxLength(260)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.SUB_MimeType)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.SUB_HashSHA256)
            .HasMaxLength(64)
            .IsFixedLength(true)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.SUB_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(ArtifactSubmissionEstatus.Draft)
            .IsRequired();

        builder.Property(e => e.SUB_FechaInicio)
            .IsRequired(false);

        builder.Property(e => e.SUB_FechaEnvio)
            .IsRequired(false);

        builder.Property(e => e.SUB_EsEntregaFinal)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.SUB_RequiereEvaluacion)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.SUB_FueGeneradoConAsistencia)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.SUB_TipoAsistencia)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.SUB_ErrorCodigo)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.SUB_ErrorDescripcion)
            .HasMaxLength(1000)
            .IsUnicode(true)
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

        // Relación 1:N obligatoria con LAB_Attempt: FK compuesta (SEG_IdTenant, LAB_IdVersion, ATT_IdAttempt)
        builder.HasOne(e => e.Attempt)
            .WithMany(a => a.ArtifactSubmissions)
            .HasPrincipalKey(a => new { a.SEG_IdTenant, a.LAB_IdVersion, a.ATT_IdAttempt })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.ATT_IdAttempt })
            .HasConstraintName("FK_LAB_ArtifactSubmission_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación 1:N opcional con LAB_Stage — FK simple (STG_IdStage), para permitir labs
        // globales practicados desde otro tenant.
        builder.HasOne(e => e.Stage)
            .WithMany(s => s.ArtifactSubmissions)
            .HasForeignKey(e => e.STG_IdStage)
            .HasConstraintName("FK_LAB_ArtifactSubmission_LAB_Stage_STG_IdStage")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Relación 1:N opcional con LAB_Objective — FK simple (OBJ_IdObjective), mismo motivo.
        builder.HasOne(e => e.Objective)
            .WithMany(o => o.ArtifactSubmissions)
            .HasForeignKey(e => e.OBJ_IdObjective)
            .HasConstraintName("FK_LAB_ArtifactSubmission_LAB_Objective_OBJ_IdObjective")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + ATT_IdAttempt + SUB_CodigoArtefacto + SUB_Version
        builder.HasIndex(
                e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.SUB_CodigoArtefacto, e.SUB_Version },
                "UQ_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_CodigoArtefacto_SUB_Version")
            .IsUnique();

        // 2. Índice no único: SEG_IdTenant + ATT_IdAttempt + SUB_Estatus
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.SUB_Estatus },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_Estatus");

        // 3. Índice no único: SEG_IdTenant + ATT_IdAttempt + SUB_EsEntregaFinal
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.SUB_EsEntregaFinal },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_EsEntregaFinal");

        // 4. Índice no único: SEG_IdTenant + LAB_IdVersion + STG_IdStage
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.STG_IdStage },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_STG_IdStage");

        // 5. Índice no único: SEG_IdTenant + LAB_IdVersion + OBJ_IdObjective
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.OBJ_IdObjective },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective");

        // 6. Índice no único: SEG_IdTenant + SUB_HashSHA256
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.SUB_HashSHA256 },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_SUB_HashSHA256");

        // 7. Índice para la Foreign Key del intento: SEG_IdTenant + LAB_IdVersion + ATT_IdAttempt
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.ATT_IdAttempt },
            "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt");
    }
}
