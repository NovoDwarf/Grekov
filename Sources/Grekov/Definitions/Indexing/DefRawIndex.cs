using Grekov.Abstractions;

namespace Grekov.Definitions.Indexing;

internal sealed class DefRawIndex
{
    private readonly Dictionary<DefId, DefRaw> _definitions = new();

    public IReadOnlyCollection<DefRaw> All => _definitions.Values;

    public int Count => _definitions.Count;

    public void Add(DefRaw definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_definitions.TryAdd(definition.Id, definition))
            throw new InvalidOperationException($"Raw definition [{definition.Id}] is already present in the index. Resource: [{definition.ResourcePath}].");
    }

    public bool TryGet(DefId id, out DefRaw definition)
    {
        return _definitions.TryGetValue(id, out definition!);
    }

    public DefRaw Get(DefId id)
    {
        if (!_definitions.TryGetValue(id, out var definition))
            throw new KeyNotFoundException($"Raw definition [{id}] was not found");

        return definition;
    }

    public IEnumerable<DefRaw> GetByPackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        return _definitions.Values.Where(definition => string.Equals(definition.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
    }

    public void UnloadPackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var ids = _definitions
                  .Where(pair => string.Equals(
                      pair.Value.PackageId,
                      packageId,
                      StringComparison.OrdinalIgnoreCase))
                  .Select(static pair => pair.Key)
                  .ToArray();

        foreach (var id in ids)
            _definitions.Remove(id);
    }

    public void Clear()
    {
        _definitions.Clear();
    }
}