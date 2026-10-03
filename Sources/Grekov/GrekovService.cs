using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Assemblies.Services;
using Grekov.Definitions;
using Grekov.Definitions.Registry;
using Grekov.Localizations.Services;

namespace Grekov;

internal sealed class GrekovService : IGrekovService
{
	private readonly IPackageService _packages;
	private readonly AssemblyService _assemblies;
	private readonly AssemblyRegistry _assemblyRegistry;
	private readonly EntrypointService _entrypoints;
	private readonly DefService _defs;
	private readonly IDefStorage _storage;
	private readonly DefTypeRegistry _defTypes;
	private readonly LocalizationService _localization;

	public GrekovService(
		IPackageService packages,
		AssemblyService assemblies,
		AssemblyRegistry assemblyRegistry,
		DefService defs,
		IDefStorage storage,
		DefTypeRegistry defTypes,
		LocalizationService localization, EntrypointService entrypoints)
	{
		_packages = packages;
		_assemblies = assemblies;
		_assemblyRegistry = assemblyRegistry;
		_defs = defs;
		_storage = storage;
		_defTypes = defTypes;
		_localization = localization;
		_entrypoints = entrypoints;
	}

	public async Task StartAsync(CancellationToken token = default)
	{
		await _packages.LoadAsync(token);

		var packages = GetActivePackages();
		
		await _assemblies.LoadAsync(packages, token);
		
		_defTypes.Refresh(_assemblyRegistry.GetAllAssemblies());
		
		await _storage.LoadAsync(_defTypes.Types, token);
		await _entrypoints.LoadAsync(packages);
		await _defs.LoadAsync(packages, token);
		await _localization.LoadAsync(packages);
		
		await _entrypoints.NotifyLoadedAsync(packages);
	}

	public async Task StopAsync(CancellationToken token = default)
	{
		var packages = GetActivePackages();
		
		await _localization.UnloadAsync(packages);
		await _defs.UnloadAsync(packages, token);
		await _entrypoints.UnloadAsync(packages);
		await _assemblies.UnloadAsync(packages, token);
		await _packages.UnloadAsync(token);
		await _storage.UnloadAsync(token);
	}
	
	private IReadOnlyList<PackageInstance> GetActivePackages() 
		=> [.. _packages.LoadOrder.Where(static package => package is { Enabled: true, HasErrors: false })];
}
