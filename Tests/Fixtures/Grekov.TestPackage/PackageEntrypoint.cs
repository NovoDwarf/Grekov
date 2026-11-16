using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.TestPackage;

public sealed class PackageEntrypoint : IEntrypoint
{
    public static bool ThrowOnUnload { get; set; }
    public static int UnloadCount { get; private set; }

    public void OnLoad(IPackageContext context)
    {
    }

    public void OnLoaded()
    {
    }

    public void OnUnload()
    {
        UnloadCount++;

        if (ThrowOnUnload)
        {
            throw new InvalidOperationException("Test entrypoint unload failed.");
        }
    }
}
