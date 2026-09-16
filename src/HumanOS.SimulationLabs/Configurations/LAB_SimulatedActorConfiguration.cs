using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_SimulatedActorConfiguration : IEntityTypeConfiguration<LAB_SimulatedActor>
{
    public void Configure(EntityTypeBuilder<LAB_SimulatedActor> builder)
    {
        builder.ToTable("LAB_SimulatedActor", t =>
        {
            // 4. ACT_Tipo solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_SimulatedActor_ACT_Tipo",
                "[ACT_Tipo] IN ('CLIENT', 'STAKEHOLDER', 'MANAGER', 'SUBJECT_MATTER_EXPERT', 'APPROVER', 'SYSTEM_OPERATOR', 'OBSERVER')");

            // 5. ACT_EstiloComunicacion solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_SimulatedActor_ACT_EstiloComunicacion",
                "[ACT_EstiloComunicacion] IN ('DIRECT', 'COLLABORATIVE', 'RESERVED', 'SKEPTICAL', 'IMPATIENT', 'DETAIL_ORIENTED', 'EXECUTIVE')");

            // 6. ACT_NivelConocimiento solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_SimulatedActor_ACT_NivelConocimiento",
                "[ACT_NivelConocimiento] IN ('LOW', 'MEDIUM', 'HIGH', 'EXPERT')");

            // 7. ACT_Estatus solo acepta los valores definidos
            t.HasCheckConstraint(
                "CK_LAB_SimulatedActor_ACT_Estatus",
                "[ACT_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");

            // 8. ACT_Orden debe ser mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_SimulatedActor_ACT_Orden",
                "[ACT_Orden] > 0");
        });

        // 1. Primary Key
        builder.HasKey(e => e.ACT_IdActor);

        builder.Property(e => e.ACT_IdActor)
            .IsRequired()
            .ValueGeneratedNever();

        // Clave alternativa requerida para la FK compuesta desde LAB_ConversationTurn
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.ACT_IdActor })
            .HasName("AK_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor");

        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        builder.Property(e => e.SCN_IdScenario)
            .IsRequired();

        builder.Property(e => e.ACT_Codigo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_Rol)
            .HasMaxLength(150)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_Objetivo)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_ContextoConocido)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_BriefOculto)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(e => e.ACT_InformacionPuedeRevelar)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_InformacionNoRevelarAutomaticamente)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_Restricciones)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_Objeciones)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_Contradicciones)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_EstiloComunicacion)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_NivelConocimiento)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_Idioma)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ACT_VoiceName)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_MensajeInicial)
            .IsUnicode(true)
            .IsRequired(false);

        builder.Property(e => e.ACT_PuedeIniciarConversacion)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ACT_EsPrincipal)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ACT_Orden)
            .IsRequired();

        builder.Property(e => e.ACT_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(ActorEstatus.Draft)
            .IsRequired();

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

        // Relación 1:N con LAB_Scenario: FK compuesta (SEG_IdTenant, LAB_IdVersion, SCN_IdScenario)
        builder.HasOne(a => a.Scenario)
            .WithMany(s => s.SimulatedActors)
            .HasPrincipalKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion, s.SCN_IdScenario })
            .HasForeignKey(a => new { a.SEG_IdTenant, a.LAB_IdVersion, a.SCN_IdScenario })
            .HasConstraintName("FK_LAB_SimulatedActor_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario")
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + SCN_IdScenario + ACT_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.ACT_Codigo }, "UQ_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Codigo")
            .IsUnique();

        // 2. Índice único: SEG_IdTenant + SCN_IdScenario + ACT_Orden
        builder.HasIndex(e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.ACT_Orden }, "UQ_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Orden")
            .IsUnique();

        // 3. Índice no único: SEG_IdTenant + SCN_IdScenario + ACT_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.ACT_Estatus }, "IX_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Estatus");

        // 4. Índice no único: SEG_IdTenant + SCN_IdScenario + ACT_EsPrincipal
        builder.HasIndex(e => new { e.SEG_IdTenant, e.SCN_IdScenario, e.ACT_EsPrincipal }, "IX_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_EsPrincipal");

        // 5. Índice para la Foreign Key compuesta: SEG_IdTenant + LAB_IdVersion + SCN_IdScenario
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.SCN_IdScenario }, "IX_LAB_SimulatedActor_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario");
    }
}
