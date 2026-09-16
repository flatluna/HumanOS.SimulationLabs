using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Contracts;
using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Services;
using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors.Functions;

public abstract class SimulatedActorFunctionBase
{
    protected readonly ISimulatedActorService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected SimulatedActorFunctionBase(ISimulatedActorService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, SimulatedActorResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
    {
        var response = await ApiResponses.JsonAsync(r, code, value, User.CorrelationId, ct);
        response.Headers.Add("ETag", $"\"{value.RowVersion}\"");
        if (location is not null) response.Headers.Add("Location", location);
        return response;
    }
    protected async Task<HttpResponseData> Handle(HttpRequestData r, Exception ex, CancellationToken ct)
    {
        return ex switch
        {
            SimulatedActorScenarioNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Escenario no encontrado.", "SCENARIO_NOT_FOUND", ct),
            SimulatedActorNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Actor simulado no encontrado.", "SIMULATED_ACTOR_NOT_FOUND", ct),
            SimulatedActorVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión de Lab no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            SimulatedActorScenarioNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El escenario no está en DRAFT.", "SCENARIO_NOT_EDITABLE", ct),
            SimulatedActorCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro del escenario.", "SIMULATED_ACTOR_CODE_DUPLICATE", ct),
            SimulatedActorOrderDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El orden contiene duplicados o es inválido.", "SIMULATED_ACTOR_ORDER_DUPLICATE", ct),
            SimulatedActorPrincipalAlreadyExistsException => await Problem(r, HttpStatusCode.Conflict, "Ya existe un actor principal para este escenario.", "PRINCIPAL_ACTOR_ALREADY_EXISTS", ct),
            SimulatedActorPreconditionException p => await Problem(r,
                p.Code == "INVALID_STATUS_TRANSITION" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.PreconditionFailed,
                p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.",
                p.Code, ct),
            SimulatedActorConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class SimulatedActorCreateFunction : SimulatedActorFunctionBase
{
    private readonly IIdempotencyService _idempotency;
    public SimulatedActorCreateFunction(ISimulatedActorService s, ICurrentUserContext u, IIdempotencyService idempotency, ILogger<SimulatedActorCreateFunction> l) : base(s, u, l) => _idempotency = idempotency;

    [Function("SimulatedActor_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/actors")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Create)) return await Auth(r, SimulatedActorPolicies.Create, ct);
        if (!Guid.TryParse(idScenario, out var scenarioId)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<CreateSimulatedActorRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = SimulatedActorValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var idempotencyKey = Header(r, "Idempotency-Key");
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotency.TryGetResult(User.TenantId, idempotencyKey, out var cached) && cached is SimulatedActorResponse cachedResponse)
            {
                return await Result(r, cachedResponse, HttpStatusCode.Created, ct, $"/api/simulated-actors/{cachedResponse.IdActor}");
            }

            var value = await Service.CreateAsync(User.TenantId, scenarioId, User.DisplayName, body, ct);
            if (!string.IsNullOrWhiteSpace(idempotencyKey)) _idempotency.StoreResult(User.TenantId, idempotencyKey, value);

            Logger.LogInformation("SimulatedActor_Create ok. TenantId={TenantId} UserId={UserId} ScenarioId={ScenarioId} ActorId={ActorId} CorrelationId={CorrelationId} Action={Action} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, scenarioId, value.IdActor, User.CorrelationId, "CREATE", value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/simulated-actors/{value.IdActor}");
        }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_Create failed. TenantId={TenantId} ScenarioId={ScenarioId} CorrelationId={CorrelationId}", User.TenantId, scenarioId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorGetByIdFunction : SimulatedActorFunctionBase
{
    public SimulatedActorGetByIdFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorGetByIdFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "simulated-actors/{idActor}")] HttpRequestData r, string idActor, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Read)) return await Auth(r, SimulatedActorPolicies.Read, ct);
        if (!Guid.TryParse(idActor, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idActor debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Actor simulado no encontrado.", "SIMULATED_ACTOR_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorListByScenarioFunction : SimulatedActorFunctionBase
{
    public SimulatedActorListByScenarioFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorListByScenarioFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_ListByScenario")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scenarios/{idScenario}/actors")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Read)) return await Auth(r, SimulatedActorPolicies.Read, ct);
        if (!Guid.TryParse(idScenario, out var scenarioId)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        try
        {
            var result = await Service.ListAsync(User.TenantId, scenarioId, q["tipo"], q["estiloComunicacion"], q["nivelConocimiento"], bool.TryParse(q["principal"], out var principal) ? principal : null, q["estatus"], page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_ListByScenario failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorUpdateFunction : SimulatedActorFunctionBase
{
    public SimulatedActorUpdateFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorUpdateFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_Update")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "simulated-actors/{idActor}")] HttpRequestData r, string idActor, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Update)) return await Auth(r, SimulatedActorPolicies.Update, ct);
        if (!Guid.TryParse(idActor, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idActor debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try
        {
            var body = await Body<UpdateSimulatedActorRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = SimulatedActorValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_Update failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorReorderFunction : SimulatedActorFunctionBase
{
    public SimulatedActorReorderFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorReorderFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_Reorder")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/actors/reorder")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Update)) return await Auth(r, SimulatedActorPolicies.Update, ct);
        if (!Guid.TryParse(idScenario, out var scenarioId)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<ReorderSimulatedActorsRequest>(r, ct);
            if (body?.Actors is null || body.Actors.Count == 0 || body.Actors.Any(x => x.IdActor == Guid.Empty || string.IsNullOrWhiteSpace(x.RowVersion))) return await Problem(r, HttpStatusCode.BadRequest, "La lista de actores es inválida.", "REQUEST_INVALID", ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, await Service.ReorderAsync(User.TenantId, scenarioId, User.DisplayName, body, ct), User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_Reorder failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorActivateFunction : SimulatedActorFunctionBase
{
    public SimulatedActorActivateFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorActivateFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_Activate")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "simulated-actors/{idActor}/activate")] HttpRequestData r, string idActor, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Activate)) return await Auth(r, SimulatedActorPolicies.Activate, ct);
        if (!Guid.TryParse(idActor, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idActor debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.ActivateAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_Activate failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class SimulatedActorInactivateFunction : SimulatedActorFunctionBase
{
    public SimulatedActorInactivateFunction(ISimulatedActorService s, ICurrentUserContext u, ILogger<SimulatedActorInactivateFunction> l) : base(s, u, l) { }
    [Function("SimulatedActor_Inactivate")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "simulated-actors/{idActor}/inactivate")] HttpRequestData r, string idActor, CancellationToken ct)
    {
        if (!Authorized(SimulatedActorPolicies.Inactivate)) return await Auth(r, SimulatedActorPolicies.Inactivate, ct);
        if (!Guid.TryParse(idActor, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idActor debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.InactivateAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "SimulatedActor_Inactivate failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}
