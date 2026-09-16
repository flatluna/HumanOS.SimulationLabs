using System.Reflection;
using FluentAssertions;
using HumanOS.SimulationLabs.Api.Features.Objectives;
using HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;
using HumanOS.SimulationLabs.Api.Features.Objectives.Functions;
using HumanOS.SimulationLabs.Api.Features.Objectives.Validators;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Azure.Functions.Worker;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class ObjectiveServiceTests
{
    [Fact]
    public void ValidateCreate_WhenValid_ReturnsNoErrors()
    {
        var req = new CreateObjectiveRequest
        {
            Codigo = "DEFINE_MEASURABLE_GOAL",
            Descripcion = "El participante define un objetivo empresarial medible.",
            TipoEvidencia = ObjectiveTipoEvidencia.Artifact,
            EsCritico = true,
            Peso = 20.0000m,
            CondicionExito = "El objetivo identifica la población y mejora esperada."
        };

        var errors = ObjectiveValidators.ValidateCreate(req);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCreate_WhenInvalid_ReturnsErrors()
    {
        var req = new CreateObjectiveRequest
        {
            Codigo = "",
            Descripcion = "",
            TipoEvidencia = "INVALID_TYPE",
            EsCritico = null,
            Peso = 0,
            CondicionExito = ""
        };

        var errors = ObjectiveValidators.ValidateCreate(req);
        errors.Should().Contain(e => e.Contains("codigo"));
        errors.Should().Contain(e => e.Contains("descripcion"));
        errors.Should().Contain(e => e.Contains("tipoEvidencia"));
        errors.Should().Contain(e => e.Contains("esCritico"));
        errors.Should().Contain(e => e.Contains("peso"));
        errors.Should().Contain(e => e.Contains("condicionExito"));
    }

    [Fact]
    public void ValidateUpdate_WhenNoFieldsProvided_ReturnsError()
    {
        var req = new UpdateObjectiveRequest();
        var errors = ObjectiveValidators.ValidateUpdate(req);
        errors.Should().ContainSingle().Which.Should().Contain("al menos un campo");
    }

    [Fact]
    public void ValidateUpdate_WhenValidFieldsProvided_ReturnsNoErrors()
    {
        var req = new UpdateObjectiveRequest
        {
            Descripcion = "Nueva descripción",
            Peso = 15.5m,
            TipoEvidencia = ObjectiveTipoEvidencia.UserAction,
            EsCritico = false,
            CondicionExito = "Condición actualizada"
        };
        var errors = ObjectiveValidators.ValidateUpdate(req);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAction_WhenMotivoEmpty_ReturnsError()
    {
        var req = new ObjectiveActionRequest { Motivo = "" };
        var errors = ObjectiveValidators.ValidateAction(req);
        errors.Should().ContainSingle().Which.Should().Contain("motivo");
    }

    [Fact]
    public void ObjectiveFunctions_ExposeExpectedHttpTriggers()
    {
        var functions = new[]
        {
            typeof(ObjectiveCreateFunction),
            typeof(ObjectiveGetByIdFunction),
            typeof(ObjectiveListByVersionFunction),
            typeof(ObjectiveListByStageFunction),
            typeof(ObjectiveUpdateFunction),
            typeof(ObjectiveReorderFunction),
            typeof(ObjectiveActivateFunction),
            typeof(ObjectiveInactivateFunction)
        };

        var functionNames = new List<string>();
        foreach (var fn in functions)
        {
            var method = fn.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<FunctionAttribute>() != null);
            method.Should().NotBeNull($"Function method with [Function] attribute expected in {fn.Name}");
            functionNames.Add(method!.GetCustomAttribute<FunctionAttribute>()!.Name);
        }

        functionNames.Should().BeEquivalentTo(new[]
        {
            "Objective_Create",
            "Objective_GetById",
            "Objective_ListByVersion",
            "Objective_ListByStage",
            "Objective_Update",
            "Objective_Reorder",
            "Objective_Activate",
            "Objective_Inactivate"
        });
    }

    [Fact]
    public void ObjectivePolicies_MatchExpectedValues()
    {
        ObjectivePolicies.Read.Should().Be("Objectives.Read");
        ObjectivePolicies.Create.Should().Be("Objectives.Create");
        ObjectivePolicies.Update.Should().Be("Objectives.Update");
        ObjectivePolicies.Activate.Should().Be("Objectives.Activate");
        ObjectivePolicies.Inactivate.Should().Be("Objectives.Inactivate");
    }
}
