using System;
using Godot;

/// <summary>
/// Finds what a node's ancestors provide (<see cref="IProvide{T}"/>): the nearest ancestor providing the type wins,
/// so a subtree can provide its own value in place of the one above it.
/// </summary>
public static class TreeDependencies
{
    /// <summary>The nearest ancestor of <paramref name="start"/> (itself excluded) that is a <typeparamref name="TFound"/>, or null.</summary>
    public static TFound? FindAncestor<TNode, TFound>(TNode start, Func<TNode, TNode?> parentOf) where TNode : class where TFound : class
    {
        for(var node = parentOf(start); node is not null; node = parentOf(node))
        {
            if(node is TFound found) return found;
        }
        return null;
    }

    /// <summary>
    /// The <typeparamref name="T"/> the nearest providing ancestor provides. Fails, naming the node, when no ancestor
    /// provides one.
    /// </summary>
    public static T DependOn<T>(this Node node)
    {
        var provider = FindAncestor<Node, IProvide<T>>(node, current => current.GetParent())
            ?? throw new InvalidOperationException($"{node.GetPath()} depends on a {typeof(T).Name}, but no ancestor provides one (IProvide<{typeof(T).Name}>)");
        return provider.Value();
    }

    /// <summary>The <typeparamref name="T"/> the nearest providing ancestor provides, or <paramref name="fallback"/> when none does.</summary>
    public static T DependOn<T>(this Node node, T fallback)
    {
        var provider = FindAncestor<Node, IProvide<T>>(node, current => current.GetParent());
        return provider is null ? fallback : provider.Value();
    }
}
