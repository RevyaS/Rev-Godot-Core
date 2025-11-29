using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IChildManagerComponent
{
    sealed void ApplyToChildren<ChildType>(Action<ChildType> applyFunction)
    {
        if (this is Node node)
        {
            node.GetChildren().ForEach((_, node) =>
            {
                if (node is ChildType child)
                {
                    applyFunction(child);
                }
            });
        }
    }

    sealed bool HasChild<ChildType>(Func<ChildType, bool> predicate)
    {
        if (this is Node node)
        {
            foreach (var childNode in node.GetChildren())
            {
                if (childNode is ChildType child && predicate(child))
                {
                    return true;
                }
            }
        }
        return false;
    }

    sealed void ZipShortest<ChildType, T>(List<T> zippedList, Action<ChildType, T> applyFunction)
    {
        if (this is Node node)
        {
            var childrenEnumerator = node.GetChildren().Where(x => x is ChildType).Cast<ChildType>().GetEnumerator();
            var zippedEnumerator = zippedList.GetEnumerator();

            while (childrenEnumerator.MoveNext() && zippedEnumerator.MoveNext())
            {
                applyFunction(childrenEnumerator.Current, zippedEnumerator.Current);
            }
        }
    }

    sealed void ZipLongest<ChildType, T>(List<T> zippedList, Action<ChildType, T> applyFunction)
    {
        if (this is Node node)
        {
            var childrenEnumerator = node.GetChildren().Where(x => x is ChildType).Cast<ChildType>().GetEnumerator();
            var zippedEnumerator = zippedList.GetEnumerator();

            bool hasChild, hasZip;

            while ((hasChild = childrenEnumerator.MoveNext()) | (hasZip = zippedEnumerator.MoveNext()))
            {
                #pragma warning disable CS8604
                applyFunction(
                    hasChild ? childrenEnumerator.Current : default,
                    hasZip ? zippedEnumerator.Current : default);
                #pragma warning restore CS8604
            }
        }
    }
}