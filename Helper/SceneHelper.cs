using Godot;

public static class SceneHelper
{
    public static T GetScene<[MustBeVariant] T>(string path) where T : class
    {
        var componentList = GD.Load<PackedScene>(path);
        return componentList.Instantiate<T>();
    }
}