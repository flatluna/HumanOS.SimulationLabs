using System.Net;
using System.Text.Json;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Contracts;
using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Services;
using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria.Functions;

public abstract class RubricCriterionFunctionBase
{
    protected readonly IRubricCriterionService Service;
    protected readonly ICurrentUserContext User;
    protected readonly ILogger Logger;

    protected RubricCriterionFunctionBase(IRubricCriterionService service, ICurrentUserContext user, ILogger logger)
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

    protected async Task<HttpResponseData> Result(HttpRequestData r, RubricCriterionResponse value, HttpStatusCode code, CancellationToken ct, string? location = null)
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
            RubricCriterionVersionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", "LAB_VERSION_NOT_FOUND", ct),
            RubricCriterionRubricNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Rúbrica no encontrada.", "RUBRIC_NOT_FOUND", ct),
            RubricCriterionNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Criterio de evaluación no encontrado.", "RUBRIC_CRITERION_NOT_FOUND", ct),
            RubricCriterionObjectiveNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Objetivo no encontrado.", "OBJECTIVE_NOT_FOUND", ct),
            RubricCriterionExpectedMomentNotFoundException => await Problem(r, HttpStatusCode.NotFound, "Momento esperado no encontrado.", "EXPECTED_MOMENT_NOT_FOUND", ct),
            RubricCriterionVersionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La versión de Lab no está en DRAFT.", "LAB_VERSION_NOT_EDITABLE", ct),
            RubricCriterionRubricNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "La rúbrica no está en DRAFT.", "RUBRIC_NOT_EDITABLE", ct),
            RubricCriterionNotEditableException => await Problem(r, HttpStatusCode.UnprocessableEntity, "El criterio de evaluación no está en DRAFT.", "RUBRIC_NOT_EDITABLE", ct),
            RubricCriterionObjectiveMomentMismatchException p => await Problem(r, HttpStatusCode.UnprocessableEntity, "El objetivo y el momento esperado no son coherentes.", p.Code, ct),
            RubricCriterionCodeDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El código del criterio ya existe dentro de la rúbrica.", "RUBRIC_CRITERION_CODE_DUPLICATE", ct),
            RubricCriterionOrderDuplicateException => await Problem(r, HttpStatusCode.Conflict, "El orden contiene duplicados o es inválido.", "RUBRIC_CRITERION_ORDER_DUPLICATE", ct),
            RubricCriterionPreconditionException p => await Problem(r,
                p.Code == "INVALID_STATUS_TRANSITION" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.PreconditionFailed,
                p.Code == "ETAG_REQUIRED" ? "El header If-Match es requerido." : p.Code == "ETAG_MISMATCH" ? "El ETag no coincide." : "Transición de estatus inválida.",
                p.Code, ct),
            RubricCriterionConcurrencyException => await Problem(r, HttpStatusCode.Conflict, "Conflicto de concurrencia.", "CONCURRENCY_CONFLICT", ct),
            _ => await Problem(r, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", "INTERNAL_ERROR", ct),
        };
    }

    protected async Task<HttpResponseData> Auth(HttpRequestData r, string permission, CancellationToken ct) =>
        !User.IsAuthenticated
            ? await Problem(r, HttpStatusCode.Unauthorized, "No autenticado.", "UNAUTHENTICATED", ct)
            : await Problem(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", "FORBIDDEN", ct);
}

public sealed class RubricCriterionCreateFunction : RubricCriterionFunctionBase
{
    private readonly IIdempotencyService _idempotency;

    public RubricCriterionCreateFunction(IRubricCriterionService s, ICurrentUserContext u, IIdempotencyService idempotency, ILogger<RubricCriterionCreateFunction> l)
        : base(s, u, l)
    {
        _idempotency = idempotency;
    }

