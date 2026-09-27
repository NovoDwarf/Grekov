using Grekov.Assemblies.Services;
using Grekov.Definitions;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Registry;
using Grekov.Localizations.Services;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;

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

	public async Task Start(CancellationToken token = default)
	{
		await _packages.Load(token);

		var packages = GetActivePackages();
		
		await _assemblies.Load(packages, token);
		_defTypes.Refresh(_assemblyRegistry.GetAllAssemblies());
		await _storage.Load(_defTypes.Types, token);
		await _entrypoints.Load(packages);
		await _defs.Load(packages, token);
		await _localization.Load(packages);
		
		await _entrypoints.NotifyLoaded(packages);
	}

	public async Task Stop(CancellationToken token = default)
	{
		var packages = GetActivePackages();
		
		await _localization.Unload(packages);
		await _defs.Unload(packages, token);
		await _entrypoints.Unload(packages);
		await _assemblies.Unload(packages, token);
		await _packages.Unload(token);
		await _storage.ClearAsync(token);
	}
	
	private IReadOnlyList<PackageInstance> GetActivePackages() 
		=> [.. _packages.LoadOrder.Where(static package => package is { Enabled: true, HasErrors: false })];
}
