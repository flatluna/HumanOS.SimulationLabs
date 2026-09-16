using System.Reflection;
using FluentAssertions;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabModelDefinitionTests
{
    [Fact]
    public void Test8_NoHardDeleteOperationExposed()
    {
        // 8. No existe una operación pública de hard delete
        // Verificamos que las entidades y SimulationLabsDbContext no expongan métodos públicos Delete/RemoveHard/HardDelete
        var entityMethods = typeof(LAB_Lab).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        entityMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        entityMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var versionMethods = typeof(LAB_LabVersion).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        versionMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        versionMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var stageMethods = typeof(LAB_Stage).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        stageMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        stageMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var objectiveMethods = typeof(LAB_Objective).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        objectiveMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        objectiveMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var momentMethods = typeof(LAB_ExpectedMoment).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        momentMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        momentMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var rubricMethods = typeof(LAB_Rubric).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        rubricMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        rubricMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var criterionMethods = typeof(LAB_RubricCriterion).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        criterionMethods.Should().NotContain(m => m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        criterionMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));

        var dbContextMethods = typeof(SimulationLabsDbContext).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        dbContextMethods.Should().NotContain(m => m.Name.Contains("HardDelete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModelConfiguration_HasOnlyLAB_LabTable_AndCorrectConstraints()
    {
        var options = new DbContextOptionsBuilder<SimulationLabsDbContext>()
            .UseSqlServer("Server=fake;Database=fake;")
            .Options;

        using var context = new SimulationLabsDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;

        // Entidades mapeadas en este DbContext (creció con cada nueva feature, ver Configurations/*.cs)
        var entityTypes = model.GetEntityTypes().ToList();
        entityTypes.Should().HaveCount(16);

        var labType = entityTypes.First(e => e.GetTableName() == "LAB_Lab");
        var versionType = entityTypes.First(e => e.GetTableName() == "LAB_LabVersion");
        var stageType = entityTypes.First(e => e.GetTableName() == "LAB_Stage");
        var objectiveType = entityTypes.First(e => e.GetTableName() == "LAB_Objective");
        var momentType = entityTypes.First(e => e.GetTableName() == "LAB_ExpectedMoment");
        var rubricType = entityTypes.First(e => e.GetTableName() == "LAB_Rubric");
        var criterionType = entityTypes.First(e => e.GetTableName() == "LAB_RubricCriterion");

        // Primary key LAB_Lab
        var pk = labType.FindPrimaryKey();
        pk.Should().NotBeNull();
        pk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Lab.LAB_IdLab));

        // Clave alternativa (Alternate Key) para asegurar SEG_IdTenant en LAB_Lab
        var altKey = labType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey());
        altKey.Should().NotBeNull();
        altKey!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Lab.SEG_IdTenant), nameof(LAB_Lab.LAB_IdLab));

        // Primary key LAB_LabVersion
        var versionPk = versionType.FindPrimaryKey();
        versionPk.Should().NotBeNull();
        versionPk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_LabVersion.LAB_IdVersion));

        // Clave alternativa (Alternate Key) en LAB_LabVersion para asegurar SEG_IdTenant
        var versionAltKey = versionType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey());
        versionAltKey.Should().NotBeNull();
        versionAltKey!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_LabVersion.SEG_IdTenant), nameof(LAB_LabVersion.LAB_IdVersion));

        // Primary key LAB_Stage
        var stagePk = stageType.FindPrimaryKey();
        stagePk.Should().NotBeNull();
        stagePk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Stage.STG_IdStage));

        // Clave alternativa en LAB_Stage sobre (SEG_IdTenant, LAB_IdVersion, STG_IdStage)
        var stageAltKey = stageType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey());
        stageAltKey.Should().NotBeNull();
        stageAltKey!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Stage.SEG_IdTenant), nameof(LAB_Stage.LAB_IdVersion), nameof(LAB_Stage.STG_IdStage));

        // Foreign Key compuesta LAB_Stage -> LAB_LabVersion
        var stageFk = stageType.GetForeignKeys().FirstOrDefault();
        stageFk.Should().NotBeNull();
        stageFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Stage.SEG_IdTenant), nameof(LAB_Stage.LAB_IdVersion));
        stageFk.PrincipalKey.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_LabVersion.SEG_IdTenant), nameof(LAB_LabVersion.LAB_IdVersion));
        stageFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // Restricciones únicas en LAB_Stage
        var stageUniqueCode = stageType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_Stage.SEG_IdTenant), nameof(LAB_Stage.LAB_IdVersion), nameof(LAB_Stage.STG_Codigo) }));
        stageUniqueCode.Should().NotBeNull();

        var stageUniqueOrder = stageType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_Stage.SEG_IdTenant), nameof(LAB_Stage.LAB_IdVersion), nameof(LAB_Stage.STG_Orden) }));
        stageUniqueOrder.Should().NotBeNull();

        // Check constraints de LAB_LabVersion
        var versionCheckConstraints = versionType.GetCheckConstraints().ToList();
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_LAB_NumeroVersion");
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_LAB_DuracionMinutos");
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_LAB_ScoreMinimo");
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_LAB_Estatus");
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_Vigencia");
        versionCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_LabVersion_LAB_HashConfiguracion");

        // RowVersion concurrency token de LAB_LabVersion
        var versionRowVersionProp = versionType.FindProperty(nameof(LAB_LabVersion.RowVersion));
        versionRowVersionProp.Should().NotBeNull();
        versionRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Check constraints de LAB_Stage
        var stageCheckConstraints = stageType.GetCheckConstraints().ToList();
        stageCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Stage_STG_Orden" && c.Sql == "[STG_Orden] > 0");
        stageCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Stage_STG_TiempoSugeridoMinutos");
        stageCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Stage_STG_TipoInteraccion");
        stageCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Stage_STG_Estatus");

        // RowVersion concurrency token de LAB_Stage
        var stageRowVersionProp = stageType.FindProperty(nameof(LAB_Stage.RowVersion));
        stageRowVersionProp.Should().NotBeNull();
        stageRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Primary Key LAB_Objective
        var objPk = objectiveType.FindPrimaryKey();
        objPk.Should().NotBeNull();
        objPk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.OBJ_IdObjective));

        // FK obligatoria LAB_Objective -> LAB_LabVersion (SEG_IdTenant, LAB_IdVersion)
        var objVersionFk = objectiveType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == versionType);
        objVersionFk.Should().NotBeNull();
        objVersionFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion));
        objVersionFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // FK opcional LAB_Objective -> LAB_Stage (SEG_IdTenant, LAB_IdVersion, STG_IdStage)
        var objStageFk = objectiveType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == stageType);
        objStageFk.Should().NotBeNull();
        objStageFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion), nameof(LAB_Objective.STG_IdStage));
        objStageFk.IsRequired.Should().BeFalse();
        objStageFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // Check constraints de LAB_Objective
        var objCheckConstraints = objectiveType.GetCheckConstraints().ToList();
        objCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Objective_OBJ_Peso" && c.Sql == "[OBJ_Peso] > 0");
        objCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Objective_OBJ_Orden" && c.Sql == "[OBJ_Orden] > 0");
        objCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Objective_OBJ_TipoEvidencia");
        objCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Objective_OBJ_Estatus");

        // RowVersion concurrency token de LAB_Objective
        var objRowVersionProp = objectiveType.FindProperty(nameof(LAB_Objective.RowVersion));
        objRowVersionProp.Should().NotBeNull();
        objRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Índices únicos y filtrados de LAB_Objective
        var objUniqueCode = objectiveType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion), nameof(LAB_Objective.OBJ_Codigo) }));
        objUniqueCode.Should().NotBeNull();

        var objGeneralOrder = objectiveType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.GetFilter() == "[STG_IdStage] IS NULL");
        objGeneralOrder.Should().NotBeNull();
        objGeneralOrder!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion), nameof(LAB_Objective.OBJ_Orden));

        var objStageOrder = objectiveType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.GetFilter() == "[STG_IdStage] IS NOT NULL");
        objStageOrder.Should().NotBeNull();
        objStageOrder!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion), nameof(LAB_Objective.STG_IdStage), nameof(LAB_Objective.OBJ_Orden));

        // Primary key LAB_ExpectedMoment
        var momPk = momentType.FindPrimaryKey();
        momPk.Should().NotBeNull();
        momPk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.MOM_IdExpectedMoment));

        // FK hacia LAB_LabVersion (SEG_IdTenant, LAB_IdVersion)
        var momVersionFk = momentType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == versionType);
        momVersionFk.Should().NotBeNull();
        momVersionFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion));
        momVersionFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // FK hacia LAB_Stage (SEG_IdTenant, LAB_IdVersion, STG_IdStage)
        var momStageFk = momentType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == stageType);
        momStageFk.Should().NotBeNull();
        momStageFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.STG_IdStage));
        momStageFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // FK hacia LAB_Objective (SEG_IdTenant, LAB_IdVersion, OBJ_IdObjective)
        var momObjFk = momentType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == objectiveType);
        momObjFk.Should().NotBeNull();
        momObjFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.OBJ_IdObjective));
        momObjFk.IsRequired.Should().BeFalse();
        momObjFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // Check constraints de LAB_ExpectedMoment
        var momCheckConstraints = momentType.GetCheckConstraints().ToList();
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_OrdenSugerido" && c.Sql == "[MOM_OrdenSugerido] > 0");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_Tipo");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_Estatus");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_Codigo");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_Nombre");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_Trigger");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_MOM_IntencionEsperada");
        momCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_ExpectedMoment_CreadoPor");

        // RowVersion concurrency token de LAB_ExpectedMoment
        var momRowVersionProp = momentType.FindProperty(nameof(LAB_ExpectedMoment.RowVersion));
        momRowVersionProp.Should().NotBeNull();
        momRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Índices únicos de LAB_ExpectedMoment
        var momUniqueCode = momentType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.MOM_Codigo) }));
        momUniqueCode.Should().NotBeNull();

        var momUniqueOrder = momentType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.STG_IdStage), nameof(LAB_ExpectedMoment.MOM_OrdenSugerido) }));
        momUniqueOrder.Should().NotBeNull();

        // Primary key LAB_Rubric
        var rubPk = rubricType.FindPrimaryKey();
        rubPk.Should().NotBeNull();
        rubPk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Rubric.RUB_IdRubric));

        // FK 1:0..1 hacia LAB_LabVersion (SEG_IdTenant, LAB_IdVersion)
        var rubVersionFk = rubricType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == versionType);
        rubVersionFk.Should().NotBeNull();
        rubVersionFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Rubric.SEG_IdTenant), nameof(LAB_Rubric.LAB_IdVersion));
        rubVersionFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // Check constraints de LAB_Rubric
        var rubCheckConstraints = rubricType.GetCheckConstraints().ToList();
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_EscalaMinima" && c.Sql == "[RUB_EscalaMinima] = 1.00");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_EscalaMaxima" && c.Sql == "[RUB_EscalaMaxima] = 10.00");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_Escala");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_ScoreMinimoAprobacion");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_TipoEvaluacion");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_MetodoCalculo");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_Estatus");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_Vigencia");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_Codigo");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_Nombre");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_Descripcion");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_RUB_InstruccionesEvaluador");
        rubCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_Rubric_CreadoPor");

        // RowVersion concurrency token de LAB_Rubric
        var rubRowVersionProp = rubricType.FindProperty(nameof(LAB_Rubric.RowVersion));
        rubRowVersionProp.Should().NotBeNull();
        rubRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Índices únicos de LAB_Rubric
        var rubUniqueVersion = rubricType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_Rubric.SEG_IdTenant), nameof(LAB_Rubric.LAB_IdVersion) }));
        rubUniqueVersion.Should().NotBeNull();

        var rubUniqueCode = rubricType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_Rubric.SEG_IdTenant), nameof(LAB_Rubric.RUB_Codigo) }));
        rubUniqueCode.Should().NotBeNull();

        // Clave alternativa en LAB_Rubric sobre (SEG_IdTenant, LAB_IdVersion, RUB_IdRubric)
        var rubAltKey = rubricType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey());
        rubAltKey.Should().NotBeNull();
        rubAltKey!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Rubric.SEG_IdTenant), nameof(LAB_Rubric.LAB_IdVersion), nameof(LAB_Rubric.RUB_IdRubric));

        // Clave alternativa en LAB_ExpectedMoment sobre (SEG_IdTenant, LAB_IdVersion, MOM_IdExpectedMoment)
        var momAltKey = momentType.GetKeys().FirstOrDefault(k => !k.IsPrimaryKey());
        momAltKey.Should().NotBeNull();
        momAltKey!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.MOM_IdExpectedMoment));

        // Primary key LAB_RubricCriterion
        var critPk = criterionType.FindPrimaryKey();
        critPk.Should().NotBeNull();
        critPk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_RubricCriterion.CRT_IdCriterion));

        // FK obligatoria LAB_RubricCriterion -> LAB_Rubric (SEG_IdTenant, LAB_IdVersion, RUB_IdRubric)
        var critRubricFk = criterionType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == rubricType);
        critRubricFk.Should().NotBeNull();
        critRubricFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.RUB_IdRubric));
        critRubricFk.PrincipalKey.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Rubric.SEG_IdTenant), nameof(LAB_Rubric.LAB_IdVersion), nameof(LAB_Rubric.RUB_IdRubric));
        critRubricFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        critRubricFk.IsRequired.Should().BeTrue();

        // FK opcional LAB_RubricCriterion -> LAB_Objective (SEG_IdTenant, LAB_IdVersion, OBJ_IdObjective)
        var critObjectiveFk = criterionType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == objectiveType);
        critObjectiveFk.Should().NotBeNull();
        critObjectiveFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.OBJ_IdObjective));
        critObjectiveFk.PrincipalKey.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_Objective.SEG_IdTenant), nameof(LAB_Objective.LAB_IdVersion), nameof(LAB_Objective.OBJ_IdObjective));
        critObjectiveFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        critObjectiveFk.IsRequired.Should().BeFalse();

        // FK opcional LAB_RubricCriterion -> LAB_ExpectedMoment (SEG_IdTenant, LAB_IdVersion, MOM_IdExpectedMoment)
        var critMomentFk = criterionType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == momentType);
        critMomentFk.Should().NotBeNull();
        critMomentFk!.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.MOM_IdExpectedMoment));
        critMomentFk.PrincipalKey.Properties.Select(p => p.Name).Should().Equal(nameof(LAB_ExpectedMoment.SEG_IdTenant), nameof(LAB_ExpectedMoment.LAB_IdVersion), nameof(LAB_ExpectedMoment.MOM_IdExpectedMoment));
        critMomentFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        critMomentFk.IsRequired.Should().BeFalse();

        // Check constraints de LAB_RubricCriterion
        var critCheckConstraints = criterionType.GetCheckConstraints().ToList();
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Peso" && c.Sql == "[CRT_Peso] > 0");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_ScoreMinimoEsperado" && c.Sql == "[CRT_ScoreMinimoEsperado] >= 1.00 AND [CRT_ScoreMinimoEsperado] <= 10.00");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Orden" && c.Sql == "[CRT_Orden] > 0");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_TipoEvidencia");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Estatus");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Codigo");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Nombre");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_Descripcion");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CRT_IndicadoresPositivos");
        critCheckConstraints.Should().Contain(c => c.Name == "CK_LAB_RubricCriterion_CreadoPor");

        // RowVersion concurrency token de LAB_RubricCriterion
        var critRowVersionProp = criterionType.FindProperty(nameof(LAB_RubricCriterion.RowVersion));
        critRowVersionProp.Should().NotBeNull();
        critRowVersionProp!.IsConcurrencyToken.Should().BeTrue();

        // Índices únicos de LAB_RubricCriterion
        var critUniqueCode = criterionType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.RUB_IdRubric), nameof(LAB_RubricCriterion.CRT_Codigo) }));
        critUniqueCode.Should().NotBeNull();

        var critUniqueOrder = criterionType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.RUB_IdRubric), nameof(LAB_RubricCriterion.CRT_Orden) }));
        critUniqueOrder.Should().NotBeNull();

        // Índices no únicos de LAB_RubricCriterion
        var critIndexStatus = criterionType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.RUB_IdRubric), nameof(LAB_RubricCriterion.CRT_Estatus) }));
        critIndexStatus.Should().NotBeNull();

        var critIndexCritical = criterionType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.RUB_IdRubric), nameof(LAB_RubricCriterion.CRT_EsCritico) }));
        critIndexCritical.Should().NotBeNull();

        var critIndexObjective = criterionType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.OBJ_IdObjective) }));
        critIndexObjective.Should().NotBeNull();

        var critIndexMoment = criterionType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.MOM_IdExpectedMoment) }));
        critIndexMoment.Should().NotBeNull();

        var critIndexRubric = criterionType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LAB_RubricCriterion.SEG_IdTenant), nameof(LAB_RubricCriterion.LAB_IdVersion), nameof(LAB_RubricCriterion.RUB_IdRubric) }));
        critIndexRubric.Should().NotBeNull();
    }
}
