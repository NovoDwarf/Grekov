using Grekov.Core;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultDefCatalog : IDefCatalog
{
	private readonly DefIndex _index;

	public DefaultDefCatalog(DefIndex index)
	{
		ArgumentNullException.ThrowIfNull(index);

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

	public T? GetByPath<T>(string? resourcePath) where T : Def
	{
		return _index.Get<T>(DefId.Parse(resourcePath)); // TODO: fix add GetByPath<T>
	}

	public IEnumerable<T> All<T>() where T : Def
	{
		return _index.All<T>();
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