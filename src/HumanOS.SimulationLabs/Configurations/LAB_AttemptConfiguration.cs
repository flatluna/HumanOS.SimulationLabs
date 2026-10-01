using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_AttemptConfiguration : IEntityTypeConfiguration<LAB_Attempt>
{
    public void Configure(EntityTypeBuilder<LAB_Attempt> builder)
    {
        builder.ToTable("LAB_Attempt", t =>
        {
            // ATT_NumeroIntento debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_NumeroIntento",
                "[ATT_NumeroIntento] > 0");

            // ATT_DuracionSegundos NULL o mayor o igual que cero
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_DuracionSegundos",
                "([ATT_DuracionSegundos] IS NULL) OR ([ATT_DuracionSegundos] >= 0)");

            // ATT_FechaFin NULL o mayor o igual que ATT_FechaInicio
            t.HasCheckConstraint(
                "CK_LAB_Attempt_Fechas",
                "([ATT_FechaFin] IS NULL) OR ([ATT_FechaInicio] IS NULL) OR ([ATT_FechaFin] >= [ATT_FechaInicio])");

            // ATT_ScoreFinal NULL o entre 1.00 y 5.00
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_ScoreFinal",
                "([ATT_ScoreFinal] IS NULL) OR ([ATT_ScoreFinal] >= 1.00 AND [ATT_ScoreFinal] <= 5.00)");

            // ATT_Estatus solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_Estatus",
                "[ATT_Estatus] IN ('NOT_STARTED', 'IN_PROGRESS', 'PAUSED', 'COMPLETED', 'ABANDONED', 'EVALUATING', 'EVALUATED', 'CANCELLED', 'ERROR')");

            // ATT_Modalidad solo acepta VOICE, DESKTOP, HYBRID
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_Modalidad",
                "[ATT_Modalidad] IN ('VOICE', 'DESKTOP', 'HYBRID')");

            // ATT_Resultado NULL o uno de los valores permitidos
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_Resultado",
                "([ATT_Resultado] IS NULL) OR ([ATT_Resultado] IN ('PASSED', 'PARTIAL', 'REPEAT_RECOMMENDED', 'NOT_COMPLETED', 'CRITICAL_FAILURE'))");

            // ATT_HashConfiguracion NULL o exactamente 64 caracteres
            t.HasCheckConstraint(
                "CK_LAB_Attempt_ATT_HashConfiguracion",
                "([ATT_HashConfiguracion] IS NULL) OR (LEN([ATT_HashConfiguracion]) = 64)");
        });

        // Primary Key
        builder.HasKey(e => e.ATT_IdAttempt);

        builder.Property(e => e.ATT_IdAttempt)
            .IsRequired()
            .ValueGeneratedNever();

        // Clave alternativa requerida para la FK compuesta desde LAB_ConversationTurn
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.ATT_IdAttempt })
            .HasName("AK_LAB_Attempt_SEG_IdTenant_ATT_IdAttempt");

        // Clave alternativa requerida para la FK compuesta desde LAB_UserAction
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.ATT_IdAttempt })
            .HasName("AK_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt");

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.SCN_IdScenario)
            .IsRequired();

        builder.Property(e => e.USR_IdParticipant)
            .IsRequired();

        builder.Property(e => e.ATT_NumeroIntento)
            .IsRequired();

        builder.Property(e => e.ATT_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(AttemptEstatus.NotStarted)
            .IsRequired();

        builder.Property(e => e.ATT_FechaInicio)
            .IsRequired(false);

        builder.Property(e => e.ATT_FechaFin)
            .IsRequired(false);

        builder.Property(e => e.ATT_DuracionSegundos)
            .IsRequired(false);

        builder.Property(e => e.ATT_Modalidad)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ATT_SeedEjecucion)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ATT_HashConfiguracion)
            .HasMaxLength(64)
            .IsFixedLength(true)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ATT_ScoreFinal)
            .HasPrecision(5, 2)
            .IsRequired(false);

        builder.Property(e => e.ATT_Resultado)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ATT_Idioma)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ATT_UsaVoz)
            .IsRequired();

        builder.Property(e => e.ATT_UsaEscritorio)
            .IsRequired();

        builder.Property(e => e.ATT_UsaArtefactos)
            .IsRequired();

        builder.Property(e => e.ATT_ErrorCodigo)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ATT_ErrorDescripcion)
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

        // Relación 1:N con LAB_LabVersion — FK simple (LAB_IdVersion) para permitir Attempts
        // de participantes de OTRO tenant contra un LabVersion global (LAB_EsGlobal=true).
        builder.HasOne(a => a.LabVersion)
            .WithMany(v => v.Attempts)
            .HasForeignKey(a => a.LAB_IdVersion)
            .HasConstraintName("FK_LAB_Attempt_LAB_LabVersion_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1:N con LAB_Scenario — FK simple (SCN_IdScenario), mismo motivo.
        builder.HasOne(a => a.Scenario)
            .WithMany(s => s.Attempts)
            .HasForeignKey(a => a.SCN_IdScenario)
            .HasConstraintName("FK_LAB_Attempt_LAB_Scenario_SCN_IdScenario")
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + SCN_IdScenario + USR_IdParticipant + ATT_NumeroIntento
        builder.HasIndex(
                e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.USR_IdParticipant, e.ATT_NumeroIntento },
                "UQ_LAB_Attempt_SEG_IdTenant_SCN_IdScenario_USR_IdParticipant_ATT_NumeroIntento")
            .IsUnique();

        // 2. Índice no único: SEG_IdTenant + USR_IdParticipant + ATT_Estatus
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.USR_IdParticipant, e.ATT_Estatus },
            "IX_LAB_Attempt_SEG_IdTenant_USR_IdParticipant_ATT_Estatus");

        // 3. Índice no único: SEG_IdTenant + SCN_IdScenario + ATT_Estatus
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.ATT_Estatus },
            "IX_LAB_Attempt_SEG_IdTenant_SCN_IdScenario_ATT_Estatus");

        // 4. Índice para la relación con la versión: SEG_IdTenant + LAB_IdVersion
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion },
            "IX_LAB_Attempt_SEG_IdTenant_LAB_IdVersion");

        // 5. Índice para la relación con el escenario: SEG_IdTenant + LAB_IdVersion + SCN_IdScenario
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.SCN_IdScenario },
            "IX_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario");
    }
}
