using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using HumanOS.SimulationLabs.Api.Features.Attempts.Services;
using HumanOS.SimulationLabs.Api.Features.Attempts.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Functions;

public abstract class AttemptFunctionBase
{
    protected readonly IAttemptService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected AttemptFunctionBase(IAttemptService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, AttemptResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            AttemptScenarioNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Escenario no encontrado.", "SCENARIO_NOT_FOUND", ct),
            AttemptNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Intento no encontrado.", "ATTEMPT_NOT_FOUND", ct),
            ScenarioNotPublishedException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El escenario no está publicado o vigente.", "SCENARIO_NOT_PUBLISHED", ct),
            AttemptLimitReachedException => await Problem(r, HttpStatusCode.UnprocessableEntity, "Se alcanzó el límite de intentos permitidos.", "ATTEMPT_LIMIT_REACHED", ct),
            AttemptNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El intento no es editable.", "ATTEMPT_NOT_EDITABLE", ct),
            InvalidAttemptTransitionException => await Problem(r, HttpStatusCode.UnprocessableEntity, "Transición de intento inválida.", "INVALID_ATTEMPT_TRANSITION", ct),
            AttemptPreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : "El ETag no coincide.", p.Code, ct),
            AttemptConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class AttemptStartFunction : AttemptFunctionBase
{
    private readonly IIdempotencyService _idempotency;
    public AttemptStartFunction(IAttemptService s, ICurrentUserContext u, IIdempotencyService idempotency, ILogger<AttemptStartFunction> l) : base(s, u, l) => _idempotency = idempotency;

    [Function("Attempt_Start")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenarios/{idScenario}/attempts")] HttpRequestData r, string idScenario, CancellationToken ct)
    {
        if (!Authorized(AttemptPolicies.Start)) return await Auth(r, AttemptPolicies.Start, ct);
        if (!Guid.TryParse(idScenario, out var scenarioId)) return await Problem(r, HttpStatusCode.BadRequest, "idScenario debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<StartAttemptRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = AttemptValidators.ValidateStart(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var idempotencyKey = Header(r, "Idempotency-Key");
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotency.TryGetResult(User.TenantId, idempotencyKey, out var cached) && cached is AttemptResponse cachedResponse)
            {
                return await Result(r, cachedResponse, HttpStatusCode.Created, ct, $"/api/attempts/{cachedResponse.IdAttempt}");
            }

            var value = await Service.StartAsync(User.TenantId, scenarioId, User.UserId, User.DisplayName, body, ct);
            if (!string.IsNullOrWhiteSpace(idempotencyKey)) _idempotency.StoreResult(User.TenantId, idempotencyKey, value);

            Logger.LogInformation("Attempt_Start ok. TenantId={TenantId} UserId={UserId} ScenarioId={ScenarioId} AttemptId={AttemptId} CorrelationId={CorrelationId} Action={Action} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, scenarioId, value.IdAttempt, User.CorrelationId, "START", value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/attempts/{value.IdAttempt}");
        }
        catch (Exception ex) { Logger.LogError(ex, "Attempt_Start failed. TenantId={TenantId} ScenarioId={ScenarioId} CorrelationId={CorrelationId}", User.TenantId, scenarioId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class AttemptGetByIdFunction : AttemptFunctionBase
{
    public AttemptGetByIdFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptGetByIdFunction> l) : base(s, u, l) { }
    [Function("Attempt_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "attempts/{idAttempt}")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(AttemptPolicies.ReadOwn) && !Authorized(AttemptPolicies.ReadAll)) return await Auth(r, AttemptPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idAttempt, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, User.UserId, Authorized(AttemptPolicies.ReadAll), ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Intento no encontrado.", "ATTEMPT_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "Attempt_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class AttemptListMineFunction : AttemptFunctionBase
{
    public AttemptListMineFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptListMineFunction> l) : base(s, u, l) { }
    [Function("Attempt_ListMine")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/attempts")] HttpRequestData r, CancellationToken ct)
    {
        if (!Authorized(AttemptPolicies.ReadOwn)) return await Auth(r, AttemptPolicies.ReadOwn, ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        Guid? scenarioId = Guid.TryParse(q["scenarioId"], out var sid) ? sid : null;
        try
        {
            var result = await Service.ListMineAsync(User.TenantId, User.UserId, scenarioId, q["status"], q["result"], page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "Attempt_ListMine failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public abstract class AttemptTransitionFunctionBase : AttemptFunctionBase
{
    protected AttemptTransitionFunctionBase(IAttemptService s, ICurrentUserContext u, ILogger l) : base(s, u, l) { }
    protected async Task<HttpResponseData> RunTransition(HttpRequestData r, string idAttempt, Func<Guid, Guid, Guid, string, string, CancellationToken, Task<AttemptResponse>> transition, CancellationToken ct)
    {
        if (!Authorized(AttemptPolicies.ManageOwn)) return await Auth(r, AttemptPolicies.ManageOwn, ct);
        if (!Guid.TryParse(idAttempt, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await transition(User.TenantId, id, User.UserId, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "Attempt transition failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class AttemptPauseFunction : AttemptTransitionFunctionBase
{
    public AttemptPauseFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptPauseFunction> l) : base(s, u, l) { }
    [Function("Attempt_Pause")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/pause")] HttpRequestData r, string idAttempt, CancellationToken ct) =>
        RunTransition(r, idAttempt, Service.PauseAsync, ct);
}

public sealed class AttemptResumeFunction : AttemptTransitionFunctionBase
{
    public AttemptResumeFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptResumeFunction> l) : base(s, u, l) { }
    [Function("Attempt_Resume")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/resume")] HttpRequestData r, string idAttempt, CancellationToken ct) =>
        RunTransition(r, idAttempt, Service.ResumeAsync, ct);
}

public sealed class AttemptCompleteFunction : AttemptTransitionFunctionBase
{
    public AttemptCompleteFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptCompleteFunction> l) : base(s, u, l) { }
    [Function("Attempt_Complete")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/complete")] HttpRequestData r, string idAttempt, CancellationToken ct) =>
        RunTransition(r, idAttempt, Service.CompleteAsync, ct);
}

public sealed class AttemptAbandonFunction : AttemptTransitionFunctionBase
{
    public AttemptAbandonFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptAbandonFunction> l) : base(s, u, l) { }
    [Function("Attempt_Abandon")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/abandon")] HttpRequestData r, string idAttempt, CancellationToken ct) =>
        RunTransition(r, idAttempt, Service.AbandonAsync, ct);
}

public sealed class AttemptCancelFunction : AttemptTransitionFunctionBase
{
    public AttemptCancelFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptCancelFunction> l) : base(s, u, l) { }
    [Function("Attempt_Cancel")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/cancel")] HttpRequestData r, string idAttempt, CancellationToken ct) =>
        RunTransition(r, idAttempt, Service.CancelAsync, ct);
}

public sealed class AttemptDeleteFunction : AttemptFunctionBase
{
    public AttemptDeleteFunction(IAttemptService s, ICurrentUserContext u, ILogger<AttemptDeleteFunction> l) : base(s, u, l) { }
    [Function("Attempt_Delete")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "attempts/{idAttempt}")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(AttemptPolicies.ManageOwn) && !Authorized(AttemptPolicies.ReadAll)) return await Auth(r, AttemptPolicies.ManageOwn, ct);
        if (!Guid.TryParse(idAttempt, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            await Service.DeleteAsync(User.TenantId, id, User.UserId, Authorized(AttemptPolicies.ReadAll), ct);
            Logger.LogInformation("Attempt_Delete ok. TenantId={TenantId} UserId={UserId} AttemptId={AttemptId} CorrelationId={CorrelationId}", User.TenantId, User.UserId, id, User.CorrelationId);
            return r.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex) { Logger.LogError(ex, "Attempt_Delete failed. AttemptId={AttemptId} CorrelationId={CorrelationId}", id, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}
