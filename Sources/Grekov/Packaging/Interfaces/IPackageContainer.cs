using Grekov.Packaging.Enums;

namespace Grekov.Packaging.Interfaces;

public interface IPackageContainer : IDisposable
{
	bool Exists(PackageContentType type, string path);

	Stream OpenRead(PackageContentType type, string path);

	IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "");
}
