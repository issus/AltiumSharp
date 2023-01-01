namespace OriginalCircuit.AltiumSharp
{
    public interface IComponent : IContainer
    {
        string Name { get; }

        string Description { get; }
    }
}
