using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Services;
using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Services;
using HumanOS.SimulationLabs.Api.Features.Attempts.Services;
using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Services;
using HumanOS.SimulationLabs.Api.Features.Enrollments;
using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Services;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Services;
using HumanOS.SimulationLabs.Api.Features.Objectives.Services;
using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Services;
using HumanOS.SimulationLabs.Api.Features.Rubrics.Services;
using HumanOS.SimulationLabs.Api.Features.Scenarios.Services;
using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Services;
using HumanOS.SimulationLabs.Api.Features.Stages.Services;
using HumanOS.SimulationLabs.Api.Features.UserActions.Services;
using HumanOS.SimulationLabs.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UseMiddleware<CorrelationIdMiddleware>();
        worker.UseMiddleware<EntraIdAuthenticationMiddleware>();
        worker.UseMiddleware<StudioLaunchJwtAuthenticationMiddleware>();
        worker.UseMiddleware<HeaderIdentityAuthenticationMiddleware>();
        worker.UseMiddleware<CurrentUserInitializationMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services.AddScoped<SimulationLabsDbContext>(_ => DbContextHelper.CreateDbContext());
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<ILabVersionService, LabVersionService>();
        services.AddScoped<IStageService, StageService>();
        services.AddScoped<IObjectiveService, ObjectiveService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<ISimulatedActorService, SimulatedActorService>();
        services.AddScoped<IRubricService, RubricService>();
        services.AddScoped<IRubricCriterionService, RubricCriterionService>();
        services.AddScoped<IExpectedMomentService, ExpectedMomentService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<AttemptEvaluationAgent>();
        services.AddScoped<AttemptEvaluationService>();
        services.AddHttpClient();
        services.AddScoped<LabRealtimeVoiceSessionService>();
        services.AddScoped<IConversationTurnService, ConversationTurnService>();
        services.AddScoped<IArtifactSubmissionService, ArtifactSubmissionService>();
        services.AddScoped<IUserActionService, UserActionService>();
        services.AddScoped<ILabDraftSaveService, LabDraftSaveService>();
        services.AddScoped<LabBuilderAgent>();
        services.AddScoped<EnrollmentService>();
        services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();
    })
    .Build();

host.Run();
