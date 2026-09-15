using Grekov.Core;

namespace Grekov.Definitions.Indexing;

public sealed class DefIndex
{
    private readonly Dictionary<Type, Dictionary<DefId, Def>> _defsByTypeAndId = [];
    private readonly Dictionary<Def, (string PackageId, string ResourcePath)> _origins = [];

    public void Add(Def definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var type = definition.GetType();

        while (type is not null && typeof(Def).IsAssignableFrom(type))
        {
            var bucket = GetBucket(type);

            if (!bucket.TryAdd(definition.Id, definition))
            {
                var existing = bucket[definition.Id];

                throw new InvalidOperationException(
                    $"Definition [{definition.Id}] of type [{type.FullName}] is already registered. " +
                    $"Existing package: [{existing.PackageId}], new package: [{definition.PackageId}].");
            }

            type = type.BaseType;
        }

        _origins[definition] = (definition.PackageId, definition.ResourcePath);
    }

    public bool Contains(Type type, DefId id)
    {
        return _defsByTypeAndId.TryGetValue(type, out var bucket) && bucket.ContainsKey(id);
    }

    public bool Contains<T>(DefId id) where T : Def
    {
        return Contains(typeof(T), id);
    }

    public T? Get<T>(DefId id) where T : Def
    {
        return Get(typeof(T), id) as T;
    }

    public Def? Get(Type type, DefId id)
    {
        return _defsByTypeAndId.TryGetValue(type, out var bucket) && bucket.TryGetValue(id, out var definition)
            ? definition
            : null;
    }

    public IEnumerable<T> All<T>() where T : Def
    {
        return _defsByTypeAndId.TryGetValue(typeof(T), out var bucket) 
            ? bucket.Values.Cast<T>()
            : [];
    }

    public IEnumerable<Def> All()
    {
        return _origins.Keys;
    }

    public void RemovePackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var bucket in _defsByTypeAndId.Values)
        {
            var definitions = bucket
                              .Where(pair => string.Equals(pair.Value.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                              .Select(pair => pair.Key)
                              .ToArray();

            foreach (var id in definitions)
            {
                bucket.Remove(id);
            }
        }

        var origins = _origins
                      .Where(pair => string.Equals(pair.Value.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                      .Select(pair => pair.Key)
                      .ToArray();

        foreach (var definition in origins)
        {
            _origins.Remove(definition);
        }
    }

    public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_origins.TryGetValue(definition, out var origin))
        {
            packageId = origin.PackageId;
            resourcePath = origin.ResourcePath;

            return true;
        }

        packageId = string.Empty;
        resourcePath = string.Empty;

        return false;
    }

    public void Clear()
    {
        _defsByTypeAndId.Clear();
        _origins.Clear();
    }

    private Dictionary<DefId, Def> GetBucket(Type type)
    {
        if (_defsByTypeAndId.TryGetValue(type, out var bucket)) 
            return bucket;
        
        bucket = new Dictionary<DefId, Def>();

        _defsByTypeAndId[type] = bucket;

        return bucket;
    }
}