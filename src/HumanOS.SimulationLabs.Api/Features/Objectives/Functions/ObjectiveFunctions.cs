using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;
using HumanOS.SimulationLabs.Api.Features.Objectives.Services;
using HumanOS.SimulationLabs.Api.Features.Objectives.Validators;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Objectives.Functions;

public abstract class ObjectiveFunctionBase
{
    protected readonly IObjectiveService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;

    protected ObjectiveFunctionBase(IObjectiveService service, ICurrentUserContext user, ILogger logger)
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

    protected async Task<HttpResponseData> Result(HttpRequestData r, ObjectiveResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            ObjectiveVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            ObjectiveStageNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Etapa no encontrada.", "STAGE_NOT_FOUND", ct),
            ObjectiveNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Objetivo no encontrado.", "OBJECTIVE_NOT_FOUND", ct),
            ObjectiveVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            ObjectiveCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro de la versión.", "OBJECTIVE_CODE_DUPLICATE", ct),
            ObjectiveOrderDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El orden contiene duplicados o es inválido.", "OBJECTIVE_ORDER_DUPLICATE", ct),
            ObjectivePreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.", p.Code, ct),
            ObjectiveConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }

    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) =>
        !User.IsAuthenticated
            ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct)
            : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class ObjectiveCreateFunction : ObjectiveFunctionBase
{
    public ObjectiveCreateFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveCreateFunction> l) : base(s, u, l) { }

    [Function("Objective_Create")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/objectives")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Create)) return await Auth(r, ObjectivePolicies.Create, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<CreateObjectiveRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = ObjectiveValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.CreateAsync(User.TenantId, versionId, User.DisplayName, body, ct);
            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/objectives/{value.IdObjective}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_Create failed. CorrelationId={CorrelationId}", User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveGetByIdFunction : ObjectiveFunctionBase
{
    public ObjectiveGetByIdFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveGetByIdFunction> l) : base(s, u, l) { }

    [Function("Objective_GetById")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "objectives/{idObjective}")] HttpRequestData r,
        string idObjective,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Read)) return await Auth(r, ObjectivePolicies.Read, ct);
        if (!Guid.TryParse(idObjective, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idObjective debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            return value is null
                ? await Problem(r, HttpStatusCode.NotFound, "Objetivo no encontrado.", "OBJECTIVE_NOT_FOUND", ct)
                : await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_GetById failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveListByVersionFunction : ObjectiveFunctionBase
{
    public ObjectiveListByVersionFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveListByVersionFunction> l) : base(s, u, l) { }

    [Function("Objective_ListByVersion")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/objectives")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Read)) return await Auth(r, ObjectivePolicies.Read, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);

        Guid? stageId = Guid.TryParse(q["stageId"], out var parsedStageId) ? parsedStageId : null;
        bool? onlyGeneral = bool.TryParse(q["onlyGeneral"], out var og) ? og : bool.TryParse(q["soloGenerales"], out var sg) ? sg : null;
        bool? isCritical = bool.TryParse(q["esCritico"], out var crit) ? crit : null;

        try
        {
            var result = await Service.ListByVersionAsync(
                User.TenantId,
                versionId,
                stageId,
                onlyGeneral,
                q["estatus"],
                q["tipoEvidencia"],
                isCritical,
                page,
                size,
                string.Equals(q["sortDirection"], "DESC", StringComparison.OrdinalIgnoreCase),
                ct);

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_ListByVersion failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveListByStageFunction : ObjectiveFunctionBase
{
    public ObjectiveListByStageFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveListByStageFunction> l) : base(s, u, l) { }

    [Function("Objective_ListByStage")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stages/{idStage}/objectives")] HttpRequestData r,
        string idStage,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Read)) return await Auth(r, ObjectivePolicies.Read, ct);
        if (!Guid.TryParse(idStage, out var stageId)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);

        bool? isCritical = bool.TryParse(q["esCritico"], out var crit) ? crit : null;

        try
        {
            var result = await Service.ListByStageAsync(
                User.TenantId,
                stageId,
                q["estatus"],
                q["tipoEvidencia"],
                isCritical,
                page,
                size,
                string.Equals(q["sortDirection"], "DESC", StringComparison.OrdinalIgnoreCase),
                ct);

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_ListByStage failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveUpdateFunction : ObjectiveFunctionBase
{
    public ObjectiveUpdateFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveUpdateFunction> l) : base(s, u, l) { }

    [Function("Objective_Update")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "objectives/{idObjective}")] HttpRequestData r,
        string idObjective,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Update)) return await Auth(r, ObjectivePolicies.Update, ct);
        if (!Guid.TryParse(idObjective, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idObjective debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<UpdateObjectiveRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = ObjectiveValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_Update failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveReorderFunction : ObjectiveFunctionBase
{
    public ObjectiveReorderFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveReorderFunction> l) : base(s, u, l) { }

    [Function("Objective_Reorder")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/objectives/reorder")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Update)) return await Auth(r, ObjectivePolicies.Update, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<ReorderObjectivesRequest>(r, ct);
            if (body?.Objectives is null || body.Objectives.Count == 0 || body.Objectives.Any(x => x.IdObjective == Guid.Empty || string.IsNullOrWhiteSpace(x.RowVersion) || x.Orden <= 0))
            {
                return await Problem(r, HttpStatusCode.BadRequest, "La lista de objetivos es inválida.", "REQUEST_INVALID", ct);
            }

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, await Service.ReorderAsync(User.TenantId, versionId, User.DisplayName, body, ct), User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_Reorder failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveActivateFunction : ObjectiveFunctionBase
{
    public ObjectiveActivateFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveActivateFunction> l) : base(s, u, l) { }

    [Function("Objective_Activate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "objectives/{idObjective}/activate")] HttpRequestData r,
        string idObjective,
        CancellationToken ct)
    {
        return await Change(r, idObjective, ObjectivePolicies.Activate, ObjectiveEstatus.Active, ct);
    }

    private async Task<HttpResponseData> Change(HttpRequestData r, string raw, string policy, string target, CancellationToken ct)
    {
        if (!Authorized(policy)) return await Auth(r, policy, ct);
        if (!Guid.TryParse(raw, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idObjective debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var body = await Body<ObjectiveActionRequest>(r, ct);
        if (body is not null)
        {
            var errors = ObjectiveValidators.ValidateAction(body);
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
            Logger.LogError(ex, "Objective status change failed");
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class ObjectiveInactivateFunction : ObjectiveFunctionBase
{
    public ObjectiveInactivateFunction(IObjectiveService s, ICurrentUserContext u, ILogger<ObjectiveInactivateFunction> l) : base(s, u, l) { }

    [Function("Objective_Inactivate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "objectives/{idObjective}/inactivate")] HttpRequestData r,
        string idObjective,
        CancellationToken ct)
    {
        if (!Authorized(ObjectivePolicies.Inactivate)) return await Auth(r, ObjectivePolicies.Inactivate, ct);
        if (!Guid.TryParse(idObjective, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idObjective debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var body = await Body<ObjectiveActionRequest>(r, ct);
        if (body is not null)
        {
            var errors = ObjectiveValidators.ValidateAction(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);
        }

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            return await Result(r, await Service.SetStatusAsync(User.TenantId, id, etag, User.DisplayName, ObjectiveEstatus.Inactive, ct), HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Objective_Inactivate failed");
            return await Handle(r, ex, ct);
        }
    }
}
