namespace Grekov.Packaging.Interfaces;

public interface IPackageCatalog
{
	public IEnumerable<string> GetPackageDirectories();
}
