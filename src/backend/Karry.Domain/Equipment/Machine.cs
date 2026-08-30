using Karry.Domain.Common;

namespace Karry.Domain.Equipment;

public enum MachineType
{
    Crusher = 0,
    Screen = 1,
    Washer = 2,
    Loader = 3,
    Conveyor = 4,
    HaulTruck = 5,
    Excavator = 6,
}

/// <summary>
/// Abstracted "Graph Node Engine" (ℳ_e) from the codex: a single machine carries
/// its type, tracked wear parts, and downstream routing edges (E_out).
/// </summary>
public sealed class Machine : BaseEntity, IAuditableEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid SiteId { get; private set; }

    public string Name { get; private set; } = default!;

    public MachineType Type { get; private set; }

    public string Model { get; private set; } = default!;

    public string SerialNumber { get; private set; } = default!;

    public double AccumulatedHours { get; private set; }

    public double AccumulatedKilometers { get; private set; }

    private readonly List<Guid> _downstreamNodeIds = [];
    public IReadOnlyList<Guid> DownstreamNodeIds => _downstreamNodeIds.AsReadOnly();

    public Guid CreatedBy { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    private Machine()
    {
    }

    public static Machine Create(Guid tenantId, Guid siteId, string name, MachineType type, string model, string serialNumber, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Machine name is required.", nameof(name));
        }

        return new Machine
        {
            TenantId = tenantId,
            SiteId = siteId,
            Name = name.Trim(),
            Type = type,
            Model = model.Trim(),
            SerialNumber = serialNumber.Trim(),
            CreatedBy = createdBy,
        };
    }

    public void ConnectTo(Guid downstreamMachineId)
    {
        if (downstreamMachineId == Id)
        {
            throw new InvalidOperationException("A machine cannot be a downstream edge of itself.");
        }

        if (!_downstreamNodeIds.Contains(downstreamMachineId))
        {
            _downstreamNodeIds.Add(downstreamMachineId);
            MarkUpdated();
        }
    }

    public void DisconnectFrom(Guid downstreamMachineId)
    {
        if (_downstreamNodeIds.Remove(downstreamMachineId))
        {
            MarkUpdated();
        }
    }

    public void RecordUsage(double deltaHours, double deltaKilometers, Guid modifiedBy)
    {
        AccumulatedHours += deltaHours;
        AccumulatedKilometers += deltaKilometers;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    void ITenantScoped.SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}