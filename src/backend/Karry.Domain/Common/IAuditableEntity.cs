namespace Karry.Domain.Common;

public interface IAuditableEntity
{
    Guid CreatedBy { get; }

    Guid? ModifiedBy { get; }
}