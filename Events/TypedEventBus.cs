using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Publishes events of <typeparamref name="TBase"/> to subscribers of their type (a base type or interface works too).
/// Synchronous: <see cref="Publish"/> calls every matching subscriber, in the order they subscribed, before returning.
/// A handler's exception is not caught.
/// </summary>
public sealed class TypedEventBus<TBase> where TBase : class
{
    sealed record Subscription(Type EventType, Action<TBase> Handler);

    readonly List<Subscription> subscriptions = [];

    /// <summary>Calls <paramref name="handler"/> for every published <typeparamref name="T"/>. Dispose the result to unsubscribe.</summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : TBase
    {
        var subscription = new Subscription(typeof(T), published => handler((T)published));
        subscriptions.Add(subscription);
        return new Disposable(() => subscriptions.Remove(subscription));
    }

    public void Publish<T>(T published) where T : TBase
    {
        // A copy: a handler may subscribe or unsubscribe while the event is handled
        foreach(var subscription in subscriptions.Where(subscription => subscription.EventType.IsInstanceOfType(published)).ToList())
        {
            subscription.Handler(published);
        }
    }

    /// <summary>
    /// The next published <typeparamref name="T"/> that <paramref name="accept"/> accepts (any, without it). Cancelled
    /// by <paramref name="cancellation"/>, e.g. when the scope that asked is disposed.
    /// </summary>
    public Task<T> Next<T>(Func<T, bool>? accept = null, CancellationToken cancellation = default) where T : TBase
    {
        cancellation.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        IDisposable? subscription = null;

        subscription = Subscribe<T>(published =>
        {
            if(accept is not null && !accept(published)) return;
            if(!completion.TrySetResult(published)) return;
            subscription!.Dispose();
            registration.Dispose();
        });
        if(cancellation.CanBeCanceled)
        {
            registration = cancellation.Register(() =>
            {
                if(completion.TrySetCanceled(cancellation)) subscription.Dispose();
            });
        }
        return completion.Task;
    }

    public int SubscriberCount => subscriptions.Count;
}

/// <summary>An <see cref="IDisposable"/> that runs an action once, the first time it is disposed.</summary>
public sealed class Disposable(Action dispose) : IDisposable
{
    bool disposed;

    public void Dispose()
    {
        if(disposed) return;
        disposed = true;
        dispose();
    }
}
