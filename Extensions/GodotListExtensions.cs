using System;
using Godot;
using Godot.Collections;

public static class GodotListExtensions
{
  public static void ForEach<[MustBeVariant] T>(this Array<T> collection, Action<int, T> action)
  {
    for (int i = 0; i < collection.Count; i++)
    {
      action(i, collection[i]);
    }
  }
}