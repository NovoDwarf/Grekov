using Grekov.Core;

namespace Grekov.Definitions.Indexing;

public sealed class DefIndex
{
    private readonly Dictionary<Type, Dictionary<DefId, Def>> _defsByTypeAndId = [];
    private readonly Dictionary<string, Def> _defsByResourcePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Def, (string PackageId, string ResourcePath)> _origins = new(ReferenceEqualityComparer.Instance);

    public void Add(Def definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var type = definition.GetType();
        var types = new List<Type>();

        while (type is not null && typeof(Def).IsAssignableFrom(type))
        {
            types.Add(type);
            type = type.BaseType;
        }

        foreach (var currentType in types)
            if (_defsByTypeAndId.TryGetValue(currentType, out var bucket) && bucket.TryGetValue(definition.Id, out var existing))
                throw new InvalidOperationException($"Definition [{definition.Id}] of type [{currentType.FullName}] is already registered. Existing package: [{existing.PackageId}], new package: [{definition.PackageId}].");

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath) && _defsByResourcePath.TryGetValue(definition.ResourcePath, out var existing1))
            throw new InvalidOperationException($"Definition resource [{definition.ResourcePath}] is already registered. Existing definition: [{existing1.Id}], new definition: [{definition.Id}].");

        foreach (var currentType in types)
        {
            var bucket = GetBucket(currentType);
            bucket.Add(definition.Id, definition);
        }

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath))
            _defsByResourcePath.Add(definition.ResourcePath, definition);

        _origins.Add(
            definition,
            (definition.PackageId, definition.ResourcePath));
    }
    
    public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return _defsByTypeAndId.TryGetValue(typeof(T), out var bucket)
            ? bucket.Values.Where(def => string.Equals(def.PackageId, packageId, StringComparison.OrdinalIgnoreCase)).Cast<T>()
            : [];
    }

    public IEnumerable<Def> GetByPackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return _origins
               .Where(pair => string.Equals(pair.Value.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
               .Select(static pair => pair.Key);
    }

    public bool Contains(Type type, DefId id)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _defsByTypeAndId.TryGetValue(type, out var bucket) && bucket.ContainsKey(id);
    }

    public bool Contains<T>(DefId id) where T : Def
    {
        return Contains(typeof(T), id);
    }

    public T? Get<T>(DefId id) where T : Def
    {
        if (!_defsByTypeAndId.TryGetValue(typeof(T), out var bucket))
            return null;

        return bucket.TryGetValue(id, out var definition)
            ? (T)definition 
            : null;
    }

    public T? GetByResourcePath<T>(string resourcePath)
        where T : Def
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        if (!_defsByResourcePath.TryGetValue(resourcePath, out var definition))
            return null;

        return definition is T typed
            ? typed
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

    public void RemovePackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var definitionsToRemove = _origins
                                  .Where(pair => string.Equals(pair.Value.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                                  .Select(static pair => pair.Key)
                                  .ToArray();

        foreach (var definition in definitionsToRemove)
        {
            var type = definition.GetType();

            while (type is not null && typeof(Def).IsAssignableFrom(type))
            {
                if (_defsByTypeAndId.TryGetValue(type, out var bucket))
                {
                    bucket.Remove(definition.Id);

                    if (bucket.Count == 0) 
                        _defsByTypeAndId.Remove(type);
                }

                type = type.BaseType;
            }

            _origins.Remove(definition);
        }
    }
    
    private Dictionary<DefId, Def> GetBucket(Type type)
    {
        if (_defsByTypeAndId.TryGetValue(type, out var bucket))
            return bucket;

        bucket = new Dictionary<DefId, Def>();
        _defsByTypeAndId.Add(type, bucket);

        return bucket;
    }
}