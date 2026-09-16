using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabScenarioDatabaseIntegrationTests
{
    private SimulationLabsDbContext CreateContext()
    {
        return DbContextHelper.CreateDbContext();
    }

    private static LAB_Lab CreateLabModel(Guid? tenantId = null, string? code = null)
    {
        return new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenantId ?? Guid.NewGuid(),
            LAB_Codigo = code ?? $"LAB_{Guid.NewGuid():N}"[..20],
            LAB_Nombre = "Lab Base For Scenario Tests",
            LAB_Descripcion = "Lab creado para probar escenarios",
            LAB_Tipo = LabTipos.Conversational,
            LAB_Dominio = "DISCOVERY",
            LAB_Estatus = LabEstatus.Published,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_LabVersion CreateVersionModel(LAB_Lab lab, int versionNumber = 1)
    {
        return new LAB_LabVersion
        {
            LAB_IdVersion = Guid.NewGuid(),
            LAB_IdLab = lab.LAB_IdLab,
            SEG_IdTenant = lab.SEG_IdTenant,
            LAB_NumeroVersion = versionNumber,
            LAB_ObjetivoGeneral = "Objetivo general para probar escenarios",
            LAB_InstruccionesParticipante = "Instrucciones de versión",
            LAB_DuracionMinutos = 60,
            LAB_ScoreMinimo = 7.00m,
            LAB_Estatus = LabVersionEstatus.Published,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Scenario CreateScenarioModel(LAB_LabVersion version, string code = "SCN_TEST")
    {
        return new LAB_Scenario
        {
            SCN_IdScenario = Guid.NewGuid(),
            SEG_IdTenant = version.SEG_IdTenant,
            LAB_IdVersion = version.LAB_IdVersion,
            SCN_Codigo = code,
            SCN_Nombre = "Discovery con cliente que quiere implementar IA",
            SCN_Descripcion = "El participante conduce una entrevista de discovery con un cliente interesado en IA.",
            SCN_Tipo = ScenarioTipo.Conversational,
            SCN_Dificultad = ScenarioDificultad.Intermediate,
            SCN_ContextoParticipante = "Eres consultor y tendrás una llamada de discovery con un cliente potencial.",
            SCN_BriefOculto = "El cliente en realidad ya evaluó dos competidores y busca validar precio.",
            SCN_ProblemaCentral = "El cliente no tiene claridad sobre su caso de uso de IA.",
            SCN_ResultadoEsperado = "El participante identifica el caso de uso y los criterios de éxito del cliente.",
            SCN_PermiteReintento = true,
            SCN_UsaVariacion = false,
            SCN_Estatus = ScenarioEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabScenarioTable_AppliesSuccessfully()
    {
        // 1. La migración se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabScenarioTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabScenarioTable_ExistsInAzureSql()
    {
        // 2. LAB_Scenario existe
        using var context = CreateContext();

        var count = await context.Scenarios.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanInsertValidScenario_AndRelationWithLabVersionWorks()
    {
        // 3. Se puede insertar un escenario válido
        // 4. La relación con LAB_LabVersion funciona
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var scenario = CreateScenarioModel(version, "SCN_DISCOVERY_IA");

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Scenarios.Add(scenario);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.Scenarios
            .Include(s => s.LabVersion)
            .FirstOrDefaultAsync(s => s.SCN_IdScenario == scenario.SCN_IdScenario);

        saved.Should().NotBeNull();
        saved!.SCN_Codigo.Should().Be("SCN_DISCOVERY_IA");
        saved.SCN_Estatus.Should().Be(ScenarioEstatus.Draft);
        saved.SCN_PermiteReintento.Should().BeTrue();
        saved.RowVersion.Should().NotBeNull().And.NotBeEmpty();
        saved.LabVersion.Should().NotBeNull();
        saved.LabVersion!.LAB_IdVersion.Should().Be(version.LAB_IdVersion);

        // Limpieza: Scenario -> Version -> Lab
        using var cleanup = CreateContext();
        var sDel = await cleanup.Scenarios.FindAsync(scenario.SCN_IdScenario);
        if (sDel is not null) cleanup.Scenarios.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }
}
