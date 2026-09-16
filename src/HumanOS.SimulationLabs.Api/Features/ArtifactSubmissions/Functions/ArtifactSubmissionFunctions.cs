using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Contracts;
using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Services;
using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Functions;

public abstract class ArtifactSubmissionFunctionBase
{
    protected readonly IArtifactSubmissionService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected ArtifactSubmissionFunctionBase(IArtifactSubmissionService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, ArtifactSubmissionResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            ArtifactSubmissionAttemptNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Intento no encontrado.", "ATTEMPT_NOT_FOUND", ct),
            ArtifactSubmissionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Artefacto no encontrado.", "ARTIFACT_NOT_FOUND", ct),
            ArtifactSubmissionAttemptNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El intento no admite cambios de artefactos.", "ATTEMPT_NOT_EDITABLE", ct),
            ArtifactSubmissionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El artefacto ya es una entrega final y no puede modificarse.", "ARTIFACT_NOT_EDITABLE", ct),
            ArtifactSubmissionVersionDuplicateException => await Problem(r, HttpStatusCode.Conflict, "Ya existe una entrega final para este código de artefacto.", "ARTIFACT_VERSION_DUPLICATE", ct),
            ArtifactSubmissionStageMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El stage no pertenece a la misma versión.", "ARTIFACT_STAGE_MISMATCH", ct),
            ArtifactSubmissionObjectiveMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El objetivo no pertenece a la misma versión.", "ARTIFACT_OBJECTIVE_MISMATCH", ct),
            InvalidArtifactJsonException => await Problem(r, HttpStatusCode.BadRequest, "El JSON del artefacto no es válido.", "INVALID_ARTIFACT_JSON", ct),
            ArtifactSubmissionPreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : "El ETag no coincide.", p.Code, ct),
            ArtifactSubmissionConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class ArtifactSubmissionCreateFunction : ArtifactSubmissionFunctionBase
{
    public ArtifactSubmissionCreateFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionCreateFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt}/artifacts")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(ArtifactSubmissionPolicies.Create)) return await Auth(r, ArtifactSubmissionPolicies.Create, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var body = await Body<CreateArtifactSubmissionRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ArtifactSubmissionValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            var value = await Service.CreateAsync(User.TenantId, attemptId, User.DisplayName, body, ct);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/artifacts/{value.IdSubmission}");
        }
        catch (Exception ex) { Logger.LogError(ex, "ArtifactSubmission_Create failed. TenantId={TenantId} AttemptId={AttemptId} CorrelationId={CorrelationId}", User.TenantId, attemptId, User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ArtifactSubmissionGetByIdFunction : ArtifactSubmissionFunctionBase
{
    public ArtifactSubmissionGetByIdFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionGetByIdFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "artifacts/{idSubmission}")] HttpRequestData r, string idSubmission, CancellationToken ct)
    {
        if (!Authorized(ArtifactSubmissionPolicies.ReadOwn)) return await Auth(r, ArtifactSubmissionPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idSubmission, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idSubmission debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, User.UserId, false, ct);
            return value is null ? await Problem(r, HttpStatusCode.NotFound, "Artefacto no encontrado.", "ARTIFACT_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ArtifactSubmission_GetById failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ArtifactSubmissionListByAttemptFunction : ArtifactSubmissionFunctionBase
{
    public ArtifactSubmissionListByAttemptFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionListByAttemptFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_ListByAttempt")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "attempts/{idAttempt}/artifacts")] HttpRequestData r, string idAttempt, CancellationToken ct)
    {
        if (!Authorized(ArtifactSubmissionPolicies.ReadOwn)) return await Auth(r, ArtifactSubmissionPolicies.ReadOwn, ct);
        if (!Guid.TryParse(idAttempt, out var attemptId)) return await Problem(r, HttpStatusCode.BadRequest, "idAttempt debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);
        Guid? stageId = Guid.TryParse(q["stageId"], out var sid) ? sid : null;
        Guid? objectiveId = Guid.TryParse(q["objectiveId"], out var oid) ? oid : null;
        try
        {
            var result = await Service.ListByAttemptAsync(User.TenantId, attemptId, User.UserId, false, stageId, objectiveId, q["tipo"], q["formato"], q["estatus"], bool.TryParse(q["final"], out var final) ? final : null, page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ArtifactSubmission_ListByAttempt failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ArtifactSubmissionUpdateFunction : ArtifactSubmissionFunctionBase
{
    public ArtifactSubmissionUpdateFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionUpdateFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_Update")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "artifacts/{idSubmission}")] HttpRequestData r, string idSubmission, CancellationToken ct)
    {
        if (!Authorized(ArtifactSubmissionPolicies.Update)) return await Auth(r, ArtifactSubmissionPolicies.Update, ct);
        if (!Guid.TryParse(idSubmission, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idSubmission debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try
        {
            var body = await Body<UpdateArtifactSubmissionRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);
            var errors = ArtifactSubmissionValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex) { Logger.LogError(ex, "ArtifactSubmission_Update failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public abstract class ArtifactSubmissionTransitionFunctionBase : ArtifactSubmissionFunctionBase
{
    protected ArtifactSubmissionTransitionFunctionBase(IArtifactSubmissionService s, ICurrentUserContext u, ILogger l) : base(s, u, l) { }
    protected async Task<HttpResponseData> RunTransition(HttpRequestData r, string idSubmission, string policy, Func<Guid, Guid, string, string, CancellationToken, Task<ArtifactSubmissionResponse>> transition, CancellationToken ct)
    {
        if (!Authorized(policy)) return await Auth(r, policy, ct);
        if (!Guid.TryParse(idSubmission, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idSubmission debe ser un GUID válido.", "REQUEST_INVALID", ct);
        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);
        try { return await Result(r, await transition(User.TenantId, id, etag, User.DisplayName, ct), HttpStatusCode.OK, ct); }
        catch (Exception ex) { Logger.LogError(ex, "ArtifactSubmission transition failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class ArtifactSubmissionSubmitFunction : ArtifactSubmissionTransitionFunctionBase
{
    public ArtifactSubmissionSubmitFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionSubmitFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_Submit")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artifacts/{idSubmission}/submit")] HttpRequestData r, string idSubmission, CancellationToken ct) =>
        RunTransition(r, idSubmission, ArtifactSubmissionPolicies.Submit, Service.SubmitAsync, ct);
}

public sealed class ArtifactSubmissionFinalizeFunction : ArtifactSubmissionTransitionFunctionBase
{
    public ArtifactSubmissionFinalizeFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionFinalizeFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_Finalize")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artifacts/{idSubmission}/finalize")] HttpRequestData r, string idSubmission, CancellationToken ct) =>
        RunTransition(r, idSubmission, ArtifactSubmissionPolicies.Finalize, Service.FinalizeAsync, ct);
}

public sealed class ArtifactSubmissionExcludeFunction : ArtifactSubmissionTransitionFunctionBase
{
    public ArtifactSubmissionExcludeFunction(IArtifactSubmissionService s, ICurrentUserContext u, ILogger<ArtifactSubmissionExcludeFunction> l) : base(s, u, l) { }
    [Function("ArtifactSubmission_Exclude")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artifacts/{idSubmission}/exclude")] HttpRequestData r, string idSubmission, CancellationToken ct) =>
        RunTransition(r, idSubmission, ArtifactSubmissionPolicies.Exclude, Service.ExcludeAsync, ct);
}
