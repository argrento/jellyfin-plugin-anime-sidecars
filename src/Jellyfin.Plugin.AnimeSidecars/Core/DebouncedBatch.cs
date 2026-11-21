namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>
/// Collects items and hands them over in one batch once no new item has arrived for a while.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class DebouncedBatch<T> : IDisposable
    where T : notnull
{
    private readonly object _gate = new();
    private readonly HashSet<T> _pending = new();
    private readonly TimeSpan _delay;
    private readonly Func<IReadOnlyCollection<T>, Task> _flush;
    private readonly Action<Exception>? _onError;
    private readonly Timer _timer;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="DebouncedBatch{T}"/> class.</summary>
    /// <param name="delay">Quiet time after the last added item before the batch is flushed.</param>
    /// <param name="flush">Receives the distinct items of a batch.</param>
    /// <param name="onError">Receives exceptions thrown by <paramref name="flush"/>.</param>
    public DebouncedBatch(TimeSpan delay, Func<IReadOnlyCollection<T>, Task> flush, Action<Exception>? onError = null)
    {
        _delay = delay;
        _flush = flush;
        _onError = onError;
        _timer = new Timer(_ => _ = FlushAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Adds an item and restarts the wait.</summary>
    /// <param name="item">Item.</param>
    public void Add(T item)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _pending.Add(item);
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pending.Clear();
            _timer.Dispose();
        }
    }

    private async Task FlushAsync()
    {
        T[] items;
        lock (_gate)
        {
            items = _pending.ToArray();
            _pending.Clear();
        }

        if (items.Length == 0)
        {
            return;
        }

        try
        {
            await _flush(items).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _onError?.Invoke(e);
        }
    }
}
