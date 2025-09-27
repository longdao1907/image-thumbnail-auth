using System;
using Microsoft.Extensions.Logging;
using AuthAPI.Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Dummy.SharedLib.Abstract;

namespace AuthAPI.Infrastructure.Persistence;

internal sealed class PostgresRepository<TEntity, TId>(ILogger<PostgresRepository<TEntity, TId>> logger, AppDbContext dbContext) : IRepository<TEntity, TId>
where TEntity : class, IEntity<TId>
{
  private readonly string _entityName = typeof(TEntity).Name;
  private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

  public async Task DeleteAsync(TId id, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(id);

      var entityToDelete = await _dbSet.FindAsync(id, cancellationToken);
      if (entityToDelete is null)
      {
        throw new EntityNotFoundException($"`{_entityName}` with id `{id}` was not found.");
      }
      _dbSet.Remove(entityToDelete);
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      throw new RepositoryException($"Error occurred while deleting `{_entityName}` with id `{id}`", ex);
    }
  }

  public Task<bool> ExistAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(filter);
      return _dbSet.AnyAsync(filter, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new RepositoryException($"Error occurred while checking existence of `{_entityName}`", ex);
    }
  }

  public async Task<IEnumerable<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(filter);
      return await _dbSet.Where(filter).ToListAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      throw new RepositoryException($"Error occurred while finding all `{_entityName}`", ex);
    }
  }

  public async Task<(IEnumerable<TEntity> Items, long TotalCount)> GetAsync(Expression<Func<TEntity, bool>> filter, int pageNumber, int pageSize, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(filter);
      if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");
      if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

      var query = _dbSet.Where(filter);
      var totalCount = await query.LongCountAsync(cancellationToken);
      var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

      return (items, totalCount);
    }
    catch (Exception ex)
    {
      throw new RepositoryException($"Error occurred while getting paginated `{_entityName}`", ex);
    }
  }

  public async Task<TEntity> GetByIdAsync(TId id, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(id);

      var entity = await _dbSet.FindAsync(id, cancellationToken);
      if (entity is null)
      {
        throw new EntityNotFoundException($"`{_entityName}` with id `{id}` was not found.");
      }
      return entity;
    }
    catch (Exception ex) when (ex is not EntityNotFoundException)
    {
      throw new RepositoryException($"Error occurred while retrieving `{_entityName}` with id `{id}`", ex);
    }
  }

  public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(entity);

      _dbSet.Add(entity);
      await dbContext.SaveChangesAsync(cancellationToken);
      logger.LogInformation("`{entityName}` with id `{id}` was successfully inserted", _entityName, entity.Id);
    }
    catch (Exception ex)
    {
      throw new RepositoryException($"Error occurred while inserting `{_entityName}`", ex);
    }
  }

  public async Task UpdateAsync(TEntity entity, uint originalVersion, CancellationToken cancellationToken)
  {
    try
    {
      ArgumentNullException.ThrowIfNull(entity);
      ArgumentNullException.ThrowIfNull(entity.Id);

      var entry = dbContext.Entry(entity);
      entry.Property("Version").OriginalValue = originalVersion;
      entry.State = EntityState.Modified;
      _dbSet.Update(entity);

      var result = await dbContext.SaveChangesAsync(cancellationToken);
      if (result == 0)
      {
        throw new EntityNotFoundException($"Failed to update `{_entityName}` with id `{entity.Id}`. Entity does not exist or version conflict.");
      }
      logger.LogInformation("`{entityName}` with id `{id}` was successfully updated", _entityName, entity.Id);
    }
    catch (Exception ex) when (ex is not EntityNotFoundException)
    {
      throw new RepositoryException($"Error occurred while updating `{_entityName}` with id `{entity.Id}`", ex);
    }
  }
}
