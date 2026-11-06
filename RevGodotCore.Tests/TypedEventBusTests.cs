namespace RevGodotCore.Tests;

public class TypedEventBusTests
{
    interface IGesture;
    record Pressed(string Target) : IGesture;
    record Dropped(string Target) : IGesture;

    [Test]
    public void SubscribersGetTheirTypeAndItsDerivedTypes()
    {
        var bus = new TypedEventBus<IGesture>();
        var pressed = new List<Pressed>();
        var all = new List<IGesture>();
        bus.Subscribe<Pressed>(pressed.Add);
        bus.Subscribe<IGesture>(all.Add);

        bus.Publish(new Pressed("a"));
        bus.Publish(new Dropped("b"));

        Assert.That(pressed, Has.Count.EqualTo(1));
        Assert.That(all, Has.Count.EqualTo(2));
    }

    [Test]
    public void DisposingUnsubscribes()
    {
        var bus = new TypedEventBus<IGesture>();
        int count = 0;
        var subscription = bus.Subscribe<Pressed>(_ => count++);

        subscription.Dispose();
        bus.Publish(new Pressed("a"));

        Assert.That(count, Is.Zero);
        Assert.That(bus.SubscriberCount, Is.Zero);
    }

    [Test]
    public async Task NextWaitsForAnAcceptedEventThenUnsubscribes()
    {
        var bus = new TypedEventBus<IGesture>();
        var next = bus.Next<Pressed>(pressed => pressed.Target == "b");

        bus.Publish(new Pressed("a"));
        Assert.That(next.IsCompleted, Is.False);
        bus.Publish(new Pressed("b"));

        Assert.That((await next).Target, Is.EqualTo("b"));
        Assert.That(bus.SubscriberCount, Is.Zero);
    }

    [Test]
    public void NextIsCancelledByItsToken()
    {
        var bus = new TypedEventBus<IGesture>();
        using var cancellation = new CancellationTokenSource();
        var next = bus.Next<Pressed>(cancellation: cancellation.Token);

        cancellation.Cancel();

        Assert.That(next.IsCanceled);
        Assert.That(bus.SubscriberCount, Is.Zero);
    }
}
