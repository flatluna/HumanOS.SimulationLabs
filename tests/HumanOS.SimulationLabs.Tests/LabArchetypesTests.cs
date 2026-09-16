using FluentAssertions;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Entities;
using Xunit;

namespace HumanOS.SimulationLabs.Tests;

public class LabArchetypesTests
{
    [Fact]
    public void All_ContainsExactlyTheTenAllowedCodes()
    {
        LabArchetypes.All.Select(a => a.Code).Should().BeEquivalentTo(LabArquetipos.Allowed);
    }

    [Theory]
    [InlineData(LabArquetipos.JobInterview)]
    [InlineData(LabArquetipos.ClientNegotiation)]
    [InlineData(LabArquetipos.PerformanceReview)]
    [InlineData(LabArquetipos.ConflictResolution)]
    [InlineData(LabArquetipos.SalesDiscovery)]
    [InlineData(LabArquetipos.DifficultFeedback)]
    [InlineData(LabArquetipos.Onboarding)]
    [InlineData(LabArquetipos.AngryCustomer)]
    [InlineData(LabArquetipos.ExecutivePitch)]
    [InlineData(LabArquetipos.CareerMentoring)]
    [InlineData(LabArquetipos.CustomerSupport)]
    [InlineData(LabArquetipos.RequirementsGathering)]
    [InlineData(LabArquetipos.BusinessReportUpdate)]
    public void Resolve_KnownCode_ReturnsMatchingInfo(string code)
    {
        var info = LabArchetypes.Resolve(code);
        info.Code.Should().Be(code);
        info.InstructionBlock.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOT_A_REAL_ARCHETYPE")]
    public void Resolve_NullOrUnknownCode_FallsBackToClientNegotiation(string? code)
    {
        LabArchetypes.Resolve(code).Code.Should().Be(LabArquetipos.ClientNegotiation);
    }

    [Fact]
    public void BuildInstructions_InjectsArchetypeBlock_ForSelectedArchetype()
    {
        var context = new LabSimulationPromptBuilder.SimulationContext
        {
            LabNombre = "Test Lab",
            ObjetivoGeneral = "Test objective",
            Arquetipo = LabArquetipos.JobInterview,
        };

        var instructions = LabSimulationPromptBuilder.BuildInstructions(context, adminDisplayName: null);

        instructions.Should().Contain("ARCHETYPE — JOB INTERVIEW");
    }

    [Fact]
    public void BuildInstructions_NullArquetipo_FallsBackToClientNegotiationBlock()
    {
        var context = new LabSimulationPromptBuilder.SimulationContext
        {
            LabNombre = "Test Lab",
            ObjetivoGeneral = "Test objective",
            Arquetipo = null,
        };

        var instructions = LabSimulationPromptBuilder.BuildInstructions(context, adminDisplayName: null);

        instructions.Should().Contain("ARCHETYPE — CLIENT NEGOTIATION");
    }
}
