using ArC.Common.Exceptions;
using Godot;

public static class TypeHelper
{
    public static T MustBeANode<T>(object obj) where T : Node
    {
        if (!typeof(T).IsAssignableFrom(obj.GetType()))
        {
            throw new DevelopmentException(string.Format("{0} must be a {1}", obj.GetType(), typeof(T)));
        }
        return (T)obj;
    }
}