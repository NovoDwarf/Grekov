namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefCatalog
{
	public T Get<T>(DefId id) where T : Def;
	
	public T Get<T>(string id) where T : Def;

	public T? Find<T>(DefId id) where T : Def;
	
	public T? Find<T>(string id) where T : Def;

	public Def Get(Type type, DefId id);
	
	public Def? Find(Type type, DefId id);

	public bool TryGet<T>(DefId id, out T? definition) where T : Def;
	
	public bool TryGet(Type type, DefId id, out Def? definition);

	public bool Contains<T>(DefId id) where T : Def;
	
	public bool Contains(Type type, DefId id);

	public IEnumerable<T> All<T>() where T : Def;
	
	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def;

	public IEnumerable<Def> GetByPackage(string packageId);

	public T? GetByResourcePath<T>(string resourcePath) where T : Def;

	public T? FirstOrDefault<T>() where T : Def;

	public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath);
}