    [Function("RubricCriterion_Create")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubrics/{idRubric}/criteria")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Create)) return await Auth(r, RubricCriterionPolicies.Create, ct);
        if (!Guid.TryParse(idRubric, out var rubricId)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<CreateRubricCriterionRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricCriterionValidators.ValidateCreate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var idempotencyKey = Header(r, "Idempotency-Key");
            if (!string.IsNullOrWhiteSpace(idempotencyKey) &&
                _idempotency.TryGetResult(User.TenantId, idempotencyKey, out var cached) &&
                cached is RubricCriterionResponse cachedResponse)
            {
                return await Result(r, cachedResponse, HttpStatusCode.Created, ct, $"/api/rubric-criteria/{cachedResponse.IdCriterion}");
            }

            var value = await Service.CreateAsync(User.TenantId, rubricId, User.DisplayName, body, ct);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _idempotency.StoreResult(User.TenantId, idempotencyKey, value);
            }

            Logger.LogInformation("RubricCriterion_Create ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} VersionId={LabVersionId} CriterionId={CriterionId} ObjectiveId={ObjectiveId} ExpectedMomentId={ExpectedMomentId} CorrelationId={CorrelationId} Action={Action} PreviousState={PreviousState} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, rubricId, value.IdVersion, value.IdCriterion, value.ObjectiveId, value.ExpectedMomentId, User.CorrelationId, "CREATE", null, value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.Created, ct, $"/api/rubric-criteria/{value.IdCriterion}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_Create failed. TenantId={TenantId} RubricId={RubricId} CorrelationId={CorrelationId}", User.TenantId, rubricId, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionGetByIdFunction : RubricCriterionFunctionBase
{
    public RubricCriterionGetByIdFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionGetByIdFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_GetById")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rubric-criteria/{idCriterion}")] HttpRequestData r,
        string idCriterion,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Read)) return await Auth(r, RubricCriterionPolicies.Read, ct);
        if (!Guid.TryParse(idCriterion, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idCriterion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var value = await Service.GetByIdAsync(User.TenantId, id, ct);
            if (value is null)
            {
                return await Problem(r, HttpStatusCode.NotFound, "Criterio de evaluación no encontrado.", "RUBRIC_CRITERION_NOT_FOUND", ct);
            }

            Logger.LogInformation("RubricCriterion_GetById ok. TenantId={TenantId} UserId={UserId} CriterionId={CriterionId} CorrelationId={CorrelationId} Action={Action} Result={Result}",
                User.TenantId, User.UserId, id, User.CorrelationId, "GET_BY_ID", "SUCCESS");

            return await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_GetById failed. TenantId={TenantId} CriterionId={CriterionId} CorrelationId={CorrelationId}", User.TenantId, id, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionListByRubricFunction : RubricCriterionFunctionBase
{
    public RubricCriterionListByRubricFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionListByRubricFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_ListByRubric")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rubrics/{idRubric}/criteria")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Read)) return await Auth(r, RubricCriterionPolicies.Read, ct);
        if (!Guid.TryParse(idRubric, out var rubricId)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var q = HttpUtility.ParseQueryString(r.Url.Query);
        var page = int.TryParse(q["page"], out var p) ? p : 1;
        var size = int.TryParse(q["pageSize"], out var ps) ? ps : 50;
        if (page < 1 || size is < 1 or > 100) return await Problem(r, HttpStatusCode.BadRequest, "Paginación inválida.", "REQUEST_INVALID", ct);

        Guid? objectiveId = Guid.TryParse(q["objectiveId"], out var oid) ? oid : null;
        Guid? expectedMomentId = Guid.TryParse(q["expectedMomentId"], out var mid) ? mid : null;
        bool? critico = bool.TryParse(q["critico"], out var crit) ? crit : null;
        var tipoEvidencia = q["tipoEvidencia"];
        var estatus = q["estatus"];
        var sortDirection = q["sortDirection"];

        try
        {
            var result = await Service.ListByRubricAsync(User.TenantId, rubricId, objectiveId, expectedMomentId, tipoEvidencia, critico, estatus, page, size, sortDirection, ct);

            Logger.LogInformation("RubricCriterion_ListByRubric ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} TotalItems={TotalItems} CorrelationId={CorrelationId} Action={Action} Result={Result}",
                User.TenantId, User.UserId, rubricId, result.TotalItems, User.CorrelationId, "LIST", "SUCCESS");

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, result, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_ListByRubric failed. TenantId={TenantId} RubricId={RubricId} CorrelationId={CorrelationId}", User.TenantId, rubricId, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionUpdateFunction : RubricCriterionFunctionBase
{
    public RubricCriterionUpdateFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionUpdateFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_Update")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "rubric-criteria/{idCriterion}")] HttpRequestData r,
        string idCriterion,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Update)) return await Auth(r, RubricCriterionPolicies.Update, ct);
        if (!Guid.TryParse(idCriterion, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idCriterion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<UpdateRubricCriterionRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricCriterionValidators.ValidateUpdate(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.UpdateAsync(User.TenantId, id, etag, User.DisplayName, body, ct);

            Logger.LogInformation("RubricCriterion_Update ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} VersionId={LabVersionId} CriterionId={CriterionId} CorrelationId={CorrelationId} Action={Action} Result={Result}",
                User.TenantId, User.UserId, value.IdRubric, value.IdVersion, value.IdCriterion, User.CorrelationId, "UPDATE", "SUCCESS");

            return await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_Update failed. TenantId={TenantId} CriterionId={CriterionId} CorrelationId={CorrelationId}", User.TenantId, id, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionReorderFunction : RubricCriterionFunctionBase
{
    public RubricCriterionReorderFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionReorderFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_Reorder")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubrics/{idRubric}/criteria/reorder")] HttpRequestData r,
        string idRubric,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Update)) return await Auth(r, RubricCriterionPolicies.Update, ct);
        if (!Guid.TryParse(idRubric, out var rubricId)) return await Problem(r, HttpStatusCode.BadRequest, "idRubric debe ser un GUID válido.", "REQUEST_INVALID", ct);

        try
        {
            var body = await Body<ReorderRubricCriteriaRequest>(r, ct);
            if (body is null) return await Problem(r, HttpStatusCode.BadRequest, "El cuerpo es requerido.", "REQUEST_INVALID", ct);

            var errors = RubricCriterionValidators.ValidateReorder(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.ReorderAsync(User.TenantId, rubricId, User.DisplayName, body, ct);

            Logger.LogInformation("RubricCriterion_Reorder ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} Count={Count} CorrelationId={CorrelationId} Action={Action} Result={Result}",
                User.TenantId, User.UserId, rubricId, value.Items.Count, User.CorrelationId, "REORDER", "SUCCESS");

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, value, User.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_Reorder failed. TenantId={TenantId} RubricId={RubricId} CorrelationId={CorrelationId}", User.TenantId, rubricId, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionActivateFunction : RubricCriterionFunctionBase
{
    public RubricCriterionActivateFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionActivateFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_Activate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubric-criteria/{idCriterion}/activate")] HttpRequestData r,
        string idCriterion,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Activate)) return await Auth(r, RubricCriterionPolicies.Activate, ct);
        if (!Guid.TryParse(idCriterion, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idCriterion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<RubricCriterionActionRequest>(r, ct);
            var errors = RubricCriterionValidators.ValidateAction(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.ActivateAsync(User.TenantId, id, etag, User.DisplayName, body!, ct);

            Logger.LogInformation("RubricCriterion_Activate ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} VersionId={LabVersionId} CriterionId={CriterionId} CorrelationId={CorrelationId} Action={Action} PreviousState={PreviousState} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, value.IdRubric, value.IdVersion, value.IdCriterion, User.CorrelationId, "ACTIVATE", null, value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_Activate failed. TenantId={TenantId} CriterionId={CriterionId} CorrelationId={CorrelationId}", User.TenantId, id, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}

public sealed class RubricCriterionInactivateFunction : RubricCriterionFunctionBase
{
    public RubricCriterionInactivateFunction(IRubricCriterionService s, ICurrentUserContext u, ILogger<RubricCriterionInactivateFunction> l) : base(s, u, l) { }

    [Function("RubricCriterion_Inactivate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rubric-criteria/{idCriterion}/inactivate")] HttpRequestData r,
        string idCriterion,
        CancellationToken ct)
    {
        if (!Authorized(RubricCriterionPolicies.Inactivate)) return await Auth(r, RubricCriterionPolicies.Inactivate, ct);
        if (!Guid.TryParse(idCriterion, out var id)) return await Problem(r, HttpStatusCode.BadRequest, "idCriterion debe ser un GUID válido.", "REQUEST_INVALID", ct);

        var etag = Header(r, "If-Match");
        if (string.IsNullOrWhiteSpace(etag)) return await Problem(r, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", "ETAG_REQUIRED", ct);

        try
        {
            var body = await Body<RubricCriterionActionRequest>(r, ct);
            var errors = RubricCriterionValidators.ValidateAction(body);
            if (errors.Count > 0) return await Problem(r, HttpStatusCode.BadRequest, "Solicitud inválida.", "REQUEST_INVALID", ct, errors);

            var value = await Service.InactivateAsync(User.TenantId, id, etag, User.DisplayName, body!, ct);

            Logger.LogInformation("RubricCriterion_Inactivate ok. TenantId={TenantId} UserId={UserId} RubricId={RubricId} VersionId={LabVersionId} CriterionId={CriterionId} CorrelationId={CorrelationId} Action={Action} PreviousState={PreviousState} NewState={NewState} Result={Result}",
                User.TenantId, User.UserId, value.IdRubric, value.IdVersion, value.IdCriterion, User.CorrelationId, "INACTIVATE", null, value.Estatus, "SUCCESS");

            return await Result(r, value, HttpStatusCode.OK, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RubricCriterion_Inactivate failed. TenantId={TenantId} CriterionId={CriterionId} CorrelationId={CorrelationId}", User.TenantId, id, User.CorrelationId);
            return await Handle(r, ex, ct);
        }
    }
}
