using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabVersionDatabaseIntegrationTests
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
            LAB_Nombre = "Lab Base For Version Tests",
            LAB_Descripcion = "Lab creado para probar versiones",
            LAB_Tipo = LabTipos.Conversational,
            LAB_Dominio = "DISCOVERY",
            LAB_Estatus = LabEstatus.Published,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    private static LAB_LabVersion CreateValidVersionModel(LAB_Lab lab, int versionNumber = 1)
    {
        return new LAB_LabVersion
        {
            LAB_IdVersion = Guid.NewGuid(),
            LAB_IdLab = lab.LAB_IdLab,
            SEG_IdTenant = lab.SEG_IdTenant,
            LAB_NumeroVersion = versionNumber,
            LAB_ObjetivoGeneral = "Demostrar habilidad para conducir entrevistas con stakeholders.",
            LAB_InstruccionesParticipante = "Lea con atención el caso de negocio antes de iniciar.",
            LAB_BriefOculto = "El usuario simulado responderá con reticencia al inicio.",
            LAB_DuracionMinutos = 45,
            LAB_ScoreMinimo = 7.50m,
            LAB_Estatus = LabVersionEstatus.Draft,
            LAB_VigenciaDesde = new DateTime(2026, 1, 1),
            LAB_VigenciaHasta = new DateTime(2026, 12, 31),
            LAB_HashConfiguracion = new string('a', 64),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };
    }

    [Fact]
    public async Task Test1_MigrationCreateLabVersionTable_AppliesSuccessfully()
    {
        // 1. La migración CreateLabVersionTable se aplica correctamente
        using var context = CreateContext();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabVersionTable"));

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test2_LabVersionTable_ExistsInAzureSql()
    {
        // 2. La tabla LAB_LabVersion existe
        using var context = CreateContext();

        var count = await context.LabVersions.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test3_CanCreateValidVersion_ForExistingLab()
    {
        // 3. Se puede crear una versión válida para un Lab existente
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var version = CreateValidVersionModel(lab, 1);
        context.LabVersions.Add(version);
        await context.SaveChangesAsync();

        // Verificar lectura
        using var readContext = CreateContext();
        var savedVersion = await readContext.LabVersions
            .Include(v => v.Lab)
            .FirstOrDefaultAsync(v => v.LAB_IdVersion == version.LAB_IdVersion);

        savedVersion.Should().NotBeNull();
        savedVersion!.LAB_NumeroVersion.Should().Be(1);
        savedVersion.LAB_ScoreMinimo.Should().Be(7.50m);
        savedVersion.LAB_Estatus.Should().Be(LabVersionEstatus.Draft);
        savedVersion.RowVersion.Should().NotBeNull().And.NotBeEmpty();
        savedVersion.Lab.Should().NotBeNull();
        savedVersion.Lab!.LAB_IdLab.Should().Be(lab.LAB_IdLab);

        // Limpieza ordenada
        using var cleanupContext = CreateContext();
        var vToDelete = await cleanupContext.LabVersions.FindAsync(version.LAB_IdVersion);
        if (vToDelete is not null) cleanupContext.LabVersions.Remove(vToDelete);
        var labToDelete = await cleanupContext.Labs.FindAsync(lab.LAB_IdLab);
        if (labToDelete is not null) cleanupContext.Labs.Remove(labToDelete);
        await cleanupContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Test4_LabCanHaveMultipleVersions_WithDifferentNumbers()
    {
        // 4. Un Lab puede tener múltiples versiones con números diferentes
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v1 = CreateValidVersionModel(lab, 1);
        var v2 = CreateValidVersionModel(lab, 2);
        v2.LAB_Estatus = LabVersionEstatus.Approved;
        var v3 = CreateValidVersionModel(lab, 3);
        v3.LAB_Estatus = LabVersionEstatus.Published;

        context.LabVersions.AddRange(v1, v2, v3);
        await context.SaveChangesAsync();

        using var readContext = CreateContext();
        var versions = await readContext.LabVersions
            .Where(v => v.LAB_IdLab == lab.LAB_IdLab)
            .OrderBy(v => v.LAB_NumeroVersion)
            .ToListAsync();

        versions.Should().HaveCount(3);
        versions.Select(v => v.LAB_NumeroVersion).Should().Equal(1, 2, 3);

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == lab.LAB_IdLab));
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_CannotCreateVersion_ForNonExistentLab()
    {
        // 5. No se puede crear una versión para un LAB_IdLab inexistente
        using var context = CreateContext();

        var fakeLab = new LAB_Lab { LAB_IdLab = Guid.NewGuid(), SEG_IdTenant = Guid.NewGuid() };
        var version = CreateValidVersionModel(fakeLab, 1);

        context.LabVersions.Add(version);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Foreign key violation
        sqlEx.Message.Should().Contain("FK_LAB_LabVersion_LAB_Lab");
    }

    [Fact]
    public async Task Test6_CannotRepeatVersionNumber_ForSameLabAndTenant()
    {
        // 6. No se puede repetir LAB_NumeroVersion para el mismo Lab y tenant
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v1 = CreateValidVersionModel(lab, 1);
        context.LabVersions.Add(v1);
        await context.SaveChangesAsync();

        var v1Duplicate = CreateValidVersionModel(lab, 1);
        context.LabVersions.Add(v1Duplicate);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(2601); // Unique constraint violation

        // Limpieza
        using var cleanup = CreateContext();
        var v = await cleanup.LabVersions.FindAsync(v1.LAB_IdVersion);
        if (v is not null) cleanup.LabVersions.Remove(v);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test7_TwoDifferentLabs_CanHaveVersionNumber1()
    {
        // 7. Dos Labs diferentes pueden tener LAB_NumeroVersion igual a 1
        using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var labA = CreateLabModel(tenantId);
        var labB = CreateLabModel(tenantId);
        context.Labs.AddRange(labA, labB);
        await context.SaveChangesAsync();

        var vA = CreateValidVersionModel(labA, 1);
        var vB = CreateValidVersionModel(labB, 1);
        context.LabVersions.AddRange(vA, vB);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == labA.LAB_IdLab || v.LAB_IdLab == labB.LAB_IdLab));
        var la = await cleanup.Labs.FindAsync(labA.LAB_IdLab);
        var lb = await cleanup.Labs.FindAsync(labB.LAB_IdLab);
        if (la is not null) cleanup.Labs.Remove(la);
        if (lb is not null) cleanup.Labs.Remove(lb);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test8_SameVersionNumber_CanExistInDifferentTenants()
    {
        // 8. El mismo número de versión puede existir para Labs de distintos tenants
        using var context = CreateContext();

        var labTenant1 = CreateLabModel();
        var labTenant2 = CreateLabModel();
        context.Labs.AddRange(labTenant1, labTenant2);
        await context.SaveChangesAsync();

        var v1 = CreateValidVersionModel(labTenant1, 1);
        var v2 = CreateValidVersionModel(labTenant2, 1);
        context.LabVersions.AddRange(v1, v2);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        cleanup.LabVersions.RemoveRange(cleanup.LabVersions.Where(v => v.LAB_IdLab == labTenant1.LAB_IdLab || v.LAB_IdLab == labTenant2.LAB_IdLab));
        var l1 = await cleanup.Labs.FindAsync(labTenant1.LAB_IdLab);
        var l2 = await cleanup.Labs.FindAsync(labTenant2.LAB_IdLab);
        if (l1 is not null) cleanup.Labs.Remove(l1);
        if (l2 is not null) cleanup.Labs.Remove(l2);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test9_LabNumeroVersion_RejectsZero()
    {
        // 9. LAB_NumeroVersion rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 0); // Cero
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_NumeroVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test10_LabNumeroVersion_RejectsNegativeNumbers()
    {
        // 10. LAB_NumeroVersion rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, -1);
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_NumeroVersion");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test11_LabDuracionMinutos_RejectsZero()
    {
        // 11. LAB_DuracionMinutos rechaza cero
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_DuracionMinutos = 0;
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_DuracionMinutos");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test12_LabDuracionMinutos_RejectsNegativeNumbers()
    {
        // 12. LAB_DuracionMinutos rechaza números negativos
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_DuracionMinutos = -15;
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_DuracionMinutos");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test13_LabScoreMinimo_RejectsLessThan1()
    {
        // 13. LAB_ScoreMinimo rechaza valores menores que 1.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_ScoreMinimo = 0.99m;
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_ScoreMinimo");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test14_LabScoreMinimo_RejectsGreaterThan10()
    {
        // 14. LAB_ScoreMinimo rechaza valores mayores que 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_ScoreMinimo = 10.01m;
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_ScoreMinimo");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test15_LabScoreMinimo_AcceptsBoundary1()
    {
        // 15. LAB_ScoreMinimo acepta 1.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_ScoreMinimo = 1.00m;
        context.LabVersions.Add(v);
        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var savedV = await cleanup.LabVersions.FindAsync(v.LAB_IdVersion);
        if (savedV is not null) cleanup.LabVersions.Remove(savedV);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test16_LabScoreMinimo_AcceptsBoundary10()
    {
        // 16. LAB_ScoreMinimo acepta 10.00
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_ScoreMinimo = 10.00m;
        context.LabVersions.Add(v);
        var act = async () => await context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Limpieza
        using var cleanup = CreateContext();
        var savedV = await cleanup.LabVersions.FindAsync(v.LAB_IdVersion);
        if (savedV is not null) cleanup.LabVersions.Remove(savedV);
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test17_LabEstatus_RejectsInvalidValue()
    {
        // 17. LAB_Estatus rechaza un valor no permitido
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_Estatus = "ARCHIVED"; // No permitido
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_Estatus");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test18_LabVigenciaHasta_RejectsDateEarlierThanVigenciaDesde()
    {
        // 18. LAB_VigenciaHasta rechaza una fecha anterior a LAB_VigenciaDesde
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_VigenciaDesde = new DateTime(2026, 6, 1);
        v.LAB_VigenciaHasta = new DateTime(2026, 5, 31); // Anterior
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_Vigencia");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test19_LabHashConfiguracion_RejectsLengthDifferentFrom64()
    {
        // 19. LAB_HashConfiguracion rechaza valores con longitud diferente de 64 caracteres
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        v.LAB_HashConfiguracion = "123456"; // Solo 6 caracteres
        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("CK_LAB_LabVersion_LAB_HashConfiguracion");

        // Limpieza
        using var cleanup = CreateContext();
        var l = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (l is not null) cleanup.Labs.Remove(l);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test20_CannotHardDeleteLab_WhenItHasVersions()
    {
        // 20. No se puede eliminar físicamente un LAB_Lab que tenga versiones (Restrict / No Action)
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        context.LabVersions.Add(v);
        await context.SaveChangesAsync();

        // Intentar eliminar el Lab directamente mientras tiene la versión
        using var deleteContext = CreateContext();
        var labToDelete = await deleteContext.Labs.FindAsync(lab.LAB_IdLab);
        deleteContext.Labs.Remove(labToDelete!);
        Func<Task> act = async () => await deleteContext.SaveChangesAsync();

        // Restrict / NoAction en SQL Server o EF lanza DbUpdateException
        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Conflicto de referencia DELETE statement conflicted with the REFERENCE constraint

        // Limpieza en orden referencial correcto: primero versión, luego Lab
        using var cleanup = CreateContext();
        var vToDelete = await cleanup.LabVersions.FindAsync(v.LAB_IdVersion);
        if (vToDelete is not null) cleanup.LabVersions.Remove(vToDelete);
        await cleanup.SaveChangesAsync();

        var lToDelete = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lToDelete is not null) cleanup.Labs.Remove(lToDelete);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test21_RowVersion_DetectsConcurrentUpdate()
    {
        // 21. RowVersion detecta una actualización concurrente de LAB_LabVersion
        using var context = CreateContext();

        var lab = CreateLabModel();
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        var v = CreateValidVersionModel(lab, 1);
        context.LabVersions.Add(v);
        await context.SaveChangesAsync();

        // Dos clientes cargando la misma versión simultáneamente
        using var client1 = CreateContext();
        using var client2 = CreateContext();

        var entity1 = await client1.LabVersions.FindAsync(v.LAB_IdVersion);
        var entity2 = await client2.LabVersions.FindAsync(v.LAB_IdVersion);

        entity1!.LAB_ObjetivoGeneral = "Objetivo modificado por cliente 1";
        await client1.SaveChangesAsync();

        entity2!.LAB_ObjetivoGeneral = "Objetivo modificado por cliente 2";
        Func<Task> act = async () => await client2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanup = CreateContext();
        var vToDelete = await cleanup.LabVersions.FindAsync(v.LAB_IdVersion);
        if (vToDelete is not null) cleanup.LabVersions.Remove(vToDelete);
        var lToDelete = await cleanup.Labs.FindAsync(lab.LAB_IdLab);
        if (lToDelete is not null) cleanup.Labs.Remove(lToDelete);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test22_CannotRelateVersion_ToLabOfDifferentTenant()
    {
        // 22. No se puede relacionar una versión de un tenant con un Lab perteneciente a otro tenant
        using var context = CreateContext();

        var labTenant1 = CreateLabModel();
        context.Labs.Add(labTenant1);
        await context.SaveChangesAsync();

        // Versión creada con un SEG_IdTenant distinto al del Lab
        var mismatchedTenantId = Guid.NewGuid();
        var v = CreateValidVersionModel(labTenant1, 1);
        v.SEG_IdTenant = mismatchedTenantId; // Tenant diferente al lab

        context.LabVersions.Add(v);
        Func<Task> act = async () => await context.SaveChangesAsync();

        // La FK compuesta (SEG_IdTenant, LAB_IdLab) debe fallar
        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        var sqlEx = (SqlException)ex.Which.InnerException!;
        sqlEx.Number.Should().Be(547);
        sqlEx.Message.Should().Contain("FK_LAB_LabVersion_LAB_Lab_SEG_IdTenant_LAB_IdLab");

        // Limpieza
        using var cleanup = CreateContext();
        var lToDelete = await cleanup.Labs.FindAsync(labTenant1.LAB_IdLab);
        if (lToDelete is not null) cleanup.Labs.Remove(lToDelete);
        await cleanup.SaveChangesAsync();
    }

    [Fact]
    public async Task Test23_MigrationDoesNotCreateAdditionalTables()
    {
        // 23. La migración CreateLabVersionTable no crea tablas adicionales fuera de las esperadas
        using var context = CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME LIKE 'LAB_%'")
            .ToListAsync();

        tables.Should().Contain(new[] { "LAB_Lab", "LAB_LabVersion" });
        tables.Should().OnlyContain(t =>
            t == "LAB_ArtifactSubmission" || t == "LAB_Attempt" || t == "LAB_AttemptEvaluation" ||
            t == "LAB_ConversationTurn" || t == "LAB_Enrollment" || t == "LAB_ExpectedMoment" ||
            t == "LAB_Lab" || t == "LAB_LabVersion" || t == "LAB_Objective" || t == "LAB_Rubric" ||
            t == "LAB_RubricCriterion" || t == "LAB_Scenario" || t == "LAB_SimulatedActor" ||
            t == "LAB_Stage" || t == "LAB_TestedSkill" || t == "LAB_UserAction");
    }
}
