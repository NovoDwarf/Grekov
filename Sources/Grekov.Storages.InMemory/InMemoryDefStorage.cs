using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Storage.InMemory;

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

		var types = GetAssignableDefTypes(definition.GetType());
		var enumerable = types as Type[] ?? [.. types];
		
		foreach (var type in enumerable)
		{
			if (_definitionsByType.TryGetValue(type, out var bucket) && bucket.TryGetValue(definition.Id, out var existing))
				throw new InvalidOperationException($"Definition [{definition.Id}] of type [{type.FullName}] is already registered. Existing package: [{existing.PackageId}], new package: [{definition.PackageId}].");
		}

		if (!string.IsNullOrWhiteSpace(definition.ResourcePath) && _definitionsByResourcePath.TryGetValue(definition.ResourcePath, out var existingByPath))
			throw new InvalidOperationException($"Definition resource [{definition.ResourcePath}] is already registered. Existing definition: [{existingByPath.Id}], new definition: [{definition.Id}].");

		foreach (var type in enumerable)
			GetBucket(type).Add(definition.Id, definition);

		if (!string.IsNullOrWhiteSpace(definition.ResourcePath))
			_definitionsByResourcePath.Add(definition.ResourcePath, definition);

		_origins.Add(definition, (definition.PackageId, definition.ResourcePath));
		return Task.CompletedTask;
	}
	
	public Task AddRangeAsync(List<Def> definitions, CancellationToken cancellationToken = default)
	{
		foreach (var def in definitions)
		{
			AddAsync(def, cancellationToken);
		}

		return Task.CompletedTask;
	}

	public Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var definitions = GetByPackage(packageId).ToArray();

		foreach (var definition in definitions)
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

	public Task UnloadAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		_definitionsByType.Clear();
		_definitionsByResourcePath.Clear();
		_origins.Clear();
		
		return Task.CompletedTask;
	}

	public T? Get<T>(DefId id) where T : Def
	{
		return GetBucketOrEmpty(typeof(T)).TryGetValue(id, out var definition) ? definition as T : null;
	}

	public T? Get<T>(string id) where T : Def
	{
		return Get<T>(DefId.Parse(id));
	}

	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
	{
		return GetBucketOrEmpty(typeof(T)).Values.Where(definition => SamePackage(definition, packageId)).Cast<T>();
	}

	public IEnumerable<Def> GetByPackage(string packageId)
	{
		return _origins.Keys.Where(definition => SamePackage(definition, packageId));
	}

	public T? GetByResourcePath<T>(string resourcePath) where T : Def
	{
		return _definitionsByResourcePath.TryGetValue(resourcePath, out var definition) ? definition as T : null;
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
		return _definitionsByType.TryGetValue(type, out var bucket) ? bucket : _definitionsByType[type] = [];
	}

	private IReadOnlyDictionary<DefId, Def> GetBucketOrEmpty(Type type)
	{
		return _definitionsByType.TryGetValue(type, out var bucket) ? bucket : EmptyBucket;
	}

	private static readonly IReadOnlyDictionary<DefId, Def> EmptyBucket = new Dictionary<DefId, Def>();
	
	private static bool SamePackage(Def definition, string packageId)
	{
		return string.Equals(definition.PackageId, packageId, StringComparison.OrdinalIgnoreCase);
	}

	private static IEnumerable<Type> GetAssignableDefTypes(Type type)
	{
		for (var current = type; current is not null && typeof(Def).IsAssignableFrom(current); current = current.BaseType)
			yield return current;
	}
}
