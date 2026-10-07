using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanOS.SimulationLabs.Configurations;

public class LAB_LabConfiguration : IEntityTypeConfiguration<LAB_Lab>
{
    public void Configure(EntityTypeBuilder<LAB_Lab> builder)
    {
        builder.ToTable("LAB_Lab", t =>
        {
            t.HasCheckConstraint(
                "CK_LAB_Lab_LAB_Tipo",
                "[LAB_Tipo] IN ('CONVERSATIONAL', 'DESKTOP', 'HYBRID')");

            t.HasCheckConstraint(
                "CK_LAB_Lab_LAB_Estatus",
                "[LAB_Estatus] IN ('DRAFT', 'PUBLISHED', 'RETIRED')");
        });

        // Primary Key
        builder.HasKey(e => e.LAB_IdLab);

        builder.Property(e => e.LAB_IdLab)
            .IsRequired()
            .ValueGeneratedNever();

        // SEG_IdTenant
        builder.Property(e => e.SEG_IdTenant)
            .IsRequired();

        // LAB_Codigo varchar(50)
        builder.Property(e => e.LAB_Codigo)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        // LAB_Nombre nvarchar(200)
        builder.Property(e => e.LAB_Nombre)
            .HasMaxLength(200)
            .IsUnicode(true)
            .IsRequired();

        // LAB_Descripcion nvarchar(max)
        builder.Property(e => e.LAB_Descripcion)
            .IsUnicode(true)
            .IsRequired();

        // LAB_Tipo varchar(30)
        builder.Property(e => e.LAB_Tipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // LAB_Arquetipo varchar(30) — nullable (Labs created before this concept existed)
        builder.Property(e => e.LAB_Arquetipo)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired(false);

        // LAB_Dominio varchar(50)
        builder.Property(e => e.LAB_Dominio)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        // LAB_Estatus varchar(30)
        builder.Property(e => e.LAB_Estatus)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        // LAB_EsGlobal bit — visible to every tenant, not just SEG_IdTenant's own
        builder.Property(e => e.LAB_EsGlobal)
            .HasDefaultValue(false)
            .IsRequired();

        // LAB_OwnerId
        builder.Property(e => e.LAB_OwnerId)
            .IsRequired();

        // FechaCreacion
        builder.Property(e => e.FechaCreacion)
            .IsRequired();

        // CreadoPor nvarchar(100)
        builder.Property(e => e.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        // FechaActualizacion
        builder.Property(e => e.FechaActualizacion)
            .IsRequired(false);

        // ActualizadoPor nvarchar(100)
        builder.Property(e => e.ActualizadoPor)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired(false);

        // RowVersion rowversion concurrency token
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        // GEN_ModelName varchar(100) — AI Lab Builder generation cost snapshot (all nullable)
        builder.Property(e => e.GEN_ModelName)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.GEN_EstimatedCostUsd)
            .HasPrecision(10, 4)
            .IsRequired(false);

        // Restricción única para SEG_IdTenant + LAB_Codigo
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_Codigo }, "UQ_LAB_Lab_SEG_IdTenant_LAB_Codigo")
            .IsUnique();

        // Clave alternativa (Alternate Key) para permitir Foreign Key compuesta (SEG_IdTenant + LAB_IdLab)
        builder.HasAlternateKey(e => new { e.SEG_IdTenant, e.LAB_IdLab })
            .HasName("AK_LAB_Lab_SEG_IdTenant_LAB_IdLab");

        // Índice para SEG_IdTenant + LAB_Estatus
        builder.HasIndex(e => new { e.SEG_IdTenant, e.LAB_Estatus }, "IX_LAB_Lab_SEG_IdTenant_LAB_Estatus");

        // Relación 1:N con LAB_LabVersion
        builder.HasMany(e => e.Versiones)
            .WithOne(v => v.Lab)
            .HasPrincipalKey(e => new { e.SEG_IdTenant, e.LAB_IdLab })
            .HasForeignKey(v => new { v.SEG_IdTenant, v.LAB_IdLab })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
