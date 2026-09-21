using Grekov.Assemblies.Services;
using Grekov.Definitions;
using Grekov.Localizations.Services;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;

namespace Grekov;

public interface IGrekovService
{
	Task Start(CancellationToken token = default);

	Task Stop(CancellationToken token = default);
}

internal sealed class GrekovService : IGrekovService
{
	private readonly IPackageService _packages;
	private readonly AssemblyService _assemblies;
	private readonly EntrypointService _entrypoints;
	private readonly DefService _defs;
	private readonly LocalizationService _localization;

	public GrekovService(
		IPackageService packages,
		AssemblyService assemblies,
		DefService defs,
		LocalizationService localization, EntrypointService entrypoints)
	{
		_packages = packages;
		_assemblies = assemblies;
		_defs = defs;
		_localization = localization;
		_entrypoints = entrypoints;
	}

	public async Task Start(CancellationToken token = default)
	{
		await _packages.Load(token);

		var packages = GetActivePackages();
		
		await _entrypoints.Load(packages);
		await _assemblies.Load(packages, token);
		await _defs.Load(packages);
		await _localization.Load(packages);
		
		await _entrypoints.NotifyLoaded(packages);
	}

	public async Task Stop(CancellationToken token = default)
	{
		var packages = GetActivePackages();
		
		await _localization.Unload(packages);
		await _defs.Unload(packages);
		await _assemblies.Unload(packages, token);
		await _entrypoints.Unload(packages);
		await _packages.Unload(token);
	}
	
	private IReadOnlyList<PackageInstance> GetActivePackages() 
		=> [.. _packages.LoadOrder.Where(static package => package is { Enabled: true, HasErrors: false })];
}