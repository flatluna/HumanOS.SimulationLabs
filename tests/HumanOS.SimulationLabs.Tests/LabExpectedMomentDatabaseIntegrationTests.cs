using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabExpectedMomentDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Moment Tests",
            LAB_Descripcion = "Lab creado para probar momentos esperados",
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
            LAB_ObjetivoGeneral = "Objetivo de versión para momentos",
            LAB_InstruccionesParticipante = "Instrucciones de momentos",
            LAB_DuracionMinutos = 60,
            LAB_ScoreMinimo = 7.00m,
            LAB_Estatus = LabVersionEstatus.Published,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Stage CreateStageModel(LAB_LabVersion version, string code = "STG_MOM_TEST", int order = 1)
    {
        return new LAB_Stage
        {
            STG_IdStage = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            SEG_IdTenant = version.SEG_IdTenant,
            STG_Codigo = code,
            STG_Nombre = "Etapa de momentos",
            STG_Descripcion = "Descripción etapa",
            STG_Orden = order,
            STG_TipoInteraccion = StageTipoInteraccion.Conversation,
            STG_EsObligatorio = true,
            STG_Estatus = StageEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Objective CreateObjectiveModel(LAB_LabVersion version, LAB_Stage? stage = null, string code = "OBJ_MOM_TEST", int order = 1)
    {
        return new LAB_Objective
        {
            OBJ_IdObjective = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            STG_IdStage = stage?.STG_IdStage,
            SEG_IdTenant = version.SEG_IdTenant,
            OBJ_Codigo = code,
            OBJ_Descripcion = "Descripción objetivo",
            OBJ_TipoEvidencia = ObjectiveTipoEvidencia.Conversation,
            OBJ_EsCritico = true,
            OBJ_Peso = 10.0m,
            OBJ_CondicionExito = "Condición de éxito",
            OBJ_Orden = order,
            OBJ_Estatus = ObjectiveEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_ExpectedMoment CreateMomentModel(
        LAB_LabVersion version,
        LAB_Stage stage,
        LAB_Objective? objective = null,
        string code = "MOM_TEST",
        int order = 1,
        string tipo = MomentTipo.Dialogue)
    {
        return new LAB_ExpectedMoment
        {
            MOM_IdExpectedMoment = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            STG_IdStage = stage.STG_IdStage,
            OBJ_IdObjective = objective?.OBJ_IdObjective,
            SEG_IdTenant = version.SEG_IdTenant,
            MOM_Codigo = code,
            MOM_Nombre = "Momento esperado de prueba",
            MOM_Tipo = tipo,
            MOM_Trigger = "El cliente simulado dice: 'Quiero un agente que identifique a los clientes que no pagaron.'",
            MOM_IntencionEsperada = "El participante evita diseñar inmediatamente y pregunta por el problema, el proceso actual y el impacto.",
            MOM_RespuestaEjemplar = "Antes de hablar de la solución, ¿podría explicarme cómo identifican actualmente los pagos vencidos?",
            MOM_InformacionDescubrible = "El proceso toma 4 horas diarias y dos personas preparan el reporte.",
            MOM_ErrorFrecuente = "Proponer directamente un chatbot o agente sin hacer Discovery.",
            MOM_Recomendacion = "Formula preguntas abiertas sobre el proceso actual antes de diseñar.",
            MOM_EsCritico = true,
            MOM_OrdenSugerido = order,
            MOM_PermiteOrdenFlexible = true,
            MOM_RequiereRespuesta = true,
            MOM_Estatus = MomentEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabExpectedMomentTable_AppliesSuccessfully()
    {
        // 1. CreateLabExpectedMomentTable se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabVersionTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabStageTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabObjectiveTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabExpectedMomentTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabExpectedMomentTable_ExistsInAzureSql()
    {
        // 2. LAB_ExpectedMoment existe
        using var context = CreateContext();

        var count = await context.ExpectedMoments.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanCreateValidMoment_AssociatedWithStage()
    {
        // 3. Se puede crear un momento válido asociado con una etapa
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_VALID", 1);
        var moment = CreateMomentModel(version, stage, null, "MOM_STAGE_ONLY", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.ExpectedMoments
            .Include(m => m.Stage)
            .Include(m => m.LabVersion)
            .FirstOrDefaultAsync(m => m.MOM_IdExpectedMoment == moment.MOM_IdExpectedMoment);

        saved.Should().NotBeNull();
        saved!.STG_IdStage.Should().Be(stage.STG_IdStage);
        saved.Stage.Should().NotBeNull();
        saved.LabVersion.Should().NotBeNull();
        saved.OBJ_IdObjective.Should().BeNull();
        saved.RowVersion.Should().NotBeNull().And.NotBeEmpty();

        // Limpieza: Moment -> Stage -> Version -> Lab
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test4_5_CanCreateValidMoment_WithObjectiveAndWithoutObjective()
    {
        // 4. Se puede crear un momento válido asociado con una etapa y un objetivo
        // 5. Se puede crear un momento sin objetivo
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_DUO", 1);
        var objective = CreateObjectiveModel(version, stage, "OBJ_LINKED", 1);
        var momentWithObj = CreateMomentModel(version, stage, objective, "MOM_WITH_OBJ", 1);
        var momentWithoutObj = CreateMomentModel(version, stage, null, "MOM_WITHOUT_OBJ", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.Objectives.Add(objective);
        context.ExpectedMoments.AddRange(momentWithObj, momentWithoutObj);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var savedWith = await readContext.ExpectedMoments
            .Include(m => m.Objective)
            .FirstOrDefaultAsync(m => m.MOM_IdExpectedMoment == momentWithObj.MOM_IdExpectedMoment);
        var savedWithout = await readContext.ExpectedMoments
            .FirstOrDefaultAsync(m => m.MOM_IdExpectedMoment == momentWithoutObj.MOM_IdExpectedMoment);

        savedWith.Should().NotBeNull();
        savedWith!.OBJ_IdObjective.Should().Be(objective.OBJ_IdObjective);
        savedWith.Objective.Should().NotBeNull();

        savedWithout.Should().NotBeNull();
        savedWithout!.OBJ_IdObjective.Should().BeNull();

        // Limpieza ordenada: Moments -> Objective -> Stage -> Version -> Lab
        using var cleanup = CreateContext();
        cleanup.ExpectedMoments.RemoveRange(cleanup.ExpectedMoments.Where(m => m.LAB_IdVersion == version.LAB_IdVersion));
        var oDel = await cleanup.Objectives.FindAsync(objective.OBJ_IdObjective);
        if (oDel is not null) cleanup.Objectives.Remove(oDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test6_7_StageCanHaveMultipleMoments_AndVersionHasMomentsAcrossStages()
    {
        // 6. Una etapa puede tener múltiples momentos
        // 7. Una versión puede tener momentos en distintas etapas
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage1 = CreateStageModel(version, "STG_A_MOM", 1);
        var stage2 = CreateStageModel(version, "STG_B_MOM", 2);

        var m1 = CreateMomentModel(version, stage1, null, "MOM_S1_1", 1);
        var m2 = CreateMomentModel(version, stage1, null, "MOM_S1_2", 2);
        var m3 = CreateMomentModel(version, stage2, null, "MOM_S2_1", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.AddRange(stage1, stage2);
        context.ExpectedMoments.AddRange(m1, m2, m3);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var s1Moments = await readContext.ExpectedMoments.Where(m => m.STG_IdStage == stage1.STG_IdStage).ToListAsync();
        s1Moments.Should().HaveCount(2);

        var versionMoments = await readContext.ExpectedMoments.Where(m => m.LAB_IdVersion == version.LAB_IdVersion).ToListAsync();
        versionMoments.Should().HaveCount(3);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.ExpectedMoments.RemoveRange(cleanup.ExpectedMoments.Where(m => m.LAB_IdVersion == version.LAB_IdVersion));
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version.LAB_IdVersion));
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test8_CannotCreateMoment_ForNonExistentVersion()
    {
        // 8. No se puede crear un momento para una versión inexistente
        using var context = CreateContext();

        var fakeVersion = new LAB_LabVersion { LAB_IdVersion = Guid.NewGuid(), SEG_IdTenant = Guid.NewGuid() };
        var fakeStage = new LAB_Stage { STG_IdStage = Guid.NewGuid(), LAB_IdVersion = fakeVersion.LAB_IdVersion, SEG_IdTenant = fakeVersion.SEG_IdTenant };
        var moment = CreateMomentModel(fakeVersion, fakeStage, null, "ORPHAN_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_LabVersion");
    }

    [Fact]
    public async Task Test9_CannotCreateMoment_ForNonExistentStage()
    {
        // 9. No se puede crear un momento para una etapa inexistente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var fakeStage = new LAB_Stage { STG_IdStage = Guid.NewGuid(), LAB_IdVersion = version.LAB_IdVersion, SEG_IdTenant = version.SEG_IdTenant };
        var moment = CreateMomentModel(version, fakeStage, null, "BAD_STAGE_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_CannotCreateMoment_AssociatedWithStageOfDifferentTenant()
    {
        // 10. No se puede crear un momento asociado con una etapa de otro tenant
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        context.Labs.Add(lab1);
        context.LabVersions.Add(version1);

        var lab2 = CreateLabModel(); // Tenant distinto
        var version2 = CreateVersionModel(lab2, 1);
        var stage2 = CreateStageModel(version2, "STG_TENANT2", 1);
        context.Labs.Add(lab2);
        context.LabVersions.Add(version2);
        context.Stages.Add(stage2);

        await context.SaveChangesAsync();

        // Momento de version1 pero intentando apuntar a stage2 de tenant2
        var moment = CreateMomentModel(version1, stage2, null, "CROSS_TENANT_STAGE_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_Stage_Tenant_Version_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var s2Del = await cleanup.Stages.FindAsync(stage2.STG_IdStage);
        if (s2Del is not null) cleanup.Stages.Remove(s2Del);
        var v1Del = await cleanup.LabVersions.FindAsync(version1.LAB_IdVersion);
        if (v1Del is not null) cleanup.LabVersions.Remove(v1Del);
        var v2Del = await cleanup.LabVersions.FindAsync(version2.LAB_IdVersion);
        if (v2Del is not null) cleanup.LabVersions.Remove(v2Del);
        var l1Del = await cleanup.Labs.FindAsync(lab1.LAB_IdLab);
        if (l1Del is not null) cleanup.Labs.Remove(l1Del);
        var l2Del = await cleanup.Labs.FindAsync(lab2.LAB_IdLab);
        if (l2Del is not null) cleanup.Labs.Remove(l2Del);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test11_CannotCreateMoment_AssociatedWithStageOfDifferentVersion()
    {
        // 11. No se puede crear un momento asociado con una etapa de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        var stageInV2 = CreateStageModel(version2, "STG_IN_V2", 1);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.Add(stageInV2);
        await context.SaveChangesAsync();

        // Momento asignado a version1 pero con stageInV2
        var moment = CreateMomentModel(version1, stageInV2, null, "CROSS_VER_STAGE_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_Stage_Tenant_Version_Stage");

        // Limpieza
        using var cleanup = CreateContext();
        var sDel = await cleanup.Stages.FindAsync(stageInV2.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test12_CannotAssociateMoment_WithObjectiveOfDifferentTenant()
    {
        // 12. No se puede asociar un momento con un objetivo de otro tenant
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        var stage1 = CreateStageModel(version1, "STG_T1", 1);
        context.Labs.Add(lab1);
        context.LabVersions.Add(version1);
        context.Stages.Add(stage1);

        var lab2 = CreateLabModel(); // Tenant distinto
        var version2 = CreateVersionModel(lab2, 1);
        var obj2 = CreateObjectiveModel(version2, null, "OBJ_T2", 1);
        context.Labs.Add(lab2);
        context.LabVersions.Add(version2);
        context.Objectives.Add(obj2);

        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version1, stage1, obj2, "CROSS_TENANT_OBJ_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_Objective_Tenant_Version_Objective");

        // Limpieza
        using var cleanup = CreateContext();
        var o2Del = await cleanup.Objectives.FindAsync(obj2.OBJ_IdObjective);
        if (o2Del is not null) cleanup.Objectives.Remove(o2Del);
        var s1Del = await cleanup.Stages.FindAsync(stage1.STG_IdStage);
        if (s1Del is not null) cleanup.Stages.Remove(s1Del);
        var v1Del = await cleanup.LabVersions.FindAsync(version1.LAB_IdVersion);
        if (v1Del is not null) cleanup.LabVersions.Remove(v1Del);
        var v2Del = await cleanup.LabVersions.FindAsync(version2.LAB_IdVersion);
        if (v2Del is not null) cleanup.LabVersions.Remove(v2Del);
        var l1Del = await cleanup.Labs.FindAsync(lab1.LAB_IdLab);
        if (l1Del is not null) cleanup.Labs.Remove(l1Del);
        var l2Del = await cleanup.Labs.FindAsync(lab2.LAB_IdLab);
        if (l2Del is not null) cleanup.Labs.Remove(l2Del);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test13_CannotAssociateMoment_WithObjectiveOfDifferentVersion()
    {
        // 13. No se puede asociar un momento con un objetivo de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var stage1 = CreateStageModel(version1, "STG_V1", 1);

        var version2 = CreateVersionModel(lab, 2);
        var objInV2 = CreateObjectiveModel(version2, null, "OBJ_IN_V2", 1);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.Add(stage1);
        context.Objectives.Add(objInV2);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version1, stage1, objInV2, "CROSS_VER_OBJ_MOM", 1);

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_ExpectedMoment_LAB_Objective_Tenant_Version_Objective");

        // Limpieza
        using var cleanup = CreateContext();
        var oDel = await cleanup.Objectives.FindAsync(objInV2.OBJ_IdObjective);
        if (oDel is not null) cleanup.Objectives.Remove(oDel);
        var sDel = await cleanup.Stages.FindAsync(stage1.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test14_15_CanCreateMomentWithoutObjective_AndVerifyObjectiveAssociation()
    {
        // 14. Cuando la protección SQL implementada lo permita, se valida la integridad referencial.
        // 15. Se puede crear un momento sin OBJ_IdObjective
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_TEST_NULL_OBJ", 1);
        var moment = CreateMomentModel(version, stage, null, "MOM_NO_OBJ_15", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        saved.Should().NotBeNull();
        saved!.OBJ_IdObjective.Should().BeNull();

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test16_CannotRepeatMomentCodigo_WithinSameVersion()
    {
        // 16. No se puede repetir MOM_Codigo dentro de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_DUP_CODE", 1);
        var m1 = CreateMomentModel(version, stage, null, "DUP_MOM_CODE", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(m1);
        await context.SaveChangesAsync();

        var m2 = CreateMomentModel(version, stage, null, "DUP_MOM_CODE", 2);
        context.ExpectedMoments.Add(m2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_Codigo");

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(m1.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test17_SameMomentCodigo_CanBeUsedInDifferentVersions()
    {
        // 17. El mismo MOM_Codigo puede utilizarse en versiones diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        var stage1 = CreateStageModel(version1, "STG_V1_CODE", 1);
        var stage2 = CreateStageModel(version2, "STG_V2_CODE", 1);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.AddRange(stage1, stage2);
        await context.SaveChangesAsync();

        var m1 = CreateMomentModel(version1, stage1, null, "SHARED_MOM_CODE", 1);
        var m2 = CreateMomentModel(version2, stage2, null, "SHARED_MOM_CODE", 1);
        context.ExpectedMoments.AddRange(m1, m2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.ExpectedMoments.RemoveRange(cleanup.ExpectedMoments.Where(m => m.LAB_IdVersion == version1.LAB_IdVersion || m.LAB_IdVersion == version2.LAB_IdVersion));
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version1.LAB_IdVersion || s.LAB_IdVersion == version2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test18_CannotRepeatMomentOrdenSugerido_WithinSameStage()
    {
        // 18. No se puede repetir MOM_OrdenSugerido dentro de la misma etapa
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_ORDER_CHECK", 1);
        var m1 = CreateMomentModel(version, stage, null, "MOM_ORD_1", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(m1);
        await context.SaveChangesAsync();

        var m2 = CreateMomentModel(version, stage, null, "MOM_ORD_2", 1); // Mismo orden 1 en la misma etapa
        context.ExpectedMoments.Add(m2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_ExpectedMoment_Tenant_Version_Stage_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(m1.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test19_DifferentStages_CanUseSameOrdenSugerido()
    {
        // 19. Etapas diferentes pueden utilizar el mismo MOM_OrdenSugerido
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var s1 = CreateStageModel(version, "STAGE_1_ORD", 1);
        var s2 = CreateStageModel(version, "STAGE_2_ORD", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var m1 = CreateMomentModel(version, s1, null, "MOM_S1_ORD1", 1);
        var m2 = CreateMomentModel(version, s2, null, "MOM_S2_ORD1", 1); // Mismo orden 1 en distinta etapa
        context.ExpectedMoments.AddRange(m1, m2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.ExpectedMoments.RemoveRange(cleanup.ExpectedMoments.Where(m => m.LAB_IdVersion == version.LAB_IdVersion));
        cleanup.Stages.RemoveRange(cleanup.Stages.Where(s => s.LAB_IdVersion == version.LAB_IdVersion));
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test20_21_MomentOrdenSugerido_RejectsZeroAndNegative()
    {
        // 20. MOM_OrdenSugerido rechaza cero
        // 21. MOM_OrdenSugerido rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_ORD_CHECKS", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var mZero = CreateMomentModel(version, stage, null, "ZERO_ORD_MOM", 0);
        context.ExpectedMoments.Add(mZero);
        Func<Task> actZero = async () => await context.SaveChangesAsync();

        var exZero = await actZero.Should().ThrowAsync<DbUpdateException>();
        var sqlExZero = (SqlException)exZero.Which.InnerException!;
        sqlExZero.Number.Should().Be(547);
        sqlExZero.Message.Should().Contain("CK_LAB_ExpectedMoment_MOM_OrdenSugerido");

        context.Entry(mZero).State = EntityState.Detached;

        var mNeg = CreateMomentModel(version, stage, null, "NEG_ORD_MOM", -3);
        context.ExpectedMoments.Add(mNeg);
        Func<Task> actNeg = async () => await context.SaveChangesAsync();

        var exNeg = await actNeg.Should().ThrowAsync<DbUpdateException>();
        var sqlExNeg = (SqlException)exNeg.Which.InnerException!;
        sqlExNeg.Number.Should().Be(547);
        sqlExNeg.Message.Should().Contain("CK_LAB_ExpectedMoment_MOM_OrdenSugerido");

        // Limpieza
        using var cleanup = CreateContext();
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("DIALOGUE")]
    [InlineData("USER_ACTION")]
    [InlineData("SYSTEM_EVENT")]
    [InlineData("DECISION_POINT")]
    [InlineData("ARTIFACT_REVIEW")]
    public async Task Test22_26_MomentTipo_AcceptsAllAllowedValues(string tipo)
    {
        // 22-26. MOM_Tipo acepta DIALOGUE, USER_ACTION, SYSTEM_EVENT, DECISION_POINT, ARTIFACT_REVIEW
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, $"STG_T_{tipo}", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version, stage, null, $"MOM_{tipo}", 1, tipo: tipo);
        context.ExpectedMoments.Add(moment);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test27_MomentTipo_RejectsInvalidValue()
    {
        // 27. MOM_Tipo rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_BAD_TIPO", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version, stage, null, "BAD_TIPO_MOM", 1);
        moment.MOM_Tipo = "UNSUPPORTED_TYPE";
        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_ExpectedMoment_MOM_Tipo");

        // Limpieza
        using var cleanup = CreateContext();
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("ACTIVE")]
    [InlineData("INACTIVE")]
    public async Task Test28_MomentEstatus_AcceptsValidStatuses(string status)
    {
        // 28. MOM_Estatus acepta DRAFT, ACTIVE e INACTIVE
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, $"STG_STAT_{status}", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version, stage, null, $"MOM_STAT_{status}", 1);
        moment.MOM_Estatus = status;
        context.ExpectedMoments.Add(moment);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test29_MomentEstatus_RejectsInvalidValue()
    {
        // 29. MOM_Estatus rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_BAD_STAT", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version, stage, null, "BAD_STAT_MOM", 1);
        moment.MOM_Estatus = "CLOSED";
        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_ExpectedMoment_MOM_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test30_36_BooleansAndNullableFields_PersistCorrectly()
    {
        // 30. MOM_EsCritico se guarda correctamente
        // 31. MOM_PermiteOrdenFlexible se guarda correctamente
        // 32. MOM_RequiereRespuesta se guarda correctamente
        // 33. MOM_RespuestaEjemplar acepta NULL
        // 34. MOM_InformacionDescubrible acepta NULL
        // 35. MOM_ErrorFrecuente acepta NULL
        // 36. MOM_Recomendacion acepta NULL
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_BOOLS_NULLS", 1);
        var moment = CreateMomentModel(version, stage, null, "MOM_NULLS", 1);

        moment.MOM_EsCritico = false;
        moment.MOM_PermiteOrdenFlexible = true;
        moment.MOM_RequiereRespuesta = false;
        moment.MOM_RespuestaEjemplar = null;
        moment.MOM_InformacionDescubrible = null;
        moment.MOM_ErrorFrecuente = null;
        moment.MOM_Recomendacion = null;

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        saved.Should().NotBeNull();
        saved!.MOM_EsCritico.Should().BeFalse();
        saved.MOM_PermiteOrdenFlexible.Should().BeTrue();
        saved.MOM_RequiereRespuesta.Should().BeFalse();
        saved.MOM_RespuestaEjemplar.Should().BeNull();
        saved.MOM_InformacionDescubrible.Should().BeNull();
        saved.MOM_ErrorFrecuente.Should().BeNull();
        saved.MOM_Recomendacion.Should().BeNull();

        // Limpieza
        using var cleanup = CreateContext();
        var mDel = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (mDel is not null) cleanup.ExpectedMoments.Remove(mDel);
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("MOM_Codigo")]
    [InlineData("MOM_Nombre")]
    [InlineData("MOM_Trigger")]
    [InlineData("MOM_IntencionEsperada")]
    public async Task Test37_40_WhitespaceAndEmptyStrings_AreRejected(string fieldName)
    {
        // 37-40. MOM_Codigo, MOM_Nombre, MOM_Trigger, MOM_IntencionEsperada rechazan texto vacío o espacios
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, $"STG_WS_{fieldName}", 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        await context.SaveChangesAsync();

        var moment = CreateMomentModel(version, stage, null, $"MOM_WS_{fieldName}", 1);
        if (fieldName == "MOM_Codigo") moment.MOM_Codigo = "   ";
        else if (fieldName == "MOM_Nombre") moment.MOM_Nombre = "   ";
        else if (fieldName == "MOM_Trigger") moment.MOM_Trigger = "   ";
        else if (fieldName == "MOM_IntencionEsperada") moment.MOM_IntencionEsperada = "   ";

        context.ExpectedMoments.Add(moment);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain(fieldName);

        // Limpieza
        using var cleanup = CreateContext();
        var sDel = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (sDel is not null) cleanup.Stages.Remove(sDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test41_CannotHardDeleteLabVersion_WhenItHasMoments()
    {
        // 41. No se puede eliminar físicamente una versión que tenga momentos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_DEL_VER", 1);
        var moment = CreateMomentModel(version, stage, null, "MOM_BLOCK_VER_DEL", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var delCtx = CreateContext();
        var vDel = await delCtx.LabVersions.FindAsync(version.LAB_IdVersion);
        delCtx.LabVersions.Remove(vDel!);
        Func<Task> act = async () => await delCtx.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);

        // Limpieza en orden referencial
        using var cleanup = CreateContext();
        var m = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
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
    public async Task Test42_CannotHardDeleteStage_WhenItHasMoments()
    {
        // 42. No se puede eliminar físicamente una etapa que tenga momentos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_DEL_MOM", 1);
        var moment = CreateMomentModel(version, stage, null, "MOM_BLOCK_STG_DEL", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var delCtx = CreateContext();
        var sDel = await delCtx.Stages.FindAsync(stage.STG_IdStage);
        delCtx.Stages.Remove(sDel!);
        Func<Task> act = async () => await delCtx.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);

        // Limpieza en orden referencial
        using var cleanup = CreateContext();
        var m = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
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
    public async Task Test43_CannotHardDeleteObjective_WhenItHasAssociatedMoments()
    {
        // 43. No se puede eliminar físicamente un objetivo que tenga momentos asociados
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_OBJ_DEL_MOM", 1);
        var objective = CreateObjectiveModel(version, stage, "OBJ_BLOCK_DEL", 1);
        var moment = CreateMomentModel(version, stage, objective, "MOM_BLOCK_OBJ_DEL", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.Objectives.Add(objective);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var delCtx = CreateContext();
        var oDel = await delCtx.Objectives.FindAsync(objective.OBJ_IdObjective);
        delCtx.Objectives.Remove(oDel!);
        Func<Task> act = async () => await delCtx.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);

        // Limpieza en orden referencial: Moment -> Objective -> Stage -> Version -> Lab
        using var cleanup = CreateContext();
        var m = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
        await cleanup.SaveChangesAsync();
        var o = await cleanup.Objectives.FindAsync(objective.OBJ_IdObjective);
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
    public async Task Test44_RowVersion_DetectsConcurrentUpdateOnMoment()
    {
        // 44. RowVersion detecta una actualización concurrente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, "STG_CONCURR_MOM", 1);
        var moment = CreateMomentModel(version, stage, null, "CONCURR_MOM", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        await context.SaveChangesAsync();

        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        var entity2 = await client2.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);

        entity1!.MOM_Nombre = "Nombre Actualizado por Cliente 1";
        await client1.SaveChangesAsync();

        entity2!.MOM_Nombre = "Nombre Actualizado por Cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanup = CreateContext();
        var m = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
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
    public async Task Test45_MigrationDoesNotCreateAdditionalTables()
    {
        // 45. La migración no crea tablas adicionales fuera de las 6 tablas aprobadas
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
