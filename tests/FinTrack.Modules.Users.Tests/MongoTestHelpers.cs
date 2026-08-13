using MongoDB.Driver;
using NSubstitute;

namespace FinTrack.Modules.Users.Tests;

internal static class MongoTestHelpers
{
    /// <summary>
    /// Builds an <see cref="IAsyncCursor{T}"/> substitute whose single enumeration yields
    /// <paramref name="result"/> (or nothing when <paramref name="result"/> is <c>null</c>).
    /// </summary>
    public static IAsyncCursor<TDocument> CursorWith<TDocument>(TDocument? result)
        where TDocument : class
    {
        var cursor = Substitute.For<IAsyncCursor<TDocument>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
        cursor.Current.Returns(result is null ? Array.Empty<TDocument>() : new[] { result });
        return cursor;
    }

    public static IMongoCollection<TDocument> CollectionWith<TDocument>(
        TDocument? result,
        out IMongoDatabase database)
        where TDocument : class
    {
        var collection = Substitute.For<IMongoCollection<TDocument>>();
        var cursor = CursorWith(result);
        collection
            .FindAsync<TDocument>(
                Arg.Any<FilterDefinition<TDocument>>(),
                Arg.Any<FindOptions<TDocument, TDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);

        database = Substitute.For<IMongoDatabase>();
        database.GetCollection<TDocument>(Arg.Any<string>()).Returns(collection);

        return collection;
    }
}