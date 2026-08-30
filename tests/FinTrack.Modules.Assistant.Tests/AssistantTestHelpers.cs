using FinTrack.Modules.Assistant.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace FinTrack.Modules.Assistant.Tests;

internal static class AssistantTestHelpers
{
    public static IAsyncCursor<TDocument> MockCursor<TDocument>(IEnumerable<TDocument> documents)
    {
        var cursor = Substitute.For<IAsyncCursor<TDocument>>();
        var list = documents.ToList();
        var moved = false;

        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!moved) { moved = true; return true; }
            return false;
        });

        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!moved) { moved = true; return Task.FromResult(true); }
            return Task.FromResult(false);
        });

        cursor.Current.Returns(_ => list);
        return cursor;
    }

    public static IMongoCollection<BsonDocument> MockCollection(IEnumerable<BsonDocument> documents)
    {
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        var docList = documents.ToList();

        collection.FindAsync(
                Arg.Any<FilterDefinition<BsonDocument>>(),
                Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MockCursor(docList)));

        return collection;
    }

    public static IMongoDatabase MockDatabase(Dictionary<string, List<BsonDocument>> collections)
    {
        var db = Substitute.For<IMongoDatabase>();

        foreach (var (name, docs) in collections)
        {
            var coll = MockCollection(docs);
            db.GetCollection<BsonDocument>(name, Arg.Any<MongoCollectionSettings>()).Returns(coll);
        }

        return db;
    }

    public static IMongoCollection<T> MockTypedCollection<T>(List<T> store) where T : class
    {
        var collection = Substitute.For<IMongoCollection<T>>();
        var indexManager = Substitute.For<IMongoIndexManager<T>>();
        collection.Indexes.Returns(indexManager);

        collection.FindAsync(
                Arg.Any<FilterDefinition<T>>(),
                Arg.Any<FindOptions<T, T>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MockCursor(store)));

        collection.InsertOneAsync(
                Arg.Any<T>(),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var doc = callInfo.Arg<T>();
                store.Add(doc);
                return Task.CompletedTask;
            });

        collection.CountDocumentsAsync(
                Arg.Any<FilterDefinition<T>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult((long)store.Count));

        collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<T>>(),
                Arg.Any<UpdateDefinition<T>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((UpdateResult)new UpdateResult.Acknowledged(1, 1, null)));

        collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<T>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                if (store.Count > 0)
                {
                    store.RemoveAt(0);
                    return Task.FromResult((DeleteResult)new DeleteResult.Acknowledged(1));
                }
                return Task.FromResult((DeleteResult)new DeleteResult.Acknowledged(0));
            });

        collection.DeleteManyAsync(
                Arg.Any<FilterDefinition<T>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var count = store.Count;
                store.Clear();
                return Task.FromResult((DeleteResult)new DeleteResult.Acknowledged(count));
            });

        return collection;
    }

    public static IMongoDatabase MockConversationDatabase(
        List<AssistantConversation> conversations,
        List<AssistantMessage> messages)
    {
        var db = Substitute.For<IMongoDatabase>();
        var convColl = MockTypedCollection(conversations);
        var msgColl = MockTypedCollection(messages);

        db.GetCollection<AssistantConversation>("assistant_conversations", Arg.Any<MongoCollectionSettings>())
            .Returns(convColl);
        db.GetCollection<AssistantMessage>("assistant_messages", Arg.Any<MongoCollectionSettings>())
            .Returns(msgColl);

        return db;
    }
}
