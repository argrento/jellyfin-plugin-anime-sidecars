using System.Collections.Concurrent;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class DebouncedBatchTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(150);

    [Fact]
    public async Task Items_Added_Close_Together_Are_Flushed_Once()
    {
        var calls = new ConcurrentQueue<string[]>();
        using var batch = new DebouncedBatch<string>(Delay, items =>
        {
            calls.Enqueue(items.OrderBy(i => i, StringComparer.Ordinal).ToArray());
            return Task.CompletedTask;
        });

        batch.Add("a");
        await Task.Delay(50);
        batch.Add("b");
        batch.Add("a");
        await Task.Delay(500);

        var call = Assert.Single(calls);
        Assert.Equal(new[] { "a", "b" }, call);
    }

    [Fact]
    public async Task Each_Add_Restarts_The_Wait()
    {
        var calls = new ConcurrentQueue<string[]>();
        using var batch = new DebouncedBatch<string>(Delay, items =>
        {
            calls.Enqueue(items.ToArray());
            return Task.CompletedTask;
        });

        for (var i = 0; i < 4; i++)
        {
            batch.Add($"e{i}");
            await Task.Delay(80);
        }

        Assert.Empty(calls); // 4 × 80 ms > 150 ms, but never 150 ms of quiet
        await Task.Delay(500);
        Assert.Equal(4, Assert.Single(calls).Length);
    }

    [Fact]
    public async Task Items_After_A_Flush_Go_To_The_Next_One()
    {
        var calls = new ConcurrentQueue<string[]>();
        using var batch = new DebouncedBatch<string>(Delay, items =>
        {
            calls.Enqueue(items.ToArray());
            return Task.CompletedTask;
        });

        batch.Add("a");
        await Task.Delay(500);
        batch.Add("b");
        await Task.Delay(500);

        Assert.Equal(new[] { new[] { "a" }, new[] { "b" } }, calls.ToArray());
    }

    [Fact]
    public async Task Failing_Flush_Does_Not_Stop_Later_Ones()
    {
        var calls = 0;
        using var batch = new DebouncedBatch<string>(Delay, _ =>
        {
            Interlocked.Increment(ref calls);
            throw new InvalidOperationException("boom");
        });

        batch.Add("a");
        await Task.Delay(500);
        batch.Add("b");
        await Task.Delay(500);

        Assert.Equal(2, calls);
    }
}
