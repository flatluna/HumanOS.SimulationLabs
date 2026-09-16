using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabRubricDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Rubric Tests",
            LAB_Descripcion = "Lab creado para probar rúbricas",
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
            LAB_ObjetivoGeneral = "Objetivo general para probar rúbrica",
            LAB_InstruccionesParticipante = "Instrucciones de versión",
            LAB_DuracionMinutos = 60,
            LAB_ScoreMinimo = 7.00m,
            LAB_Estatus = LabVersionEstatus.Published,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_Rubric CreateRubricModel(
        LAB_LabVersion version,
        string code = "RUB_TEST",
        string tipo = RubricTipoEvaluacion.Conversation,
        string metodo = RubricMetodoCalculo.WeightedWithCriticalGate)
    {
        return new LAB_Rubric
        {
            RUB_IdRubric = Guid.NewGuid(),
            SEG_IdTenant = version.SEG_IdTenant,
            LAB_IdVersion = version.LAB_IdVersion,
            RUB_Codigo = code,
            RUB_Nombre = "Rúbrica de evaluación de Scoping",
            RUB_Descripcion = "Evalúa capacidades de definición de alcance y captura de valor",
            RUB_TipoEvaluacion = tipo,
            RUB_EscalaMinima = 1.00m,
            RUB_EscalaMaxima = 10.00m,
            RUB_ScoreMinimoAprobacion = 7.00m,
            RUB_MetodoCalculo = metodo,
            RUB_RequiereEvidencia = true,
            RUB_PermiteFallaCritica = true,
            RUB_InstruccionesEvaluador = "Evaluar comportamientos observables y resultados. No comparar frases exactas.",
            RUB_Estatus = RubricEstatus.Draft,
            RUB_VigenciaDesde = new DateTime(2026, 1, 1),
            RUB_VigenciaHasta = new DateTime(2026, 12, 31),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabRubricTable_AppliesSuccessfully()
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

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabRubricTable_ExistsInAzureSql()
    {
        // 2. LAB_Rubric existe
        using var context = CreateContext();

        var count = await context.Rubrics.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_4_CanCreateValidRubric_AndVersionHasOneRubric()
    {
        // 3. Se puede crear una rúbrica válida
        // 4. Una versión puede tener una rúbrica
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, "RUB_SCOPING_MAIN");

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var saved = await readContext.Rubrics
            .Include(r => r.LabVersion)
            .FirstOrDefaultAsync(r => r.RUB_IdRubric == rubric.RUB_IdRubric);

        saved.Should().NotBeNull();
        saved!.RUB_Codigo.Should().Be("RUB_SCOPING_MAIN");
        saved.RUB_ScoreMinimoAprobacion.Should().Be(7.00m);
        saved.RUB_RequiereEvidencia.Should().BeTrue();
        saved.RUB_PermiteFallaCritica.Should().BeTrue();
        saved.LabVersion.Should().NotBeNull();
        saved.LabVersion!.LAB_IdVersion.Should().Be(version.LAB_IdVersion);
        saved.RowVersion.Should().NotBeNull().And.NotBeEmpty();

        // Limpieza: Rubric -> Version -> Lab
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_VersionCannotHaveTwoRubrics()
    {
        // 5. Una versión no puede tener dos rúbricas (1 a 0..1 uniqueness)
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric1 = CreateRubricModel(version, "RUB_V1_FIRST");

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric1);
        await context.SaveChangesAsync();

        // En un nuevo contexto para que se envíe la inserción a Azure SQL y se dispare el índice único
        using var context2 = CreateContext();
        var rubric2 = CreateRubricModel(version, "RUB_V1_SECOND");
        context2.Rubrics.Add(rubric2);
        Func<Task> act = async () => await context2.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Rubric_SEG_IdTenant_LAB_IdVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric1.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test6_CannotCreateRubric_ForNonExistentVersion()
    {
        // 6. No se puede crear una rúbrica para una versión inexistente
        using var context = CreateContext();

        var fakeVersion = new LAB_LabVersion { LAB_IdVersion = Guid.NewGuid(), SEG_IdTenant = Guid.NewGuid() };
        var rubric = CreateRubricModel(fakeVersion, "ORPHAN_RUBRIC");

        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Rubric_LAB_LabVersion");
    }

    [Fact]
    public async Task Test7_CannotAssociateRubric_WithVersionOfDifferentTenant()
    {
        // 7. No se puede asociar una rúbrica con una versión de otro tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, "CROSS_TENANT_RUBRIC");
        rubric.SEG_IdTenant = Guid.NewGuid(); // Tenant distinto al de la versión

        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_Rubric_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test8_DifferentVersions_CanHaveDifferentRubrics()
    {
        // 8. Versiones diferentes pueden tener rúbricas diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        var v1 = CreateVersionModel(lab, 1);
        var v2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(v1, v2);

        var r1 = CreateRubricModel(v1, "RUB_V1_SPECIFIC");
        var r2 = CreateRubricModel(v2, "RUB_V2_SPECIFIC");
        context.Rubrics.AddRange(r1, r2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Rubrics.RemoveRange(cleanup.Rubrics.Where(r => r.LAB_IdVersion == v1.LAB_IdVersion || r.LAB_IdVersion == v2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test9_CannotRepeatRubricCodigo_WithinSameTenant()
    {
        // 9. RUB_Codigo no se puede repetir dentro del mismo tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        var v1 = CreateVersionModel(lab, 1);
        var v2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(v1, v2);

        var r1 = CreateRubricModel(v1, "DUP_RUBRIC_CODE");
        context.Rubrics.Add(r1);
        await context.SaveChangesAsync();

        var r2 = CreateRubricModel(v2, "DUP_RUBRIC_CODE"); // Mismo código en el mismo tenant
        context.Rubrics.Add(r2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601);
        sqlEx.Message.Should().Contain("UQ_LAB_Rubric_SEG_IdTenant_RUB_Codigo");

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(r1.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_SameRubricCodigo_CanExistInDifferentTenants()
    {
        // 10. RUB_Codigo puede repetirse en tenants diferentes
        using var context = CreateContext();

        var lab1 = CreateLabModel();
        var v1 = CreateVersionModel(lab1, 1);
        var lab2 = CreateLabModel();
        var v2 = CreateVersionModel(lab2, 1);

        context.Labs.AddRange(lab1, lab2);
        context.LabVersions.AddRange(v1, v2);

        var r1 = CreateRubricModel(v1, "SHARED_RUBRIC_CODE");
        var r2 = CreateRubricModel(v2, "SHARED_RUBRIC_CODE");
        context.Rubrics.AddRange(r1, r2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Rubrics.RemoveRange(cleanup.Rubrics.Where(r => r.LAB_IdVersion == v1.LAB_IdVersion || r.LAB_IdVersion == v2.LAB_IdVersion));
        var v1Del = await cleanup.LabVersions.FindAsync(v1.LAB_IdVersion);
        if (v1Del is not null) cleanup.LabVersions.Remove(v1Del);
        var v2Del = await cleanup.LabVersions.FindAsync(v2.LAB_IdVersion);
        if (v2Del is not null) cleanup.LabVersions.Remove(v2Del);
        var l1Del = await cleanup.Labs.FindAsync(lab1.LAB_IdLab);
        if (l1Del is not null) cleanup.Labs.Remove(l1Del);
        var l2Del = await cleanup.Labs.FindAsync(lab2.LAB_IdLab);
        if (l2Del is not null) cleanup.Labs.Remove(l2Del);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test11_12_EscalaMinima_Accepts1AndRejectsDifferent()
    {
        // 11. RUB_EscalaMinima acepta 1.00
        // 12. RUB_EscalaMinima rechaza valores distintos de 1.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rInvalid = CreateRubricModel(version, "BAD_ESCALA_MIN");
        rInvalid.RUB_EscalaMinima = 2.00m; // Distinto de 1.00
        context.Rubrics.Add(rInvalid);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_RUB_EscalaMinima");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test13_14_EscalaMaxima_Accepts10AndRejectsDifferent()
    {
        // 13. RUB_EscalaMaxima acepta 10.00
        // 14. RUB_EscalaMaxima rechaza valores distintos de 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rInvalid = CreateRubricModel(version, "BAD_ESCALA_MAX");
        rInvalid.RUB_EscalaMaxima = 100.00m; // Distinto de 10.00
        context.Rubrics.Add(rInvalid);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_RUB_EscalaMaxima");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test15_18_ScoreMinimoAprobacion_BoundariesAndRejections()
    {
        // 15. RUB_ScoreMinimoAprobacion acepta 1.00
        // 16. RUB_ScoreMinimoAprobacion acepta 10.00
        // 17. RUB_ScoreMinimoAprobacion rechaza valores menores de 1.00
        // 18. RUB_ScoreMinimoAprobacion rechaza valores mayores de 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        var v1 = CreateVersionModel(lab, 1);
        var v2 = CreateVersionModel(lab, 2);
        context.Labs.Add(lab);
        context.LabVersions.AddRange(v1, v2);

        var r1 = CreateRubricModel(v1, "SCORE_MIN_1");
        r1.RUB_ScoreMinimoAprobacion = 1.00m; // Acepta 1.00
        var r2 = CreateRubricModel(v2, "SCORE_MAX_10");
        r2.RUB_ScoreMinimoAprobacion = 10.00m; // Acepta 10.00

        context.Rubrics.AddRange(r1, r2);
        var actValid = async () => await context.SaveChangesAsync();
        await actValid.Should().NotThrowAsync();

        // Probar rechazo menor a 1.00
        var v3 = CreateVersionModel(lab, 3);
        context.LabVersions.Add(v3);
        var rLow = CreateRubricModel(v3, "SCORE_LOW");
        rLow.RUB_ScoreMinimoAprobacion = 0.99m;
        context.Rubrics.Add(rLow);
        Func<Task> actLow = async () => await context.SaveChangesAsync();

        var exLow = await actLow.Should().ThrowAsync<DbUpdateException>();
        var sqlExLow = (SqlException)exLow.Which.InnerException!;
        sqlExLow.Number.Should().Be(547);
        sqlExLow.Message.Should().Contain("CK_LAB_Rubric_RUB_ScoreMinimoAprobacion");

        context.Entry(rLow).State = EntityState.Detached;

        // Probar rechazo mayor a 10.00
        var rHigh = CreateRubricModel(v3, "SCORE_HIGH");
        rHigh.RUB_ScoreMinimoAprobacion = 10.01m;
        context.Rubrics.Add(rHigh);
        Func<Task> actHigh = async () => await context.SaveChangesAsync();

        var exHigh = await actHigh.Should().ThrowAsync<DbUpdateException>();
        var sqlExHigh = (SqlException)exHigh.Which.InnerException!;
        sqlExHigh.Number.Should().Be(547);
        sqlExHigh.Message.Should().Contain("CK_LAB_Rubric_RUB_ScoreMinimoAprobacion");

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.Rubrics.RemoveRange(cleanup.Rubrics.Where(r => r.LAB_IdVersion == v1.LAB_IdVersion || r.LAB_IdVersion == v2.LAB_IdVersion));
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("CONVERSATION")]
    [InlineData("DESKTOP")]
    [InlineData("HYBRID")]
    [InlineData("ARTIFACT")]
    [InlineData("MULTI_SOURCE")]
    public async Task Test19_TipoEvaluacion_AcceptsAllFiveAllowedValues(string tipoEvaluacion)
    {
        // 19. RUB_TipoEvaluacion acepta los cinco valores permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);

        var rubric = CreateRubricModel(version, $"RUB_{tipoEvaluacion}", tipo: tipoEvaluacion);
        context.Rubrics.Add(rubric);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test20_TipoEvaluacion_RejectsInvalidValue()
    {
        // 20. RUB_TipoEvaluacion rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, "BAD_TIPO");
        rubric.RUB_TipoEvaluacion = "UNKNOWN_EVALUATION";
        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_RUB_TipoEvaluacion");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("WEIGHTED_AVERAGE")]
    [InlineData("SIMPLE_AVERAGE")]
    [InlineData("CRITICAL_GATE")]
    [InlineData("WEIGHTED_WITH_CRITICAL_GATE")]
    public async Task Test21_MetodoCalculo_AcceptsAllFourAllowedValues(string metodoCalculo)
    {
        // 21. RUB_MetodoCalculo acepta los cuatro valores permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);

        var rubric = CreateRubricModel(version, $"RUB_{metodoCalculo}", metodo: metodoCalculo);
        context.Rubrics.Add(rubric);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test22_MetodoCalculo_RejectsInvalidValue()
    {
        // 22. RUB_MetodoCalculo rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, "BAD_METODO");
        rubric.RUB_MetodoCalculo = "RANDOM_GUESS";
        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_RUB_MetodoCalculo");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("APPROVED")]
    [InlineData("PUBLISHED")]
    [InlineData("RETIRED")]
    public async Task Test23_Estatus_AcceptsAllFourAllowedValues(string estatus)
    {
        // 23. RUB_Estatus acepta los cuatro valores permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);

        var rubric = CreateRubricModel(version, $"RUB_STAT_{estatus}");
        rubric.RUB_Estatus = estatus;
        context.Rubrics.Add(rubric);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test24_Estatus_RejectsInvalidValue()
    {
        // 24. RUB_Estatus rechaza valores no permitidos
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, "BAD_ESTATUS");
        rubric.RUB_Estatus = "OBSOLETE";
        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_RUB_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test25_VigenciaHasta_RejectsDateEarlierThanVigenciaDesde()
    {
        // 25. RUB_VigenciaHasta rechaza una fecha anterior a RUB_VigenciaDesde
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, "BAD_VIGENCIA");
        rubric.RUB_VigenciaDesde = new DateTime(2026, 6, 1);
        rubric.RUB_VigenciaHasta = new DateTime(2026, 5, 31); // Anterior
        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_Rubric_Vigencia");

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test26_27_BooleanProperties_PersistCorrectly()
    {
        // 26. RUB_RequiereEvidencia se guarda correctamente
        // 27. RUB_PermiteFallaCritica se guarda correctamente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);

        var rubric = CreateRubricModel(version, "RUB_BOOLS");
        rubric.RUB_RequiereEvidencia = false;
        rubric.RUB_PermiteFallaCritica = false;
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var readCtx = CreateContext();
        var saved = await readCtx.Rubrics.FindAsync(rubric.RUB_IdRubric);
        saved.Should().NotBeNull();
        saved!.RUB_RequiereEvidencia.Should().BeFalse();
        saved.RUB_PermiteFallaCritica.Should().BeFalse();

        // Limpieza
        using var cleanup = CreateContext();
        var rDel = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (rDel is not null) cleanup.Rubrics.Remove(rDel);
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Theory]
    [InlineData("RUB_Codigo")]
    [InlineData("RUB_Nombre")]
    [InlineData("RUB_Descripcion")]
    [InlineData("RUB_InstruccionesEvaluador")]
    [InlineData("CreadoPor")]
    public async Task Test28_RequiredTextFields_RejectEmptyOrSpaces(string fieldName)
    {
        // 28. Los campos de texto requeridos rechazan texto vacío o espacios
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        var rubric = CreateRubricModel(version, $"RUB_WS_{fieldName}");
        if (fieldName == "RUB_Codigo") rubric.RUB_Codigo = "    ";
        else if (fieldName == "RUB_Nombre") rubric.RUB_Nombre = "    ";
        else if (fieldName == "RUB_Descripcion") rubric.RUB_Descripcion = "    ";
        else if (fieldName == "RUB_InstruccionesEvaluador") rubric.RUB_InstruccionesEvaluador = "    ";
        else if (fieldName == "CreadoPor") rubric.CreadoPor = "    ";

        context.Rubrics.Add(rubric);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain(fieldName);

        // Limpieza
        using var cleanup = CreateContext();
        var vDel = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vDel is not null) cleanup.LabVersions.Remove(vDel);
        var lDel = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lDel is not null) cleanup.Labs.Remove(lDel);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test29_CannotHardDeleteLabVersion_WhenItHasRubric()
    {
        // 29. No se puede eliminar una versión que tenga una rúbrica (Restrict)
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, "RUB_BLOCK_VER_DEL");

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var delCtx = CreateContext();
        var vDel = await delCtx.LabVersions.FindAsync(version.LAB_IdVersion);
        delCtx.LabVersions.Remove(vDel!);
        Func<Task> act = async () => await delCtx.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);

        // Limpieza ordenada: Rubric -> Version -> Lab
        using var cleanup = CreateContext();
        var r = await cleanup.Rubrics.FindAsync(rubric.RUB_IdRubric);
        if (r is not null) cleanup.Rubrics.Remove(r);
        await cleanup.SaveChangesAsync();

        var v = await cleanup.LabVersions.FindAsync(version.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        await cleanup.SaveChangesAsync();

        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test30_RowVersion_DetectsConcurrentUpdateOnRubric()
    {
        // 30. RowVersion detecta una actualización concurrente
        using var context = CreateContext();

        var lab = CreateLabModel();
        var version = CreateVersionModel(lab, 1);
        var rubric = CreateRubricModel(version, "RUB_CONCURR");

        context.Labs.Add(lab);
        context.LabVersions.Add(version);
        context.Rubrics.Add(rubric);
        await context.SaveChangesAsync();

        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.Rubrics.FindAsync(rubric.RUB_IdRubric);
        var entity2 = await client2.Rubrics.FindAsync(rubric.RUB_IdRubric);

        entity1!.RUB_Nombre = "Nombre Actualizado por Cliente 1";
        await client1.SaveChangesAsync();

        entity2!.RUB_Nombre = "Nombre Actualizado por Cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

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
    public async Task Test31_MigrationDoesNotCreateAdditionalTables()
    {
        // 31. La migración no crea tablas adicionales fuera de las autorizadas
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
