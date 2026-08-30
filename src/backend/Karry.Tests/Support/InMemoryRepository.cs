using System.Linq.Expressions;
using Karry.Domain.Common;

namespace Karry.Tests.Support;

/// <summary>
/// In-memory <see cref="IRepository{TEntity}"/> and <see cref="IUnitOfWork"/> backed by a
/// shared <see cref="List{T}"/>. Bases are registered (BaseKey) so handlers see persisted rows
/// immediately (they re-read via the same fake instance).
/// </summary>
public sealed class InMemoryRepository<TEntity> : IRepository<TEntity>, IUnitOfWork
    where TEntity : BaseEntity
{
    private readonly List<TEntity> _items;

    public InMemoryRepository(IEnumerable<TEntity>? seed = null)
    {
        _items = seed?.ToList() ?? [];
    }

    public IReadOnlyList<TEntity> Items => _items.AsReadOnly();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TEntity>>(_items.ToList());

    public Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TEntity>>(_items.Where(predicate.Compile()).ToList());

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.Any(predicate.Compile()));

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    /// <summary>Convenience synchronous add used to seed fixtures.</summary>
    public void Add(TEntity entity) => _items.Add(entity);

    public void Update(TEntity entity)
    {
    }

    public void Remove(TEntity entity) => _items.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}
