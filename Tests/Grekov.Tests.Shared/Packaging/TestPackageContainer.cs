using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Tests.Shared.Packaging;

internal sealed class TestPackageContainer : IPackageContainer
{
    public bool Exists(PackageContentType type, string path)
    {
        return false;
    }

    public Stream OpenRead(PackageContentType type, string path)
    {
        throw new FileNotFoundException(path);
    }

    public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "")
    {
        return [];
    }

    public void Dispose() { }
}
