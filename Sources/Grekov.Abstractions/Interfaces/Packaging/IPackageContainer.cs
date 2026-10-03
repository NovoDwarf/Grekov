namespace Grekov.Abstractions.Interfaces.Packaging;

public interface IPackageContainer : IDisposable
{
	public bool Exists(PackageContentType type, string path);

	public Stream OpenRead(PackageContentType type, string path);

	public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "");
}
