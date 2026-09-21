using Grekov.Core;

namespace Grekov.Definitions.Interfaces;

public interface IDefCatalog
{
	public T? Get<T>(DefId id) where T : Def;
	
	public T? Get<T>(string id) where T : Def;

	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def;

	public IEnumerable<Def> GetByPackage(string packageId);
	
	public T? GetByResourcePath<T>(string resourcePath) where T : Def;

	public IEnumerable<T> All<T>() where T : Def;

	public bool Contains<T>(DefId id) where T : Def;

	public bool Contains(Type type, DefId id);
	
	public T? FirstOrDefault<T>() where T : Def;

	public bool TryGetOrigin(Def def, out string packageId, out string resourcePath);
}
