namespace RevGodotCore.Tests;

public class BindingsTests
{
    // Models, like a game's zones
    interface IZone;
    record Graveyard : IZone;
    record MonsterZone : IZone;
    record ExtraMonsterZone : MonsterZone;

    // Views, like zone nodes
    class AnyZoneView : IBindable<IZone>
    {
        public IZone? Bound { get; private set; }
        public void Bind(IZone model) => Bound = model;
    }

    class MonsterZoneView : IBindable<MonsterZone>
    {
        public MonsterZone? Bound { get; private set; }
        public void Bind(MonsterZone model) => Bound = model;
    }

    class GenericAndSpecificView : IBindable<IZone>, IBindable<MonsterZone>
    {
        public string? BoundAs { get; private set; }
        void IBindable<IZone>.Bind(IZone model) => BoundAs = nameof(IZone);
        void IBindable<MonsterZone>.Bind(MonsterZone model) => BoundAs = nameof(MonsterZone);
    }

    interface IOther;
    record OtherZone : IZone, IOther;
    class AmbiguousView : IBindable<IZone>, IBindable<IOther>
    {
        void IBindable<IZone>.Bind(IZone model) {}
        void IBindable<IOther>.Bind(IOther model) {}
    }

    class FailingView : IBindable<IZone>
    {
        public void Bind(IZone model) => throw new InvalidOperationException("view failed");
    }

    [Test]
    public void ViewsBindModelsOfTheirTypeAndDerivedTypes()
    {
        var report = new BindingReport();
        var monsterView = new MonsterZoneView();
        var extra = new ExtraMonsterZone();

        Assert.That(Bindings.TryBind(monsterView, extra, "Board", "ExtraMonsterZone1", report));
        Assert.That(monsterView.Bound, Is.SameAs(extra));
        Assert.That(report.HasProblems, Is.False);
    }

    [Test]
    public void AViewForAnotherTypeIsReported()
    {
        var report = new BindingReport();

        Assert.That(Bindings.TryBind(new MonsterZoneView(), new Graveyard(), "Board", "Graveyard", report), Is.False);

        var mismatch = (TypeMismatch)report.Problems.Single();
        Assert.That(mismatch.ModelType, Is.EqualTo(typeof(Graveyard)));
        Assert.That(mismatch.Accepted, Is.EqualTo(new[] { typeof(MonsterZone) }));
    }

    [Test]
    public void TheMostSpecificBindingWins()
    {
        var report = new BindingReport();
        var view = new GenericAndSpecificView();

        Bindings.TryBind(view, new MonsterZone(), "Board", "MonsterZone1", report);
        Assert.That(view.BoundAs, Is.EqualTo(nameof(MonsterZone)));

        Bindings.TryBind(view, new Graveyard(), "Board", "Graveyard", report);
        Assert.That(view.BoundAs, Is.EqualTo(nameof(IZone)));
        Assert.That(report.HasProblems, Is.False);
    }

    [Test]
    public void UnrelatedBindingsOfTheSameModelAreAmbiguous()
    {
        var report = new BindingReport();

        Assert.That(Bindings.TryBind(new AmbiguousView(), new OtherZone(), "Board", "Other", report), Is.False);
        Assert.That(report.Problems.Single(), Is.InstanceOf<AmbiguousBinding>());
    }

    [Test]
    public void TheViewsOwnExceptionComesThrough()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Bindings.TryBind(new FailingView(), new Graveyard(), "Board", "Graveyard", new BindingReport()));
        Assert.That(error!.Message, Is.EqualTo("view failed"));
    }

    [Test]
    public void MatchingByNameReportsEveryMissingAndUnusedView()
    {
        var report = new BindingReport();
        var models = new Dictionary<string, IZone>
        {
            ["MonsterZone1"] = new MonsterZone(),
            ["Graveyard"] = new Graveyard(),
            ["Hand"] = new Graveyard(),
        };
        var views = new Dictionary<string, object>
        {
            ["MonsterZone1"] = new MonsterZoneView(),
            ["Graveyard"] = new MonsterZoneView(), // wrong kind of view
            ["Banish"] = new AnyZoneView(),         // no such zone
        };

        var bound = Bindings.BindByName(models, views, "PlayerArea", report, unmatchedAllowed: new HashSet<string> { "Hand" });

        Assert.That(bound.Select(match => match.Name), Is.EqualTo(new[] { "MonsterZone1" }));
        Assert.That(report.Problems.Select(problem => problem.GetType()), Is.EquivalentTo(new[] { typeof(TypeMismatch), typeof(UnusedView) }));

        var exception = Assert.Throws<BindingException>(report.ThrowIfAny);
        Assert.That(exception!.Message, Does.Contain("PlayerArea/Graveyard").And.Contain("PlayerArea/Banish"));
    }

    [Test]
    public void AModelWithoutAViewIsReported()
    {
        var report = new BindingReport();
        Bindings.MatchByName(new Dictionary<string, IZone> { ["Deck"] = new Graveyard() }, new Dictionary<string, object>(), "PlayerArea", report);

        var missing = (MissingView)report.Problems.Single();
        Assert.That(missing.Name, Is.EqualTo("Deck"));
    }

    [Test]
    public void BindableTypesAreFoundFromTheInterfaces()
    {
        Assert.That(Bindings.ModelTypesOf(typeof(GenericAndSpecificView)), Is.EquivalentTo(new[] { typeof(IZone), typeof(MonsterZone) }));
        Assert.That(Bindings.IsBindable(new object()), Is.False);
    }
}

public class TreeDependenciesTests
{
    class TreeNode(TreeNode? parent)
    {
        public TreeNode? Parent { get; } = parent;
    }

    class SessionProvider(TreeNode? parent, string session) : TreeNode(parent), IProvide<string>
    {
        public string Value() => session;
    }

    [Test]
    public void TheNearestProvidingAncestorWins()
    {
        var root = new SessionProvider(null, "outer");
        var middle = new SessionProvider(root, "inner");
        var leaf = new TreeNode(new TreeNode(middle));

        var provider = TreeDependencies.FindAncestor<TreeNode, IProvide<string>>(leaf, node => node.Parent);

        Assert.That(provider!.Value(), Is.EqualTo("inner"));
    }

    [Test]
    public void ANodeDoesntProvideToItself()
    {
        var root = new SessionProvider(null, "only");

        Assert.That(TreeDependencies.FindAncestor<TreeNode, IProvide<string>>(root, node => node.Parent), Is.Null);
    }
}
