using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabObjectiveDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Objective Tests",
            LAB_Descripcion = "Lab creado para probar objetivos",
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
            LAB_ObjetivoGeneral = "Objetivo general de la versión para probar objetivos",
            LAB_InstruccionesParticipante = "Instrucciones de prueba",
            LAB_BriefOculto = "Brief oculto",
            LAB_DuracionMinutos = 60,
            LAB_ScoreMinimo = 7.00m,
            LAB_Estatus = LabVersionEstatus.Published,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Stage CreateStageModel(LAB_LabVersion version, string code = "STG_TEST", int order = 1)
    {
        return new LAB_Stage
        {
            STG_IdStage = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            SEG_IdTenant = version.SEG_IdTenant,
            STG_Codigo = code,
            STG_Nombre = "Etapa para objetivos",
            STG_Descripcion = "Descripción de la etapa para objetivos",
            STG_Orden = order,
            STG_TipoInteraccion = StageTipoInteraccion.Conversation,
            STG_EsObligatorio = true,
            STG_CondicionCompletitud = "Condicion completada",
            STG_TiempoSugeridoMinutos = 20,
            STG_PermiteOrdenFlexible = false,
            STG_Estatus = StageEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Objective CreateObjectiveModel(
        LAB_LabVersion version,
        LAB_Stage? stage = null,
        string code = "OBJ_TEST",
        int order = 1,
        string evidenceType = ObjectiveTipoEvidencia.Conversation,
        decimal weight = 10.0m)
    {
        return new LAB_Objective
        {
            OBJ_IdObjective = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            STG_IdStage = stage?.STG_IdStage,
            SEG_IdTenant = version.SEG_IdTenant,
            OBJ_Codigo = code,
            OBJ_Descripcion = "Descripción del objetivo observable",
            OBJ_TipoEvidencia = evidenceType,
            OBJ_EsCritico = true,
            OBJ_Peso = weight,
            OBJ_CondicionExito = "El participante demostró el comportamiento esperado",
            OBJ_Orden = order,
            OBJ_Estatus = ObjectiveEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabObjectiveTable_AppliesSuccessfully()
    {
        // 1. La migración se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabVersionTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabStageTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabObjectiveTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabObjectiveTable_ExistsInAzureSql()
    {
        // 2. LAB_Objective existe
        using var context = CreateContext();

        var count = await context.Objectives.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanCreateGeneralObjective_WithoutStage()
    {
        // 3. Se puede crear un objetivo general sin STG_IdStage
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var objective = CreateObjectiveModel(version, stage: null, "GENERAL_OBJ", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.Objectives
            .Include(o => o.LabVersion)
            .Include(o => o.Stage)
            .FirstOrDefaultAsync(o => o.OBJ_IdObjective == objective.OBJ_IdObjective);

        saved.Should().NotBeNull();
        saved!.STG_IdStage.Should().BeNull();
        saved.Stage.Should().BeNull();
        saved.LabVersion.Should().NotBeNull();
        saved.LabVersion!.LAB_IdVersion.Should().Be(version.LAB_IdVersion);
        saved.RowVersion.Should().NotBeNull().And.NotBeEmpty();

        // Limpieza: Objective -> Version -> Lab
        using var cleanup = CreateContext();
        var oToDelete = await cleanup.Objectives.FindAsync(objective.OBJ_IdObjective);
        if (oToDelete is not null) cleanup.Objectives.Remove(oToDelete);
        var vToDelete = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vToDelete is not null) cleanup.LabVersions.Remove(vToDelete);
        var lToDelete = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lToDelete is not null) cleanup.Labs.Remove(lToDelete);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test4_CanCreateObjective_AssociatedWithStage()
    {
        // 4. Se puede crear un objetivo asociado con una etapa
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STAGE_FOR_OBJ", 1);
        var objective = CreateObjectiveModel(version, stage, "STAGE_OBJ", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.Objectives
            .Include(o => o.LabVersion)
            .Include(o => o.Stage)
            .FirstOrDefaultAsync(o => o.OBJ_IdObjective == objective.OBJ_IdObjective);

        saved.Should().NotBeNull();
        saved!.STG_IdStage.Should().Be(stage.STG_IdStage);
        saved.Stage.Should().NotBeNull();
        saved.Stage!.STG_IdStage.Should().Be(stage.STG_IdStage);
        saved.LabVersion.Should().NotBeNull();
        saved.LabVersion!.LAB_IdVersion.Should().Be(version.LAB_IdVersion);

        // Limpieza: Objective -> Stage -> Version -> Lab
        using var cleanup = CreateContext();
        var oToDelete = await cleanup.Objectives.FindAsync(objective.OBJ_IdObjective);
        if (oToDelete is not null) cleanup.Objectives.Remove(oToDelete);
        var sToDelete = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sToDelete is not null) cleanup.Stages.Remove(sToDelete);
        var vToDelete = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vToDelete is not null) cleanup.LabVersions.Remove(vToDelete);
        var lToDelete = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lToDelete is not null) cleanup.Labs.Remove(lToDelete);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_VersionCanHaveMultipleObjectives()
    {
        // 5. Una versión puede tener múltiples objetivos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var o1 = CreateObjectiveModel(version, null, "VER_OBJ_1", 1);
        var o2 = CreateObjectiveModel(version, null, "VER_OBJ_2", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.AddRange(o1, o2);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var objectives = await readContext.Objectives
            .Where(o => o.LAB_IdVersion == version.LAB_IdVersion)
            .ToListAsync();

        objectives.Should().HaveCount(2);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Objectives.RemoveRange(cleanup.Objectives.Where(o => o.LAB_IdVersion == version.LAB_IdVersion));
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test6_StageCanHaveMultipleObjectives()
    {
        // 6. Una etapa puede tener múltiples objetivos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STAGE_MULTI_OBJ", 1);
        var o1 = CreateObjectiveModel(version, stage, "STG_OBJ_1", 1);
        var o2 = CreateObjectiveModel(version, stage, "STG_OBJ_2", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.Objectives.AddRange(o1, o2);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var objectives = await readContext.Objectives
            .Where(o => o.STG_IdStage == stage.STG_IdStage)
            .ToListAsync();

        objectives.Should().HaveCount(2);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Objectives.RemoveRange(cleanup.Objectives.Where(o => o.STG_IdStage == stage.STG_IdStage));
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test7_CannotCreateObjective_ForNonExistentVersion()
    {
        // 7. No se puede crear un objetivo para una versión inexistente
        using var context = CreateContext();

        var fakeVersion = new LAB_LabVersion { LAB_IdVersion = Guid.NewGuid(), SEG_IdTenant = Guid.NewGuid() };
        var objective = CreateObjectiveModel(fakeVersion, null, "ORPHAN_OBJ", 1);

        context.Objectives.Add(objective);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Objective_LAB_LabVersion");
    }

    [Fact]
    public async Task Test8_CannotCreateObjective_ForVersionOfDifferentTenant()
    {
        // 8. No se puede crear un objetivo para una versión de otro tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var objective = CreateObjectiveModel(version, null, "CROSS_TENANT_OBJ", 1);
        objective.SEG_IdTenant = Guid.NewGuid(); // Tenant distinto

        context.Objectives.Add(objective);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Objective_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test9_CannotAssociateObjective_WithNonExistentStage()
    {
        // 9. No se puede asociar un objetivo con una etapa inexistente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var objective = CreateObjectiveModel(version, null, "BAD_STAGE_OBJ", 1);
        objective.STG_IdStage = Guid.NewGuid(); // Etapa inexistente

        context.Objectives.Add(objective);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_CannotAssociateObjective_WithStageOfDifferentTenant()
    {
        // 10. No se puede asociar un objetivo con una etapa de otro tenant
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        context.Labs.Add(lab1);
        context.LabVersions.Add(version1);

        var lab2 = CreateLabModel(); // Tenant distinto
        var version2 = CreateVersionModel(lab2, 1);
        var stage2 = CreateStageModel(version2, "STG_TENANT_2", 1);
        context.Labs.Add(lab2);
        context.LabVersions.Add(version2);
        context.Stages.Add(stage2);

        await context.SaveChangesAsync();

        // Objetivo perteneciente al Tenant 1 y Version 1, pero intentando asociar stage2 del Tenant 2
        var objective = CreateObjectiveModel(version1, null, "MISMATCH_TENANT_OBJ", 1);
        objective.STG_IdStage = stage2.STG_IdStage;

        context.Objectives.Add(objective);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage2.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v1 = await cleanup.LabVersions.FindAsync(version1.LAB_IdVersion);
        if (v1 is not null) cleanup.LabVersions.Remove(v1);
        var v2 = await cleanup.LabVersions.FindAsync(version2.LAB_IdVersion);
        if (v2 is not null) cleanup.LabVersions.Remove(v2);
        var l1 = await cleanup.Labs.FindAsync(lab1.LAB_IdLab);
        if (l1 is not null) cleanup.Labs.Remove(l1);
        var l2 = await cleanup.Labs.FindAsync(lab2.LAB_IdLab);
        if (l2 is not null) cleanup.Labs.Remove(l2);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test11_CannotAssociateObjective_WithStageOfDifferentVersion()
    {
        // 11. No se puede asociar un objetivo de una versión con una etapa de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        var stageInVersion2 = CreateStageModel(version2, "STG_IN_V2", 1);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.Add(stageInVersion2);
        await context.SaveChangesAsync();

        // Objetivo para version1 pero apuntando a stageInVersion2
        var objective = CreateObjectiveModel(version1, null, "CROSS_VER_OBJ", 1);
        objective.STG_IdStage = stageInVersion2.STG_IdStage;

        context.Objectives.Add(objective);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Objective_LAB_Stage_Tenant_Version_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stageInVersion2.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test12_CannotRepeatObjectiveCodigo_WithinSameVersion()
    {
        // 12. No se puede repetir OBJ_Codigo dentro de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var o1 = CreateObjectiveModel(version, null, "DUP_OBJ_CODE", 1);
        context.Objectives.Add(o1);
        await context.SaveChangesAsync();

        var o2 = CreateObjectiveModel(version, null, "DUP_OBJ_CODE", 2);
        context.Objectives.Add(o2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_Codigo");

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(o1.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test13_SameObjectiveCodigo_CanBeUsedInDifferentVersions()
    {
        // 13. El mismo OBJ_Codigo puede utilizarse en versiones diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        var o1 = CreateObjectiveModel(version1, null, "SHARED_OBJ_CODE", 1);
        var o2 = CreateObjectiveModel(version2, null, "SHARED_OBJ_CODE", 1);
        context.Objectives.AddRange(o1, o2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Objectives.RemoveRange(cleanup.Objectives.Where(o => o.LAB_IdVersion == version1.LAB_IdVersion || o.LAB_IdVersion == version2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test14_ObjectivePeso_RejectsZero()
    {
        // 14. OBJ_Peso rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "ZERO_PESO", 1, weight: 0.0000m);
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_Peso");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test15_ObjectivePeso_RejectsNegativeNumbers()
    {
        // 15. OBJ_Peso rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "NEG_PESO", 1, weight: -2.5m);
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_Peso");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test16_ObjectiveOrden_RejectsZero()
    {
        // 16. OBJ_Orden rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "ZERO_ORDEN", 0);
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test17_ObjectiveOrden_RejectsNegativeNumbers()
    {
        // 17. OBJ_Orden rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "NEG_ORDEN", -10);
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("CONVERSATION")]
    [InlineData("USER_ACTION")]
    [InlineData("ARTIFACT")]
    [InlineData("DECISION")]
    [InlineData("SYSTEM_RESULT")]
    public async Task Test18_ObjectiveTipoEvidencia_AcceptsAllFiveAllowedValues(string tipoEvidencia)
    {
        // 18. OBJ_TipoEvidencia acepta los cinco valores permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, $"EVID_{tipoEvidencia}", 1, evidenceType: tipoEvidencia);
        context.Objectives.Add(obj);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(obj.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test19_ObjectiveTipoEvidencia_RejectsInvalidValue()
    {
        // 19. OBJ_TipoEvidencia rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "INVALID_EVID", 1);
        obj.OBJ_TipoEvidencia = "INVALID_EVIDENCE_TYPE";
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_TipoEvidencia");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("ACTIVE")]
    [InlineData("INACTIVE")]
    public async Task Test20_ObjectiveEstatus_AcceptsDraftActiveInactive(string status)
    {
        // 20. OBJ_Estatus acepta DRAFT, ACTIVE e INACTIVE
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, $"STATUS_{status}", 1);
        obj.OBJ_Estatus = status;
        context.Objectives.Add(obj);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(obj.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test21_ObjectiveEstatus_RejectsInvalidValue()
    {
        // 21. OBJ_Estatus rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var obj = CreateObjectiveModel(version, null, "INVALID_OBJ_STATUS", 1);
        obj.OBJ_Estatus = "DEPRECATED";
        context.Objectives.Add(obj);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Objective_OBJ_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test22_ObjectiveEsCritico_PersistsCorrectly()
    {
        // 22. OBJ_EsCritico se guarda correctamente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var oCritical = CreateObjectiveModel(version, null, "CRITICAL_OBJ", 1);
        oCritical.OBJ_EsCritico = true;
        var oNotCritical = CreateObjectiveModel(version, null, "NOT_CRITICAL_OBJ", 2);
        oNotCritical.OBJ_EsCritico = false;

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.AddRange(oCritical, oNotCritical);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved1 = await readContext.Objectives.FindAsync(oCritical.OBJ_IdObjective);
        var saved2 = await readContext.Objectives.FindAsync(oNotCritical.OBJ_IdObjective);

        saved1!.OBJ_EsCritico.Should().BeTrue();
        saved2!.OBJ_EsCritico.Should().BeFalse();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Objectives.RemoveRange(cleanup.Objectives.Where(o => o.LAB_IdVersion == version.LAB_IdVersion));
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test23_TwoGeneralObjectives_CannotRepeatOrden_WithinSameVersion()
    {
        // 23. Dos objetivos generales no pueden repetir OBJ_Orden dentro de la misma versión (Índice filtrado)
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var o1 = CreateObjectiveModel(version, null, "GEN_1", 1);
        context.Objectives.Add(o1);
        await context.SaveChangesAsync();

        var o2 = CreateObjectiveModel(version, null, "GEN_2", 1); // Mismo orden 1 para objetivos generales
        context.Objectives.Add(o2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Objective_General_Tenant_Version_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(o1.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test24_ObjectivesOfDifferentStages_CanUseSameOrden()
    {
        // 24. Objetivos de etapas diferentes pueden utilizar el mismo OBJ_Orden
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var s1 = CreateStageModel(version, "STG_A", 1);
        var s2 = CreateStageModel(version, "STG_B", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var o1 = CreateObjectiveModel(version, s1, "OBJ_STAGE_A", 1);
        var o2 = CreateObjectiveModel(version, s2, "OBJ_STAGE_B", 1); // Mismo orden 1 pero en distinta etapa
        context.Objectives.AddRange(o1, o2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Objectives.RemoveRange(cleanup.Objectives.Where(o => o.LAB_IdVersion == version.LAB_IdVersion));
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version.LAB_IdVersion));
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test25_TwoObjectivesInSameStage_CannotRepeatOrden()
    {
        // 25. Dos objetivos dentro de la misma etapa no pueden repetir OBJ_Orden
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_ORDER_TEST", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var o1 = CreateObjectiveModel(version, stage, "STAGE_OBJ_A", 1);
        context.Objectives.Add(o1);
        await context.SaveChangesAsync();

        var o2 = CreateObjectiveModel(version, stage, "STAGE_OBJ_B", 1); // Mismo orden 1 dentro de la misma etapa
        context.Objectives.Add(o2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Objective_Stage_Tenant_Version_Stage_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(o1.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test26_CannotHardDeleteLabVersion_WhenItHasObjectives()
    {
        // 26. No se puede eliminar físicamente una versión que tenga objetivos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var obj = CreateObjectiveModel(version, null, "BLOCK_DEL_OBJ", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.Add(obj);
        await context.SaveChangesAsync();

        using var deleteContext = CreateContext();
        var vToDelete = await deleteContext.LabVersions.FindAsync(version.LAB_IdVersion);
        deleteContext.LabVersions.Remove(vToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Foreign key conflict on delete

        // Limpieza en orden referencial: Objective -> Version -> Lab
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(obj.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        await cleanup.SaveChangesAsync();

        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        await cleanup.SaveChangesAsync();

        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test27_CannotHardDeleteStage_WhenItHasObjectives()
    {
        // 27. No se puede eliminar físicamente una etapa que tenga objetivos asociados
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_BLOCK_DEL", 1);
        var obj = CreateObjectiveModel(version, stage, "STAGE_BLOCK_DEL_OBJ", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.Objectives.Add(obj);
        await context.SaveChangesAsync();

        using var deleteContext = CreateContext();
        var sToDelete = await deleteContext.Stages.FindAsync(stage.STG_IdStage);
        deleteContext.Stages.Remove(sToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Foreign key conflict on delete

        // Limpieza en orden referencial: Objective -> Stage -> Version -> Lab
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(obj.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        await cleanup.SaveChangesAsync();

        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        await cleanup.SaveChangesAsync();

        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        await cleanup.SaveChangesAsync();

        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test28_RowVersion_DetectsConcurrentUpdateOnObjective()
    {
        // 28. RowVersion detecta una actualización concurrente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var obj = CreateObjectiveModel(version, null, "CONCURRENT_OBJ", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.Add(obj);
        await context.SaveChangesAsync();

        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.Objectives.FindAsync(obj.OBJ_IdObjective);
        var entity2 = await client2.Objectives.FindAsync(obj.OBJ_IdObjective);

        entity1!.OBJ_Descripcion = "Descripción cliente 1";
        await client1.SaveChangesAsync();

        entity2!.OBJ_Descripcion = "Descripción cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanup = CreateContext();
        var o = await cleanup.Objectives.FindAsync(obj.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test29_MigrationDoesNotCreateAdditionalTables()
    {
        // 29. La migración no crea tablas adicionales fuera de las especificadas
        using var context = CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME LIKE 'LAB_%' ORDER BY TABLE_NAME")
            .ToListAsync();

        tables.Should().BeEquivalentTo(new[]
        {
            "LAB_ArtifactSubmission", "LAB_Attempt", "LAB_AttemptEvaluation", "LAB_ConversationTurn",
            "LAB_Enrollment", "LAB_ExpectedMoment", "LAB_Lab", "LAB_LabVersion", "LAB_Objective",
            "LAB_Rubric", "LAB_RubricCriterion", "LAB_Scenario", "LAB_SimulatedActor", "LAB_Stage",
            "LAB_TestedSkill", "LAB_UserAction"
        });
    }
}
