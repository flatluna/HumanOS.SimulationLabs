using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.UserActions.Contracts;
using HumanOS.SimulationLabs.Api.Features.UserActions.Services;
using HumanOS.SimulationLabs.Api.Features.UserActions.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.UserActions.Functions;

public abstract class UserActionFunctionBase
{
    protected readonly IUserActionService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected UserActionFunctionBase(IUserActionService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, UserActionResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            UserActionAttemptNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Intento no encontrado.", "ATTEMPT_NOT_FOUND", ct),
            UserActionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Acción de usuario no encontrada.", "USER_ACTION_NOT_FOUND", ct),
            UserActionAttemptNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El intento no admite nuevas acciones.", "ATTEMPT_NOT_EDITABLE", ct),
            UserActionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La acción no admite esta transición.", "USER_ACTION_NOT_EDITABLE", ct),
            InvalidActionJsonException => await Problem(r, HttpStatusCode.BadRequest, "El JSON de la acción no es válido.", "INVALID_ACTION_JSON", ct),
            ActionExpectedMomentMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El momento esperado no pertenece a la misma versión.", "ACTION_EXPECTED_MOMENT_MISMATCH", ct),
            UserActionPreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : "El ETag no coincide.", p.Code, ct),
            UserActionConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class UserActionCreateFunction : UserActionFunctionBase
{
    public UserActionCreateFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionCreateFunction> l) : base(s, u, l) { }
    [Function("UserAction_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/user-actions")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.CreateInternal)) return await Auth(r, UserActionPolicies.CreateInternal, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<CreateUserActionRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = UserActionValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            var value = await Service.CreateAsync(User.TenantId, attemptId, User.DisplayName, body, ct);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/user-actions/{value.IdUserAction}");
        }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_Create failed. TenantId={TenantId} AttemptId={AttemptId} CorrelationId={CorrelationId}", User.TenantId, attemptId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class UserActionGetByIdFunction : UserActionFunctionBase
{
    public UserActionGetByIdFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionGetByIdFunction> l) : base(s, u, l) { }
    [Function("UserAction_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user-actions/{idUserAction}")] HttpRequestData r, string idUserAction, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.ReadOwn) && !Authorized(UserActionPolicies.CreateInternal)) return await Auth(r, UserActionPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idUserAction, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idUserAction debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, User.UserId, Authorized(UserActionPolicies.CreateInternal), ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Acción de usuario no encontrada.", "USER_ACTION_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class UserActionListByAttemptFunction : UserActionFunctionBase
{
    public UserActionListByAttemptFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionListByAttemptFunction> l) : base(s, u, l) { }
    [Function("UserAction_ListByAttempt")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "attempts/{idAttempt}/user-actions")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.ReadOwn) && !Authorized(UserActionPolicies.CreateInternal)) return await Auth(r, UserActionPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        Guid? expectedMomentId = Guid.TryParse(q["expectedMomentId"], out var mid) ? mid : null;
        try
        {
            var result = await Service.ListByAttemptAsync(User.TenantId, attemptId, User.UserId, Authorized(UserActionPolicies.CreateInternal), q["tipo"], q["resultado"], q["severidad"], bool.TryParse(q["critica"], out var critica) ? critica : null, expectedMomentId, page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_ListByAttempt failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class UserActionFinalizeFunction : UserActionFunctionBase
{
    public UserActionFinalizeFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionFinalizeFunction> l) : base(s, u, l) { }
    [Function("UserAction_Finalize")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user-actions/{idUserAction}/finalize")] HttpRequestData r, string idUserAction, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.Finalize)) return await Auth(r, UserActionPolicies.Finalize, ct);
        if (!Guid.TryParse(idUserAction, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idUserAction debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.FinalizeAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_Finalize failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class UserActionReverseFunction : UserActionFunctionBase
{
    public UserActionReverseFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionReverseFunction> l) : base(s, u, l) { }
    [Function("UserAction_Reverse")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user-actions/{idUserAction}/reverse")] HttpRequestData r, string idUserAction, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.Reverse)) return await Auth(r, UserActionPolicies.Reverse, ct);
        if (!Guid.TryParse(idUserAction, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idUserAction debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.ReverseAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_Reverse failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class UserActionExcludeFunction : UserActionFunctionBase
{
    public UserActionExcludeFunction(IUserActionService s, ICurrentUserContext u, ILogger<UserActionExcludeFunction> l) : base(s, u, l) { }
    [Function("UserAction_Exclude")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user-actions/{idUserAction}/exclude")] HttpRequestData r, string idUserAction, CancellationToken ct)
    {
        if (!Authorized(UserActionPolicies.Exclude)) return await Auth(r, UserActionPolicies.Exclude, ct);
        if (!Guid.TryParse(idUserAction, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idUserAction debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await Service.ExcludeAsync(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "UserAction_Exclude failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}
