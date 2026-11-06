using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

/// <summary>
/// Matches views to models by name and binds them through <see cref="IBindable{TModel}"/>, reporting what doesn't fit
/// in a <see cref="BindingReport"/> instead of failing on the first problem.
/// </summary>
public static class Bindings
{
    static readonly ConcurrentDictionary<Type, Type[]> BindableModelTypes = new();

    /// <summary>The model types a view binds (its <see cref="IBindable{TModel}"/> type arguments); empty when it binds none.</summary>
    public static IReadOnlyList<Type> ModelTypesOf(Type viewType) => BindableModelTypes.GetOrAdd(viewType, type => type.GetInterfaces()
        .Where(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IBindable<>))
        .Select(implemented => implemented.GetGenericArguments()[0])
        .ToArray());

    public static bool IsBindable(object view) => ModelTypesOf(view.GetType()).Count > 0;

    /// <summary>
    /// Pairs <paramref name="models"/> with <paramref name="views"/> of the same name. Models without a view and views
    /// without a model are reported, except the names in <paramref name="unmatchedAllowed"/> (e.g. a zone shown by a
    /// view bound in code instead).
    /// </summary>
    public static IReadOnlyList<(string Name, TModel Model, TView View)> MatchByName<TModel, TView>(
        IReadOnlyDictionary<string, TModel> models, IReadOnlyDictionary<string, TView> views,
        string path, BindingReport report, IReadOnlySet<string>? unmatchedAllowed = null)
        where TModel : notnull where TView : notnull
    {
        var matches = new List<(string, TModel, TView)>();
        foreach(var (name, model) in models.OrderBy(model => model.Key, StringComparer.Ordinal))
        {
            if(views.TryGetValue(name, out var view))
            {
                matches.Add((name, model, view));
            }
            else if(unmatchedAllowed?.Contains(name) != true)
            {
                report.Add(new MissingView(path, name, model.GetType()));
            }
        }
        foreach(var (name, view) in views.OrderBy(view => view.Key, StringComparer.Ordinal))
        {
            if(!models.ContainsKey(name) && unmatchedAllowed?.Contains(name) != true)
            {
                report.Add(new UnusedView(path, name, view.GetType()));
            }
        }
        return matches;
    }

    /// <summary>
    /// Binds <paramref name="view"/> to <paramref name="model"/> through its most specific
    /// <see cref="IBindable{TModel}"/> accepting the model. Reports (and returns false) when none accepts it, or when
    /// several accept it and none is more specific.
    /// </summary>
    public static bool TryBind(object view, object model, string path, string name, BindingReport report)
    {
        var modelTypes = ModelTypesOf(view.GetType());
        var accepting = modelTypes.Where(type => type.IsInstanceOfType(model)).ToList();
        if(accepting.Count == 0)
        {
            report.Add(new TypeMismatch(path, name, view.GetType(), model.GetType(), modelTypes));
            return false;
        }

        // The most specific: one every other accepting type is a base of
        var mostSpecific = accepting.Where(candidate => accepting.All(other => other.IsAssignableFrom(candidate))).ToList();
        if(mostSpecific.Count != 1)
        {
            report.Add(new AmbiguousBinding(path, name, view.GetType(), accepting));
            return false;
        }

        Invoke(view, mostSpecific[0], model);
        return true;
    }

    /// <summary>
    /// <see cref="TryBind"/> for every pair of <see cref="MatchByName"/>. Returns the pairs that were bound.
    /// </summary>
    public static IReadOnlyList<(string Name, TModel Model, TView View)> BindByName<TModel, TView>(
        IReadOnlyDictionary<string, TModel> models, IReadOnlyDictionary<string, TView> views,
        string path, BindingReport report, IReadOnlySet<string>? unmatchedAllowed = null)
        where TModel : notnull where TView : notnull
    {
        return MatchByName(models, views, path, report, unmatchedAllowed)
            .Where(match => TryBind(match.View, match.Model, path, match.Name, report))
            .ToList();
    }

    static void Invoke(object view, Type modelType, object model)
    {
        var bind = typeof(IBindable<>).MakeGenericType(modelType).GetMethod(nameof(IBindable<object>.Bind))!;
        try
        {
            bind.Invoke(view, [ model ]);
        }
        catch(TargetInvocationException e) when (e.InnerException is not null)
        {
            // The view's own exception, not reflection's wrapper
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
        }
    }
}
