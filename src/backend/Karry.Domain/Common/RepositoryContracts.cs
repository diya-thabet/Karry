using Karry.Domain.Common;

namespace Karry.Domain.Common;

/// <summary>
/// Port (interface) for generic persistence access. Implemented by the Infrastructure layer.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}