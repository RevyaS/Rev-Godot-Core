using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class NodeExtensions
{
  private static string fontColorOverride = "theme_override_colors/font_color";
  public static void ClearChildren(this Node node)
  {
    node.GetChildren().ForEach((_, child) => child.QueueFree());
  }

  /// <summary>Like <see cref="ClearChildren"/>, but the children leave the tree now so containers don't lay them out this frame.</summary>
  public static void FreeChildren(this Node node)
  {
    foreach (var child in node.GetChildren())
    {
      node.RemoveChild(child);
      child.QueueFree();
    }
  }

  public static void SetFontColor(this Control control, Color color)
  {
    control.Set(fontColorOverride, color);
  }

  public static int GetChildCount<[MustBeVariant] ChildType>(this Node node)
  {
    return GetChildren<ChildType>(node).Count;
  }

  public static List<ChildType> GetChildren<[MustBeVariant] ChildType>(this Node node)
  {
    return node.GetChildren().Where(child => child is ChildType).Cast<ChildType>().ToList();
  }

  public static bool HasChild<[MustBeVariant] ChildType>(this Node node)
  {
    return GetChildCount<ChildType>(node) > 0;
  }

  public static ChildType FirstChild<[MustBeVariant] ChildType>(this Node node, Func<ChildType, bool> predicate) where ChildType : Node
  {
    return (ChildType)node.GetChildren().First(x => x is ChildType child && predicate(child));
  }

  public static ChildType FirstChild<[MustBeVariant] ChildType>(this Node node) where ChildType : Node
  {
    return (ChildType)node.GetChildren().First(x => x is ChildType child);
  }

  /// <summary>
  /// Every node under <paramref name="node"/>, depth first, in tree order. With <paramref name="where"/>, a child it
  /// rejects is skipped along with everything under it (e.g. not looking inside cards).
  /// </summary>
  public static IEnumerable<Node> Descendants(this Node node, Func<Node, bool>? where = null)
  {
    foreach (var child in node.GetChildren())
    {
      if (where is not null && !where(child)) continue;
      yield return child;
      foreach (var descendant in child.Descendants(where)) yield return descendant;
    }
  }

  /// <summary>Every node under <paramref name="node"/> that is a <typeparamref name="T"/> (a node type or an interface), depth first.</summary>
  public static IEnumerable<T> Descendants<T>(this Node node) => node.Descendants().OfType<T>();

  /// <summary>The node and every control under it let the mouse through (e.g. a card shown inside something that takes every press itself).</summary>
  public static void IgnoreMouse(this Node node)
  {
    if (node is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
    foreach (var descendant in node.Descendants<Control>()) descendant.MouseFilter = Control.MouseFilterEnum.Ignore;
  }
}
