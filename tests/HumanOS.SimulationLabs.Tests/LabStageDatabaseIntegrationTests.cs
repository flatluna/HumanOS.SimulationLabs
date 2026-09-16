using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabStageDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Stage Tests",
            LAB_Descripcion = "Lab creado para probar stages",
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
            LAB_ObjetivoGeneral = "Objetivo general para probar stages",
            LAB_InstruccionesParticipante = "Instrucciones para participantes en etapa",
            LAB_BriefOculto = "Brief confidencial",
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
            STG_Nombre = "Etapa de prueba",
            STG_Descripcion = "Descripción de la etapa de prueba",
            STG_Orden = order,
            STG_TipoInteraccion = StageTipoInteraccion.Conversation,
            STG_EsObligatorio = true,
            STG_CondicionCompletitud = "El participante completó la discusión",
            STG_TiempoSugeridoMinutos = 15,
            STG_PermiteOrdenFlexible = false,
            STG_Estatus = StageEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabStageTable_AppliesSuccessfully()
    {
        // 1. CreateLabStageTable se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabVersionTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabStageTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabStageTable_ExistsInAzureSql()
    {
        // 2. LAB_Stage existe
        using var context = CreateContext();

        var count = await context.Stages.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanCreateValidStage_ForExistingVersion()
    {
        // 3. Se puede crear una etapa válida para una versión existente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "DISCOVERY_REVIEW", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        // Verificar lectura
        using var readContext = CreateContext();
        var savedStage = await readContext.Stages
            .Include(s => s.LabVersion)
            .FirstOrDefaultAsync(s => s.STG_IdStage == stage.STG_IdStage);

        savedStage.Should().NotBeNull();
        savedStage!.STG_Codigo.Should().Be("DISCOVERY_REVIEW");
        savedStage.STG_Orden.Should().Be(1);
        savedStage.STG_TipoInteraccion.Should().Be(StageTipoInteraccion.Conversation);
        savedStage.STG_EsObligatorio.Should().BeTrue();
        savedStage.RowVersion.Should().NotBeNull().And.NotBeEmpty();
        savedStage.LabVersion.Should().NotBeNull();
        savedStage.LabVersion!.LAB_IdVersion.Should().Be(version.LAB_IdVersion);

        // Limpieza ordenada: Stage -> Version -> Lab
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test4_VersionCanHaveMultipleStages()
    {
        // 4. Una versión puede tener varias etapas
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var s1 = CreateStageModel(version, "STG_1", 1);
        var s2 = CreateStageModel(version, "STG_2", 2);
        var s3 = CreateStageModel(version, "STG_3", 3);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.AddRange(s1, s2, s3);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var stages = await readContext.Stages
            .Where(s => s.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(s => s.STG_Orden)
            .ToListAsync();

        stages.Should().HaveCount(3);
        stages.Select(s => s.STG_Orden).Should().Equal(1, 2, 3);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version.LAB_IdVersion));
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_CannotCreateStage_ForNonExistentVersion()
    {
        // 5. No se puede crear una etapa para un LAB_IdVersion inexistente
        using var context = CreateContext();

        var fakeVersion = new LAB_LabVersion { LAB_IdVersion = Guid.NewGuid(), SEG_IdTenant = Guid.NewGuid() };
        var stage = CreateStageModel(fakeVersion, "ORPHAN_STAGE", 1);

        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Stage_LAB_LabVersion");
    }

    [Fact]
    public async Task Test6_CannotRelateStage_ToVersionOfDifferentTenant()
    {
        // 6. No se puede relacionar una etapa con una versión de otro tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stageWithMismatchedTenant = CreateStageModel(version, "CROSS_TENANT", 1);
        stageWithMismatchedTenant.SEG_IdTenant = Guid.NewGuid(); // Tenant distinto

        context.Stages.Add(stageWithMismatchedTenant);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // FK violation sobre clave compuesta
        sqlEx.Message.Should().Contain("FK_LAB_Stage_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test7_CannotRepeatStageCodigo_WithinSameVersion()
    {
        // 7. No se puede repetir STG_Codigo dentro de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var s1 = CreateStageModel(version, "DUPLICATE_CODE", 1);
        context.Stages.Add(s1);
        await context.SaveChangesAsync();

        var s2 = CreateStageModel(version, "DUPLICATE_CODE", 2); // Mismo código, distinto orden
        context.Stages.Add(s2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Codigo");

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(s1.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test8_SameStageCodigo_CanBeUsedInDifferentVersions()
    {
        // 8. El mismo STG_Codigo puede utilizarse en versiones diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        var s1 = CreateStageModel(version1, "SHARED_CODE", 1);
        var s2 = CreateStageModel(version2, "SHARED_CODE", 1);
        context.Stages.AddRange(s1, s2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version1.LAB_IdVersion || s.LAB_IdVersion == version2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test9_CannotRepeatStageOrden_WithinSameVersion()
    {
        // 9. No se puede repetir STG_Orden dentro de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var s1 = CreateStageModel(version, "ORDER_CODE_1", 1);
        context.Stages.Add(s1);
        await context.SaveChangesAsync();

        var s2 = CreateStageModel(version, "ORDER_CODE_2", 1); // Distinto código, mismo orden 1
        context.Stages.Add(s2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(s1.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_SameStageOrden_CanBeUsedInDifferentVersions()
    {
        // 10. El mismo STG_Orden puede utilizarse en versiones diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        await context.SaveChangesAsync();

        var s1 = CreateStageModel(version1, "STAGE_A", 1);
        var s2 = CreateStageModel(version2, "STAGE_B", 1);
        context.Stages.AddRange(s1, s2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version1.LAB_IdVersion || s.LAB_IdVersion == version2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test11_StageOrden_RejectsZero()
    {
        // 11. STG_Orden rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "ZERO_ORDER", 0);
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test12_StageOrden_RejectsNegativeNumbers()
    {
        // 12. STG_Orden rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "NEGATIVE_ORDER", -5);
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_Orden");

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
    [InlineData("DESKTOP")]
    [InlineData("HYBRID")]
    [InlineData("ARTIFACT")]
    public async Task Test13_16_StageTipoInteraccion_AcceptsValidTypes(string interactionType)
    {
        // 13-16. STG_TipoInteraccion acepta CONVERSATION, DESKTOP, HYBRID, ARTIFACT
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, $"TIPO_{interactionType}", 1);
        stage.STG_TipoInteraccion = interactionType;
        context.Stages.Add(stage);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test17_StageTipoInteraccion_RejectsInvalidValue()
    {
        // 17. STG_TipoInteraccion rechaza un valor no permitido
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "INVALID_TYPE", 1);
        stage.STG_TipoInteraccion = "VOICE_LIVE_INVALID";
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_TipoInteraccion");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test18_StageTiempoSugeridoMinutos_AcceptsNull()
    {
        // 18. STG_TiempoSugeridoMinutos acepta NULL
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "NULL_MINUTES", 1);
        stage.STG_TiempoSugeridoMinutos = null;
        context.Stages.Add(stage);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test19_StageTiempoSugeridoMinutos_RejectsZero()
    {
        // 19. STG_TiempoSugeridoMinutos rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "ZERO_MINUTES", 1);
        stage.STG_TiempoSugeridoMinutos = 0;
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_TiempoSugeridoMinutos");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test20_StageTiempoSugeridoMinutos_RejectsNegativeNumbers()
    {
        // 20. STG_TiempoSugeridoMinutos rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "NEGATIVE_MINUTES", 1);
        stage.STG_TiempoSugeridoMinutos = -10;
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_TiempoSugeridoMinutos");

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
    public async Task Test21_StageEstatus_AcceptsValidStatuses(string status)
    {
        // 21. STG_Estatus acepta DRAFT, ACTIVE e INACTIVE
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, $"STATUS_{status}", 1);
        stage.STG_Estatus = status;
        context.Stages.Add(stage);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test22_StageEstatus_RejectsInvalidValue()
    {
        // 22. STG_Estatus rechaza un valor no permitido
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var stage = CreateStageModel(version, "INVALID_STATUS", 1);
        stage.STG_Estatus = "OBSOLETE";
        context.Stages.Add(stage);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Stage_STG_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test23_CannotHardDeleteLabVersion_WhenItHasStages()
    {
        // 23. No se puede eliminar físicamente una versión que tenga etapas relacionadas (Restrict / No Action)
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STAGE_BLOCKING_DELETE", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        // Intentar eliminar la versión mientras tiene etapa
        using var deleteContext = CreateContext();
        var vToDelete = await deleteContext.LabVersions.FindAsync(version.LAB_IdVersion);
        deleteContext.LabVersions.Remove(vToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Conflicto con constraint de FK

        // Limpieza en orden referencial: Stage -> Version -> Lab
        using var cleanup = CreateContext();
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
    public async Task Test24_RowVersion_DetectsConcurrentUpdateOnStage()
    {
        // 24. RowVersion detecta una actualización concurrente de LAB_Stage
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "CONCURRENT_STAGE", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        // Dos contextos cargando el mismo stage simultáneamente
        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.Stages.FindAsync(stage.STG_IdStage);
        var entity2 = await client2.Stages.FindAsync(stage.STG_IdStage);

        entity1!.STG_Nombre = "Nombre Actualizado por Cliente 1";
        await client1.SaveChangesAsync();

        entity2!.STG_Nombre = "Nombre Actualizado por Cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanup = CreateContext();
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test25_MigrationDoesNotCreateAdditionalTables()
    {
        // 25. La migración no crea tablas adicionales fuera de las permitidas
        using var context = CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME LIKE 'LAB_%' ORDER BY TABLE_NAME")
            .ToListAsync();

        tables.Should().Contain(new[] { "LAB_Lab", "LAB_LabVersion", "LAB_Stage" });
        tables.Should().OnlyContain(t =>
            t == "LAB_ArtifactSubmission" || t == "LAB_Attempt" || t == "LAB_AttemptEvaluation" ||
            t == "LAB_ConversationTurn" || t == "LAB_Enrollment" || t == "LAB_ExpectedMoment" ||
            t == "LAB_Lab" || t == "LAB_LabVersion" || t == "LAB_Objective" || t == "LAB_Rubric" ||
            t == "LAB_RubricCriterion" || t == "LAB_Scenario" || t == "LAB_SimulatedActor" ||
            t == "LAB_Stage" || t == "LAB_TestedSkill" || t == "LAB_UserAction");
    }
}
