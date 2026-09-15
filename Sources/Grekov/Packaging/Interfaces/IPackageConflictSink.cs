namespace Grekov.Packaging.Interfaces;

public interface IPackageConflictSink
{
	public void Register(string key, string packageId);
}
