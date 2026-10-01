using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_ConversationTurnConfiguration : IEntityTypeConfiguration<LAB_ConversationTurn>
{
    public void Configure(EntityTypeBuilder<LAB_ConversationTurn> builder)
    {
        builder.ToTable("LAB_ConversationTurn", t =>
        {
            // TRN_NumeroTurno debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_NumeroTurno",
                "[TRN_NumeroTurno] > 0");

            // TRN_SpeakerType solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_SpeakerType",
                "[TRN_SpeakerType] IN ('PARTICIPANT', 'SIMULATED_ACTOR', 'SYSTEM')");

            // TRN_TipoEntrada solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_TipoEntrada",
                "[TRN_TipoEntrada] IN ('VOICE', 'TEXT', 'SYSTEM')");

            // TRN_FechaFin NULL o igual o posterior a TRN_FechaInicio
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_Fechas",
                "([TRN_FechaFin] IS NULL) OR ([TRN_FechaFin] >= [TRN_FechaInicio])");

            // TRN_DuracionMs NULL o mayor o igual que cero
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_DuracionMs",
                "([TRN_DuracionMs] IS NULL) OR ([TRN_DuracionMs] >= 0)");

            // TRN_ConfianzaTranscripcion NULL o entre 0.0000 y 1.0000
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_ConfianzaTranscripcion",
                "([TRN_ConfianzaTranscripcion] IS NULL) OR ([TRN_ConfianzaTranscripcion] >= 0.0000 AND [TRN_ConfianzaTranscripcion] <= 1.0000)");

            // TRN_Estatus solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_Estatus",
                "[TRN_Estatus] IN ('PARTIAL', 'FINAL', 'CORRECTED', 'EXCLUDED', 'ERROR')");

            // TRN_Texto no debe estar vacío después de aplicar TRIM
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_TRN_Texto",
                "LEN(TRIM([TRN_Texto])) > 0");

            // CreadoPor no debe estar vacío después de aplicar TRIM
            t.HasCheckConstraint(
                "CK_LAB_ConversationTurn_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");
        });

        // Primary Key
        builder.HasKey(e => e.TRN_IdTurn);

        builder.Property(e => e.TRN_IdTurn)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.ATT_IdAttempt)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.TRN_NumeroTurno)
            .IsRequired();

        builder.Property(e => e.TRN_SpeakerType)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_IdActor)
            .IsRequired(false);

        builder.Property(e => e.TRN_Texto)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.TRN_TipoEntrada)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.TRN_FechaInicio)
            .IsRequired();

        builder.Property(e => e.TRN_FechaFin)
            .IsRequired(false);

        builder.Property(e => e.TRN_DuracionMs)
            .IsRequired(false);

        builder.Property(e => e.TRN_ConfianzaTranscripcion)
            .HasPrecision(5, 4)
            .IsRequired(false);

        builder.Property(e => e.TRN_FueInterrumpido)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.TRN_InterrumpioTurnoAnterior)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.TRN_EsRespuesta)
            .IsRequired();

        builder.Property(e => e.TRN_IdTurnoRespondido)
            .IsRequired(false);

        builder.Property(e => e.TRN_IdExpectedMoment)
            .IsRequired(false);

        builder.Property(e => e.TRN_TextoFueEditado)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.TRN_TextoOriginal)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.TRN_EditadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.TRN_FechaEdicion)
            .IsRequired(false);

        builder.Property(e => e.TRN_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(TurnEstatus.Final)
            .IsRequired();

        builder.Property(e => e.TRN_ErrorCodigo)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.TRN_ErrorDescripcion)
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

        // Relación 1:N con LAB_Attempt: FK compuesta (SEG_IdTenant, ATT_IdAttempt), protege el tenant
        builder.HasOne(e => e.Attempt)
            .WithMany(a => a.ConversationTurns)
            .HasPrincipalKey(a => new { a.SEG_IdTenant, a.ATT_IdAttempt })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.ATT_IdAttempt })
            .HasConstraintName("FK_LAB_ConversationTurn_LAB_Attempt_SEG_IdTenant_ATT_IdAttempt")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación N:1 opcional con LAB_SimulatedActor — FK simple (ACT_IdActor), para permitir
        // labs globales practicados desde otro tenant.
        builder.HasOne(e => e.SimulatedActor)
            .WithMany()
            .HasForeignKey(e => e.ACT_IdActor)
            .HasConstraintName("FK_LAB_ConversationTurn_LAB_SimulatedActor_ACT_IdActor")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Relación N:1 opcional con LAB_ExpectedMoment — FK simple (TRN_IdExpectedMoment -> MOM_IdExpectedMoment).
        builder.HasOne(e => e.ExpectedMoment)
            .WithMany()
            .HasPrincipalKey(m => m.MOM_IdExpectedMoment)
            .HasForeignKey(e => e.TRN_IdExpectedMoment)
            .HasConstraintName("FK_LAB_ConversationTurn_LAB_ExpectedMoment_TRN_IdExpectedMoment")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Autorreferencia opcional: un turno puede responder a otro turno del mismo intento
        builder.HasOne(e => e.RespondedTurn)
            .WithMany(e => e.Responses)
            .HasForeignKey(e => e.TRN_IdTurnoRespondido)
            .HasConstraintName("FK_LAB_ConversationTurn_LAB_ConversationTurn_TRN_IdTurnoRespondido")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + ATT_IdAttempt + TRN_NumeroTurno
        builder.HasIndex(
                e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.TRN_NumeroTurno },
                "UQ_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_NumeroTurno")
            .IsUnique();

        // 2. Índice: SEG_IdTenant + ATT_IdAttempt + TRN_FechaInicio
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.TRN_FechaInicio },
            "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_FechaInicio");

        // 3. Índice: SEG_IdTenant + ATT_IdAttempt + TRN_SpeakerType
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.TRN_SpeakerType },
            "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_SpeakerType");

        // 4. Índice: SEG_IdTenant + ATT_IdAttempt + TRN_Estatus
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.TRN_Estatus },
            "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_Estatus");

        // 5. Índice para la relación con LAB_ExpectedMoment
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.TRN_IdExpectedMoment },
            "IX_LAB_ConversationTurn_SEG_IdTenant_LAB_IdVersion_TRN_IdExpectedMoment");

        // 6. Índice para la relación con LAB_SimulatedActor
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ACT_IdActor },
            "IX_LAB_ConversationTurn_SEG_IdTenant_ACT_IdActor");

        // 7. Índice para la autorreferencia
        builder.HasIndex(
            e => e.TRN_IdTurnoRespondido,
            "IX_LAB_ConversationTurn_TRN_IdTurnoRespondido");
    }
}
