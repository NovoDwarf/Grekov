using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Interfaces;

public interface IPackageContentLoader
{
	public int Stage { get; }
	
	public int Order => 0;

	public void BeginTransaction() { }

	public void CommitTransaction() { }

	public void RollbackTransaction() { }

	public void ClearStaging() { }
	
	public void Clear() { }

	public void LoadPackage(PackageLoadContext context);
	
	public void UnloadPackage(string packageId);
}
