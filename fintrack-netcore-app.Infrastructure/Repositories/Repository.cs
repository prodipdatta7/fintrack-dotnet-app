using fintrack_netcore_app.Application.Interfaces;
using fintrack_netcore_app.Domain.Entities;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace fintrack_netcore_app.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : EntityBase
    {
        private readonly IMongoCollection<T> _collection;

        public Repository(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("FinTrackDb");

            // Automatically pluralize or map the collection name based on the class name
            var collectionName = typeof(T).Name + "s";
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task<ReplaceOneResult?> SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            var filters = Builders<T>.Filter.Eq(e => e.ItemId, entity.ItemId);
            var options = new ReplaceOptions { IsUpsert = true };
            
            return await _collection.ReplaceOneAsync(filters, entity, options, cancellationToken);
        }

        public async Task<T> GetItemAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.Where(predicate);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.Where(predicate);
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
    }
}
