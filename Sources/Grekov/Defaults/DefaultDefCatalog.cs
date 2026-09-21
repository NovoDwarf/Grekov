using Grekov.Core;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultDefCatalog : IDefCatalog
{
	private readonly DefIndex _index;

	public DefaultDefCatalog(DefIndex index)
	{
		_index = index;
	}

	public T? Get<T>(DefId id) where T : Def
	{
		return _index.Get<T>(id);
	}

	public T? Get<T>(string id) where T : Def
	{
		return _index.Get<T>(DefId.Parse(id));
	}
	
	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
	{
		return _index.GetByPackage<T>(packageId);
	}
	
	public IEnumerable<Def> GetByPackage(string packageId)
	{
		return _index.GetByPackage(packageId);
	}

	public T? GetByResourcePath<T>(string resourcePath)
		where T : Def
	{
		return _index.GetByResourcePath<T>(resourcePath);
	}

	public IEnumerable<T> All<T>() where T : Def
	{
		return _index.All<T>();
	}

	public bool Contains<T>(DefId id) where T : Def
	{
		return _index.Contains<T>(id);
	}

	public bool Contains(Type type, DefId id)
	{
		return _index.Contains(type, id);
	}

	public T? FirstOrDefault<T>() where T : Def
	{
		return All<T>().FirstOrDefault();
	}

	public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
	{
		return _index.TryGetOrigin(definition, out packageId, out resourcePath);
	}
}