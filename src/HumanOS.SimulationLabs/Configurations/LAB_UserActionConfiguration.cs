using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_UserActionConfiguration : IEntityTypeConfiguration<LAB_UserAction>
{
    public void Configure(EntityTypeBuilder<LAB_UserAction> builder)
    {
        builder.ToTable("LAB_UserAction", t =>
        {
            // ACT_NumeroSecuencia debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_NumeroSecuencia",
                "[ACT_NumeroSecuencia] > 0");

            // ACT_Tipo solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Tipo",
                "[ACT_Tipo] IN ('NAVIGATION', 'VIEW', 'SEARCH', 'CREATE', 'UPDATE', 'SELECT', 'APPROVE', 'REJECT', 'ESCALATE', 'SUBMIT', 'DOWNLOAD', 'UPLOAD', 'DECISION', 'SYSTEM_COMMAND')");

            // ACT_Resultado solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Resultado",
                "[ACT_Resultado] IN ('SUCCESS', 'BLOCKED', 'WARNING', 'FAILED', 'CANCELLED', 'PENDING')");

            // ACT_Severidad solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Severidad",
                "[ACT_Severidad] IN ('INFO', 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL')");

            // ACT_Origen solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Origen",
                "[ACT_Origen] IN ('PARTICIPANT', 'SYSTEM', 'SIMULATION')");

            // ACT_Estatus solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Estatus",
                "[ACT_Estatus] IN ('STARTED', 'FINAL', 'REVERSED', 'EXCLUDED', 'ERROR')");

            // ACT_FechaFin NULL o igual o posterior a ACT_FechaInicio
            t.HasCheckConstraint(
                "CK_LAB_UserAction_Fechas",
                "([ACT_FechaFin] IS NULL) OR ([ACT_FechaFin] >= [ACT_FechaInicio])");

            // ACT_DuracionMs NULL o mayor o igual que cero
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_DuracionMs",
                "([ACT_DuracionMs] IS NULL) OR ([ACT_DuracionMs] >= 0)");

            // ACT_Codigo no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Codigo",
                "LEN(TRIM([ACT_Codigo])) > 0");

            // ACT_Nombre no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_Nombre",
                "LEN(TRIM([ACT_Nombre])) > 0");

            // CreadoPor no debe estar vacío después de TRIM
            t.HasCheckConstraint(
                "CK_LAB_UserAction_CreadoPor",
                "LEN(TRIM([CreadoPor])) > 0");

            // ACT_InputJson debe contener JSON válido cuando tenga contenido
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_InputJson",
                "([ACT_InputJson] IS NULL) OR (ISJSON([ACT_InputJson]) = 1)");

            // ACT_OutputJson debe contener JSON válido cuando tenga contenido
            t.HasCheckConstraint(
                "CK_LAB_UserAction_ACT_OutputJson",
                "([ACT_OutputJson] IS NULL) OR (ISJSON([ACT_OutputJson]) = 1)");
        });

        // Primary Key
        builder.HasKey(e => e.ACT_IdUserAction);

        builder.Property(e => e.ACT_IdUserAction)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.ATT_IdAttempt)
            .IsRequired();

        builder.Property(e => e.ACT_NumeroSecuencia)
            .IsRequired();

        builder.Property(e => e.ACT_Codigo)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Pagina)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ACT_Componente)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ACT_EntidadTipo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ACT_EntidadId)
            .HasMaxLength(150)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_ValorAnterior)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_ValorNuevo)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_InputJson)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_OutputJson)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_Resultado)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_MensajeResultado)
            .HasMaxLength(1000)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_EraPermitida)
            .IsRequired();

        builder.Property(e => e.ACT_EraEsperada)
            .IsRequired();

        builder.Property(e => e.ACT_EsCritica)
            .IsRequired();

        builder.Property(e => e.ACT_Severidad)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_MotivoEvaluacion)
            .HasMaxLength(1000)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.MOM_IdExpectedMoment)
            .IsRequired(false);

        builder.Property(e => e.ACT_FechaInicio)
            .IsRequired();

        builder.Property(e => e.ACT_FechaFin)
            .IsRequired(false);

        builder.Property(e => e.ACT_DuracionMs)
            .IsRequired(false);

        builder.Property(e => e.ACT_Origen)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Estatus)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasDefaultValue(UserActionEstatus.Final)
            .IsRequired();

        builder.Property(e => e.ACT_ErrorCodigo)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ACT_ErrorDescripcion)
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
            .WithMany(a => a.UserActions)
            .HasPrincipalKey(a => new { a.SEG_IdTenant, a.LAB_IdVersion, a.ATT_IdAttempt })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.ATT_IdAttempt })
            .HasConstraintName("FK_LAB_UserAction_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación 1:N opcional con LAB_ExpectedMoment: FK compuesta (SEG_IdTenant, LAB_IdVersion, MOM_IdExpectedMoment)
        builder.HasOne(e => e.ExpectedMoment)
            .WithMany(m => m.UserActions)
            .HasPrincipalKey(m => new { m.SEG_IdTenant, m.LAB_IdVersion, m.MOM_IdExpectedMoment })
            .HasForeignKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.MOM_IdExpectedMoment })
            .HasConstraintName("FK_LAB_UserAction_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + ATT_IdAttempt + ACT_NumeroSecuencia
        builder.HasIndex(
                e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.ACT_NumeroSecuencia },
                "UQ_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_NumeroSecuencia")
            .IsUnique();

        // 2. Índice: SEG_IdTenant + ATT_IdAttempt + ACT_FechaInicio
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.ACT_FechaInicio },
            "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_FechaInicio");

        // 3. Índice: SEG_IdTenant + ATT_IdAttempt + ACT_Resultado
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.ACT_Resultado },
            "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_Resultado");

        // 4. Índice: SEG_IdTenant + ATT_IdAttempt + ACT_EsCritica
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ATT_IdAttempt, e.ACT_EsCritica },
            "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_EsCritica");

        // 5. Índice para la relación con LAB_ExpectedMoment
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.MOM_IdExpectedMoment },
            "IX_LAB_UserAction_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment");

        // 6. Índice: SEG_IdTenant + ACT_EntidadTipo + ACT_EntidadId
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.ACT_EntidadTipo, e.ACT_EntidadId },
            "IX_LAB_UserAction_SEG_IdTenant_ACT_EntidadTipo_ACT_EntidadId");

        // 7. Índice para la Foreign Key del intento: SEG_IdTenant + LAB_IdVersion + ATT_IdAttempt
        builder.HasIndex(
            e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.ATT_IdAttempt },
            "IX_LAB_UserAction_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt");
    }
}
