using Karry.Application.Common;
using Karry.Domain.Units;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Units.Commands;

public sealed record SetUnitPreferencesRequest(string? MassUnit, string? VolumeUnit);

public sealed record SetUnitPreferencesCommand(SetUnitPreferencesRequest Input) : IRequest<Unit>;

public sealed class SetUnitPreferencesCommandHandler : IRequestHandler<SetUnitPreferencesCommand, Unit>
{
    private readonly IRepository<UserUnitPreference> _userPrefs;
    private readonly ICurrentSession _session;
    private readonly IUnitOfWork _unitOfWork;

    public SetUnitPreferencesCommandHandler(
        IRepository<UserUnitPreference> userPrefs,
        ICurrentSession session,
        IUnitOfWork unitOfWork)
    {
        _userPrefs = userPrefs;
        _session = session;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(SetUnitPreferencesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _session.TenantId ?? throw new ForbiddenException("Unit preferences require a tenant.");
        var userId = _session.UserId ?? throw new ForbiddenException("Not authenticated.");

        var mass = ParseMass(request.Input.MassUnit);
        var volume = ParseVolume(request.Input.VolumeUnit);

        var existing = await _userPrefs.FirstOrDefaultAsync(
            p => p.TenantId == tenantId && p.UserId == userId, cancellationToken);

        if (existing is null)
        {
            var prefs = UserUnitPreference.Create(tenantId, userId, mass, volume);
            await _userPrefs.AddAsync(prefs, cancellationToken);
        }
        else
        {
            existing.Set(mass, volume);
            _userPrefs.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static MassUnit? ParseMass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "t" or "metric_t" or "metricton" => MassUnit.MetricTon,
            "st" or "short_t" or "shortton" => MassUnit.ShortTon,
            _ => throw new ConflictException($"Unsupported mass unit '{value}'."),
        };
    }

    private static VolumeUnit ParseVolume(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return VolumeUnit.CubicMeter;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "m3" or "cubic_meter" => VolumeUnit.CubicMeter,
            _ => throw new ConflictException($"Unsupported volume unit '{value}'."),
        };
    }
}