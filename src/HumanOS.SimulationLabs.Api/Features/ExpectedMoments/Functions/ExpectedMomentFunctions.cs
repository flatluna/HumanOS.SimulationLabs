using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Contracts;
using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Services;
using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Validators;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Functions;

public abstract class ExpectedMomentFunctionBase
{
    protected readonly IExpectedMomentService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;

    protected ExpectedMomentFunctionBase(IExpectedMomentService service, ICurrentUserContext user, ILogger logger)
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

    protected async Task<HttpResponseData> Result(HttpRequestData r, ExpectedMomentResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            ExpectedMomentVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            ExpectedMomentStageNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Etapa no encontrada.", "STAGE_NOT_FOUND", ct),
            ExpectedMomentObjectiveNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Objetivo no encontrado.", "OBJECTIVE_NOT_FOUND", ct),
            ExpectedMomentObjectiveStageMismatchException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El objetivo no pertenece a la misma versión/etapa.", "OBJECTIVE_STAGE_MISMATCH", ct),
            ExpectedMomentNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Expected Moment no encontrado.", "EXPECTED_MOMENT_NOT_FOUND", ct),
            ExpectedMomentVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            ExpectedMomentCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro de la versión.", "EXPECTED_MOMENT_CODE_DUPLICATE", ct),
            ExpectedMomentOrderDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El orden contiene duplicados o es inválido.", "EXPECTED_MOMENT_ORDER_DUPLICATE", ct),
            ExpectedMomentPreconditionException p => await Problem(r, p.Code == "INVALID_STATUS_TRANSITION" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.PreconditionFailed,
                p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.", p.Code, ct),
            ExpectedMomentConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }

    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) =>
        !User.IsAuthenticated
            ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct)
            : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class ExpectedMomentCreateFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentCreateFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentCreateFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_Create")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stages/{idStage}/expected-moments")] HttpRequestData r,
        string idStage,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Create)) return await Auth(r, ExpectedMomentPolicies.Create, ct);
        if (!Guid.TryParse(idStage, out var stageId)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<CreateExpectedMomentRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = ExpectedMomentValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.CreateAsync(User.TenantId, stageId, User.DisplayName, body, ct);
            Logger.LogInformation("ExpectedMoment_Create ok. TenantId={TenantId} StageId={StageId} MomentId={MomentId} CorrelationId={CorrelationId}", User.TenantId, stageId, value.IdExpectedMoment, User.CorrelationId);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/expected-moments/{value.IdExpectedMoment}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_Create failed. CorrelationId={CorrelationId}", User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentGetByIdFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentGetByIdFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentGetByIdFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_GetById")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "expected-moments/{idExpectedMoment}")] HttpRequestData r,
        string idExpectedMoment,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Read)) return await Auth(r, ExpectedMomentPolicies.Read, ct);
        if (!Guid.TryParse(idExpectedMoment, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idExpectedMoment debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            return value is null
                ? await Problem(r, HttpStatusCode.NotFound, "Expected Moment no encontrado.", "EXPECTED_MOMENT_NOT_FOUND", ct)
                : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_GetById failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentListByVersionFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentListByVersionFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentListByVersionFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_ListByVersion")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/expected-moments")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Read)) return await Auth(r, ExpectedMomentPolicies.Read, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);

        Guid? stageId = Guid.TryParse(q["stageId"], out var sid) ? sid : null;
        Guid? objectiveId = Guid.TryParse(q["objectiveId"], out var oid) ? oid : null;
        bool? critico = bool.TryParse(q["critico"], out var crit) ? crit : null;
        bool? requiereRespuesta = bool.TryParse(q["requiereRespuesta"], out var rr) ? rr : null;

        try
        {
            var result = await Service.ListByVersionAsync(User.TenantId, versionId, stageId, objectiveId, q["tipo"], critico, requiereRespuesta, q["estatus"], page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_ListByVersion failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentListByStageFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentListByStageFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentListByStageFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_ListByStage")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stages/{idStage}/expected-moments")] HttpRequestData r,
        string idStage,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Read)) return await Auth(r, ExpectedMomentPolicies.Read, ct);
        if (!Guid.TryParse(idStage, out var stageId)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);

        Guid? objectiveId = Guid.TryParse(q["objectiveId"], out var oid) ? oid : null;
        bool? critico = bool.TryParse(q["critico"], out var crit) ? crit : null;

        try
        {
            var result = await Service.ListByStageAsync(User.TenantId, stageId, objectiveId, q["tipo"], critico, q["estatus"], page, size, ct);
            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_ListByStage failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentUpdateFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentUpdateFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentUpdateFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_Update")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "expected-moments/{idExpectedMoment}")] HttpRequestData r,
        string idExpectedMoment,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Update)) return await Auth(r, ExpectedMomentPolicies.Update, ct);
        if (!Guid.TryParse(idExpectedMoment, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idExpectedMoment debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<UpdateExpectedMomentRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = ExpectedMomentValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_Update failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentReorderFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentReorderFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentReorderFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_Reorder")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stages/{idStage}/expected-moments/reorder")] HttpRequestData r,
        string idStage,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Update)) return await Auth(r, ExpectedMomentPolicies.Update, ct);
        if (!Guid.TryParse(idStage, out var stageId)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<ReorderExpectedMomentsRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = ExpectedMomentValidators.ValidateReorder(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, await Service.ReorderAsync(User.TenantId, stageId, User.DisplayName, body, ct), User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_Reorder failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentActivateFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentActivateFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentActivateFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_Activate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "expected-moments/{idExpectedMoment}/activate")] HttpRequestData r,
        string idExpectedMoment,
        CancellationToken ct) =>
        await ChangeStatus(r, idExpectedMoment, ExpectedMomentPolicies.Activate, MomentEstatus.Active, "ExpectedMoment_Activate", ct);

    private async Task<HttpResponseData> ChangeStatus(HttpRequestData r, string raw, string policy, string target, string name, CancellationToken ct)
    {
        if (!Authorized(policy)) return await Auth(r, policy, ct);
        if (!Guid.TryParse(raw, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idExpectedMoment debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var body = await Body<ExpectedMomentActionRequest>(r, ct);
        if (body is not null)
        {
            var errors = ExpectedMomentValidators.ValidateAction(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
        }

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            return await Result(r, await Service.SetStatusAsync(User.TenantId, id, etag, User.DisplayName, target, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Name} failed", name);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ExpectedMomentInactivateFunction : ExpectedMomentFunctionBase
{
    public ExpectedMomentInactivateFunction(IExpectedMomentService s, ICurrentUserContext u, ILogger<ExpectedMomentInactivateFunction> l) : base(s, u, l) { }

    [Function("ExpectedMoment_Inactivate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "expected-moments/{idExpectedMoment}/inactivate")] HttpRequestData r,
        string idExpectedMoment,
        CancellationToken ct)
    {
        if (!Authorized(ExpectedMomentPolicies.Inactivate)) return await Auth(r, ExpectedMomentPolicies.Inactivate, ct);
        if (!Guid.TryParse(idExpectedMoment, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idExpectedMoment debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var body = await Body<ExpectedMomentActionRequest>(r, ct);
        if (body is not null)
        {
            var errors = ExpectedMomentValidators.ValidateAction(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
        }

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            return await Result(r, await Service.SetStatusAsync(User.TenantId, id, etag, User.DisplayName, MomentEstatus.Inactive, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExpectedMoment_Inactivate failed");
            return await Handle(r, ex, ct);
        }
    }
}
