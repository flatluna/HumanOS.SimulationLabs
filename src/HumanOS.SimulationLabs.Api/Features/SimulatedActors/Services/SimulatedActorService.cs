using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors.Services;

public sealed class SimulatedActorService : ISimulatedActorService
{
    private readonly SimulationLabsDbContext _db;
    public SimulatedActorService(SimulationLabsDbContext db) => _db = db;

    public async Task<SimulatedActorResponse> CreateAsync(Guid tenantId, Guid scenarioId, string user, CreateSimulatedActorRequest request, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.Include(s => s.LabVersion).AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new SimulatedActorScenarioNotFoundException();
        EnsureEditable(scenario);
        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.SimulatedActors.AnyAsync(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId && a.ACT_Codigo == code, ct)) throw new SimulatedActorCodeDuplicateException();
        var isPrincipal = request.EsPrincipal ?? false;
        if (isPrincipal && await _db.SimulatedActors.AnyAsync(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId && a.ACT_EsPrincipal, ct)) throw new SimulatedActorPrincipalAlreadyExistsException();
        var order = (await _db.SimulatedActors.Where(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId).MaxAsync(a => (int?)a.ACT_Orden, ct) ?? 0) + 1;
        var actor = new LAB_SimulatedActor
        {
            ACT_IdActor = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = scenario.LAB_IdVersion,
            SCN_IdScenario = scenarioId,
            ACT_Codigo = code,
            ACT_Nombre = request.Nombre!.Trim(),
            ACT_Rol = request.Rol!.Trim(),
            ACT_Tipo = request.Tipo!.Trim().ToUpperInvariant(),
            ACT_Descripcion = request.Descripcion!.Trim(),
            ACT_Objetivo = request.Objetivo!.Trim(),
            ACT_ContextoConocido = request.ContextoConocido!.Trim(),
            ACT_BriefOculto = request.BriefOculto!.Trim(),
            ACT_InformacionPuedeRevelar = Clean(request.InformacionPuedeRevelar),
            ACT_InformacionNoRevelarAutomaticamente = Clean(request.InformacionNoRevelarAutomaticamente),
            ACT_Restricciones = Clean(request.Restricciones),
            ACT_Objeciones = Clean(request.Objeciones),
            ACT_Contradicciones = Clean(request.Contradicciones),
            ACT_EstiloComunicacion = request.EstiloComunicacion!.Trim().ToUpperInvariant(),
            ACT_NivelConocimiento = request.NivelConocimiento!.Trim().ToUpperInvariant(),
            ACT_Idioma = request.Idioma!.Trim(),
            ACT_VoiceName = Clean(request.VoiceName),
            ACT_MensajeInicial = Clean(request.MensajeInicial),
            ACT_PuedeIniciarConversacion = request.PuedeIniciarConversacion ?? false,
            ACT_EsPrincipal = isPrincipal,
            ACT_Orden = order,
            ACT_Estatus = ActorEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user,
        };
        _db.SimulatedActors.Add(actor);
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (IsUnique(ex)) { throw new SimulatedActorCodeDuplicateException(); }
        return ToResponse(actor);
    }

    public async Task<SimulatedActorResponse?> GetByIdAsync(Guid tenantId, Guid actorId, CancellationToken ct) =>
        (await _db.SimulatedActors.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdActor == actorId, ct)) is { } actor ? ToResponse(actor) : null;

    public async Task<SimulatedActorListResponse> ListAsync(Guid tenantId, Guid scenarioId, string? tipo, string? estiloComunicacion, string? nivelConocimiento, bool? principal, string? estatus, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.Scenarios.AsNoTracking().AnyAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct)) throw new SimulatedActorScenarioNotFoundException();
        var query = _db.SimulatedActors.AsNoTracking().Where(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(a => a.ACT_Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(estiloComunicacion)) query = query.Where(a => a.ACT_EstiloComunicacion == estiloComunicacion);
        if (!string.IsNullOrWhiteSpace(nivelConocimiento)) query = query.Where(a => a.ACT_NivelConocimiento == nivelConocimiento);
        if (principal.HasValue) query = query.Where(a => a.ACT_EsPrincipal == principal.Value);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(a => a.ACT_Estatus == estatus);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.ACT_Orden).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new SimulatedActorListItemResponse { IdActor = a.ACT_IdActor, Codigo = a.ACT_Codigo, Nombre = a.ACT_Nombre, Tipo = a.ACT_Tipo, EstiloComunicacion = a.ACT_EstiloComunicacion, NivelConocimiento = a.ACT_NivelConocimiento, EsPrincipal = a.ACT_EsPrincipal, Orden = a.ACT_Orden, Estatus = a.ACT_Estatus, RowVersion = Convert.ToBase64String(a.RowVersion) })
            .ToListAsync(ct);
        return new SimulatedActorListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<SimulatedActorResponse> UpdateAsync(Guid tenantId, Guid actorId, string etag, string user, UpdateSimulatedActorRequest request, CancellationToken ct)
    {
        var actor = await _db.SimulatedActors.Include(a => a.Scenario!).ThenInclude(s => s.LabVersion).FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdActor == actorId, ct) ?? throw new SimulatedActorNotFoundException();
        EnsureEditable(actor.Scenario!);
        EnsureEtag(actor.RowVersion, etag);

        if (request.EsPrincipal == true && !actor.ACT_EsPrincipal &&
            await _db.SimulatedActors.AnyAsync(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == actor.SCN_IdScenario && a.ACT_EsPrincipal && a.ACT_IdActor != actorId, ct))
        {
            throw new SimulatedActorPrincipalAlreadyExistsException();
        }

        if (request.Nombre is not null) actor.ACT_Nombre = request.Nombre.Trim();
        if (request.Rol is not null) actor.ACT_Rol = request.Rol.Trim();
        if (request.Tipo is not null) actor.ACT_Tipo = request.Tipo.Trim().ToUpperInvariant();
        if (request.Descripcion is not null) actor.ACT_Descripcion = request.Descripcion.Trim();
        if (request.Objetivo is not null) actor.ACT_Objetivo = request.Objetivo.Trim();
        if (request.ContextoConocido is not null) actor.ACT_ContextoConocido = request.ContextoConocido.Trim();
        if (request.BriefOculto is not null) actor.ACT_BriefOculto = request.BriefOculto.Trim();
        if (request.InformacionPuedeRevelar is not null) actor.ACT_InformacionPuedeRevelar = Clean(request.InformacionPuedeRevelar);
        if (request.InformacionNoRevelarAutomaticamente is not null) actor.ACT_InformacionNoRevelarAutomaticamente = Clean(request.InformacionNoRevelarAutomaticamente);
        if (request.Restricciones is not null) actor.ACT_Restricciones = Clean(request.Restricciones);
        if (request.Objeciones is not null) actor.ACT_Objeciones = Clean(request.Objeciones);
        if (request.Contradicciones is not null) actor.ACT_Contradicciones = Clean(request.Contradicciones);
        if (request.EstiloComunicacion is not null) actor.ACT_EstiloComunicacion = request.EstiloComunicacion.Trim().ToUpperInvariant();
        if (request.NivelConocimiento is not null) actor.ACT_NivelConocimiento = request.NivelConocimiento.Trim().ToUpperInvariant();
        if (request.Idioma is not null) actor.ACT_Idioma = request.Idioma.Trim();
        if (request.VoiceName is not null) actor.ACT_VoiceName = Clean(request.VoiceName);
        if (request.MensajeInicial is not null) actor.ACT_MensajeInicial = Clean(request.MensajeInicial);
        if (request.PuedeIniciarConversacion.HasValue) actor.ACT_PuedeIniciarConversacion = request.PuedeIniciarConversacion.Value;
        if (request.EsPrincipal.HasValue) actor.ACT_EsPrincipal = request.EsPrincipal.Value;

        Touch(actor, user);
        await SaveAsync(ct);
        return ToResponse(actor);
    }

    public async Task<ReorderSimulatedActorsResponse> ReorderAsync(Guid tenantId, Guid scenarioId, string user, ReorderSimulatedActorsRequest request, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.Include(s => s.LabVersion).AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == scenarioId, ct) ?? throw new SimulatedActorScenarioNotFoundException();
        EnsureEditable(scenario);
        var input = request.Actors!;
        if (input.Select(x => x.IdActor).Distinct().Count() != input.Count || input.Select(x => x.Orden).Distinct().Count() != input.Count || input.Any(x => x.Orden <= 0)) throw new SimulatedActorOrderDuplicateException();
        var ids = input.Select(x => x.IdActor).ToArray();
        var actors = await _db.SimulatedActors.Where(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId && ids.Contains(a.ACT_IdActor)).ToListAsync(ct);
        if (actors.Count != input.Count) throw new SimulatedActorNotFoundException();
        foreach (var item in input) EnsureEtag(actors.Single(a => a.ACT_IdActor == item.IdActor).RowVersion, item.RowVersion);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var offset = Math.Max(actors.Max(a => a.ACT_Orden), input.Max(x => x.Orden)) + actors.Count + 1;
            foreach (var actor in actors) { actor.ACT_Orden += offset; Touch(actor, user); }
            await _db.SaveChangesAsync(ct);
            foreach (var item in input) { var actor = actors.Single(a => a.ACT_IdActor == item.IdActor); actor.ACT_Orden = item.Orden; }
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { await tx.RollbackAsync(ct); throw new SimulatedActorConcurrencyException(); }
        catch (DbUpdateException ex) when (IsUnique(ex)) { await tx.RollbackAsync(ct); throw new SimulatedActorOrderDuplicateException(); }
        return new ReorderSimulatedActorsResponse { Items = actors.OrderBy(a => a.ACT_Orden).Select(a => new ReorderSimulatedActorResponseItem { IdActor = a.ACT_IdActor, Orden = a.ACT_Orden, RowVersion = Convert.ToBase64String(a.RowVersion) }).ToList() };
    }

    public Task<SimulatedActorResponse> ActivateAsync(Guid tenantId, Guid actorId, string etag, string user, CancellationToken ct) => SetStatusAsync(tenantId, actorId, etag, user, ActorEstatus.Active, ct);

    public Task<SimulatedActorResponse> InactivateAsync(Guid tenantId, Guid actorId, string etag, string user, CancellationToken ct) => SetStatusAsync(tenantId, actorId, etag, user, ActorEstatus.Inactive, ct);

    private async Task<SimulatedActorResponse> SetStatusAsync(Guid tenantId, Guid actorId, string etag, string user, string targetStatus, CancellationToken ct)
    {
        var actor = await _db.SimulatedActors.FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdActor == actorId, ct) ?? throw new SimulatedActorNotFoundException();
        EnsureEtag(actor.RowVersion, etag);
        if (targetStatus == ActorEstatus.Active && actor.ACT_EsPrincipal &&
            await _db.SimulatedActors.AnyAsync(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == actor.SCN_IdScenario && a.ACT_IdActor != actorId && a.ACT_EsPrincipal && a.ACT_Estatus == ActorEstatus.Active, ct))
        {
            throw new SimulatedActorPrincipalAlreadyExistsException();
        }
        if (actor.ACT_Estatus != targetStatus) { actor.ACT_Estatus = targetStatus; Touch(actor, user); await SaveAsync(ct); }
        return ToResponse(actor);
    }

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new SimulatedActorConcurrencyException(); } }
    private static void EnsureEditable(LAB_Scenario scenario)
    {
        if (!string.Equals(scenario.LabVersion?.LAB_Estatus, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase)) throw new SimulatedActorVersionNotEditableException();
        if (!string.Equals(scenario.SCN_Estatus, ScenarioEstatus.Draft, StringComparison.OrdinalIgnoreCase)) throw new SimulatedActorScenarioNotEditableException();
    }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new SimulatedActorPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new SimulatedActorPreconditionException("ETAG_MISMATCH"); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Touch(LAB_SimulatedActor a, string user) { a.FechaActualizacion = DateTimeOffset.UtcNow; a.ActualizadoPor = user; }
    private static bool IsUnique(DbUpdateException ex) => ex.InnerException?.Message.Contains("UQ_LAB_SimulatedActor", StringComparison.OrdinalIgnoreCase) == true;

    private static SimulatedActorResponse ToResponse(LAB_SimulatedActor a) => new()
    {
        IdActor = a.ACT_IdActor,
        IdVersion = a.LAB_IdVersion,
        IdScenario = a.SCN_IdScenario,
        Codigo = a.ACT_Codigo,
        Nombre = a.ACT_Nombre,
        Rol = a.ACT_Rol,
        Tipo = a.ACT_Tipo,
        Descripcion = a.ACT_Descripcion,
        Objetivo = a.ACT_Objetivo,
        ContextoConocido = a.ACT_ContextoConocido,
        BriefOculto = a.ACT_BriefOculto,
        InformacionPuedeRevelar = a.ACT_InformacionPuedeRevelar,
        InformacionNoRevelarAutomaticamente = a.ACT_InformacionNoRevelarAutomaticamente,
        Restricciones = a.ACT_Restricciones,
        Objeciones = a.ACT_Objeciones,
        Contradicciones = a.ACT_Contradicciones,
        EstiloComunicacion = a.ACT_EstiloComunicacion,
        NivelConocimiento = a.ACT_NivelConocimiento,
        Idioma = a.ACT_Idioma,
        VoiceName = a.ACT_VoiceName,
        MensajeInicial = a.ACT_MensajeInicial,
        PuedeIniciarConversacion = a.ACT_PuedeIniciarConversacion,
        EsPrincipal = a.ACT_EsPrincipal,
        Orden = a.ACT_Orden,
        Estatus = a.ACT_Estatus,
        FechaCreacion = a.FechaCreacion,
        CreadoPor = a.CreadoPor,
        FechaActualizacion = a.FechaActualizacion,
        ActualizadoPor = a.ActualizadoPor,
        RowVersion = Convert.ToBase64String(a.RowVersion),
    };
}
