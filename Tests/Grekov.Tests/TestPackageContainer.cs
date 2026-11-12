using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Tests;

internal sealed class TestPackageContainer : IPackageContainer
{
    public bool Exists(PackageContentType type, string path) => false;
    public Stream OpenRead(PackageContentType type, string path) => throw new FileNotFoundException(path);
    public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "") => [];
    public void Dispose() { }
}
