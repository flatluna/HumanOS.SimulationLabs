using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabDatabaseIntegrationTests
{
    private SimulationLabsDbContext CreateContext()
    {
        return DbContextHelper.CreateDbContext();
    }

    [Fact]
    public async Task Test1_MigrationAppliesSuccessfully_AndTableExists()
    {
        // 1. La migración se aplica correctamente
        using var context = CreateContext();

        // Verificar que la base de datos es accesible y la migración está aplicada
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty("Todas las migraciones deben estar aplicadas en la base de datos");

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.EndsWith("_CreateLabTable"));

        // Comprobar que podemos consultar la tabla sin errores
        var count = await context.Labs.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Test2_CanCreateValidLab()
    {
        // 2. Se puede crear un Lab válido
        using var context = CreateContext();

        var labId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var uniqueCode = $"TEST_LAB_{Guid.NewGuid():N}"[..20];

        var lab = new LAB_Lab
        {
            LAB_IdLab = labId,
            SEG_IdTenant = tenantId,
            LAB_Codigo = uniqueCode,
            LAB_Nombre = "Discovery Phase Lab Test",
            LAB_Descripcion = "Práctica para levantamiento inicial de requerimientos con el cliente.",
            LAB_Tipo = LabTipos.Conversational,
            LAB_Dominio = "DISCOVERY",
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "IntegrationTestUser"
        };

        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        // Leerlo de nuevo para confirmar persistencia y generación de RowVersion
        using var readContext = CreateContext();
        var savedLab = await readContext.Labs.FindAsync(labId);

        savedLab.Should().NotBeNull();
        savedLab!.LAB_Codigo.Should().Be(uniqueCode);
        savedLab.LAB_Tipo.Should().Be(LabTipos.Conversational);
        savedLab.LAB_Estatus.Should().Be(LabEstatus.Draft);
        savedLab.RowVersion.Should().NotBeNull();
        savedLab.RowVersion.Should().NotBeEmpty();

        // Limpieza del registro de prueba
        readContext.Labs.Remove(savedLab);
        await readContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Test3_CannotDuplicateLabCodigoWithinSameTenant()
    {
        // 3. No se puede duplicar LAB_Codigo dentro del mismo tenant
        using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var duplicatedCode = $"DUP_{Guid.NewGuid():N}"[..20];

        var lab1 = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_Codigo = duplicatedCode,
            LAB_Nombre = "Primer Lab",
            LAB_Descripcion = "Descripción 1",
            LAB_Tipo = LabTipos.Desktop,
            LAB_Dominio = "SCOPING",
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.Add(lab1);
        await context.SaveChangesAsync();

        var lab2 = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenantId, // Mismo tenant
            LAB_Codigo = duplicatedCode, // Mismo código
            LAB_Nombre = "Segundo Lab Duplicado",
            LAB_Descripcion = "Descripción 2",
            LAB_Tipo = LabTipos.Desktop,
            LAB_Dominio = "SCOPING",
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.Add(lab2);
        Func<Task> act = async () => await context.SaveChangesAsync();

        // Debe lanzar DbUpdateException debido al índice único UQ_LAB_Lab_SEG_IdTenant_LAB_Codigo
        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)exception.Which.InnerException!;
        sqlEx.Number.Should().Be(2601); // Error de clave duplicada en índice único

        // Limpieza
        using var cleanupContext = CreateContext();
        var toDelete = await cleanupContext.Labs.FindAsync(lab1.LAB_IdLab);
        if (toDelete is not null)
        {
            cleanupContext.Labs.Remove(toDelete);
            await cleanupContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Test4_SameLabCodigoCanExistInDifferentTenants()
    {
        // 4. El mismo LAB_Codigo puede existir en tenants diferentes
        using var context = CreateContext();

        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var sharedCode = $"SHARED_{Guid.NewGuid():N}"[..20];

        var labTenant1 = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenant1,
            LAB_Codigo = sharedCode,
            LAB_Nombre = "Lab Tenant 1",
            LAB_Descripcion = "Descripción Tenant 1",
            LAB_Tipo = LabTipos.Hybrid,
            LAB_Dominio = "SOLUTION_DESIGN",
            LAB_Estatus = LabEstatus.Published,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        var labTenant2 = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenant2,
            LAB_Codigo = sharedCode,
            LAB_Nombre = "Lab Tenant 2",
            LAB_Descripcion = "Descripción Tenant 2",
            LAB_Tipo = LabTipos.Hybrid,
            LAB_Dominio = "SOLUTION_DESIGN",
            LAB_Estatus = LabEstatus.Published,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.AddRange(labTenant1, labTenant2);
        var action = async () => await context.SaveChangesAsync();
        await action.Should().NotThrowAsync();

        // Limpieza
        using var cleanupContext = CreateContext();
        var item1 = await cleanupContext.Labs.FindAsync(labTenant1.LAB_IdLab);
        var item2 = await cleanupContext.Labs.FindAsync(labTenant2.LAB_IdLab);
        if (item1 is not null) cleanupContext.Labs.Remove(item1);
        if (item2 is not null) cleanupContext.Labs.Remove(item2);
        await cleanupContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Test5_LabTipoRejectsInvalidValues()
    {
        // 5. LAB_Tipo rechaza valores no permitidos (Check Constraint CK_LAB_Lab_LAB_Tipo)
        using var context = CreateContext();

        var lab = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = Guid.NewGuid(),
            LAB_Codigo = $"INVALID_TYPE_{Guid.NewGuid():N}"[..20],
            LAB_Nombre = "Lab Con Tipo Invalido",
            LAB_Descripcion = "Descripcion",
            LAB_Tipo = "INVALID_TYPE", // No permitido
            LAB_Dominio = "DISCOVERY",
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.Add(lab);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)exception.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Violación de CHECK constraint
        sqlEx.Message.Should().Contain("CK_LAB_Lab_LAB_Tipo");
    }

    [Fact]
    public async Task Test6_LabEstatusRejectsInvalidValues()
    {
        // 6. LAB_Estatus rechaza valores no permitidos (Check Constraint CK_LAB_Lab_LAB_Estatus)
        using var context = CreateContext();

        var lab = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = Guid.NewGuid(),
            LAB_Codigo = $"INVALID_STATUS_{Guid.NewGuid():N}"[..20],
            LAB_Nombre = "Lab Con Estatus Invalido",
            LAB_Descripcion = "Descripcion",
            LAB_Tipo = LabTipos.Conversational,
            LAB_Dominio = "DISCOVERY",
            LAB_Estatus = "PENDING_APPROVAL", // No permitido
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.Add(lab);
        Func<Task> act = async () => await context.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SqlException>();
        var sqlEx = (SqlException)exception.Which.InnerException!;
        sqlEx.Number.Should().Be(547); // Violación de CHECK constraint
        sqlEx.Message.Should().Contain("CK_LAB_Lab_LAB_Estatus");
    }

    [Fact]
    public async Task Test7_RowVersionDetectsConcurrentUpdate()
    {
        // 7. RowVersion detecta una actualización concurrente
        using var context = CreateContext();

        var labId = Guid.NewGuid();
        var lab = new LAB_Lab
        {
            LAB_IdLab = labId,
            SEG_IdTenant = Guid.NewGuid(),
            LAB_Codigo = $"CONCURR_{Guid.NewGuid():N}"[..20],
            LAB_Nombre = "Lab Inicial Concurrente",
            LAB_Descripcion = "Descripcion Inicial",
            LAB_Tipo = LabTipos.Desktop,
            LAB_Dominio = "PROCUREMENT",
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = Guid.NewGuid(),
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = "Tester"
        };

        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        // Simular dos usuarios cargando la misma entidad al mismo tiempo
        using var client1Context = CreateContext();
        using var client2Context = CreateContext();

        var entity1 = await client1Context.Labs.FindAsync(labId);
        var entity2 = await client2Context.Labs.FindAsync(labId);

        entity1!.LAB_Nombre = "Nombre Actualizado por Cliente 1";
        entity1.FechaActualizacion = DateTimeOffset.UtcNow;
        entity1.ActualizadoPor = "Cliente 1";

        // Cliente 1 guarda primero con éxito (esto incrementa el RowVersion en la base de datos)
        await client1Context.SaveChangesAsync();

        // Cliente 2 intenta guardar su versión sin saber que ya fue modificada
        entity2!.LAB_Nombre = "Nombre Actualizado por Cliente 2";
        entity2.FechaActualizacion = DateTimeOffset.UtcNow;
        entity2.ActualizadoPor = "Cliente 2";

        Func<Task> actClient2 = async () => await client2Context.SaveChangesAsync();

        // Debe lanzar DbUpdateConcurrencyException debido al RowVersion
        await actClient2.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Limpieza
        using var cleanupContext = CreateContext();
        var item = await cleanupContext.Labs.FindAsync(labId);
        if (item is not null)
        {
            cleanupContext.Labs.Remove(item);
            await cleanupContext.SaveChangesAsync();
        }
    }
}
