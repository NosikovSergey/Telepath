using Telepath.Middleware;
using Telepath.Models;
using Telepath.Routing;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telepath.Tests;

public class TelepathRouterMiddlewareTests
{
    private class FakeEntry : IRoute
    {
        private readonly bool _matches;
        public bool WasExecuted { get; private set; }

        public FakeEntry(bool matches) => _matches = matches;

        public bool Matches(TelepathContext _) => _matches;

        public Task ExecuteAsync(IServiceProvider _, TelepathContext __, CancellationToken ___)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    private static TelepathContext EmptyContext() => new() { Update = new Update { Id = 1 } };
    private static IServiceProvider EmptySp() => new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task WhenEntryMatches_HandlerIsExecuted()
    {
        var entry = new FakeEntry(matches: true);
        var middleware = new TelepathRouterMiddleware(EmptySp(), [entry]);

        await middleware.InvokeAsync(EmptyContext(), (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.True(entry.WasExecuted);
    }

    [Fact]
    public async Task WhenEntryMatches_NextIsNotCalled()
    {
        var entry = new FakeEntry(matches: true);
        var middleware = new TelepathRouterMiddleware(EmptySp(), [entry]);
        var nextCalled = false;

        await middleware.InvokeAsync(EmptyContext(), (_, _) => { nextCalled = true; return Task.CompletedTask; }, CancellationToken.None);

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task WhenNoEntryMatches_NextIsCalled()
    {
        var entry = new FakeEntry(matches: false);
        var middleware = new TelepathRouterMiddleware(EmptySp(), [entry]);
        var nextCalled = false;

        await middleware.InvokeAsync(EmptyContext(), (_, _) => { nextCalled = true; return Task.CompletedTask; }, CancellationToken.None);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task WhenNoEntries_NextIsCalled()
    {
        var middleware = new TelepathRouterMiddleware(EmptySp(), []);
        var nextCalled = false;

        await middleware.InvokeAsync(EmptyContext(), (_, _) => { nextCalled = true; return Task.CompletedTask; }, CancellationToken.None);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task FirstMatchingEntry_IsExecuted_SubsequentEntriesAreSkipped()
    {
        var first = new FakeEntry(matches: true);
        var second = new FakeEntry(matches: true);
        var middleware = new TelepathRouterMiddleware(EmptySp(), [first, second]);

        await middleware.InvokeAsync(EmptyContext(), (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.True(first.WasExecuted);
        Assert.False(second.WasExecuted);
    }

    [Fact]
    public async Task NonMatchingEntry_IsSkipped_MatchingEntryExecuted()
    {
        var nonMatching = new FakeEntry(matches: false);
        var matching = new FakeEntry(matches: true);
        var middleware = new TelepathRouterMiddleware(EmptySp(), [nonMatching, matching]);

        await middleware.InvokeAsync(EmptyContext(), (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.False(nonMatching.WasExecuted);
        Assert.True(matching.WasExecuted);
    }
}
