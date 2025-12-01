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
}