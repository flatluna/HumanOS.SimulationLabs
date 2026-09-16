using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Scenarios.Contracts;
using HumanOS.SimulationLabs.Api.Features.Scenarios.Services;
using HumanOS.SimulationLabs.Api.Features.Scenarios.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Scenarios.Functions;

public abstract class ScenarioFunctionBase
{
    protected readonly IScenarioService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected ScenarioFunctionBase(IScenarioService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, ScenarioResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            ScenarioVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            ScenarioNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Escenario no encontrado.", "SCENARIO_NOT_FOUND", ct),
            ScenarioVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión de Lab no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            ScenarioNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El escenario no está en DRAFT.", "SCENARIO_NOT_EDITABLE", ct),
            ScenarioCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro de la versión.", "SCENARIO_CODE_DUPLICATE", ct),
            ScenarioPreconditionException p => await Problem(r,
                p.Code == "INVALID_STATUS_TRANSITION" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.PreconditionFailed,
                p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.",
                p.Code, ct),
            ScenarioConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            // TEMP diagnostic: surface the real exception message/type to find the Scenario_GetById 500 root cause.
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct, errors: [$"{ex.GetType().Name}: {ex.Message}"]),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class ScenarioCreateFunction : ScenarioFunctionBase
{
    private readonly IIdempotencyService _idempotency;
    public ScenarioCreateFunction(IScenarioService s, ICurrentUserContext u, IIdempotencyService idempotency, ILogger<ScenarioCreateFunction> l) : base(s, u, l) => _idempotency = idempotency;

    [Function("Scenario_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/scenarios")] HttpRequestData r, string idVersion, CancellationToken ct)
    {
        if (!Authorized(ScenarioPolicies.Create)) return await Auth(r, ScenarioPolicies.Create, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<CreateScenarioRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ScenarioValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var idempotencyKey = Header(r, "Idempotency-Key");
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotency.TryGetResult(User.TenantId, idempotencyKey, out var cached) && cached is ScenarioResponse cachedResponse)
            {
                return await Result(r, cachedResponse, HttpStatusCode.Created, ct, $"/api/scenarios/{cachedResponse.IdScenario}");
            }

            var value = await Service.CreateAsync(User.TenantId, versionId, User.DisplayName, body, ct);
            if (!string.IsNullOrWhiteSpace(idempotencyKey)) _idempotency.StoreResult(User.TenantId, idempotencyKey, value);

            Logger.LogInformation("Scenario_Create ok. TenantId={TenantId} UserId={UserId} VersionId={VersionId} ScenarioId={ScenarioId} CorrelationId={CorrelationId} Action={Action} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, versionId, value.IdScenario, User.CorrelationId, "CREATE", value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/scenarios/{value.IdScenario}");
        }
        catch (Exception ex) { Logger.LogError(ex, "Scenario_Create failed. TenantId={TenantId} VersionId={VersionId} CorrelationId={CorrelationId}", User.TenantId, versionId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ScenarioGetByIdFunction : ScenarioFunctionBase
{
    public ScenarioGetByIdFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioGetByIdFunction> l) : base(s, u, l) { }
    [Function("Scenario_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scenarios/{idScenario}")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(ScenarioPolicies.Read)) return await Auth(r, ScenarioPolicies.Read, ct);
        if (!Guid.TryParse(idScenario, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Escenario no encontrado.", "SCENARIO_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "Scenario_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ScenarioListByVersionFunction : ScenarioFunctionBase
{
    public ScenarioListByVersionFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioListByVersionFunction> l) : base(s, u, l) { }
    [Function("Scenario_ListByVersion")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/scenarios")] HttpRequestData r, string idVersion, CancellationToken ct)
    {
        if (!Authorized(ScenarioPolicies.Read)) return await Auth(r, ScenarioPolicies.Read, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        try
        {
            var result = await Service.ListAsync(User.TenantId, versionId, q["tipo"], q["dificultad"], q["estatus"], q["search"], page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "Scenario_ListByVersion failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ScenarioUpdateFunction : ScenarioFunctionBase
{
    public ScenarioUpdateFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioUpdateFunction> l) : base(s, u, l) { }
    [Function("Scenario_Update")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "scenarios/{idScenario}")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(ScenarioPolicies.Update)) return await Auth(r, ScenarioPolicies.Update, ct);
        if (!Guid.TryParse(idScenario, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try
        {
            var body = await Body<UpdateScenarioRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ScenarioValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "Scenario_Update failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public abstract class ScenarioTransitionFunctionBase : ScenarioFunctionBase
{
    protected ScenarioTransitionFunctionBase(IScenarioService s, ICurrentUserContext u, ILogger l) : base(s, u, l) { }
    protected async Task<HttpResponseData> RunTransition(HttpRequestData r, string idScenario, string policy, Func<Guid, Guid, string, string, CancellationToken, Task<ScenarioResponse>> transition, CancellationToken ct)
    {
        if (!Authorized(policy)) return await Auth(r, policy, ct);
        if (!Guid.TryParse(idScenario, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await transition(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "Scenario transition failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ScenarioApproveFunction : ScenarioTransitionFunctionBase
{
    public ScenarioApproveFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioApproveFunction> l) : base(s, u, l) { }
    [Function("Scenario_Approve")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/approve")] HttpRequestData r, string idScenario, CancellationToken ct) =>
        RunTransition(r, idScenario, ScenarioPolicies.Approve, Service.ApproveAsync, ct);
}

public sealed class ScenarioPublishFunction : ScenarioTransitionFunctionBase
{
    public ScenarioPublishFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioPublishFunction> l) : base(s, u, l) { }
    [Function("Scenario_Publish")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/publish")] HttpRequestData r, string idScenario, CancellationToken ct) =>
        RunTransition(r, idScenario, ScenarioPolicies.Publish, Service.PublishAsync, ct);
}

public sealed class ScenarioRetireFunction : ScenarioTransitionFunctionBase
{
    public ScenarioRetireFunction(IScenarioService s, ICurrentUserContext u, ILogger<ScenarioRetireFunction> l) : base(s, u, l) { }
    [Function("Scenario_Retire")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/retire")] HttpRequestData r, string idScenario, CancellationToken ct) =>
        RunTransition(r, idScenario, ScenarioPolicies.Retire, Service.RetireAsync, ct);
}
