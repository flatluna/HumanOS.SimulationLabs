using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Stages.Contracts;
using HumanOS.SimulationLabs.Api.Features.Stages.Services;
using HumanOS.SimulationLabs.Api.Features.Stages.Validators;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Stages.Functions;

public abstract class StageFunctionBase
{
    protected readonly IStageService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;
    protected StageFunctionBase(IStageService service, ICurrentUserContext user, ILogger logger) { Service = service; User = user; Logger = logger; }
    protected bool Authorized(string permission) => User.IsAuthenticated && User.HasPermission(permission);
    protected Task<HttpResponseData> Problem(HttpRequestData r, HttpStatusCode code, string message, string error, CancellationToken ct, IReadOnlyList<string>? errors = null) => ApiResponses.ProblemAsync(r, code, message, User.CorrelationId, errors: errors, errorCode: error, cancellationToken: ct);
    protected static string? Header(HttpRequestData r, string name) => r.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    protected static async Task<T?> Body<T>(HttpRequestData r, CancellationToken ct) => await JsonSerializer.DeserializeAsync<T>(r.Body, ApiResponses.JsonOptions, ct);
    protected async Task<HttpResponseData> Result(HttpRequestData r, StageResponse value, HttpStatusCode code, CancellationToken ct, string? location = null) { var response = await ApiResponses.JsonAsync(r, code, value, User.CorrelationId, ct); response.Headers.Add("ETag", $"\"{value.RowVersion}\""); if (location is not null) response.Headers.Add("Location", location); return response; }
    protected async Task<HttpResponseData> Handle(HttpRequestData r, Exception ex, CancellationToken ct)
    {
        return ex switch
        {
            StageVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            StageNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Etapa no encontrada.", "STAGE_NOT_FOUND", ct),
            StageVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            StageCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código ya existe dentro de la versión.", "STAGE_CODE_DUPLICATE", ct),
            StageOrderDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El orden contiene duplicados o es inválido.", "STAGE_ORDER_DUPLICATE", ct),
            StagePreconditionException p => await Problem(r, HttpStatusCode.PreconditionFailed, p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : "El ETag no coincide.", p.Code, ct),
            StageConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }
    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) => !User.IsAuthenticated ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct) : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class StageCreateFunction : StageFunctionBase
{
    public StageCreateFunction(IStageService s, ICurrentUserContext u, ILogger<StageCreateFunction> l) : base(s, u, l) { }
    [Function("Stage_Create")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/stages")] HttpRequestData r, string idVersion, CancellationToken ct)
    {
        if (!Authorized(StagePolicies.Create)) return await Auth(r, StagePolicies.Create, ct);
        if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct);
        try { var body = await Body<CreateStageRequest>(r, ct); if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct); var errors = StageValidators.ValidateCreate(body); if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors); var value = await Service.CreateAsync(User.TenantId, versionId, User.DisplayName, body, ct); return await Result(r, value, HttpStatusCode.Created, ct, $"/api/stages/{value.IdStage}"); } catch (Exception ex) { Logger.LogError(ex, "Stage_Create failed. CorrelationId={CorrelationId}", User.CorrelationId); return await Handle(r, ex, ct); }
    }
}

