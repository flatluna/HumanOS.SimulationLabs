using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Services;

public sealed class LabDraftSaveService : ILabDraftSaveService
{
    private readonly SimulationLabsDbContext _db;
    private readonly ILogger<LabDraftSaveService> _logger;

    public LabDraftSaveService(SimulationLabsDbContext db, ILogger<LabDraftSaveService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SaveLabDraftResponse> SaveDraftAsync(
        Guid tenantId,
        Guid userId,
        string createdBy,
        SaveLabDraftRequest request,
        CancellationToken cancellationToken)
    {
        var draft = request.Draft;
        if (draft is null)
        {
            throw new ArgumentException("El borrador (Draft) es requerido.");
        }

        var labName = !string.IsNullOrWhiteSpace(request.LabName)
            ? request.LabName.Trim()
            : (!string.IsNullOrWhiteSpace(draft.LabDescription) && draft.LabDescription.Length <= 100
                ? draft.LabDescription
                : "Simulación de Aprendizaje");

        var labId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();
        var rubricId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Perform the entire complex multi-table creation ATOMICALLY in a database transaction.
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Generate unique LAB_Codigo within tenant
                var baseCode = !string.IsNullOrWhiteSpace(request.LabCodigo)
                    ? SanitizeCode(request.LabCodigo)
                    : GenerateCodeFromName(labName);

                var codigo = baseCode;
                int suffix = 1;
                while (await _db.Labs.AsNoTracking().AnyAsync(l => l.SEG_IdTenant == tenantId && l.LAB_Codigo == codigo, cancellationToken))
                {
                    var suffixStr = $"_{suffix++}";
                    codigo = baseCode.Length + suffixStr.Length > 50
                        ? $"{baseCode[..(50 - suffixStr.Length)]}{suffixStr}"
                        : $"{baseCode}{suffixStr}";
            }

            // Prefer the human-authored ScenarioDescription verbatim over the agent's own
            // LabDescription field — the agent must never rewrite/paraphrase a user-provided
            // description, only derive expectations/skills/first question from it.
            var labDescription = !string.IsNullOrWhiteSpace(request.ScenarioDescription)
                ? request.ScenarioDescription
                : (string.IsNullOrWhiteSpace(draft.LabDescription) ? labName : draft.LabDescription);
            var difficulty = NormalizeDifficulty(request.Difficulty);
            var durationMinutes = request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0 ? request.DurationMinutes.Value : 30;
            var scoreMinimo = request.ScoreMinimo.HasValue && request.ScoreMinimo.Value >= 1.00m && request.ScoreMinimo.Value <= 10.00m
                ? request.ScoreMinimo.Value
                : 7.00m;
            var language = !string.IsNullOrWhiteSpace(request.Language) ? request.Language.Trim() : "es-MX";

            // 1. Insert LAB_Lab
            var lab = new LAB_Lab
            {
                LAB_IdLab = labId,
                SEG_IdTenant = tenantId,
                LAB_Codigo = codigo,
                LAB_Nombre = labName.Length > 200 ? labName[..200] : labName,
                LAB_Descripcion = labDescription,
                LAB_Tipo = LabTipos.Conversational,
                LAB_Arquetipo = LabArchetypes.Resolve(request.Archetype).Code,
                LAB_Dominio = !string.IsNullOrWhiteSpace(request.Dominio) ? request.Dominio.Trim() : "SIMULATION",
                LAB_Estatus = LabEstatus.Draft,
                LAB_OwnerId = userId,
                FechaCreacion = now,
                CreadoPor = createdBy,
                GEN_InputTokens = request.Cost?.InputTokens,
                GEN_OutputTokens = request.Cost?.OutputTokens,
                GEN_CachedInputTokens = request.Cost?.CachedInputTokens,
                GEN_ModelName = request.Cost?.ModelName,
                GEN_ElapsedMilliseconds = request.Cost?.ElapsedMilliseconds,
                GEN_EstimatedCostUsd = request.Cost?.EstimatedCostUsd,
            };
            _db.Labs.Add(lab);

            // TestedSkills serialized for persistent retrieval
            var testedSkillsJson = draft.TestedSkills is { Count: > 0 }
                ? JsonSerializer.Serialize(draft.TestedSkills, ApiResponses.JsonOptions)
                : null;

            // 2. Insert LAB_LabVersion
            var labVersion = new LAB_LabVersion
            {
                LAB_IdVersion = versionId,
                LAB_IdLab = labId,
                SEG_IdTenant = tenantId,
                LAB_NumeroVersion = 1,
                LAB_ObjetivoGeneral = labDescription,
                LAB_InstruccionesParticipante = !string.IsNullOrWhiteSpace(draft.ScenarioProblemaCentral)
                    ? draft.ScenarioProblemaCentral
                    : "Conducir la práctica interactiva siguiendo la metodología establecida.",
                LAB_BriefOculto = testedSkillsJson,
                LAB_DuracionMinutos = durationMinutes,
                LAB_ScoreMinimo = scoreMinimo,
                LAB_Estatus = LabVersionEstatus.Draft,
                FechaCreacion = now,
                CreadoPor = createdBy,
            };
            _db.LabVersions.Add(labVersion);

            // 3. Insert LAB_Scenario
            var scenario = new LAB_Scenario
            {
                SCN_IdScenario = scenarioId,
                SEG_IdTenant = tenantId,
                LAB_IdVersion = versionId,
                SCN_Codigo = "SCN_1",
                SCN_Nombre = labName.Length > 150 ? labName[..150] : labName,
                SCN_Descripcion = labDescription,
                SCN_Tipo = ScenarioTipo.Conversational,
                SCN_Dificultad = difficulty,
                SCN_ContextoParticipante = labDescription,
                SCN_BriefOculto = testedSkillsJson ?? "Brief generado por AI Lab Builder",
                SCN_ProblemaCentral = !string.IsNullOrWhiteSpace(draft.ScenarioProblemaCentral)
                    ? draft.ScenarioProblemaCentral
                    : "Situación crítica de práctica profesional.",
                SCN_ResultadoEsperado = !string.IsNullOrWhiteSpace(draft.ScenarioResultadoEsperado)
                    ? draft.ScenarioResultadoEsperado
                    : "Completar la negociación y objetivos del rol satisfactoriamente.",
                SCN_Supuestos = string.IsNullOrWhiteSpace(request.CompanyContext) ? null : request.CompanyContext,
                SCN_Estatus = ScenarioEstatus.Draft,
                SCN_PermiteReintento = true,
                FechaCreacion = now,
                CreadoPor = createdBy,
            };
            _db.Scenarios.Add(scenario);

            // 4. Insert LAB_SimulatedActor
            var actorRefToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            int actorOrder = 1;
            foreach (var a in draft.Actors ?? [])
            {
                var actorId = Guid.NewGuid();
                var aRef = !string.IsNullOrWhiteSpace(a.Ref) ? a.Ref.Trim() : $"ACT_{actorOrder}";
                actorRefToId[aRef] = actorId;

                var actorRole = !string.IsNullOrWhiteSpace(a.Role) ? a.Role.Trim() : $"Actor {actorOrder}";
                var actor = new LAB_SimulatedActor
                {
                    ACT_IdActor = actorId,
                    SEG_IdTenant = tenantId,
                    LAB_IdVersion = versionId,
                    SCN_IdScenario = scenarioId,
                    ACT_Codigo = aRef.Length > 60 ? aRef[..60] : aRef,
                    ACT_Nombre = actorRole.Length > 150 ? actorRole[..150] : actorRole,
                    ACT_Rol = actorRole.Length > 150 ? actorRole[..150] : actorRole,
                    ACT_Tipo = a.IsPrincipal ? ActorTipo.SubjectMatterExpert : ActorTipo.Client,
                    ACT_Descripcion = !string.IsNullOrWhiteSpace(a.Description) ? a.Description : actorRole,
                    ACT_Objetivo = "Interactuar y desafiar constructivamente al estudiante en la simulación.",
                    ACT_ContextoConocido = a.Description ?? string.Empty,
                    ACT_BriefOculto = a.Description ?? string.Empty,
                    ACT_EstiloComunicacion = NormalizeCommunicationStyle(a.CommunicationStyle),
                    ACT_NivelConocimiento = ActorNivelConocimiento.High,
                    ACT_Idioma = language,
                    ACT_EsPrincipal = a.IsPrincipal,
                    ACT_Orden = actorOrder++,
                    ACT_Estatus = ActorEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                };
                _db.SimulatedActors.Add(actor);
            }

            // Fallback actor if none provided
            if (actorRefToId.Count == 0)
            {
                var defaultActorId = Guid.NewGuid();
                actorRefToId["ACT_1"] = defaultActorId;
                _db.SimulatedActors.Add(new LAB_SimulatedActor
                {
                    ACT_IdActor = defaultActorId,
                    SEG_IdTenant = tenantId,
                    LAB_IdVersion = versionId,
                    SCN_IdScenario = scenarioId,
                    ACT_Codigo = "ACT_1",
                    ACT_Nombre = "Interlocutor Principal",
                    ACT_Rol = "Interlocutor",
                    ACT_Tipo = ActorTipo.Client,
                    ACT_Descripcion = "Interlocutor de la simulación",
                    ACT_Objetivo = "Interactuar con el participante",
                    ACT_ContextoConocido = "Información general",
                    ACT_BriefOculto = "Brief de simulación",
                    ACT_EstiloComunicacion = ActorEstiloComunicacion.Direct,
                    ACT_NivelConocimiento = ActorNivelConocimiento.Medium,
                    ACT_Idioma = language,
                    ACT_EsPrincipal = true,
                    ACT_Orden = 1,
                    ACT_Estatus = ActorEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                });
            }

            // 5. Insert LAB_Stage
            var stageRefToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            int stageOrder = 1;
            var stageCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in draft.Stages ?? [])
            {
                var stageId = Guid.NewGuid();
                var sRef = !string.IsNullOrWhiteSpace(s.Ref) ? s.Ref.Trim() : $"STG_{stageOrder}";
                sRef = MakeUniqueCode(sRef, stageCodes, "STG");
                stageCodes.Add(sRef);
                stageRefToId[sRef] = stageId;

                var stageName = !string.IsNullOrWhiteSpace(s.Name) ? s.Name.Trim() : $"Etapa {stageOrder}";
                var stage = new LAB_Stage
                {
                    STG_IdStage = stageId,
                    LAB_IdVersion = versionId,
                    SEG_IdTenant = tenantId,
                    STG_Codigo = sRef.Length > 50 ? sRef[..50] : sRef,
                    STG_Nombre = stageName.Length > 150 ? stageName[..150] : stageName,
                    STG_Descripcion = !string.IsNullOrWhiteSpace(s.Description) ? s.Description : stageName,
                    STG_Orden = stageOrder++,
                    STG_TipoInteraccion = StageTipoInteraccion.Conversation,
                    STG_EsObligatorio = true,
                    STG_Estatus = StageEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                };
                _db.Stages.Add(stage);
            }

            if (stageRefToId.Count == 0)
            {
                var defaultStageId = Guid.NewGuid();
                stageRefToId["STG_1"] = defaultStageId;
                _db.Stages.Add(new LAB_Stage
                {
                    STG_IdStage = defaultStageId,
                    LAB_IdVersion = versionId,
                    SEG_IdTenant = tenantId,
                    STG_Codigo = "STG_1",
                    STG_Nombre = "Fase de Negociación y Cierre",
                    STG_Descripcion = "Fase principal de práctica",
                    STG_Orden = 1,
                    STG_TipoInteraccion = StageTipoInteraccion.Conversation,
                    STG_EsObligatorio = true,
                    STG_Estatus = StageEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                });
            }

            var firstStageId = stageRefToId.Values.First();

            // 6. Insert LAB_Objective
            var objRefToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            int objOrder = 1;
            var objectiveCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in draft.Objectives ?? [])
            {
                var objId = Guid.NewGuid();
                var oRef = !string.IsNullOrWhiteSpace(o.Ref) ? o.Ref.Trim() : $"OBJ_{objOrder}";
                oRef = MakeUniqueCode(oRef, objectiveCodes, "OBJ");
                objectiveCodes.Add(oRef);
                objRefToId[oRef] = objId;

                Guid? stageId = (!string.IsNullOrWhiteSpace(o.StageRef) && stageRefToId.TryGetValue(o.StageRef, out var sid))
                    ? sid
                    : firstStageId;

                var desc = !string.IsNullOrWhiteSpace(o.Description) ? o.Description.Trim() : $"Objetivo {objOrder}";
                var obj = new LAB_Objective
                {
                    OBJ_IdObjective = objId,
                    LAB_IdVersion = versionId,
                    STG_IdStage = stageId,
                    SEG_IdTenant = tenantId,
                    OBJ_Codigo = oRef.Length > 50 ? oRef[..50] : oRef,
                    OBJ_Descripcion = desc,
                    OBJ_TipoEvidencia = ObjectiveTipoEvidencia.Conversation,
                    OBJ_EsCritico = o.IsCritical,
                    OBJ_Peso = o.Weight > 0 ? o.Weight : 20.00m,
                    OBJ_CondicionExito = desc,
                    OBJ_Orden = objOrder++,
                    OBJ_Estatus = ObjectiveEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                };
                _db.Objectives.Add(obj);
            }

