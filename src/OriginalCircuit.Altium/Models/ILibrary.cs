namespace OriginalCircuit.Altium.Models;

/// <summary>
/// Base interface for all library types, providing read-only access to components.
/// </summary>
public interface ILibrary : IAsyncDisposable
{
    /// <summary>
    /// All components in this library (read-only, non-generic access).
    /// </summary>
    IReadOnlyList<IComponent> AllComponents { get; }

    /// <summary>
    /// Number of components in this library.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks if a component with the given name exists.
    /// </summary>
    bool Contains(string name);

    /// <summary>
    /// Removes a component by name.
    /// </summary>
    /// <returns>True if the component was found and removed.</returns>
    bool Remove(string name);

    /// <summary>
    /// Saves the library to a file.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    /// <param name="options">Optional save options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the library to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    /// <param name="options">Optional save options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a library file containing multiple components with typed access.
/// </summary>
/// <typeparam name="TComponent">The type of component in this library.</typeparam>
public interface ILibrary<TComponent> : ILibrary
    where TComponent : IComponent
{
    /// <summary>
    /// All components in this library (typed access).
    /// </summary>
    IReadOnlyList<TComponent> Components { get; }

    /// <summary>
    /// Gets a component by name.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <returns>The component, or null if not found.</returns>
    TComponent? this[string name] { get; }

    /// <summary>
    /// Adds a component to the library.
    /// </summary>
    void Add(TComponent component);
}

/// <summary>
/// Options for saving a library file.
/// </summary>
public sealed class SaveOptions
{
    /// <summary>
    /// Whether to overwrite an existing file. Default is true.
    /// </summary>
    public bool Overwrite { get; set; } = true;
}
