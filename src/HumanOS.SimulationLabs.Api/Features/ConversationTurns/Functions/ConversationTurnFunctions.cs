using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Contracts;
using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Services;
using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns.Functions;

public abstract class ConversationTurnFunctionBase
{
    protected readonly IConversationTurnService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected ConversationTurnFunctionBase(IConversationTurnService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, ConversationTurnResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            ConversationTurnAttemptNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Intento no encontrado.", "ATTEMPT_NOT_FOUND", ct),
            ConversationTurnNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Turno de conversación no encontrado.", "CONVERSATION_TURN_NOT_FOUND", ct),
            ConversationTurnAttemptNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El intento no admite nuevos turnos.", "ATTEMPT_NOT_EDITABLE", ct),
            ActorScenarioMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El actor no pertenece al escenario del intento.", "ACTOR_SCENARIO_MISMATCH", ct),
            ExpectedMomentVersionMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El momento esperado no pertenece a la misma versión.", "EXPECTED_MOMENT_VERSION_MISMATCH", ct),
            TranscriptNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El turno excluido no puede modificarse.", "TRANSCRIPT_NOT_EDITABLE", ct),
            ConversationTurnPreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : "El ETag no coincide.", p.Code, ct),
            ConversationTurnConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class ConversationTurnCreateFunction : ConversationTurnFunctionBase
{
    public ConversationTurnCreateFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnCreateFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/conversation-turns")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.CreateInternal)) return await Auth(r, ConversationTurnPolicies.CreateInternal, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<CreateConversationTurnRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ConversationTurnValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            var value = await Service.CreateAsync(User.TenantId, attemptId, User.DisplayName, body, ct);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/conversation-turns/{value.IdTurn}");
        }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_Create failed. TenantId={TenantId} AttemptId={AttemptId} CorrelationId={CorrelationId}", User.TenantId, attemptId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ConversationTurnGetByIdFunction : ConversationTurnFunctionBase
{
    public ConversationTurnGetByIdFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnGetByIdFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "conversation-turns/{idTurn}")] HttpRequestData r, string idTurn, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.ReadOwn) && !Authorized(ConversationTurnPolicies.CreateInternal)) return await Auth(r, ConversationTurnPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idTurn, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idTurn debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, User.UserId, Authorized(ConversationTurnPolicies.CreateInternal), ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Turno de conversación no encontrado.", "CONVERSATION_TURN_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ConversationTurnListByAttemptFunction : ConversationTurnFunctionBase
{
    public ConversationTurnListByAttemptFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnListByAttemptFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_ListByAttempt")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "attempts/{idAttempt}/conversation-turns")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.ReadOwn) && !Authorized(ConversationTurnPolicies.CreateInternal)) return await Auth(r, ConversationTurnPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        try
        {
            var result = await Service.ListByAttemptAsync(User.TenantId, attemptId, User.UserId, Authorized(ConversationTurnPolicies.CreateInternal), page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_ListByAttempt failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ConversationTurnFinalizeFunction : ConversationTurnFunctionBase
{
    public ConversationTurnFinalizeFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnFinalizeFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_Finalize")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "conversation-turns/{idTurn}/finalize")] HttpRequestData r, string idTurn, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.CreateInternal)) return await Auth(r, ConversationTurnPolicies.CreateInternal, ct);
        if (!Guid.TryParse(idTurn, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idTurn debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.FinalizeAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_Finalize failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ConversationTurnCorrectFunction : ConversationTurnFunctionBase
{
    public ConversationTurnCorrectFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnCorrectFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_CorrectTranscript")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "conversation-turns/{idTurn}/correct")] HttpRequestData r, string idTurn, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.Correct)) return await Auth(r, ConversationTurnPolicies.Correct, ct);
        if (!Guid.TryParse(idTurn, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idTurn debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try
        {
            var body = await Body<CorrectConversationTurnRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ConversationTurnValidators.ValidateCorrect(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            return await Result(r, await Service.CorrectAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_CorrectTranscript failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ConversationTurnExcludeFunction : ConversationTurnFunctionBase
{
    public ConversationTurnExcludeFunction(IConversationTurnService s, ICurrentUserContext u, ILogger<ConversationTurnExcludeFunction> l) : base(s, u, l) { }
    [Function("ConversationTurn_Exclude")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "conversation-turns/{idTurn}/exclude")] HttpRequestData r, string idTurn, CancellationToken ct)
    {
        if (!Authorized(ConversationTurnPolicies.Exclude)) return await Auth(r, ConversationTurnPolicies.Exclude, ct);
        if (!Guid.TryParse(idTurn, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idTurn debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.ExcludeAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "ConversationTurn_Exclude failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}
