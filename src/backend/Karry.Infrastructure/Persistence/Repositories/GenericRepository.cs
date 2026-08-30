using System.Linq.Expressions;
using Karry.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Karry.Infrastructure.Persistence.Repositories;

public sealed class GenericRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly KarryDbContext DbContext;

    public GenericRepository(KarryDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
        => await DbContext.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbContext.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbContext.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await DbContext.Set<TEntity>().AnyAsync(predicate, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => DbContext.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => DbContext.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => DbContext.Set<TEntity>().Remove(entity);
}