using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Grekov.Assemblies.Interfaces;
using Grekov.Assemblies.Services;
using Grekov.Core;
using Grekov.Defaults;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Inheritance;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Materializations;
using Grekov.Definitions.Registry;
using Grekov.Definitions.Scanning;
using Grekov.Definitions.Workflow;
using Grekov.Localizations.Interfaces;
using Grekov.Localizations.Services;
using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services.Conflicts;
using Grekov.Packaging.Services.Discovery;
using Grekov.Packaging.Services.Runtime;
using Grekov.Packaging.Services.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NovoDwarf.FS;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Extensions;

public sealed class GrekovOptions
{
	public List<string> PackageRoots { get; } = [];

	public List<Assembly> AdditionalAssemblies { get; } = [];
}

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGrekov(this IServiceCollection services, Action<GrekovOptions>? configure = null)
	{
		services.AddOptions<GrekovOptions>();

		if (configure is not null) 
			services.Configure(configure);

		services
			.AddCore()
			.AddAssemblies()
			.AddLocalization()
			.AddDefinitions()
			.AddRuntime();

		return services;
	}
	
	private static IServiceCollection AddCore(this IServiceCollection services)
	{
		services.TryAddSingleton<IFileService, DefaultFileService>();
		services.TryAddSingleton<IPathService, DefaultPathService>();

		services.TryAddSingleton<IPackagePathResolver, DefaultPathResolver>();
		services.TryAddSingleton<IPackageContextFactory, DefaultContextFactory>();
		services.TryAddSingleton<IPackageLoadOrderStore, DefaultLoadOrderStore>();
		
		services.TryAddSingleton<ILocalizationRuntime, DefaultLocalizationRuntime>();

		return services;
	}
	
	private static IServiceCollection AddAssemblies(this IServiceCollection services)
	{
		services.TryAddSingleton<AssemblyLocator>();
		services.TryAddSingleton<AssemblyRegistry>(provider =>
		{
			var options = provider.GetRequiredService<IOptions<GrekovOptions>>().Value;

			return new AssemblyRegistry(options.AdditionalAssemblies);
		});
		services.TryAddSingleton<EntrypointService>();
	
		services.TryAddSingleton<AssemblyService>();

		return services;
	}
	
	private static IServiceCollection AddLocalization(this IServiceCollection services)
	{
		services.TryAddSingleton<LocalizationServerState>();
		services.TryAddSingleton<TranslationRegistry>();
		services.TryAddSingleton<LocalizationService>();
		
		return services;
	}
	
	private static IServiceCollection AddDefinitions(this IServiceCollection services)
	{
		services.TryAddSingleton<DefIndex>();
		services.TryAddSingleton<DefRawIndex>();
		services.TryAddSingleton<DefReaderRegistry>();
		services.TryAddSingleton<DefScanner>();
		services.TryAddSingleton<DefService>();
		services.TryAddSingleton<DefWorkflow>();
		services.TryAddSingleton<DefFactory>();
		services.TryAddSingleton<DefFieldApplier>();
		services.TryAddSingleton<DefInheritanceResolver>();
		services.TryAddSingleton<DefMaterializer>();
		services.TryAddSingleton<DefValueConverterRegistry>();
		services.TryAddSingleton<DefValueConverter>();
		services.TryAddSingleton<DefValueMerger>();
		services.TryAddSingleton<DefTypeRegistry>();
		
		services.TryAddSingleton<IDefCatalog, DefaultDefCatalog>();
		
		return services;
	}
	
	private static IServiceCollection AddRuntime(this IServiceCollection services)
	{
		services.TryAddSingleton<PackageServiceState>();
		services.TryAddSingleton<PackageConflictRegistry>();
		services.TryAddSingleton<PackageDiscover>();
		services.TryAddSingleton<PackageDiscoveryWorkflow>();
		services.TryAddSingleton<PackageLoadOrderBuilder>();
		services.TryAddSingleton<PackageSnapshotStore>();
		services.TryAddSingleton<PackageWorkflow>();
		services.TryAddSingleton<PackageValidator>();

		services.TryAddSingleton<IPackageService, PackageService>();
		
		services.TryAddSingleton<IPackageCatalog>(provider =>
		{
			var options = provider.GetRequiredService<IOptions<GrekovOptions>>().Value;

			return new DefaultPackageCatalog(provider.GetRequiredService<IFileService>(), [.. options.PackageRoots]);
		});

		return services;
	}
}
