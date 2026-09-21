using Grekov.Packaging.Enums;

namespace Grekov.Packaging.Interfaces;

public interface IPackageContent : IDisposable
{
	public bool Exists(PackageContentType type, string path);
	
	public Stream OpenRead(PackageContentType type, string path);

	public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "");
}