public sealed class StageGetByIdFunction : StageFunctionBase
{
    public StageGetByIdFunction(IStageService s, ICurrentUserContext u, ILogger<StageGetByIdFunction> l) : base(s, u, l) { }
    [Function("Stage_GetById")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stages/{idStage}")] HttpRequestData r, string idStage, CancellationToken ct)
    { if (!Authorized(StagePolicies.Read)) return await Auth(r, StagePolicies.Read, ct); if (!Guid.TryParse(idStage, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct); try { var value = await Service.GetByIdAsync(User.TenantId, id, ct); return value is null ? await Problem(r, HttpStatusCode.NotFound, "Etapa no encontrada.", "STAGE_NOT_FOUND", ct) : await Result(r, value, HttpStatusCode.OK, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage_GetById failed"); return await Handle(r, ex, ct); } }
}

public sealed class StageListByVersionFunction : StageFunctionBase
{
    public StageListByVersionFunction(IStageService s, ICurrentUserContext u, ILogger<StageListByVersionFunction> l) : base(s, u, l) { }
    [Function("Stage_ListByVersion")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/stages")] HttpRequestData r, string idVersion, CancellationToken ct)
    { if (!Authorized(StagePolicies.Read)) return await Auth(r, StagePolicies.Read, ct); if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct); var q = HttpUtility.ParseQueryString(r.Url.Query); var page = int.TryParse(q["page"], out var p) ? p : 1; var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50; if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct); try { var result = await Service.ListAsync(User.TenantId, versionId, q["estatus"], q["tipoInteraccion"], bool.TryParse(q["obligatorio"], out var required) ? required : null, page, size, string.Equals(q["sortDirection"], "DESC", StringComparison.OrdinalIgnoreCase), ct); return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage_ListByVersion failed"); return await Handle(r, ex, ct); } }
}

public sealed class StageUpdateFunction : StageFunctionBase
{
    public StageUpdateFunction(IStageService s, ICurrentUserContext u, ILogger<StageUpdateFunction> l) : base(s, u, l) { }
    [Function("Stage_Update")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "stages/{idStage}")] HttpRequestData r, string idStage, CancellationToken ct)
    { if (!Authorized(StagePolicies.Update)) return await Auth(r, StagePolicies.Update, ct); if (!Guid.TryParse(idStage, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct); var etag = Header(r, "If-Match"); if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct); try { var body = await Body<UpdateStageRequest>(r, ct); if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct); var errors = StageValidators.ValidateUpdate(body); if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors); return await Result(r, await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct), HttpStatusCode.OK, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage_Update failed"); return await Handle(r, ex, ct); } }
}

public sealed class StageReorderFunction : StageFunctionBase
{
    public StageReorderFunction(IStageService s, ICurrentUserContext u, ILogger<StageReorderFunction> l) : base(s, u, l) { }
    [Function("Stage_Reorder")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lab-versions/{idVersion}/stages/reorder")] HttpRequestData r, string idVersion, CancellationToken ct)
    { if (!Authorized(StagePolicies.Update)) return await Auth(r, StagePolicies.Update, ct); if (!Guid.TryParse(idVersion, out var versionId)) return await Problem(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", "REQUEST_INVALID", ct); try { var body = await Body<ReorderStagesRequest>(r, ct); if (body?.Stages is null || body.Stages.Count == 0 || body.Stages.Any(x => x.IdStage == Guid.Empty || string.IsNullOrWhiteSpace(x.RowVersion))) return await Problem(r, HttpStatusCode.BadRequest, "La lista de etapas es inválida.", "REQUEST_INVALID", ct); return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, await Service.ReorderAsync(User.TenantId, versionId, User.DisplayName, body, ct), User.CorrelationId, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage_Reorder failed"); return await Handle(r, ex, ct); } }
}

public sealed class StageActivateFunction : StageFunctionBase
{
    public StageActivateFunction(IStageService s, ICurrentUserContext u, ILogger<StageActivateFunction> l) : base(s, u, l) { }
    [Function("Stage_Activate")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stages/{idStage}/activate")] HttpRequestData r, string idStage, CancellationToken ct)
    { return await Change(r, idStage, StagePolicies.Activate, StageEstatus.Active, ct); }
    private async Task<HttpResponseData> Change(HttpRequestData r, string raw, string policy, string target, CancellationToken ct) { if (!Authorized(policy)) return await Auth(r, policy, ct); if (!Guid.TryParse(raw, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct); var body = await Body<StageActionRequest>(r, ct); if (body is null || StageValidators.ValidateAction(body).Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, body is null ? null : StageValidators.ValidateAction(body)); var etag = Header(r, "If-Match"); if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct); try { return await Result(r, await Service.SetStatusAsync(User.TenantId, id, etag, User.DisplayName, target, ct), HttpStatusCode.OK, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage status change failed"); return await Handle(r, ex, ct); } }
}

public sealed class StageInactivateFunction : StageFunctionBase
{
    public StageInactivateFunction(IStageService s, ICurrentUserContext u, ILogger<StageInactivateFunction> l) : base(s, u, l) { }
    [Function("Stage_Inactivate")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stages/{idStage}/inactivate")] HttpRequestData r, string idStage, CancellationToken ct)
    { if (!User.IsAuthenticated) return await Auth(r, StagePolicies.Inactivate, ct); if (!User.HasPermission(StagePolicies.Inactivate)) return await Auth(r, StagePolicies.Inactivate, ct); if (!Guid.TryParse(idStage, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idStage debe ser un GUID válido.", "REQUEST_INVALID", ct); var body = await Body<StageActionRequest>(r, ct); var errors = body is null ? ["El cuerpo es requerido."] : StageValidators.ValidateAction(body); if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors); var etag = Header(r, "If-Match"); if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct); try { return await Result(r, await Service.SetStatusAsync(User.TenantId, id, etag, User.DisplayName, StageEstatus.Inactive, ct), HttpStatusCode.OK, ct); } catch (Exception ex) { Logger.LogError(ex, "Stage_Inactivate failed"); return await Handle(r, ex, ct); } }
}
