using System.Text.RegularExpressions;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.GetTransactions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.GetTransactions;

public class GetTransactionsHandlerTests
{
    private const string UserId = "user-123";

    private sealed class Captured
    {
        public FilterDefinition<Transaction>? Filter;
        public FindOptions<Transaction, Transaction>? Options;
    }

    private static (GetTransactionsHandler Handler, Captured Captured) CreateHandler()
    {
        var captured = new Captured();

        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<Transaction>>();
        var cursor = Substitute.For<IAsyncCursor<Transaction>>();

        cursor.Current.Returns(new List<Transaction>());
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        collection.CountDocumentsAsync(
                Arg.Do<FilterDefinition<Transaction>>(f => captured.Filter = f),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0L));

        collection.FindAsync(
                Arg.Any<FilterDefinition<Transaction>>(),
                Arg.Do<FindOptions<Transaction, Transaction>>(o => captured.Options = o),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        database.GetCollection<Transaction>("transactions").Returns(collection);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);

        return (new GetTransactionsHandler(database, currentUser), captured);
    }

    private static RenderArgs<Transaction> RenderArgs()
    {
        var registry = BsonSerializer.SerializerRegistry;
        return new RenderArgs<Transaction>(registry.GetSerializer<Transaction>(), registry);
    }

    private static BsonDocument Render(FilterDefinition<Transaction> filter)
        => filter.Render(RenderArgs());

    /// <summary>Flattens $and clauses so individual field conditions can be asserted on.</summary>
    private static List<BsonElement> Flatten(BsonDocument document)
    {
        var elements = new List<BsonElement>();
        foreach (var element in document)
        {
            if (element.Name == "$and")
            {
                foreach (var sub in element.Value.AsBsonArray)
                    elements.AddRange(Flatten(sub.AsBsonDocument));
            }
            else
            {
                elements.Add(element);
            }
        }

        return elements;
    }

    [Fact]
    public async Task Handle_WithoutNewFilters_OnlyScopesToUser()
    {
        var (handler, captured) = CreateHandler();

        var result = await handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var elements = Flatten(Render(captured.Filter!));
        elements.Should().NotContain(e => e.Name == "accountId");
        elements.Should().NotContain(e => e.Name == "amount");
        elements.Should().NotContain(e => e.Name == "$or");
    }

    [Fact]
    public async Task Handle_WithAccountId_FiltersByExactAccountId()
    {
        var (handler, captured) = CreateHandler();

        await handler.Handle(new GetTransactionsQuery(AccountId: "acc-1"), CancellationToken.None);

        var elements = Flatten(Render(captured.Filter!));
        elements.Should().Contain(e => e.Name == "accountId" && e.Value == "acc-1");
    }

    [Fact]
    public async Task Handle_WithAmountRange_AppliesInclusiveBounds()
    {
        var (handler, captured) = CreateHandler();

        await handler.Handle(
            new GetTransactionsQuery(MinAmount: 10m, MaxAmount: 100m), CancellationToken.None);

        var amountClauses = Flatten(Render(captured.Filter!))
            .Where(e => e.Name == "amount")
            .Select(e => e.Value.AsBsonDocument)
            .ToList();

        amountClauses.Should().NotBeEmpty();
        amountClauses.Should().Contain(d => d.Contains("$gte") && d["$gte"].ToDecimal() == 10m);
        amountClauses.Should().Contain(d => d.Contains("$lte") && d["$lte"].ToDecimal() == 100m);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_MatchesTitleAndNoteCaseInsensitively()
    {
        var (handler, captured) = CreateHandler();

        await handler.Handle(
            new GetTransactionsQuery(SearchTerm: "coffee (large)"), CancellationToken.None);

        var orElement = Flatten(Render(captured.Filter!)).Single(e => e.Name == "$or");
        var clauses = orElement.Value.AsBsonArray
            .Select(v => v.AsBsonDocument)
            .ToList();

        clauses.Should().HaveCount(2);
        clauses.Select(c => c.GetElement(0).Name).Should().BeEquivalentTo(new[] { "title", "note" });

        var expectedPattern = Regex.Escape("coffee (large)");
        foreach (var clause in clauses)
        {
            var regex = clause.GetElement(0).Value.AsBsonRegularExpression;
            regex.Pattern.Should().Be(expectedPattern);
            regex.Options.Should().Be("i");
        }
    }

    [Fact]
    public async Task Handle_AppliesSortToFindOptions()
    {
        var (handler, captured) = CreateHandler();

        await handler.Handle(new GetTransactionsQuery(SortBy: "title-asc"), CancellationToken.None);

        var sort = captured.Options!.Sort!.Render(RenderArgs()).AsBsonDocument;
        sort.Should().BeEquivalentTo(new BsonDocument("title", 1));
    }

    [Theory]
    [InlineData(null, "date", -1)]
    [InlineData("date-desc", "date", -1)]
    [InlineData("date-asc", "date", 1)]
    [InlineData("amount-desc", "amount", -1)]
    [InlineData("amount-asc", "amount", 1)]
    [InlineData("title-asc", "title", 1)]
    [InlineData("unknown-sort", "date", -1)]
    [InlineData("", "date", -1)]
    public void MapSort_MapsKnownValuesAndFallsBackToDateDescending(
        string? sortBy, string expectedField, int expectedDirection)
    {
        var sort = GetTransactionsHandler.MapSort(sortBy);

        var rendered = sort.Render(RenderArgs()).AsBsonDocument;

        rendered.Should().BeEquivalentTo(new BsonDocument(expectedField, expectedDirection));
    }
}
