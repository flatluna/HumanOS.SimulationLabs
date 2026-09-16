using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Rubrics.Contracts;
using HumanOS.SimulationLabs.Api.Features.Rubrics.Services;
using HumanOS.SimulationLabs.Api.Features.Rubrics.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Rubrics.Functions;

public abstract class RubricFunctionBase
{
    protected readonly IRubricService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;

    protected RubricFunctionBase(IRubricService service, ICurrentUserContext user, ILogger logger)
    {
        Service = service;
        User = user;
        Logger = logger;
    }

    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);

    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) =>
        ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);

    protected static string? Header(HttpRequestData r, string name) =>
        r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) =>
        await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);

    protected async Task<HttpResponseData> Result(HttpRequestData r, RubricResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
    {
        var response = await ApiResponses.JsonAsync(r, code, value, User.CorrelationId, ct);
        response.Headers.Add("ETag", $"\"{value.RowVersion}\"");
        if (location is not null)
        {
            response.Headers.Add("Location", location);
        }
        return response;
    }

    protected async Task<HttpResponseData> Handle(HttpRequestData r, Exception ex, CancellationToken ct)
    {
        return ex switch
        {
            RubricVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            RubricNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Rúbrica no encontrada.", "RUBRIC_NOT_FOUND", ct),
            RubricVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión no está en un estatus válido.", "LAB_VERSION_NOT_EDITABLE", ct),
            RubricAlreadyExistsException => await Problem(r, HttpStatusCode.Conflict, "La versión ya tiene una rúbrica.", "RUBRIC_ALREADY_EXISTS", ct),
            RubricCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro del tenant.", "RUBRIC_CODE_DUPLICATE", ct),
            RubricNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La rúbrica no está en un estatus editable o está incompleta.", "RUBRIC_NOT_EDITABLE", ct),
            RubricPreconditionException p => await Problem(r, p.Code == "INVALID_STATUS_TRANSITION" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.PreconditionFailed,
                p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.", p.Code, ct),
            RubricConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }

    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) =>
        !User.IsAuthenticated
            ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct)
            : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class RubricCreateFunction : RubricFunctionBase
{
    public RubricCreateFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricCreateFunction> l) : base(s, u, l) { }

    [Function("Rubric_Create")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/rubric")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Create)) return await Auth(r, RubricPolicies.Create, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<CreateRubricRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.CreateAsync(User.TenantId, versionId, User.DisplayName, body, ct);
            Logger.LogInformation("Rubric_Create ok. TenantId={TenantId} VersionId={VersionId} RubricId={RubricId} CorrelationId={CorrelationId}", User.TenantId, versionId, value.IdRubric, User.CorrelationId);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/rubrics/{value.IdRubric}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_Create failed. CorrelationId={CorrelationId}", User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricGetByIdFunction : RubricFunctionBase
{
    public RubricGetByIdFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricGetByIdFunction> l) : base(s, u, l) { }

    [Function("Rubric_GetById")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rubrics/{idRubric}")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Read)) return await Auth(r, RubricPolicies.Read, ct);
        if (!Guid.TryParse(idRubric, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            return value is null
                ? await Problem(r, HttpStatusCode.NotFound, "Rúbrica no encontrada.", "RUBRIC_NOT_FOUND", ct)
                : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_GetById failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricGetByVersionFunction : RubricFunctionBase
{
    public RubricGetByVersionFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricGetByVersionFunction> l) : base(s, u, l) { }

    [Function("Rubric_GetByVersion")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/rubric")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Read)) return await Auth(r, RubricPolicies.Read, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var value = await Service.GetByVersionAsync(User.TenantId, versionId, ct);
            return value is null
                ? await Problem(r, HttpStatusCode.NotFound, "La versión no tiene rúbrica.", "RUBRIC_NOT_FOUND", ct)
                : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_GetByVersion failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricUpdateFunction : RubricFunctionBase
{
    public RubricUpdateFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricUpdateFunction> l) : base(s, u, l) { }

    [Function("Rubric_Update")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "rubrics/{idRubric}")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Update)) return await Auth(r, RubricPolicies.Update, ct);
        if (!Guid.TryParse(idRubric, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<UpdateRubricRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_Update failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricApproveFunction : RubricFunctionBase
{
    public RubricApproveFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricApproveFunction> l) : base(s, u, l) { }

    [Function("Rubric_Approve")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubrics/{idRubric}/approve")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Approve)) return await Auth(r, RubricPolicies.Approve, ct);
        if (!Guid.TryParse(idRubric, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<RubricActionRequest>(r, ct) ?? new RubricActionRequest();
            var errors = RubricValidators.ValidateAction(body.Motivo);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.ApproveAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_Approve failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricPublishFunction : RubricFunctionBase
{
    public RubricPublishFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricPublishFunction> l) : base(s, u, l) { }

    [Function("Rubric_Publish")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubrics/{idRubric}/publish")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Publish)) return await Auth(r, RubricPolicies.Publish, ct);
        if (!Guid.TryParse(idRubric, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<PublishRubricRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricValidators.ValidatePublish(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.PublishAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_Publish failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricRetireFunction : RubricFunctionBase
{
    public RubricRetireFunction(IRubricService s, ICurrentUserContext u, ILogger<RubricRetireFunction> l) : base(s, u, l) { }

    [Function("Rubric_Retire")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubrics/{idRubric}/retire")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricPolicies.Retire)) return await Auth(r, RubricPolicies.Retire, ct);
        if (!Guid.TryParse(idRubric, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<RetireRubricRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricValidators.ValidateRetire(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.RetireAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rubric_Retire failed");
            return await Handle(r, ex, ct);
        }
    }
}
