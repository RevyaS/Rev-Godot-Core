using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Something wrong with how a scene's views match its models, found while binding (see <see cref="BindingReport"/>).</summary>
public abstract record BindingProblem(string Path, string Name)
{
    public abstract string Describe();
    // Sealed, so derived records don't replace it with their generated ToString
    public sealed override string ToString() => $"{Path}/{Name}: {Describe()}";
}

/// <summary>A model has no view with its name.</summary>
public sealed record MissingView(string Path, string Name, Type ModelType) : BindingProblem(Path, Name)
{
    public override string Describe() => $"no view for the {ModelType.Name}; add a unique node named \"{Name}\"";
}

/// <summary>A bindable view has no model with its name.</summary>
public sealed record UnusedView(string Path, string Name, Type ViewType) : BindingProblem(Path, Name)
{
    public override string Describe() => $"the {ViewType.Name} matches no model; rename it or remove it";
}

/// <summary>A view and a model share a name, but the view can't show that type of model.</summary>
public sealed record TypeMismatch(string Path, string Name, Type ViewType, Type ModelType, IReadOnlyList<Type> Accepted) : BindingProblem(Path, Name)
{
    public override string Describe() => Accepted.Count == 0
        ? $"the {ViewType.Name} is not bindable (no IBindable<T>), the model is a {ModelType.Name}"
        : $"the {ViewType.Name} binds {string.Join(", ", Accepted.Select(type => type.Name))}, the model is a {ModelType.Name}";
}

/// <summary>A view binds the model through several interfaces, none more specific than the others.</summary>
public sealed record AmbiguousBinding(string Path, string Name, Type ViewType, IReadOnlyList<Type> Candidates) : BindingProblem(Path, Name)
{
    public override string Describe() => $"the {ViewType.Name} binds the model as {string.Join(" and ", Candidates.Select(type => type.Name))}; keep one";
}

/// <summary>Thrown by <see cref="BindingReport.ThrowIfAny"/>, listing every problem found.</summary>
public sealed class BindingException(IReadOnlyList<BindingProblem> problems)
    : Exception($"The scene doesn't match its models ({problems.Count} problem(s)):{Environment.NewLine}" + string.Join(Environment.NewLine, problems.Select(problem => $"  - {problem}")))
{
    public IReadOnlyList<BindingProblem> Problems { get; } = problems;
}

/// <summary>
/// Collects every binding problem of a scene, so they are reported together (<see cref="ThrowIfAny"/>) rather than
/// one per run.
/// </summary>
public sealed class BindingReport
{
    readonly List<BindingProblem> problems = [];

    public IReadOnlyList<BindingProblem> Problems => problems;
    public bool HasProblems => problems.Count > 0;

    public void Add(BindingProblem problem) => problems.Add(problem);

    public void ThrowIfAny()
    {
        if(HasProblems) throw new BindingException([ ..problems ]);
    }
}
