using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Storages.InMemory;

public sealed class InMemoryDefStorage : IDefStorage
{
    private readonly Dictionary<Type, Dictionary<DefId, Def>> _definitionsByType = [];
    private readonly Dictionary<string, Def> _definitionsByResourcePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Def, (string PackageId, string ResourcePath)> _origins = new(ReferenceEqualityComparer.Instance);

    public Task LoadAsync(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task AddAsync(Def definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var types = GetAssignableDefTypes(definition.GetType()).ToArray();

        foreach (var type in types)
            if (GetBucketOrEmpty(type).ContainsKey(definition.Id))
                throw new InvalidOperationException($"Definition [{definition.Id}] of type [{type.FullName}] is already registered.");

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath) && _definitionsByResourcePath.ContainsKey(definition.ResourcePath))
            throw new InvalidOperationException($"Definition resource [{definition.ResourcePath}] is already registered.");

        foreach (var type in types)
            GetBucket(type).Add(definition.Id, definition);

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath))
            _definitionsByResourcePath.Add(definition.ResourcePath, definition);

        _origins.Add(definition, (definition.PackageId, definition.ResourcePath));
        
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(List<Def> definitions, CancellationToken cancellationToken = default)
    {
        foreach (var definition in definitions) 
            await AddAsync(definition, cancellationToken);
    }

    public Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var definition in GetByPackage(packageId).ToArray())
        {
            foreach (var type in GetAssignableDefTypes(definition.GetType()))
            {
                var bucket = _definitionsByType[type];

                bucket.Remove(definition.Id);

                if (bucket.Count == 0)
                    _definitionsByType.Remove(type);
            }

            if (!string.IsNullOrWhiteSpace(definition.ResourcePath))
                _definitionsByResourcePath.Remove(definition.ResourcePath);

            _origins.Remove(definition);
        }

        return Task.CompletedTask;
    }

    public Task UnloadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _definitionsByType.Clear();
        _definitionsByResourcePath.Clear();
        _origins.Clear();

        return Task.CompletedTask;
    }

    public T Get<T>(DefId id) where T : Def
    {
        if (GetBucketOrEmpty(typeof(T)).TryGetValue(id, out var definition))
            return (T)definition;

        throw new KeyNotFoundException($"Definition [{id}] of type [{typeof(T).FullName}] was not found.");
    }

    public T Get<T>(string id) where T : Def
    {
        return Get<T>(DefId.Parse(id));
    }

    public T? Find<T>(DefId id) where T : Def
    {
        return GetBucketOrEmpty(typeof(T)).TryGetValue(id, out var definition)
            ? (T)definition
            : null;
    }

    public T? Find<T>(string id) where T : Def
    {
        return Find<T>(DefId.Parse(id));
    }

    public Def Get(Type type, DefId id)
    {
        if (GetBucketOrEmpty(type).TryGetValue(id, out var definition))
            return definition;

        throw new KeyNotFoundException($"Definition [{id}] of type [{type.FullName}] was not found.");
    }

    public Def? Find(Type type, DefId id)
    {
        return GetBucketOrEmpty(type).GetValueOrDefault(id);
    }

    public bool TryGet<T>(DefId id, out T? definition) where T : Def
    {
        if (GetBucketOrEmpty(typeof(T)).TryGetValue(id, out var value))
        {
            definition = (T)value;
            return true;
        }

        definition = null;
        return false;
    }

    public bool TryGet(Type type, DefId id, out Def? definition)
    {
        return GetBucketOrEmpty(type).TryGetValue(id, out definition);
    }

    public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
    {
        return GetBucketOrEmpty(typeof(T)).Values
            .Where(definition => SamePackage(definition, packageId))
            .Cast<T>();
    }

    public IEnumerable<Def> GetByPackage(string packageId)
    {
        return _origins.Keys.Where(definition => SamePackage(definition, packageId));
    }

    public T? GetByResourcePath<T>(string resourcePath) where T : Def
    {
        return _definitionsByResourcePath.TryGetValue(resourcePath, out var definition)
            ? definition as T
            : null;
    }

    public IEnumerable<T> All<T>() where T : Def
    {
        return GetBucketOrEmpty(typeof(T)).Values.Cast<T>();
    }

    public bool Contains<T>(DefId id) where T : Def
    {
        return Contains(typeof(T), id);
    }

    public bool Contains(Type type, DefId id)
    {
        return GetBucketOrEmpty(type).ContainsKey(id);
    }

    public T? FirstOrDefault<T>() where T : Def
    {
        return All<T>().FirstOrDefault();
    }

    public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
    {
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

    private Dictionary<DefId, Def> GetBucket(Type type)
    {
        return _definitionsByType.TryGetValue(type, out var bucket)
            ? bucket
            : _definitionsByType[type] = [];
    }

    private IReadOnlyDictionary<DefId, Def> GetBucketOrEmpty(Type type)
    {
        return _definitionsByType.TryGetValue(type, out var bucket)
            ? bucket
            : EmptyBucket;
    }

    private static readonly IReadOnlyDictionary<DefId, Def> EmptyBucket = new Dictionary<DefId, Def>();

    private static bool SamePackage(Def definition, string packageId)
    {
        return string.Equals(definition.PackageId, packageId, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<Type> GetAssignableDefTypes(Type type)
    {
        for (var current = type; current is not null && typeof(Def).IsAssignableFrom(current); current = current.BaseType)
        {
            yield return current;
        }
    }
}
