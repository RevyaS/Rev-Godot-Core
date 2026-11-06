/// <summary>
/// A view (usually a node) that shows <typeparamref name="TModel"/>. Contravariant: a view written for a base type
/// (e.g. any card zone) binds to every model deriving from it, while one written for a specific type only binds to
/// that type. Anything else a view needs (a session, a presenter) comes from its ancestors
/// (<see cref="IProvide{T}"/>), so binding takes the model alone.
/// </summary>
public interface IBindable<in TModel>
{
    void Bind(TModel model);
}

/// <summary>
/// A node that provides a <typeparamref name="T"/> to its descendants (see <see cref="TreeDependencies"/>). A node
/// providing several types implements this once per type, explicitly.
/// </summary>
public interface IProvide<out T>
{
    T Value();
}
