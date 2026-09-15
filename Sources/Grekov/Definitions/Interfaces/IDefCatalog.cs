using Grekov.Core;

namespace Grekov.Definitions.Interfaces;

public interface IDefCatalog
{
	public T? Get<T>(string id) where T : Def;

	public T? GetByPath<T>(string? resourcePath) where T : Def;

	public IEnumerable<T> All<T>() where T : Def;

	public T? FirstOrDefault<T>() where T : Def;

	public bool TryGetOrigin(Def def, out string packageId, out string resourcePath);
}
