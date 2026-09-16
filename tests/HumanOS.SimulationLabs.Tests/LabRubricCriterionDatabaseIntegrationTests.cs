using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabRubricCriterionDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Criterion Tests",
            LAB_Descripcion = "Lab creado para probar criterios de rúbrica",
            LAB_Tipo = LabTipos.Conversational,
            LAB_Dominio = "SCOPING",
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
            LAB_ObjetivoGeneral = "Objetivo general para probar criterios de rúbrica",
            LAB_InstruccionesParticipante = "Instrucciones de versión",
            LAB_DuracionMinutos = 60,
            LAB_ScoreMinimo = 7.00m,
            LAB_Estatus = LabVersionEstatus.Published,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Stage CreateStageModel(LAB_LabVersion version, string code = "STG_CRIT_BASE", int order = 1)
    {
        return new LAB_Stage
        {
            STG_IdStage = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            SEG_IdTenant = version.SEG_IdTenant,
            STG_Codigo = code,
            STG_Nombre = "Etapa Base Para Criterios",
            STG_Descripcion = "Etapa para evaluar momentos y objetivos",
            STG_Orden = order,
            STG_TipoInteraccion = StageTipoInteraccion.Conversation,
            STG_EsObligatorio = true,
            STG_CondicionCompletitud = "Completar la etapa",
            STG_Estatus = StageEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Objective CreateObjectiveModel(LAB_LabVersion version, LAB_Stage? stage = null, string code = "OBJ_CRIT_BASE", int order = 1)
    {
        return new LAB_Objective
        {
            OBJ_IdObjective = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            STG_IdStage = stage?.STG_IdStage,
            SEG_IdTenant = version.SEG_IdTenant,
            OBJ_Codigo = code,
            OBJ_Descripcion = "Objetivo para probar relación con criterios",
            OBJ_TipoEvidencia = ObjectiveTipoEvidencia.Conversation,
            OBJ_EsCritico = false,
            OBJ_Peso = 20.0000m,
            OBJ_CondicionExito = "El participante demuestra entendimiento del problema",
            OBJ_Orden = order,
            OBJ_Estatus = ObjectiveEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_ExpectedMoment CreateMomentModel(LAB_LabVersion version, LAB_Stage stage, LAB_Objective? objective = null, string code = "MOM_CRIT_BASE", int order = 1)
    {
        return new LAB_ExpectedMoment
        {
            MOM_IdExpectedMoment = Guid.NewGuid(),
            LAB_IdVersion = version.LAB_IdVersion,
            STG_IdStage = stage.STG_IdStage,
            OBJ_IdObjective = objective?.OBJ_IdObjective,
            SEG_IdTenant = version.SEG_IdTenant,
            MOM_Codigo = code,
            MOM_Nombre = "Momento para probar relación con criterios",
            MOM_Tipo = MomentTipo.Dialogue,
            MOM_Trigger = "Disparador de momento de prueba",
            MOM_IntencionEsperada = "Intención esperada para momento de prueba",
            MOM_EsCritico = false,
            MOM_OrdenSugerido = order,
            MOM_PermiteOrdenFlexible = true,
            MOM_RequiereRespuesta = true,
            MOM_Estatus = MomentEstatus.Active,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Rubric CreateRubricModel(
        LAB_LabVersion version,
        string code = "RUB_CRIT_TEST")
    {
        return new LAB_Rubric
        {
            RUB_IdRubric = Guid.NewGuid(),
            SEG_IdTenant = version.SEG_IdTenant,
            LAB_IdVersion = version.LAB_IdVersion,
            RUB_Codigo = code,
            RUB_Nombre = "Rúbrica para probar criterios",
            RUB_Descripcion = "Evalúa capacidades con criterios detallados",
            RUB_TipoEvaluacion = RubricTipoEvaluacion.Conversation,
            RUB_EscalaMinima = 1.00m,
            RUB_EscalaMaxima = 10.00m,
            RUB_ScoreMinimoAprobacion = 7.00m,
            RUB_MetodoCalculo = RubricMetodoCalculo.WeightedWithCriticalGate,
            RUB_RequiereEvidencia = true,
            RUB_PermiteFallaCritica = true,
            RUB_InstruccionesEvaluador = "Evaluar con base en evidencia observable.",
            RUB_Estatus = RubricEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_RubricCriterion CreateCriterionModel(
        LAB_Rubric rubric,
        string code = "CRT_TEST",
        int order = 1,
        Guid? objectiveId = null,
        Guid? momentId = null,
        decimal peso = 25.0000m,
        decimal scoreMinimo = 7.00m,
        string tipoEvidencia = CriterionTipoEvidencia.Conversation,
        string estatus = CriterionEstatus.Draft,
        bool esCritico = false)
    {
        return new LAB_RubricCriterion
        {
            CRT_IdCriterion = Guid.NewGuid(),
            RUB_IdRubric = rubric.RUB_IdRubric,
            LAB_IdVersion = rubric.LAB_IdVersion,
            SEG_IdTenant = rubric.SEG_IdTenant,
            OBJ_IdObjective = objectiveId,
            MOM_IdExpectedMoment = momentId,
            CRT_Codigo = code,
            CRT_Nombre = "Explora el problema empresarial",
            CRT_Descripcion = "El participante explora el proceso, el impacto y la decisión empresarial antes de proponer soluciones.",
            CRT_TipoEvidencia = tipoEvidencia,
            CRT_Peso = peso,
            CRT_ScoreMinimoEsperado = scoreMinimo,
            CRT_EsCritico = esCritico,
            CRT_IndicadoresPositivos = "Formula preguntas abiertas, investiga el proceso actual y confirma el entendimiento.",
            CRT_IndicadoresNegativos = "Propone inmediatamente una arquitectura sin comprender el problema empresarial.",
            CRT_ErrorCritico = esCritico ? "Autoriza una acción prohibida o ignora una condición obligatoria." : null,
            CRT_RecomendacionBase = "Antes de proponer una solución, formula preguntas sobre el proceso actual.",
            CRT_Orden = order,
            CRT_Estatus = estatus,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabRubricCriterionTable_AppliesSuccessfully()
    {
        // 1. La migración se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabVersionTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabStageTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabObjectiveTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabExpectedMomentTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabRubricTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabRubricCriterionTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabRubricCriterionTable_ExistsInAzureSql()
    {
        // 2. LAB_RubricCriterion existe
        using var context = CreateContext();

        var count = await context.RubricCriteria.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanCreateValidCriterion_InsideRubric()
    {
        // 3. Se puede crear un criterio válido dentro de una rúbrica
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_EXPLORE", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved.Should().NotBeNull();
        saved!.CRT_Codigo.Should().Be("CRT_EXPLORE");
        saved.CRT_Nombre.Should().Be("Explora el problema empresarial");
        saved.CRT_Peso.Should().Be(25.0000m);
        saved.CRT_ScoreMinimoEsperado.Should().Be(7.00m);
        saved.RowVersion.Should().NotBeNull().And.NotBeEmpty();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test4_RubricCanHaveMultipleCriteria()
    {
        // 4. Una rúbrica puede tener múltiples criterios
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion1 = CreateCriterionModel(rubric, "CRT_MULTI_1", 1);
        var criterion2 = CreateCriterionModel(rubric, "CRT_MULTI_2", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.AddRange(criterion1, criterion2);
        await context.SaveChangesAsync();

        var criteriaList = await context.RubricCriteria
            .Where(c => c.RUB_IdRubric == rubric.RUB_IdRubric)
            .OrderBy(c => c.CRT_Orden)
            .ToListAsync();

        criteriaList.Should().HaveCount(2);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.RubricCriteria.RemoveRange(cleanup.RubricCriteria.Where(c => c.RUB_IdRubric == rubric.RUB_IdRubric));
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_CannotCreateCriterion_ForNonExistentRubric()
    {
        // 5. No se puede crear un criterio para una rúbrica inexistente
        using var context = CreateContext();

        var fakeRubric = new LAB_Rubric
        {
            RUB_IdRubric = Guid.NewGuid(),
            SEG_IdTenant = Guid.NewGuid(),
            LAB_IdVersion = Guid.NewGuid()
        };
        var criterion = CreateCriterionModel(fakeRubric, "CRT_ORPHAN", 1);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Rubric");
    }

    [Fact]
    public async Task Test6_CannotCreateCriterion_ForRubricOfDifferentTenant()
    {
        // 6. No se puede crear un criterio para una rúbrica de otro tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        var criterion = CreateCriterionModel(rubric, "CRT_CROSS_TENANT", 1);
        criterion.SEG_IdTenant = Guid.NewGuid(); // Tenant distinto al de la rúbrica

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Rubric_Tenant_Version_Rubric");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test7_CannotAssociateCriterion_WithRubricOfDifferentVersion()
    {
        // 7. No se puede asociar un criterio con una rúbrica de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        var criterion = CreateCriterionModel(rubric, "CRT_CROSS_VER", 1);
        criterion.LAB_IdVersion = Guid.NewGuid(); // Versión distinta a la de la rúbrica

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Rubric_Tenant_Version_Rubric");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test8_CanCreateCriterion_WithoutObjective()
    {
        // 8. Se puede crear un criterio sin objetivo
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_NO_OBJ", 1, objectiveId: null);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.OBJ_IdObjective.Should().BeNull();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test9_CanCreateCriterion_AssociatedWithObjectiveOfSameVersion()
    {
        // 9. Se puede crear un criterio asociado con un objetivo de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var objective = CreateObjectiveModel(version, code: $"OBJ_{Guid.NewGuid():N}"[..20]);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_WITH_OBJ", 1, objectiveId: objective.OBJ_IdObjective);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.Add(objective);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria
            .Include(c => c.Objective)
            .FirstOrDefaultAsync(c => c.CRT_IdCriterion == criterion.CRT_IdCriterion);

        saved.Should().NotBeNull();
        saved!.OBJ_IdObjective.Should().Be(objective.OBJ_IdObjective);
        saved.Objective.Should().NotBeNull();
        saved.Objective!.OBJ_Codigo.Should().Be(objective.OBJ_Codigo);

        // Limpieza
        context.RubricCriteria.Remove(saved);
        context.Rubrics.Remove(rubric);
        context.Objectives.Remove(objective);
        context.LabVersions.Remove(version);
        context.Labs.Remove(lab);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_CannotAssociateCriterion_WithObjectiveOfDifferentTenant()
    {
        // 10. No se puede asociar un criterio con un objetivo de otro tenant
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        var rubric = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);

        var lab2 = CreateLabModel();
        var version2 = CreateVersionModel(lab2, 1);
        var objectiveDifferentTenant = CreateObjectiveModel(version2, code: $"OBJ_{Guid.NewGuid():N}"[..20]);

        context.Labs.AddRange(lab1, lab2);
        context.LabVersions.AddRange(version1, version2);
        context.Rubrics.Add(rubric);
        context.Objectives.Add(objectiveDifferentTenant);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        // El criterio pertenece al tenant de version1, pero apunta a un objetivo de tenant2
        var criterion = CreateCriterionModel(rubric, "CRT_BAD_TEN_OBJ", 1, objectiveId: objectiveDifferentTenant.OBJ_IdObjective);

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Objective_Tenant_Version_Objective");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var o = await cleanup.Objectives.FindAsync(objectiveDifferentTenant.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
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
    public async Task Test11_CannotAssociateCriterion_WithObjectiveOfDifferentVersion()
    {
        // 11. No se puede asociar un criterio con un objetivo de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        var rubricVersion1 = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);
        var objectiveVersion2 = CreateObjectiveModel(version2, code: $"OBJ_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Rubrics.Add(rubricVersion1);
        context.Objectives.Add(objectiveVersion2);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        // Criterio en version1 pero con objetivo en version2
        var criterion = CreateCriterionModel(rubricVersion1, "CRT_BAD_VER_OBJ", 1, objectiveId: objectiveVersion2.OBJ_IdObjective);

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Objective_Tenant_Version_Objective");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubricVersion1.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var o = await cleanup.Objectives.FindAsync(objectiveVersion2.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v1 = await cleanup.LabVersions.FindAsync(version1.LAB_IdVersion);
        if (v1 is not null) cleanup.LabVersions.Remove(v1);
        var v2 = await cleanup.LabVersions.FindAsync(version2.LAB_IdVersion);
        if (v2 is not null) cleanup.LabVersions.Remove(v2);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test12_CanCreateCriterion_WithoutExpectedMoment()
    {
        // 12. Se puede crear un criterio sin momento esperado
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_NO_MOM", 1, momentId: null);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.MOM_IdExpectedMoment.Should().BeNull();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test13_CanCreateCriterion_AssociatedWithMomentOfSameVersion()
    {
        // 13. Se puede crear un criterio asociado con un momento de la misma versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, code: $"STG_{Guid.NewGuid():N}"[..20]);
        var moment = CreateMomentModel(version, stage, code: $"MOM_{Guid.NewGuid():N}"[..20]);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_WITH_MOM", 1, momentId: moment.MOM_IdExpectedMoment);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria
            .Include(c => c.ExpectedMoment)
            .FirstOrDefaultAsync(c => c.CRT_IdCriterion == criterion.CRT_IdCriterion);

        saved.Should().NotBeNull();
        saved!.MOM_IdExpectedMoment.Should().Be(moment.MOM_IdExpectedMoment);
        saved.ExpectedMoment.Should().NotBeNull();
        saved.ExpectedMoment!.MOM_Codigo.Should().Be(moment.MOM_Codigo);

        // Limpieza
        context.RubricCriteria.Remove(saved);
        context.Rubrics.Remove(rubric);
        context.ExpectedMoments.Remove(moment);
        context.Stages.Remove(stage);
        context.LabVersions.Remove(version);
        context.Labs.Remove(lab);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Test14_CannotAssociateCriterion_WithMomentOfDifferentTenant()
    {
        // 14. No se puede asociar un criterio con un momento de otro tenant
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        var rubric = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);

        var lab2 = CreateLabModel();
        var version2 = CreateVersionModel(lab2, 1);
        var stage2 = CreateStageModel(version2, code: $"STG_{Guid.NewGuid():N}"[..20]);
        var momentTenant2 = CreateMomentModel(version2, stage2, code: $"MOM_{Guid.NewGuid():N}"[..20]);

        context.Labs.AddRange(lab1, lab2);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.Add(stage2);
        context.ExpectedMoments.Add(momentTenant2);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        var criterion = CreateCriterionModel(rubric, "CRT_BAD_TEN_MOM", 1, momentId: momentTenant2.MOM_IdExpectedMoment);

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_ExpectedMoment_Tenant_Version_Moment");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var m = await cleanup.ExpectedMoments.FindAsync(momentTenant2.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
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
    public async Task Test15_CannotAssociateCriterion_WithMomentOfDifferentVersion()
    {
        // 15. No se puede asociar un criterio con un momento de otra versión
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version1 = CreateVersionModel(lab, 1);
        var version2 = CreateVersionModel(lab, 2);
        var stage2 = CreateStageModel(version2, code: $"STG_{Guid.NewGuid():N}"[..20]);
        var momentVersion2 = CreateMomentModel(version2, stage2, code: $"MOM_{Guid.NewGuid():N}"[..20]);
        var rubricVersion1 = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.AddRange(version1, version2);
        context.Stages.Add(stage2);
        context.ExpectedMoments.Add(momentVersion2);
        context.Rubrics.Add(rubricVersion1);
        await context.SaveChangesAsync();

        using var insertContext = CreateContext();
        var criterion = CreateCriterionModel(rubricVersion1, "CRT_BAD_VER_MOM", 1, momentId: momentVersion2.MOM_IdExpectedMoment);

        insertContext.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await insertContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_ExpectedMoment_Tenant_Version_Moment");

        // Limpieza
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubricVersion1.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var m = await cleanup.ExpectedMoments.FindAsync(momentVersion2.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
        var s = await cleanup.Stages.FindAsync(stage2.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v1 = await cleanup.LabVersions.FindAsync(version1.LAB_IdVersion);
        if (v1 is not null) cleanup.LabVersions.Remove(v1);
        var v2 = await cleanup.LabVersions.FindAsync(version2.LAB_IdVersion);
        if (v2 is not null) cleanup.LabVersions.Remove(v2);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test16_CannotRepeatCriterionCode_WithinSameRubric()
    {
        // 16. No se puede repetir CRT_Codigo dentro de la misma rúbrica
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion1 = CreateCriterionModel(rubric, "CRT_DUPLICATE_CODE", 1);
        var criterion2 = CreateCriterionModel(rubric, "CRT_DUPLICATE_CODE", 2);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion1);
        await context.SaveChangesAsync();

        context.RubricCriteria.Add(criterion2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        (sqlEx.Number == 2627 || sqlEx.Number == 2601).Should().BeTrue();
        sqlEx.Message.Should().Contain("UQ_LAB_RubricCriterion_Tenant_Rubric_Codigo");

        // Limpieza
        using var cleanup = CreateContext();
        var c1 = await cleanup.RubricCriteria.FindAsync(criterion1.CRT_IdCriterion);
        if (c1 is not null) cleanup.RubricCriteria.Remove(c1);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test17_TwoDifferentRubrics_CanUseSameCriterionCode()
    {
        // 17. Dos rúbricas diferentes pueden utilizar el mismo CRT_Codigo
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        var rubric1 = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);

        var lab2 = CreateLabModel();
        var version2 = CreateVersionModel(lab2, 1);
        var rubric2 = CreateRubricModel(version2, $"RUB_{Guid.NewGuid():N}"[..20]);

        var criterion1 = CreateCriterionModel(rubric1, "CRT_COMMON_CODE", 1);
        var criterion2 = CreateCriterionModel(rubric2, "CRT_COMMON_CODE", 1);

        context.Labs.AddRange(lab1, lab2);
        context.LabVersions.AddRange(version1, version2);
        context.Rubrics.AddRange(rubric1, rubric2);
        context.RubricCriteria.AddRange(criterion1, criterion2);
        await context.SaveChangesAsync();

        var saved1 = await context.RubricCriteria.FindAsync(criterion1.CRT_IdCriterion);
        var saved2 = await context.RubricCriteria.FindAsync(criterion2.CRT_IdCriterion);

        saved1.Should().NotBeNull();
        saved2.Should().NotBeNull();
        saved1!.CRT_Codigo.Should().Be(saved2!.CRT_Codigo);

        // Limpieza
        context.RubricCriteria.RemoveRange(saved1, saved2);
        context.Rubrics.RemoveRange(rubric1, rubric2);
        context.LabVersions.RemoveRange(version1, version2);
        context.Labs.RemoveRange(lab1, lab2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Test18_CannotRepeatCriterionOrder_WithinSameRubric()
    {
        // 18. No se puede repetir CRT_Orden dentro de la misma rúbrica
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion1 = CreateCriterionModel(rubric, "CRT_ORD_1", 1);
        var criterion2 = CreateCriterionModel(rubric, "CRT_ORD_2", 1); // Mismo orden 1

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion1);
        await context.SaveChangesAsync();

        context.RubricCriteria.Add(criterion2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        (sqlEx.Number == 2627 || sqlEx.Number == 2601).Should().BeTrue();
        sqlEx.Message.Should().Contain("UQ_LAB_RubricCriterion_Tenant_Rubric_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var c1 = await cleanup.RubricCriteria.FindAsync(criterion1.CRT_IdCriterion);
        if (c1 is not null) cleanup.RubricCriteria.Remove(c1);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test19_TwoDifferentRubrics_CanUseSameCriterionOrder()
    {
        // 19. Dos rúbricas diferentes pueden utilizar el mismo CRT_Orden
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var version1 = CreateVersionModel(lab1, 1);
        var rubric1 = CreateRubricModel(version1, $"RUB_{Guid.NewGuid():N}"[..20]);

        var lab2 = CreateLabModel();
        var version2 = CreateVersionModel(lab2, 1);
        var rubric2 = CreateRubricModel(version2, $"RUB_{Guid.NewGuid():N}"[..20]);

        var criterion1 = CreateCriterionModel(rubric1, "CRT_RUB1_ORD1", 1);
        var criterion2 = CreateCriterionModel(rubric2, "CRT_RUB2_ORD1", 1);

        context.Labs.AddRange(lab1, lab2);
        context.LabVersions.AddRange(version1, version2);
        context.Rubrics.AddRange(rubric1, rubric2);
        context.RubricCriteria.AddRange(criterion1, criterion2);
        await context.SaveChangesAsync();

        var saved1 = await context.RubricCriteria.FindAsync(criterion1.CRT_IdCriterion);
        var saved2 = await context.RubricCriteria.FindAsync(criterion2.CRT_IdCriterion);

        saved1.Should().NotBeNull();
        saved2.Should().NotBeNull();
        saved1!.CRT_Orden.Should().Be(1);
        saved2!.CRT_Orden.Should().Be(1);

        // Limpieza
        context.RubricCriteria.RemoveRange(saved1, saved2);
        context.Rubrics.RemoveRange(rubric1, rubric2);
        context.LabVersions.RemoveRange(version1, version2);
        context.Labs.RemoveRange(lab1, lab2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Test20_CRT_Peso_RejectsZero()
    {
        // 20. CRT_Peso rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_PESO_ZERO", 1, peso: 0.0000m);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_Peso");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test21_CRT_Peso_RejectsNegativeNumbers()
    {
        // 21. CRT_Peso rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_PESO_NEG", 1, peso: -10.0000m);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_Peso");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test22_CRT_ScoreMinimoEsperado_AcceptsOne()
    {
        // 22. CRT_ScoreMinimoEsperado acepta 1.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_SCORE_1", 1, scoreMinimo: 1.00m);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.CRT_ScoreMinimoEsperado.Should().Be(1.00m);

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test23_CRT_ScoreMinimoEsperado_AcceptsTen()
    {
        // 23. CRT_ScoreMinimoEsperado acepta 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_SCORE_10", 1, scoreMinimo: 10.00m);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.CRT_ScoreMinimoEsperado.Should().Be(10.00m);

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test24_CRT_ScoreMinimoEsperado_RejectsValuesBelowOne()
    {
        // 24. CRT_ScoreMinimoEsperado rechaza valores inferiores a 1.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_SCORE_LOW", 1, scoreMinimo: 0.99m);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_ScoreMinimoEsperado");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test25_CRT_ScoreMinimoEsperado_RejectsValuesAboveTen()
    {
        // 25. CRT_ScoreMinimoEsperado rechaza valores superiores a 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_SCORE_HIGH", 1, scoreMinimo: 10.01m);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_ScoreMinimoEsperado");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test26_CRT_Orden_RejectsZero()
    {
        // 26. CRT_Orden rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_ORD_ZERO", 0);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test27_CRT_Orden_RejectsNegativeNumbers()
    {
        // 27. CRT_Orden rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_ORD_NEG", -1);

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_Orden");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test28_CRT_TipoEvidencia_AcceptsAllSixAllowedValues()
    {
        // 28. CRT_TipoEvidencia acepta los seis valores permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var createdCriteria = new List<LAB_RubricCriterion>();
        int order = 1;

        foreach (var tipo in CriterionTipoEvidencia.Allowed)
        {
            var criterion = CreateCriterionModel(rubric, $"CRT_TE_{order}", order, tipoEvidencia: tipo);
            context.RubricCriteria.Add(criterion);
            createdCriteria.Add(criterion);
            order++;
        }

        await context.SaveChangesAsync();

        foreach (var criterion in createdCriteria)
        {
            var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
            saved.Should().NotBeNull();
            saved!.CRT_TipoEvidencia.Should().Be(criterion.CRT_TipoEvidencia);
        }

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.RubricCriteria.RemoveRange(cleanup.RubricCriteria.Where(c => c.RUB_IdRubric == rubric.RUB_IdRubric));
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test29_CRT_TipoEvidencia_RejectsInvalidValues()
    {
        // 29. CRT_TipoEvidencia rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_BAD_TE", 1, tipoEvidencia: "INVALID_EVIDENCE");

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_TipoEvidencia");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test30_CRT_Estatus_AcceptsDraftActiveInactive()
    {
        // 30. CRT_Estatus acepta DRAFT, ACTIVE e INACTIVE
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var crtDraft = CreateCriterionModel(rubric, "CRT_ST_DRAFT", 1, estatus: CriterionEstatus.Draft);
        var crtActive = CreateCriterionModel(rubric, "CRT_ST_ACTIVE", 2, estatus: CriterionEstatus.Active);
        var crtInactive = CreateCriterionModel(rubric, "CRT_ST_INACT", 3, estatus: CriterionEstatus.Inactive);

        context.RubricCriteria.AddRange(crtDraft, crtActive, crtInactive);
        await context.SaveChangesAsync();

        var s1 = await context.RubricCriteria.FindAsync(crtDraft.CRT_IdCriterion);
        var s2 = await context.RubricCriteria.FindAsync(crtActive.CRT_IdCriterion);
        var s3 = await context.RubricCriteria.FindAsync(crtInactive.CRT_IdCriterion);

        s1!.CRT_Estatus.Should().Be(CriterionEstatus.Draft);
        s2!.CRT_Estatus.Should().Be(CriterionEstatus.Active);
        s3!.CRT_Estatus.Should().Be(CriterionEstatus.Inactive);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.RubricCriteria.RemoveRange(cleanup.RubricCriteria.Where(c => c.RUB_IdRubric == rubric.RUB_IdRubric));
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test31_CRT_Estatus_RejectsInvalidValues()
    {
        // 31. CRT_Estatus rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, "CRT_BAD_STAT", 1, estatus: "ARCHIVED");

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_RubricCriterion_CRT_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test32_CRT_EsCritico_SavesCorrectly()
    {
        // 32. CRT_EsCritico se guarda correctamente (true y false)
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        var critTrue = CreateCriterionModel(rubric, "CRT_CRIT_TRUE", 1, esCritico: true);
        var critFalse = CreateCriterionModel(rubric, "CRT_CRIT_FALSE", 2, esCritico: false);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.AddRange(critTrue, critFalse);
        await context.SaveChangesAsync();

        var sTrue = await context.RubricCriteria.FindAsync(critTrue.CRT_IdCriterion);
        var sFalse = await context.RubricCriteria.FindAsync(critFalse.CRT_IdCriterion);

        sTrue!.CRT_EsCritico.Should().BeTrue();
        sFalse!.CRT_EsCritico.Should().BeFalse();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.RubricCriteria.RemoveRange(cleanup.RubricCriteria.Where(c => c.RUB_IdRubric == rubric.RUB_IdRubric));
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test33_CRT_IndicadoresNegativos_AcceptsNull()
    {
        // 33. CRT_IndicadoresNegativos acepta NULL
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_NULL_NEG", 1);
        criterion.CRT_IndicadoresNegativos = null;

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.CRT_IndicadoresNegativos.Should().BeNull();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test34_CRT_ErrorCritico_AcceptsNull()
    {
        // 34. CRT_ErrorCritico acepta NULL
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_NULL_ERR", 1, esCritico: true);
        criterion.CRT_ErrorCritico = null; // Acepta NULL incluso cuando CRT_EsCritico es true

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.CRT_ErrorCritico.Should().BeNull();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test35_CRT_RecomendacionBase_AcceptsNull()
    {
        // 35. CRT_RecomendacionBase acepta NULL
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_NULL_REC", 1);
        criterion.CRT_RecomendacionBase = null;

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        var saved = await context.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        saved!.CRT_RecomendacionBase.Should().BeNull();

        // Limpieza
        context.RubricCriteria.Remove(saved);
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("CRT_Codigo", "")]
    [InlineData("CRT_Codigo", "   ")]
    [InlineData("CRT_Nombre", "")]
    [InlineData("CRT_Nombre", "   ")]
    [InlineData("CRT_Descripcion", "")]
    [InlineData("CRT_Descripcion", "   ")]
    [InlineData("CRT_IndicadoresPositivos", "")]
    [InlineData("CRT_IndicadoresPositivos", "   ")]
    [InlineData("CreadoPor", "")]
    [InlineData("CreadoPor", "   ")]
    public async Task Test36_RequiredTextFields_RejectEmptyOrWhitespace(string fieldName, string invalidValue)
    {
        // 36. Los campos de texto requeridos rechazan texto vacío o espacios
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        var criterion = CreateCriterionModel(rubric, $"CRT_{Guid.NewGuid():N}"[..20], 1);

        switch (fieldName)
        {
            case "CRT_Codigo": criterion.CRT_Codigo = invalidValue; break;
            case "CRT_Nombre": criterion.CRT_Nombre = invalidValue; break;
            case "CRT_Descripcion": criterion.CRT_Descripcion = invalidValue; break;
            case "CRT_IndicadoresPositivos": criterion.CRT_IndicadoresPositivos = invalidValue; break;
            case "CreadoPor": criterion.CreadoPor = invalidValue; break;
        }

        context.RubricCriteria.Add(criterion);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain($"CK_LAB_RubricCriterion_{fieldName}");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test37_CannotDeleteRubric_WithAssociatedCriteria()
    {
        // 37. No se puede eliminar una rúbrica que tenga criterios
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_DEL_RUB", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        using var deleteContext = CreateContext();
        var rubToDelete = await deleteContext.Rubrics.FindAsync(rubric.RUB_IdRubric);
        deleteContext.Rubrics.Remove(rubToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Rubric_Tenant_Version_Rubric");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test38_CannotDeleteObjective_WithAssociatedCriteria()
    {
        // 38. No se puede eliminar un objetivo que tenga criterios relacionados
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var objective = CreateObjectiveModel(version, code: $"OBJ_{Guid.NewGuid():N}"[..20]);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_DEL_OBJ", 1, objectiveId: objective.OBJ_IdObjective);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Objectives.Add(objective);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        using var deleteContext = CreateContext();
        var objToDelete = await deleteContext.Objectives.FindAsync(objective.OBJ_IdObjective);
        deleteContext.Objectives.Remove(objToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_Objective_Tenant_Version_Objective");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var o = await cleanup.Objectives.FindAsync(objective.OBJ_IdObjective);
        if (o is not null) cleanup.Objectives.Remove(o);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test39_CannotDeleteExpectedMoment_WithAssociatedCriteria()
    {
        // 39. No se puede eliminar un momento esperado que tenga criterios relacionados
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var stage = CreateStageModel(version, code: $"STG_{Guid.NewGuid():N}"[..20]);
        var moment = CreateMomentModel(version, stage, code: $"MOM_{Guid.NewGuid():N}"[..20]);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_DEL_MOM", 1, momentId: moment.MOM_IdExpectedMoment);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Stages.Add(stage);
        context.ExpectedMoments.Add(moment);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        using var deleteContext = CreateContext();
        var momToDelete = await deleteContext.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        deleteContext.ExpectedMoments.Remove(momToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_RubricCriterion_LAB_ExpectedMoment_Tenant_Version_Moment");

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var m = await cleanup.ExpectedMoments.FindAsync(moment.MOM_IdExpectedMoment);
        if (m is not null) cleanup.ExpectedMoments.Remove(m);
        var s = await cleanup.Stages.FindAsync(stage.STG_IdStage);
        if (s is not null) cleanup.Stages.Remove(s);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test40_RowVersion_DetectsConcurrentUpdateOnCriterion()
    {
        // 40. RowVersion detecta una actualización concurrente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, $"RUB_{Guid.NewGuid():N}"[..20]);
        var criterion = CreateCriterionModel(rubric, "CRT_CONCURR", 1);

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        context.RubricCriteria.Add(criterion);
        await context.SaveChangesAsync();

        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        var entity2 = await client2.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);

        entity1!.CRT_Nombre = "Nombre Actualizado por Cliente 1";
        await client1.SaveChangesAsync();

        entity2!.CRT_Nombre = "Nombre Actualizado por Cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanup = CreateContext();
        var c = await cleanup.RubricCriteria.FindAsync(criterion.CRT_IdCriterion);
        if (c is not null) cleanup.RubricCriteria.Remove(c);
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test41_MigrationDoesNotCreateAdditionalTables()
    {
        // 41. La migración no crea tablas adicionales fuera de las autorizadas
        using var context = CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME LIKE 'LAB_%' ORDER BY TABLE_NAME")
            .ToListAsync();

        tables.Should().Contain(new[] { "LAB_ExpectedMoment", "LAB_Lab", "LAB_LabVersion", "LAB_Objective", "LAB_Rubric", "LAB_RubricCriterion", "LAB_Stage" });
    }
}

