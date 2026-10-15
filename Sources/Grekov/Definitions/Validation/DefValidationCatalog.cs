using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Validation;

internal sealed class DefValidationCatalog : IDefCatalog
{
	private readonly Dictionary<DefId, Def> _pending;
	private readonly IDefCatalog _stored;

	public DefValidationCatalog(IReadOnlyList<Def> definitions, IDefCatalog stored)
	{
		_pending = definitions.ToDictionary(static definition => definition.Id);
		_stored = stored;
	}

	public Def? Find(Type type, DefId id)
	{
		if (_pending.TryGetValue(id, out var definition))
		{
			return type.IsInstanceOfType(definition) ? definition : null;
		}

		return _stored.Find(type, id);
	}

	public T? Find<T>(DefId id) where T : Def
	{
		return Find(typeof(T), id) as T;
	}

	public T? Find<T>(string id) where T : Def
	{
		return Find<T>(DefId.Parse(id));
	}

	public Def Get(Type type, DefId id)
	{
		return Find(type, id)
		       ?? throw new KeyNotFoundException($"Definition [{id}] of type [{type.FullName}] was not found.");
	}

	public T Get<T>(DefId id) where T : Def
	{
		return (T)Get(typeof(T), id);
	}

	public T Get<T>(string id) where T : Def
	{
		return Get<T>(DefId.Parse(id));
	}

	public bool TryGet(Type type, DefId id, out Def? definition)
	{
		definition = Find(type, id);
		return definition is not null;
	}

	public bool TryGet<T>(DefId id, out T? definition) where T : Def
	{
		definition = Find<T>(id);
		return definition is not null;
	}

	public bool Contains(Type type, DefId id)
	{
		return Find(type, id) is not null;
	}

	public bool Contains<T>(DefId id) where T : Def
	{
		return Contains(typeof(T), id);
	}

	public IEnumerable<T> All<T>() where T : Def
	{
		return _pending.Values.OfType<T>()
		               .Concat(_stored.All<T>().Where(definition => !_pending.ContainsKey(definition.Id)));
	}

	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
	{
		return All<T>().Where(definition =>
			string.Equals(definition.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
	}

	public IEnumerable<Def> GetByPackage(string packageId)
	{
		return GetByPackage<Def>(packageId);
	}

	public T? GetByResourcePath<T>(string resourcePath) where T : Def
	{
		return All<T>().FirstOrDefault(definition =>
			string.Equals(definition.ResourcePath, resourcePath, StringComparison.OrdinalIgnoreCase));
	}

	public T? FirstOrDefault<T>() where T : Def
	{
		return All<T>().FirstOrDefault();
	}

	public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
	{
		if (_pending.TryGetValue(definition.Id, out var pending) && ReferenceEquals(pending, definition))
		{
			packageId = definition.PackageId;
			resourcePath = definition.ResourcePath;
			return true;
		}

		return _stored.TryGetOrigin(definition, out packageId, out resourcePath);
	}
}