using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Everything one interaction changed, undone together when it is disposed: values set with <see cref="Set"/> get
/// their previous value back, tracked subscriptions are disposed, and waits started with <see cref="Next"/> are
/// cancelled. Undone in reverse order, so nested changes unwind correctly.
/// <code>
/// using var scope = new InteractionScope();
/// scope.Set(() => hand.Draggable, value => hand.Draggable = value, true);
/// var dropped = await scope.Next&lt;IGesture, CardDropped&gt;(gestures);
/// // disposed: hand.Draggable is back to what it was
/// </code>
/// Using it after it was disposed fails, so a stale scope can't change anything.
/// </summary>
public class InteractionScope : IDisposable
{
    readonly Stack<Action> undo = new();
    readonly CancellationTokenSource cancellation = new();

    public bool IsDisposed { get; private set; }

    /// <summary>Cancelled when the scope is disposed.</summary>
    public CancellationToken Token => cancellation.Token;

    /// <summary>Sets a value for as long as the scope lasts; disposing the scope sets the value it had before.</summary>
    public void Set<T>(Func<T> get, Action<T> set, T value)
    {
        EnsureOpen();
        var previous = get();
        set(value);
        undo.Push(() => set(previous));
    }

    /// <summary>Runs <paramref name="action"/> when the scope is disposed.</summary>
    public void OnDispose(Action action)
    {
        EnsureOpen();
        undo.Push(action);
    }

    /// <summary>Disposes <paramref name="disposable"/> with the scope (e.g. a subscription), and returns it.</summary>
    public TDisposable Track<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
    {
        OnDispose(disposable.Dispose);
        return disposable;
    }

    /// <summary>Handles every <typeparamref name="T"/> published on <paramref name="bus"/> while the scope lasts.</summary>
    public void On<TBase, T>(TypedEventBus<TBase> bus, Action<T> handler) where TBase : class where T : TBase
    {
        Track(bus.Subscribe(handler));
    }

    /// <summary>The next <typeparamref name="T"/> on <paramref name="bus"/> that <paramref name="accept"/> accepts; cancelled when the scope is disposed.</summary>
    public Task<T> Next<TBase, T>(TypedEventBus<TBase> bus, Func<T, bool>? accept = null) where TBase : class where T : TBase
    {
        EnsureOpen();
        return bus.Next(accept, Token);
    }

    /// <summary>A scope inside this one: disposed with it at the latest, or earlier on its own.</summary>
    public InteractionScope OpenChild()
    {
        return Track(new InteractionScope());
    }

    protected void EnsureOpen()
    {
        if(IsDisposed) throw new ObjectDisposedException(GetType().Name, "This interaction already ended");
    }

    public void Dispose()
    {
        if(IsDisposed) return;
        IsDisposed = true;
        cancellation.Cancel();

        List<Exception>? errors = null;
        while(undo.TryPop(out var action))
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                (errors ??= []).Add(e);
            }
        }
        cancellation.Dispose();
        GC.SuppressFinalize(this);

        if(errors is not null) throw new AggregateException("Undoing an interaction failed", errors);
    }
}
