using Grekov.Packaging.Enums;

namespace Grekov.Packaging.Interfaces;

/// <summary>
/// Provides read access to the data that belongs to a package.
/// </summary>
public interface IPackageStorage : IDisposable
{
	bool Exists(PackageContentType type, string path);

	Stream OpenRead(PackageContentType type, string path);

	IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "");
}
