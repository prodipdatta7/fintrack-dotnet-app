using fintrack_netcore_app.Domain.Entities;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace fintrack_netcore_app.Application.Interfaces
{
    public interface IRepository<T> where T : EntityBase
    {
        Task<ReplaceOneResult?> SaveAsync(T entity, CancellationToken cancellationToken = default);
        Task<T> GetItemAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
