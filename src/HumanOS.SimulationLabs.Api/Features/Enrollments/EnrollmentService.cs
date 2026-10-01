using HumanOS.SimulationLabs.Api.Features.Labs;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Enrollments;

public sealed class EnrollmentService
{
    private readonly SimulationLabsDbContext _db;

    public EnrollmentService(SimulationLabsDbContext db) => _db = db;

    public async Task<EnrollmentResponse> EnrollAsync(Guid tenantId, Guid idLab, Guid participantId, string user, CancellationToken ct)
    {
        var labExists = await _db.Labs.AsNoTracking().AnyAsync(l => (l.SEG_IdTenant == tenantId || l.LAB_EsGlobal) && l.LAB_IdLab == idLab, ct);
        if (!labExists) throw new LabNotFoundException();

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.SEG_IdTenant == tenantId && e.LAB_IdLab == idLab && e.USR_IdParticipant == participantId, ct);

        var now = DateTimeOffset.UtcNow;
        if (existing is not null)
        {
            existing.ENR_Estatus = EnrollmentEstatus.Enrolled;
            existing.ENR_FechaInscripcion = now;
            existing.ENR_FechaCancelacion = null;
            existing.FechaActualizacion = now;
            existing.ActualizadoPor = user;
            await _db.SaveChangesAsync(ct);
            return ToResponse(existing);
        }

        var enrollment = new LAB_Enrollment
        {
            ENR_IdEnrollment = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdLab = idLab,
            USR_IdParticipant = participantId,
            ENR_Estatus = EnrollmentEstatus.Enrolled,
            ENR_FechaInscripcion = now,
            FechaCreacion = now,
            CreadoPor = user,
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);
        return ToResponse(enrollment);
    }

    public async Task<EnrollmentResponse> UnenrollAsync(Guid tenantId, Guid idLab, Guid participantId, string user, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.SEG_IdTenant == tenantId && e.LAB_IdLab == idLab && e.USR_IdParticipant == participantId, ct)
            ?? throw new EnrollmentNotFoundException();

        var now = DateTimeOffset.UtcNow;
        enrollment.ENR_Estatus = EnrollmentEstatus.Cancelled;
        enrollment.ENR_FechaCancelacion = now;
        enrollment.FechaActualizacion = now;
        enrollment.ActualizadoPor = user;
        await _db.SaveChangesAsync(ct);
        return ToResponse(enrollment);
    }

    public async Task<List<EnrollmentResponse>> ListMineAsync(Guid tenantId, Guid participantId, CancellationToken ct)
    {
        var items = await _db.Enrollments.AsNoTracking()
            .Where(e => e.SEG_IdTenant == tenantId && e.USR_IdParticipant == participantId && e.ENR_Estatus == EnrollmentEstatus.Enrolled)
            .OrderByDescending(e => e.ENR_FechaInscripcion)
            .ToListAsync(ct);
        return items.Select(ToResponse).ToList();
    }

    private static EnrollmentResponse ToResponse(LAB_Enrollment e) => new()
    {
        IdEnrollment = e.ENR_IdEnrollment,
        IdLab = e.LAB_IdLab,
        Estatus = e.ENR_Estatus,
        FechaInscripcion = e.ENR_FechaInscripcion,
        FechaCancelacion = e.ENR_FechaCancelacion,
        RowVersion = Convert.ToBase64String(e.RowVersion),
    };
}
