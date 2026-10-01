using HumanOS.SimulationLabs.Api.Features.Scenarios.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Scenarios.Services;

public sealed class ScenarioService : IScenarioService
{
    private readonly SimulationLabsDbContext _db;
    public ScenarioService(SimulationLabsDbContext db) => _db = db;

    public async Task<ScenarioResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateScenarioRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct) ?? throw new ScenarioVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);
        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.Scenarios.AnyAsync(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId && s.SCN_Codigo == code, ct)) throw new ScenarioCodeDuplicateException();
        var scenario = new LAB_Scenario
        {
            SCN_IdScenario = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = versionId,
            SCN_Codigo = code,
            SCN_Nombre = request.Nombre!.Trim(),
            SCN_Descripcion = request.Descripcion!.Trim(),
            SCN_Tipo = request.Tipo!.Trim().ToUpperInvariant(),
            SCN_Dificultad = request.Dificultad!.Trim().ToUpperInvariant(),
            SCN_ContextoParticipante = request.ContextoParticipante!.Trim(),
            SCN_BriefOculto = request.BriefOculto!.Trim(),
            SCN_ProblemaCentral = request.ProblemaCentral!.Trim(),
            SCN_ResultadoEsperado = request.ResultadoEsperado!.Trim(),
            SCN_CondicionesIniciales = Clean(request.CondicionesIniciales),
            SCN_Restricciones = Clean(request.Restricciones),
            SCN_Supuestos = Clean(request.Supuestos),
            SCN_Riesgos = Clean(request.Riesgos),
            SCN_InformacionNoRevelarAutomaticamente = Clean(request.InformacionNoRevelarAutomaticamente),
            SCN_MensajeInicial = Clean(request.MensajeInicial),
            SCN_DuracionSugeridaMinutos = request.DuracionSugeridaMinutos,
            SCN_PuntuacionObjetivo = request.PuntuacionObjetivo,
            SCN_PermiteReintento = request.PermiteReintento ?? true,
            SCN_MaximoIntentos = request.MaximoIntentos,
            SCN_UsaVariacion = request.UsaVariacion ?? false,
            SCN_SeedBase = Clean(request.SeedBase),
            SCN_Estatus = ScenarioEstatus.Draft,
            SCN_VigenciaDesde = request.VigenciaDesde,
            SCN_VigenciaHasta = request.VigenciaHasta,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user,
        };
        _db.Scenarios.Add(scenario);
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (IsUnique(ex)) { throw new ScenarioCodeDuplicateException(); }
        return ToResponse(scenario);
    }

    public async Task<ScenarioResponse?> GetByIdAsync(Guid tenantId, Guid scenarioId, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.AsNoTracking().FirstOrDefaultAsync(s => s.SCN_IdScenario == scenarioId, ct);
        if (scenario is null) return null;
        if (scenario.SEG_IdTenant != tenantId && !await IsVersionGlobalAsync(scenario.LAB_IdVersion, ct)) return null;
        return ToResponse(scenario);
    }

    public async Task<ScenarioListResponse> ListAsync(Guid tenantId, Guid versionId, string? tipo, string? dificultad, string? estatus, string? search, int page, int pageSize, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.LAB_IdVersion == versionId, ct) ?? throw new ScenarioVersionNotFoundException();
        if (version.SEG_IdTenant != tenantId && !await IsVersionGlobalAsync(versionId, ct)) throw new ScenarioVersionNotFoundException();
        var query = _db.Scenarios.AsNoTracking().Where(s => s.SEG_IdTenant == version.SEG_IdTenant && s.LAB_IdVersion == versionId);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(s => s.SCN_Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(dificultad)) query = query.Where(s => s.SCN_Dificultad == dificultad);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(s => s.SCN_Estatus == estatus);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => s.SCN_Nombre.Contains(search) || s.SCN_Codigo.Contains(search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.SCN_Codigo).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ScenarioListItemResponse { IdScenario = s.SCN_IdScenario, Codigo = s.SCN_Codigo, Nombre = s.SCN_Nombre, Tipo = s.SCN_Tipo, Dificultad = s.SCN_Dificultad, Estatus = s.SCN_Estatus, DuracionSugeridaMinutos = s.SCN_DuracionSugeridaMinutos, RowVersion = Convert.ToBase64String(s.RowVersion) })
            .ToListAsync(ct);
        return new ScenarioListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<ScenarioResponse> UpdateAsync(Guid tenantId, Guid scenarioId, string etag, string user, UpdateScenarioRequest request, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.Include(s => s.LabVersion).FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new ScenarioNotFoundException();
        EnsureDraft(scenario.LabVersion?.LAB_Estatus);
        EnsureEtag(scenario.RowVersion, etag);
        if (scenario.SCN_Estatus != ScenarioEstatus.Draft) throw new ScenarioNotEditableException();

        if (request.Nombre is not null) scenario.SCN_Nombre = request.Nombre.Trim();
        if (request.Descripcion is not null) scenario.SCN_Descripcion = request.Descripcion.Trim();
        if (request.Tipo is not null) scenario.SCN_Tipo = request.Tipo.Trim().ToUpperInvariant();
        if (request.Dificultad is not null) scenario.SCN_Dificultad = request.Dificultad.Trim().ToUpperInvariant();
        if (request.ContextoParticipante is not null) scenario.SCN_ContextoParticipante = request.ContextoParticipante.Trim();
        if (request.BriefOculto is not null) scenario.SCN_BriefOculto = request.BriefOculto.Trim();
        if (request.ProblemaCentral is not null) scenario.SCN_ProblemaCentral = request.ProblemaCentral.Trim();
        if (request.ResultadoEsperado is not null) scenario.SCN_ResultadoEsperado = request.ResultadoEsperado.Trim();
        if (request.CondicionesIniciales is not null) scenario.SCN_CondicionesIniciales = Clean(request.CondicionesIniciales);
        if (request.Restricciones is not null) scenario.SCN_Restricciones = Clean(request.Restricciones);
        if (request.Supuestos is not null) scenario.SCN_Supuestos = Clean(request.Supuestos);
        if (request.Riesgos is not null) scenario.SCN_Riesgos = Clean(request.Riesgos);
        if (request.InformacionNoRevelarAutomaticamente is not null) scenario.SCN_InformacionNoRevelarAutomaticamente = Clean(request.InformacionNoRevelarAutomaticamente);
        if (request.MensajeInicial is not null) scenario.SCN_MensajeInicial = Clean(request.MensajeInicial);
        if (request.DuracionSugeridaMinutos is not null) scenario.SCN_DuracionSugeridaMinutos = request.DuracionSugeridaMinutos;
        if (request.PuntuacionObjetivo is not null) scenario.SCN_PuntuacionObjetivo = request.PuntuacionObjetivo;
        if (request.PermiteReintento.HasValue) scenario.SCN_PermiteReintento = request.PermiteReintento.Value;
        if (request.MaximoIntentos is not null) scenario.SCN_MaximoIntentos = request.MaximoIntentos;
        if (request.UsaVariacion.HasValue) scenario.SCN_UsaVariacion = request.UsaVariacion.Value;
        if (request.SeedBase is not null) scenario.SCN_SeedBase = Clean(request.SeedBase);
        if (request.VigenciaDesde is not null) scenario.SCN_VigenciaDesde = request.VigenciaDesde;
        if (request.VigenciaHasta is not null) scenario.SCN_VigenciaHasta = request.VigenciaHasta;

        Touch(scenario, user);
        await SaveAsync(ct);
        return ToResponse(scenario);
    }

    public Task<ScenarioResponse> ApproveAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, scenarioId, etag, user, ScenarioEstatus.Draft, ScenarioEstatus.Approved, ct);

    public Task<ScenarioResponse> PublishAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, scenarioId, etag, user, ScenarioEstatus.Approved, ScenarioEstatus.Published, ct);

    public async Task<ScenarioResponse> RetireAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new ScenarioNotFoundException();
        EnsureEtag(scenario.RowVersion, etag);
        if (scenario.SCN_Estatus is not (ScenarioEstatus.Draft or ScenarioEstatus.Approved or ScenarioEstatus.Published)) throw new ScenarioPreconditionException("INVALID_STATUS_TRANSITION");
        scenario.SCN_Estatus = ScenarioEstatus.Retired;
        Touch(scenario, user);
        await SaveAsync(ct);
        return ToResponse(scenario);
    }

    /// <summary>Lets a Studio admin edit an already Approved/Published scenario again (content
    /// fields only, e.g. job description) by moving it back to DRAFT — must be re-approved and
    /// re-published afterward, real employees stop seeing it as an active Attempt target meanwhile.</summary>
    public async Task<ScenarioResponse> RevertToDraftAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new ScenarioNotFoundException();
        EnsureEtag(scenario.RowVersion, etag);
        if (scenario.SCN_Estatus is not (ScenarioEstatus.Approved or ScenarioEstatus.Published)) throw new ScenarioPreconditionException("INVALID_STATUS_TRANSITION");
        scenario.SCN_Estatus = ScenarioEstatus.Draft;
        Touch(scenario, user);
        await SaveAsync(ct);
        return ToResponse(scenario);
    }

    private async Task<ScenarioResponse> TransitionAsync(Guid tenantId, Guid scenarioId, string etag, string user, string fromStatus, string toStatus, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new ScenarioNotFoundException();
        EnsureEtag(scenario.RowVersion, etag);
        if (scenario.SCN_Estatus != fromStatus) throw new ScenarioPreconditionException("INVALID_STATUS_TRANSITION");
        scenario.SCN_Estatus = toStatus;
        Touch(scenario, user);
        await SaveAsync(ct);
        return ToResponse(scenario);
    }

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ScenarioConcurrencyException(); } }
    private static void EnsureDraft(string? status) { if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase)) throw new ScenarioVersionNotEditableException(); }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new ScenarioPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new ScenarioPreconditionException("ETAG_MISMATCH"); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> IsVersionGlobalAsync(Guid versionId, CancellationToken ct) =>
        await (from v in _db.LabVersions.AsNoTracking()
               join l in _db.Labs.AsNoTracking() on v.LAB_IdLab equals l.LAB_IdLab
               where v.LAB_IdVersion == versionId && l.LAB_EsGlobal
               select l.LAB_IdLab).AnyAsync(ct);
    private static void Touch(LAB_Scenario s, string user) { s.FechaActualizacion = DateTimeOffset.UtcNow; s.ActualizadoPor = user; }
    private static bool IsUnique(DbUpdateException ex) => ex.InnerException?.Message.Contains("UQ_LAB_Scenario", StringComparison.OrdinalIgnoreCase) == true;

    private static ScenarioResponse ToResponse(LAB_Scenario s) => new()
    {
        IdScenario = s.SCN_IdScenario,
        IdVersion = s.LAB_IdVersion,
        Codigo = s.SCN_Codigo,
        Nombre = s.SCN_Nombre,
        Descripcion = s.SCN_Descripcion,
        Tipo = s.SCN_Tipo,
        Dificultad = s.SCN_Dificultad,
        ContextoParticipante = s.SCN_ContextoParticipante,
        BriefOculto = s.SCN_BriefOculto,
        ProblemaCentral = s.SCN_ProblemaCentral,
        ResultadoEsperado = s.SCN_ResultadoEsperado,
        CondicionesIniciales = s.SCN_CondicionesIniciales,
        Restricciones = s.SCN_Restricciones,
        Supuestos = s.SCN_Supuestos,
        Riesgos = s.SCN_Riesgos,
        InformacionNoRevelarAutomaticamente = s.SCN_InformacionNoRevelarAutomaticamente,
        MensajeInicial = s.SCN_MensajeInicial,
        DuracionSugeridaMinutos = s.SCN_DuracionSugeridaMinutos,
        PuntuacionObjetivo = s.SCN_PuntuacionObjetivo,
        PermiteReintento = s.SCN_PermiteReintento,
        MaximoIntentos = s.SCN_MaximoIntentos,
        UsaVariacion = s.SCN_UsaVariacion,
        SeedBase = s.SCN_SeedBase,
        Estatus = s.SCN_Estatus,
        VigenciaDesde = s.SCN_VigenciaDesde,
        VigenciaHasta = s.SCN_VigenciaHasta,
        FechaCreacion = s.FechaCreacion,
        CreadoPor = s.CreadoPor,
        FechaActualizacion = s.FechaActualizacion,
        ActualizadoPor = s.ActualizadoPor,
        RowVersion = Convert.ToBase64String(s.RowVersion),
    };
}
