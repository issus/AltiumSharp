using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Implementation of a schematic symbol library.
/// </summary>
public sealed class SchLibrary : ISchLibrary
{
    private readonly List<ISchComponent> _components = new();
    private readonly Dictionary<string, ISchComponent> _componentsByName = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<IComponent>? _allComponentsCache;

    /// <summary>
    /// Diagnostics collected during file reading (warnings about skipped records, parse errors, etc.).
    /// </summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; internal set; } = Array.Empty<AltiumDiagnostic>();

    /// <summary>
    /// Section keys mapping preserved for round-trip fidelity.
    /// </summary>
    internal Dictionary<string, string>? SectionKeys { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ISchComponent> Components => _components;

    /// <inheritdoc />
    public IReadOnlyList<IComponent> AllComponents =>
        _allComponentsCache ??= _components.Cast<IComponent>().ToList();

    /// <inheritdoc />
    public int Count => _components.Count;

    /// <inheritdoc />
    public ISchComponent? this[string name] =>
        _componentsByName.TryGetValue(name, out var component) ? component : null;

    /// <inheritdoc />
    public bool Contains(string name) => _componentsByName.ContainsKey(name);

    /// <inheritdoc />
    public void Add(ISchComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (string.IsNullOrEmpty(component.Name))
            throw new ArgumentException("Component must have a name", nameof(component));

        if (_componentsByName.ContainsKey(component.Name))
            throw new ArgumentException($"A component named '{component.Name}' already exists", nameof(component));

        _components.Add(component);
        _componentsByName[component.Name] = component;
        _allComponentsCache = null; // Invalidate cache
    }

    /// <inheritdoc />
    public bool Remove(string name)
    {
        if (!_componentsByName.TryGetValue(name, out var component))
            return false;

        _components.Remove(component);
        _componentsByName.Remove(name);
        _allComponentsCache = null; // Invalidate cache
        return true;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new SaveOptions();

        var mode = options.Overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await SaveAsync(stream, options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        var writer = new SchLibWriter();
        await writer.WriteAsync(this, stream, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // No unmanaged resources to dispose
        return ValueTask.CompletedTask;
    }
}
