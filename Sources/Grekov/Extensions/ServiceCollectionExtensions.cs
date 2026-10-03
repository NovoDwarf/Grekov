using Grekov.Abstractions.Interfaces;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Localizations;
using Grekov.Abstractions.Interfaces.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Grekov.Assemblies.Services;
using Grekov.Builders;
using Grekov.Defaults;
using Grekov.Definitions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Inheritance;
using Grekov.Definitions.Materializations;
using Grekov.Definitions.Registry;
using Grekov.Definitions.Scanning;
using Grekov.Localizations.Services;
using Grekov.Packaging;
using Grekov.Packaging.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NovoDwarf.FS.Extensions;

namespace Grekov.Extensions;


public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGrekov(Action<GrekovBuilder>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services
                .AddGrekovCore()
                .AddGrekovAssemblies()
                .AddGrekovDefinitions()
                .AddGrekovLocalization()
                .AddGrekovRuntime();

            var builder = new GrekovBuilder(services);

            configure?.Invoke(builder);

			if (!services.Any(static descriptor => descriptor.ServiceType == typeof(IDefStorage)))
				throw new InvalidOperationException("Configure exactly one definition container with GrekovBuilder.Container.");

            return services;
        }

        private IServiceCollection AddGrekovCore()
        {
            services.AddFileSystemServices();

            services.TryAddSingleton<IGrekovService, GrekovService>();
            services.TryAddSingleton<IPackageContextFactory, DefaultContextFactory>();
            services.TryAddSingleton<IPackageSettingsStore, DefaultSettingsStore>();
            services.TryAddSingleton<ILocalizationRuntime, DefaultLocalizationRuntime>();

            return services;
        }

        private IServiceCollection AddGrekovAssemblies()
        {
            services.TryAddSingleton<AssemblyRegistry>();
            services.TryAddSingleton<EntrypointService>();
            services.TryAddSingleton<AssemblyService>();

            return services;
        }

        private IServiceCollection AddGrekovLocalization()
        {
            services.TryAddSingleton<LocalizationServerState>();
            services.TryAddSingleton<TranslationRegistry>();
            services.TryAddSingleton<LocalizationService>();

            return services;
        }

        private IServiceCollection AddGrekovDefinitions()
        {
            services.TryAddSingleton<DefRawIndex>();
            services.TryAddSingleton<IDefReaderRegistry, DefReaderRegistry>();
            services.TryAddSingleton<DefScanner>();
            services.TryAddSingleton<DefService>();
            services.TryAddSingleton<DefFactory>();
            services.TryAddSingleton<DefFieldApplier>();
            services.TryAddSingleton<DefInheritanceResolver>();
            services.TryAddSingleton<DefMaterializer>();
            services.TryAddSingleton<DefValueConverterRegistry>();
            services.TryAddSingleton<IDefConverter, DefValueConverter>();
            services.TryAddSingleton<DefValueMerger>();
            services.TryAddSingleton<DefTypeRegistry>();
            services.TryAddSingleton<DefConflictRegistry>();
            services.TryAddSingleton<DefIssueRegistry>();
            return services;
        }

        private IServiceCollection AddGrekovRuntime()
        {
            services.TryAddSingleton<PackageStateStore>();
            services.TryAddSingleton<PackageLoadOrderResolver>();
            services.TryAddSingleton<PackageValidator>();
            services.TryAddSingleton<IPackageService, PackageService>();

            return services;
        }
    }
}
