using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Storages.InMemory;

public sealed class InMemoryDefStorage : IDefStorage
{

    private static readonly IEqualityComparer<(string PackageId, string ResourcePath)> ResourceComparer =
        EqualityComparer<(string PackageId, string ResourcePath)>.Create(
            static (left, right) =>
                StringComparer.OrdinalIgnoreCase.Equals(left.PackageId, right.PackageId)
                && StringComparer.OrdinalIgnoreCase.Equals(left.ResourcePath, right.ResourcePath),
            static key => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.PackageId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.ResourcePath)));

    private readonly Dictionary<Type, Dictionary<DefId, Def>> _definitionsByType = [];
    private readonly Dictionary<(string PackageId, string ResourcePath), Def> _definitionsByResourcePath = new(ResourceComparer);
    private readonly Dictionary<Def, (string PackageId, string ResourcePath)> _origins = new(ReferenceEqualityComparer.Instance);

    public Task LoadAsync(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task AddAsync(Def definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCanAdd(definition);
        AddDefinition(definition);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(List<Def> definitions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var ids = new HashSet<DefId>();
        var resources = new HashSet<(string PackageId, string ResourcePath)>(ResourceComparer);

        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureCanAdd(definition);

            if (!ids.Add(definition.Id))
            {
                throw new InvalidOperationException($"Definition [{definition.Id}] occurs more than once in the batch.");
            }

            if (!string.IsNullOrWhiteSpace(definition.ResourcePath)
                && !resources.Add((definition.PackageId, definition.ResourcePath)))
            {
                throw new InvalidOperationException(
                    $"Definition resource [{definition.ResourcePath}] occurs more than once in package [{definition.PackageId}].");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var definition in definitions)
            AddDefinition(definition);

        return Task.CompletedTask;
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
                _definitionsByResourcePath.Remove((definition.PackageId, definition.ResourcePath));

            _origins.Remove(definition);
        }

        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default)
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
        T? result = null;

        foreach (var entry in _definitionsByResourcePath)
        {
            if (!string.Equals(entry.Key.ResourcePath, resourcePath, StringComparison.OrdinalIgnoreCase) || entry.Value is not T candidate)
            {
                continue;
            }

            if (result is not null)
            {
                throw new InvalidOperationException($"Definition resource [{resourcePath}] is ambiguous. Specify a package ID.");
            }

            result = candidate;
        }

        return result;
    }

    public T? GetByResourcePath<T>(string packageId, string resourcePath) where T : Def
    {
        return _definitionsByResourcePath.TryGetValue((packageId, resourcePath), out var definition)
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

    private void EnsureCanAdd(Def definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (GetBucketOrEmpty(typeof(Def)).ContainsKey(definition.Id))
        {
            throw new InvalidOperationException($"Definition [{definition.Id}] is already registered.");
        }

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath)
            && _definitionsByResourcePath.ContainsKey((definition.PackageId, definition.ResourcePath)))
        {
            throw new InvalidOperationException($"Definition resource [{definition.ResourcePath}] is already registered in package [{definition.PackageId}].");
        }
    }

    private void AddDefinition(Def definition)
    {
        foreach (var type in GetAssignableDefTypes(definition.GetType()))
        {
            GetBucket(type).Add(definition.Id, definition);
        }

        if (!string.IsNullOrWhiteSpace(definition.ResourcePath))
        {
            _definitionsByResourcePath.Add((definition.PackageId, definition.ResourcePath), definition);
        }

        _origins.Add(definition, (definition.PackageId, definition.ResourcePath));
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
