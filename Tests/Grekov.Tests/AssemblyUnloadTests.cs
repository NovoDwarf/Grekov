using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Grekov.Abstractions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Materializations;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests;

public sealed class AssemblyUnloadTests
{
    [Fact]
    public void MaterializedTypesDoNotKeepPackageAlive()
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "TestPackage");
        using var services = new ServiceCollection().BuildServiceProvider();
        var converter = new DefValueConverter(new DefValueConverterRegistry(services));

        // Act
        var reference = MaterializeAndUnload(directory, converter);

        for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Assert
        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MaterializeAndUnload(string directory, DefValueConverter converter)
    {
        var context = new AssemblyLoadContext("Unload regression", isCollectible: true);
        context.LoadFromAssemblyPath(Path.Combine(directory, "Grekov.TestDependency.dll"));
        var assembly = context.LoadFromAssemblyPath(Path.Combine(directory, "Grekov.TestPackage.dll"));
        var instance = Activator.CreateInstance(assembly.GetType("Grekov.TestPackage.PackagedDef", throwOnError: true)!)!;
        DefFieldApplier.Apply(instance, new Dictionary<string, DefValue>
        {
            ["Amount"] = DefValue.ScalarValue("5")
        }, converter);
        var reference = new WeakReference(context);
        context.Unload();
        return reference;
    }
}
