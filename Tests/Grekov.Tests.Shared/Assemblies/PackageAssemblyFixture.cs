using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Grekov.Abstractions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Materializations;

namespace Grekov.Tests.Shared.Assemblies;

public sealed class PackageAssemblyFixture
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "TestPackage");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference MaterializeAndUnload(DefValueConverter converter)
    {
        var context = new AssemblyLoadContext("Unload regression", isCollectible: true);

        try
        {
            context.LoadFromAssemblyPath(Path.Combine(_directory, "Grekov.TestDependency.dll"));
            var assembly = context.LoadFromAssemblyPath(Path.Combine(_directory, "Grekov.TestPackage.dll"));
            var instance = Activator.CreateInstance(assembly.GetType("Grekov.TestPackage.PackagedDef", throwOnError: true)!)!;
            DefFieldApplier.Apply(instance, new Dictionary<string, DefValue>
            {
                ["Amount"] = DefValue.ScalarValue("5")
            }, converter);
            return new WeakReference(context);
        }
        finally
        {
            context.Unload();
        }
    }
}