            if (objRefToId.Count == 0)
            {
                var defaultObjId = Guid.NewGuid();
                objRefToId["OBJ_1"] = defaultObjId;
                _db.Objectives.Add(new LAB_Objective
                {
                    OBJ_IdObjective = defaultObjId,
                    LAB_IdVersion = versionId,
                    STG_IdStage = firstStageId,
                    SEG_IdTenant = tenantId,
                    OBJ_Codigo = "OBJ_1",
                    OBJ_Descripcion = "Alcanzar acuerdo beneficioso conforme a la metodología",
                    OBJ_TipoEvidencia = ObjectiveTipoEvidencia.Conversation,
                    OBJ_EsCritico = false,
                    OBJ_Peso = 100.00m,
                    OBJ_CondicionExito = "Cierre exitoso sin vulnerar políticas comerciales",
                    OBJ_Orden = 1,
                    OBJ_Estatus = ObjectiveEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                });
            }

            var firstObjId = objRefToId.Values.First();

            // 7. Insert LAB_ExpectedMoment (from Dialogues)
            var momentRefToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var momentObjMap = new Dictionary<Guid, Guid?>();
            int momentOrder = 1;
            var momentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in draft.Dialogues ?? [])
            {
                var momId = Guid.NewGuid();
                var mRef = !string.IsNullOrWhiteSpace(d.Ref) ? d.Ref.Trim() : $"MOM_{momentOrder}";
                mRef = MakeUniqueCode(mRef, momentCodes, "MOM");
                momentCodes.Add(mRef);
                momentRefToId[mRef] = momId;
                momentRefToId[$"#{d.Order}"] = momId;
                momentRefToId[$"#{momentOrder}"] = momId;

                Guid stageId = (!string.IsNullOrWhiteSpace(d.StageRef) && stageRefToId.TryGetValue(d.StageRef, out var sid))
                    ? sid
                    : firstStageId;

                Guid? objId = (!string.IsNullOrWhiteSpace(d.ObjectiveRef) && objRefToId.TryGetValue(d.ObjectiveRef, out var oid))
                    ? oid
                    : firstObjId;

                momentObjMap[momId] = objId;

                var skillName = !string.IsNullOrWhiteSpace(d.SkillEvaluated) ? d.SkillEvaluated.Trim() : $"Momento {momentOrder}";
                var moment = new LAB_ExpectedMoment
                {
                    MOM_IdExpectedMoment = momId,
                    LAB_IdVersion = versionId,
                    STG_IdStage = stageId,
                    OBJ_IdObjective = objId,
                    SEG_IdTenant = tenantId,
                    MOM_Codigo = mRef.Length > 50 ? mRef[..50] : mRef,
                    MOM_Nombre = skillName.Length > 150 ? skillName[..150] : skillName,
                    MOM_Tipo = MomentTipo.Dialogue,
                    MOM_Trigger = !string.IsNullOrWhiteSpace(d.ActorSays) ? d.ActorSays : "Intervención del cliente/actor",
                    MOM_IntencionEsperada = !string.IsNullOrWhiteSpace(d.ExpectedIntent) ? d.ExpectedIntent : "Responder aplicando el método",
                    MOM_RespuestaEjemplar = d.ExemplaryResponse,
                    MOM_ErrorFrecuente = d.FrequentError,
                    MOM_Recomendacion = d.Recommendation,
                    MOM_EsCritico = d.IsCritical,
                    MOM_OrdenSugerido = momentOrder++,
                    MOM_PermiteOrdenFlexible = true,
                    MOM_RequiereRespuesta = true,
                    MOM_Estatus = MomentEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                };
                _db.ExpectedMoments.Add(moment);
            }

            // 8. Insert LAB_Rubric
            var rubricCode = "RUB_1";
            var rubricSuffix = 2;
            while (await _db.Rubrics.AsNoTracking().AnyAsync(r => r.SEG_IdTenant == tenantId && r.RUB_Codigo == rubricCode, cancellationToken))
            {
                rubricCode = $"RUB_{rubricSuffix++}";
            }

            var rubricName = $"Rúbrica de {labName}";
            var rubric = new LAB_Rubric
            {
                RUB_IdRubric = rubricId,
                SEG_IdTenant = tenantId,
                LAB_IdVersion = versionId,
                RUB_Codigo = rubricCode,
                RUB_Nombre = rubricName.Length > 200 ? rubricName[..200] : rubricName,
                RUB_Descripcion = "Rúbrica de evaluación de desempeño generada para simulación.",
                RUB_TipoEvaluacion = RubricTipoEvaluacion.Conversation,
                RUB_EscalaMinima = 1.00m,
                RUB_EscalaMaxima = 10.00m,
                RUB_ScoreMinimoAprobacion = scoreMinimo,
                RUB_MetodoCalculo = RubricMetodoCalculo.WeightedWithCriticalGate,
                RUB_RequiereEvidencia = true,
                RUB_PermiteFallaCritica = true,
                RUB_InstruccionesEvaluador = "Evaluar el desempeño del participante con base en evidencia observable de habilidades técnicas y blandas.",
                RUB_Estatus = RubricEstatus.Draft,
                FechaCreacion = now,
                CreadoPor = createdBy,
            };
            _db.Rubrics.Add(rubric);

            // 9. Insert LAB_RubricCriterion
            var criterionRefToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            int critOrder = 1;
            var criterionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in draft.Criteria ?? [])
            {
                var critId = Guid.NewGuid();
                var cRef = !string.IsNullOrWhiteSpace(c.Ref) ? c.Ref.Trim() : $"CRT_{critOrder}";
                cRef = MakeUniqueCode(cRef, criterionCodes, "CRT");
                criterionCodes.Add(cRef);
                criterionRefToId[cRef] = critId;

                Guid? momId = (!string.IsNullOrWhiteSpace(c.MomentRef) && momentRefToId.TryGetValue(c.MomentRef, out var mid))
                    ? mid
                    : null;

                // Ensure FK coherence between Objective and Moment:
                // If moment has an associated objective, inherit it to guarantee consistency
                Guid? objId = null;
                if (momId.HasValue && momentObjMap.TryGetValue(momId.Value, out var mObjId) && mObjId.HasValue)
                {
                    objId = mObjId;
                }
                else if (!string.IsNullOrWhiteSpace(c.ObjectiveRef) && objRefToId.TryGetValue(c.ObjectiveRef, out var oid))
                {
                    objId = oid;
                }

                var critName = !string.IsNullOrWhiteSpace(c.Name) ? c.Name.Trim() : $"Criterio {critOrder}";
                var criterion = new LAB_RubricCriterion
                {
                    CRT_IdCriterion = critId,
                    RUB_IdRubric = rubricId,
                    LAB_IdVersion = versionId,
                    SEG_IdTenant = tenantId,
                    OBJ_IdObjective = objId,
                    MOM_IdExpectedMoment = momId,
                    CRT_Codigo = cRef.Length > 50 ? cRef[..50] : cRef,
                    CRT_Nombre = critName.Length > 150 ? critName[..150] : critName,
                    CRT_Descripcion = !string.IsNullOrWhiteSpace(c.Description) ? c.Description : critName,
                    CRT_TipoEvidencia = CriterionTipoEvidencia.Conversation,
                    CRT_Peso = c.Weight > 0 ? c.Weight : 20.00m,
                    CRT_ScoreMinimoEsperado = c.MinScore >= 1.00m && c.MinScore <= 10.00m ? c.MinScore : 7.00m,
                    CRT_EsCritico = c.IsCritical,
                    CRT_IndicadoresPositivos = !string.IsNullOrWhiteSpace(c.PositiveIndicators) ? c.PositiveIndicators : "Demuestra dominio observable.",
                    CRT_IndicadoresNegativos = c.NegativeIndicators,
                    CRT_Orden = critOrder++,
                    CRT_Estatus = CriterionEstatus.Draft,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                };
                _db.RubricCriteria.Add(criterion);
            }

            // 10. Insert LAB_TestedSkill — first-class rows for every skill the AI identified as
            // tested by this Lab, replacing the old JSON-blob-only storage (SCN_BriefOculto).
            foreach (var skill in draft.TestedSkills ?? [])
            {
                if (string.IsNullOrWhiteSpace(skill.SkillName)) continue;

                Guid? linkedCriterionId = (!string.IsNullOrWhiteSpace(skill.RubricCriterionRef)
                    && criterionRefToId.TryGetValue(skill.RubricCriterionRef, out var linkedId))
                    ? linkedId
                    : null;

                var skillName = skill.SkillName.Trim();
                _db.TestedSkills.Add(new LAB_TestedSkill
                {
                    SKL_IdSkill = Guid.NewGuid(),
                    SEG_IdTenant = tenantId,
                    LAB_IdVersion = versionId,
                    RUB_IdCriterion = linkedCriterionId,
                    SKL_Nombre = skillName.Length > 200 ? skillName[..200] : skillName,
                    SKL_Tipo = string.Equals(skill.SkillType, "SOFT", StringComparison.OrdinalIgnoreCase) ? TestedSkillTipo.Soft : TestedSkillTipo.Technical,
                    SKL_RelevanceToRole = string.IsNullOrWhiteSpace(skill.RelevanceToRole) ? "No especificado." : skill.RelevanceToRole,
                    SKL_DemonstrationStandard = string.IsNullOrWhiteSpace(skill.DemonstrationStandard) ? "No especificado." : skill.DemonstrationStandard,
                    SKL_FeedbackGuidance = string.IsNullOrWhiteSpace(skill.FeedbackGuidance) ? null : skill.FeedbackGuidance,
                    FechaCreacion = now,
                    CreadoPor = createdBy,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Atomic save of Lab successful. IdLab={IdLab}, Codigo={Codigo}, TenantId={TenantId}", labId, codigo, tenantId);

            return new SaveLabDraftResponse
            {
                IdLab = labId,
                IdVersion = versionId,
                Codigo = codigo,
                Nombre = labName,
                Estatus = LabEstatus.Draft,
                StagesSaved = stageRefToId.Count,
                ObjectivesSaved = objRefToId.Count,
                MomentsSaved = momentRefToId.Count,
                CriteriaSaved = (draft.Criteria?.Count ?? 0),
                ActorsSaved = actorRefToId.Count,
                SkillsRegistered = (draft.TestedSkills?.Count ?? 0),
                Message = "Lab guardado exitosamente con todas sus entidades relacionales.",
            };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to save Lab draft atomically. TenantId={TenantId}", tenantId);
                throw;
            }
        });
    }

    private static string SanitizeCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "LAB_SIM";
        var clean = Regex.Replace(input.Normalize(NormalizationForm.FormD), @"[^a-zA-Z0-9_\s-]", "");
        clean = Regex.Replace(clean, @"[\s-]+", "_").Trim('_').ToUpperInvariant();
        return clean.Length > 50 ? clean[..50] : (string.IsNullOrWhiteSpace(clean) ? "LAB_SIM" : clean);
    }

    private static string MakeUniqueCode(string value, HashSet<string> existing, string prefix)
    {
        var baseCode = SanitizeCode(value);
        if (baseCode.Length > 50) baseCode = baseCode[..50];
        if (string.IsNullOrWhiteSpace(baseCode)) baseCode = prefix;

        var candidate = baseCode;
        var suffix = 2;
        while (existing.Contains(candidate))
        {
            var suffixText = $"_{suffix++}";
            candidate = baseCode.Length + suffixText.Length > 50
                ? $"{baseCode[..(50 - suffixText.Length)]}{suffixText}"
                : $"{baseCode}{suffixText}";
        }

        return candidate;
    }

    private static string GenerateCodeFromName(string name)
    {
        var code = "LAB_" + SanitizeCode(name);
        return code.Length > 50 ? code[..50] : code;
    }

    private static string NormalizeDifficulty(string? diff)
    {
        if (string.IsNullOrWhiteSpace(diff)) return ScenarioDificultad.Intermediate;
        var upper = diff.Trim().ToUpperInvariant();
        return ScenarioDificultad.Allowed.Contains(upper) ? upper : ScenarioDificultad.Intermediate;
    }

    private static string NormalizeCommunicationStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return ActorEstiloComunicacion.Direct;
        var s = style.ToUpperInvariant();
        if (s.Contains("COLLAB") || s.Contains("CALM") || s.Contains("APOY")) return ActorEstiloComunicacion.Collaborative;
        if (s.Contains("EXEC") || s.Contains("DIRECT")) return ActorEstiloComunicacion.Executive;
        if (s.Contains("DETAIL") || s.Contains("ANALIT")) return ActorEstiloComunicacion.DetailOriented;
        if (s.Contains("SKEPT") || s.Contains("ESCEPT")) return ActorEstiloComunicacion.Skeptical;
        if (s.Contains("IMPAT") || s.Contains("URG")) return ActorEstiloComunicacion.Impatient;
        if (s.Contains("RESERV") || s.Contains("TIMID")) return ActorEstiloComunicacion.Reserved;
        return ActorEstiloComunicacion.Direct;
    }
}
