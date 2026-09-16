using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_ScenarioConfiguration : IEntityTypeConfiguration<LAB_Scenario>
{
    public void Configure(EntityTypeBuilder<LAB_Scenario> builder)
    {
        builder.ToTable("LAB_Scenario", t =>
        {
            // 3. SCN_Tipo solo acepta CONVERSATIONAL, DESKTOP, HYBRID
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_Tipo",
                "[SCN_Tipo] IN ('CONVERSATIONAL', 'DESKTOP', 'HYBRID')");

            // 4. SCN_Dificultad solo acepta BEGINNER, INTERMEDIATE, ADVANCED, EXPERT
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_Dificultad",
                "[SCN_Dificultad] IN ('BEGINNER', 'INTERMEDIATE', 'ADVANCED', 'EXPERT')");

            // 5. SCN_Estatus solo acepta DRAFT, APPROVED, PUBLISHED, RETIRED
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_Estatus",
                "[SCN_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");

            // 6. SCN_DuracionSugeridaMinutos NULL o mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_DuracionSugeridaMinutos",
                "([SCN_DuracionSugeridaMinutos] IS NULL) OR ([SCN_DuracionSugeridaMinutos] > 0)");

            // 7. SCN_PuntuacionObjetivo NULL o entre 1.00 y 10.00
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_PuntuacionObjetivo",
                "([SCN_PuntuacionObjetivo] IS NULL) OR ([SCN_PuntuacionObjetivo] >= 1.00 AND [SCN_PuntuacionObjetivo] <= 10.00)");

            // 8. SCN_MaximoIntentos NULL o mayor que cero
            t.HasCheckConstraint(
                "CK_LAB_Scenario_SCN_MaximoIntentos",
                "([SCN_MaximoIntentos] IS NULL) OR ([SCN_MaximoIntentos] > 0)");

            // 9. SCN_VigenciaHasta NULL o mayor o igual que SCN_VigenciaDesde
            t.HasCheckConstraint(
                "CK_LAB_Scenario_Vigencia",
                "([SCN_VigenciaHasta] IS NULL) OR ([SCN_VigenciaDesde] IS NULL) OR ([SCN_VigenciaHasta] >= [SCN_VigenciaDesde])");
        });

        // 1. Primary Key
        builder.HasKey(e => e.SCN_IdScenario);

        builder.Property(e => e.SCN_IdScenario)
            .IsRequired()
            .ValueGeneratedNever();

        // Clave alternativa requerida para la FK compuesta desde LAB_SimulatedActor
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.SCN_IdScenario })
            .HasName("AK_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario");

        // 2. SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // 3. LAB_IdVersion
        builder.Property(e => e.LAB_IdVersion)
            .IsRequired();

        // 4. SCN_Codigo varchar(60)
        builder.Property(e => e.SCN_Codigo)
            .HasMaxLength(60)
            .IsUnicode(false)
            .IsRequired();

        // 5. SCN_Nombre nvarchar(200)
        builder.Property(e => e.SCN_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // 6. SCN_Descripcion nvarchar(max)
        builder.Property(e => e.SCN_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // 7. SCN_Tipo varchar(30)
        builder.Property(e => e.SCN_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // 8. SCN_Dificultad varchar(20)
        builder.Property(e => e.SCN_Dificultad)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        // 9. SCN_ContextoParticipante nvarchar(max)
        builder.Property(e => e.SCN_ContextoParticipante)
            .IsUnicode(true)
            .IsRequired();

        // 10. SCN_BriefOculto nvarchar(max)
        builder.Property(e => e.SCN_BriefOculto)
            .IsUnicode(true)
            .IsRequired();

        // 11. SCN_ProblemaCentral nvarchar(max)
        builder.Property(e => e.SCN_ProblemaCentral)
            .IsUnicode(true)
            .IsRequired();

        // 12. SCN_ResultadoEsperado nvarchar(max)
        builder.Property(e => e.SCN_ResultadoEsperado)
            .IsUnicode(true)
            .IsRequired();

        // 13. SCN_CondicionesIniciales nvarchar(max) opcional
        builder.Property(e => e.SCN_CondicionesIniciales)
            .IsUnicode(true)
            .IsRequired(false);

        // 14. SCN_Restricciones nvarchar(max) opcional
        builder.Property(e => e.SCN_Restricciones)
            .IsUnicode(true)
            .IsRequired(false);

        // 15. SCN_Supuestos nvarchar(max) opcional
        builder.Property(e => e.SCN_Supuestos)
            .IsUnicode(true)
            .IsRequired(false);

        // 16. SCN_Riesgos nvarchar(max) opcional
        builder.Property(e => e.SCN_Riesgos)
            .IsUnicode(true)
            .IsRequired(false);

        // 17. SCN_InformacionNoRevelarAutomaticamente nvarchar(max) opcional
        builder.Property(e => e.SCN_InformacionNoRevelarAutomaticamente)
            .IsUnicode(true)
            .IsRequired(false);

        // 18. SCN_MensajeInicial nvarchar(max) opcional
        builder.Property(e => e.SCN_MensajeInicial)
            .IsUnicode(true)
            .IsRequired(false);

        // 19. SCN_DuracionSugeridaMinutos int opcional
        builder.Property(e => e.SCN_DuracionSugeridaMinutos)
            .IsRequired(false);

        // 20. SCN_PuntuacionObjetivo decimal(5,2) opcional
        builder.Property(e => e.SCN_PuntuacionObjetivo)
            .HasPrecision(5, 2)
            .IsRequired(false);

        // 21. SCN_PermiteReintento bit
        builder.Property(e => e.SCN_PermiteReintento)
            .HasDefaultValue(true)
            .IsRequired();

        // 22. SCN_MaximoIntentos int opcional
        builder.Property(e => e.SCN_MaximoIntentos)
            .IsRequired(false);

        // 23. SCN_UsaVariacion bit
        builder.Property(e => e.SCN_UsaVariacion)
            .HasDefaultValue(false)
            .IsRequired();

        // 24. SCN_SeedBase nvarchar(100) opcional
        builder.Property(e => e.SCN_SeedBase)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 25. SCN_Estatus varchar(30)
        builder.Property(e => e.SCN_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasDefaultValue(ScenarioEstatus.Draft)
            .IsRequired();

        // 26. SCN_VigenciaDesde datetime2 opcional
        builder.Property(e => e.SCN_VigenciaDesde)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 27. SCN_VigenciaHasta datetime2 opcional
        builder.Property(e => e.SCN_VigenciaHasta)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // 28. FechaCreacion datetimeoffset
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // 29. CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // 30. FechaActualizacion datetimeoffset opcional
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // 31. ActualizadoPor nvarchar(100) opcional
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // 32. RowVersion rowversion - concurrency token
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // Relación 1:N con LAB_LabVersion: FK compuesta (SEG_IdTenant, LAB_IdVersion)
        builder.HasOne(s => s.LabVersion)
            .WithMany(v => v.Scenarios)
            .HasPrincipalKey(v => new { v.SEG_IdTenant, v.LAB_IdVersion })
            .HasForeignKey(s => new { s.SEG_IdTenant, s.LAB_IdVersion })
            .HasConstraintName("FK_LAB_Scenario_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion")
            .OnDelete(DeleteBehavior.Restrict);

        // ÍNDICES
        // 1. Índice único: SEG_IdTenant + LAB_IdVersion + SCN_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.SCN_Codigo }, "UQ_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_Codigo")
            .IsUnique();

        // 2. Índice no único: SEG_IdTenant + LAB_IdVersion + SCN_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion, e.SCN_Estatus }, "IX_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_Estatus");

        // 3. Índice para la Foreign Key: SEG_IdTenant + LAB_IdVersion
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_IdVersion }, "IX_LAB_Scenario_SEG_IdTenant_LAB_IdVersion");
    }
}
