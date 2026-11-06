using System.Collections.Generic;
using Godot;

/// <summary>
/// The unique-named nodes (<c>%Name</c>) of one scene instance: the nodes <paramref name="owner"/> owns. A scene
/// instanced inside another keeps its own unique names, so two play areas of the same scene don't clash.
/// </summary>
public static class UniqueNodes
{
    /// <summary>Every unique-named node owned by <paramref name="owner"/>, by name.</summary>
    public static Dictionary<string, Node> Of(Node owner)
    {
        var nodes = new Dictionary<string, Node>();
        Collect(owner, owner, nodes);
        return nodes;
    }

    /// <summary>The unique-named nodes owned by <paramref name="owner"/> that bind a model (<see cref="IBindable{TModel}"/>), by name.</summary>
    public static Dictionary<string, Node> BindableOf(Node owner)
    {
        var nodes = new Dictionary<string, Node>();
        foreach(var (name, node) in Of(owner))
        {
            if(Bindings.IsBindable(node)) nodes[name] = node;
        }
        return nodes;
    }

    static void Collect(Node node, Node owner, Dictionary<string, Node> nodes)
    {
        foreach(var child in node.GetChildren())
        {
            if(child.UniqueNameInOwner && child.Owner == owner)
            {
                nodes[child.Name] = child;
            }
            // Nodes of instanced scenes are owned by the instance, but may still be owned by owner when editable
            Collect(child, owner, nodes);
        }
    }
}
