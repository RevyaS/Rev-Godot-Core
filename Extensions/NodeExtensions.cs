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
}