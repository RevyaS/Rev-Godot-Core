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
    int count = 0;
    node.GetChildren().ToList().ForEach(child =>
    {
      if(child is ChildType)
      {
          count++;
      }
    });

    return count;
  }

  public static bool HasChild<[MustBeVariant] ChildType>(this Node node)
  {
    return GetChildCount<ChildType>(node) > 0;
  }
}