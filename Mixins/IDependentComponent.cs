using Godot;

public interface IDependentComponent 
{
    void SetDependencies(IServiceProvider serviceProvider);

    public sealed void ResolveDependencies()
    {
        if (this is Node node)
        {
            var serviceProvider = node.GetTree().Root.GetNode<IGameManager>("GameManager").ServiceProvider;
            SetDependencies(serviceProvider);
        }
    }
}