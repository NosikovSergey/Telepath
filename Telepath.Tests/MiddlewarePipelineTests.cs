using Telepath.Middleware;
using Telepath.Models;
using Telegram.Bot.Types;

namespace Telepath.Tests;

public class MiddlewarePipelineTests
{
    private class RecordingMiddleware : ITelepathMiddleware
    {
        private readonly int _id;
        private readonly List<int> _log;
        private readonly bool _callNext;

        public RecordingMiddleware(int id, List<int> log, bool callNext = true)
        {
            _id = id;
            _log = log;
            _callNext = callNext;
        }

        public async Task InvokeAsync(TelepathContext ctx, Func<TelepathContext, CancellationToken, Task> next, CancellationToken ct)
        {
            _log.Add(_id);
            if (_callNext)
                await next(ctx, ct);
        }
    }

    private static TelepathContext EmptyContext() => new() { Update = new Update { Id = 1 } };

    [Fact]
    public async Task Middlewares_ExecuteInRegistrationOrder()
    {
        var log = new List<int>();
        var pipeline = new MiddlewarePipeline([
            new RecordingMiddleware(1, log),
            new RecordingMiddleware(2, log),
            new RecordingMiddleware(3, log)
        ]);

        await pipeline.InvokeAsync(EmptyContext(), CancellationToken.None);

        Assert.Equal([1, 2, 3], log);
    }

    [Fact]
    public async Task WhenMiddlewareDoesNotCallNext_SubsequentMiddlewaresAreSkipped()
    {
        var log = new List<int>();
        var pipeline = new MiddlewarePipeline([
            new RecordingMiddleware(1, log, callNext: true),
            new RecordingMiddleware(2, log, callNext: false),
            new RecordingMiddleware(3, log, callNext: true)
        ]);

        await pipeline.InvokeAsync(EmptyContext(), CancellationToken.None);

        Assert.Equal([1, 2], log);
    }

    [Fact]
    public async Task EmptyPipeline_CompletesWithoutError()
    {
        var pipeline = new MiddlewarePipeline([]);
        await pipeline.InvokeAsync(EmptyContext(), CancellationToken.None);
    }

    [Fact]
    public async Task Context_IsPassedThrough_Unchanged()
    {
        TelepathContext? captured = null;
        var ctx = EmptyContext();
        ctx.State = null;

        var middleware = new DelegateMiddleware((c, next, ct) =>
        {
            captured = c;
            return next(c, ct);
        });

        var pipeline = new MiddlewarePipeline([middleware]);
        await pipeline.InvokeAsync(ctx, CancellationToken.None);

        Assert.Same(ctx, captured);
    }

    [Fact]
    public async Task ContextMutations_AreVisibleToSubsequentMiddlewares()
    {
        const string key = "test";
        string? valueSeenBySecond = null;

        var first = new DelegateMiddleware((ctx, next, ct) =>
        {
            ctx.Metadata = new Dictionary<string, object> { [key] = "hello" };
            return next(ctx, ct);
        });

        var second = new DelegateMiddleware((ctx, next, ct) =>
        {
            valueSeenBySecond = ctx.Metadata?[key]?.ToString();
            return next(ctx, ct);
        });

        var pipeline = new MiddlewarePipeline([first, second]);
        await pipeline.InvokeAsync(EmptyContext(), CancellationToken.None);

        Assert.Equal("hello", valueSeenBySecond);
    }

    private class DelegateMiddleware : ITelepathMiddleware
    {
        private readonly Func<TelepathContext, Func<TelepathContext, CancellationToken, Task>, CancellationToken, Task> _func;

        public DelegateMiddleware(Func<TelepathContext, Func<TelepathContext, CancellationToken, Task>, CancellationToken, Task> func)
            => _func = func;

        public Task InvokeAsync(TelepathContext ctx, Func<TelepathContext, CancellationToken, Task> next, CancellationToken ct)
            => _func(ctx, next, ct);
    }
}
