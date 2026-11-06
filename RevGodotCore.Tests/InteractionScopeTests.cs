namespace RevGodotCore.Tests;

public class InteractionScopeTests
{
    interface IGesture;
    record Pressed(string Target) : IGesture;

    class Switch
    {
        public bool Enabled { get; set; }
    }

    [Test]
    public void DisposingRestoresEveryValueInReverseOrder()
    {
        var target = new Switch { Enabled = false };
        var order = new List<string>();
        var scope = new InteractionScope();

        scope.Set(() => target.Enabled, value => target.Enabled = value, true);
        scope.Set(() => target.Enabled, value => { target.Enabled = value; order.Add("second undone"); }, true);
        scope.OnDispose(() => order.Add("last registered"));

        Assert.That(target.Enabled, Is.True);
        // Setting logged once already; only the undo is checked
        order.Clear();
        scope.Dispose();

        Assert.That(target.Enabled, Is.False);
        Assert.That(order, Is.EqualTo(new[] { "last registered", "second undone" }));
    }

    [Test]
    public void DisposingCancelsWaitsAndUnsubscribes()
    {
        var bus = new TypedEventBus<IGesture>();
        var scope = new InteractionScope();
        int handled = 0;
        scope.On<IGesture, Pressed>(bus, _ => handled++);
        var next = scope.Next<IGesture, Pressed>(bus);

        scope.Dispose();
        bus.Publish(new Pressed("a"));

        Assert.That(next.IsCanceled);
        Assert.That(handled, Is.Zero);
        Assert.That(bus.SubscriberCount, Is.Zero);
    }

    [Test]
    public void AStaleScopeCantChangeAnything()
    {
        var target = new Switch();
        var scope = new InteractionScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.Set(() => target.Enabled, value => target.Enabled = value, true));
        Assert.That(target.Enabled, Is.False);
    }

    [Test]
    public void AChildEndsWithItsParent()
    {
        var target = new Switch();
        var parent = new InteractionScope();
        var child = parent.OpenChild();
        child.Set(() => target.Enabled, value => target.Enabled = value, true);

        parent.Dispose();

        Assert.That(child.IsDisposed);
        Assert.That(target.Enabled, Is.False);
    }

    [Test]
    public void EveryUndoRunsEvenWhenOneFails()
    {
        var target = new Switch();
        var scope = new InteractionScope();
        scope.Set(() => target.Enabled, value => target.Enabled = value, true);
        scope.OnDispose(() => throw new InvalidOperationException("broken"));

        Assert.Throws<AggregateException>(scope.Dispose);
        Assert.That(target.Enabled, Is.False);
    }
}
