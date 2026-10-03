using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Extensions;
using Grekov.Providers.FileSystem;
using Grekov.Readers.Xml.Extensions;
using Grekov.Storages.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Example;

internal static class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        Console.WriteLine("=== DI ===");

        services.AddLogging();
        
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        Console.WriteLine($"Registered services : {services.Count}");
        Console.WriteLine("[OK] Service provider built.");
        Console.WriteLine();

        var grekov = provider.GetRequiredService<IGrekovService>();
        var packageService = provider.GetRequiredService<IPackageService>();

        Console.WriteLine("=== START ===");

        await grekov.StartAsync();

        Console.WriteLine("[OK] Grekov started.");
        Console.WriteLine();

        PrintPackages(packageService);

        Console.WriteLine();
        Console.WriteLine("=== DEFINITIONS ===");

        var catalog = provider.GetRequiredService<IDefCatalog>();

        PrintDefinitions(catalog);

        Console.WriteLine();
        Console.WriteLine("=== STOP ===");

        await grekov.StopAsync();

        Console.WriteLine("[OK] Grekov stopped.");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddGrekov(grekov =>
        {
			grekov.Configure(options =>
			{
				options.ManifestName = "Manifest";
				options.AdditionalAssemblies.Add(typeof(SwordDef).Assembly);
			});
            grekov.Readers(readers => readers.Xml());
            grekov.Providers(providers => providers.FileSystem(opt => opt.Roots.Add(Path.Combine(AppContext.BaseDirectory, "Content", "Packages"))));
			grekov.Storage(storage => storage.InMemory());
        });
    }

    private static void PrintPackages(IPackageService packages)
    {
        Console.WriteLine("=== PACKAGES ===");

        if (packages.Packages.Count == 0)
        {
            Console.WriteLine("  <none>");
            return;
        }

        foreach (var package in packages.Packages)
        {
            Console.WriteLine($"  {package.Id} " + $"v{package.Version}");
            Console.WriteLine($"      Enabled : {package.Enabled}");
            Console.WriteLine($"      Errors  : {package.HasErrors}");
           
            if (package.Issues.Count <= 0)
                continue;
            
            Console.WriteLine("      Issues:");

            foreach (var issue in package.Issues) 
                Console.WriteLine($"          {issue}");
        }

        Console.WriteLine();
        Console.WriteLine("Load order:");

        foreach (var package in packages.LoadOrder)
        {
            Console.WriteLine($"  -> {package.Id} " + $"v{package.Version}");
        }
    }

    private static void PrintDefinitions(IDefCatalog catalog)
    {
        Console.WriteLine($"Catalog: {catalog.GetType().Name}");
        Console.WriteLine("[OK] IDefCatalog resolved from DI.");
    }
}

[DefType("Sword")]
public sealed class SwordDef : Def
{
	[DefField]
	public int Damage { get; set; }
}
