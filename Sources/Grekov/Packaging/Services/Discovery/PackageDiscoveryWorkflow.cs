using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services.Validation;

namespace Grekov.Packaging.Services.Discovery;

internal sealed class PackageDiscoveryWorkflow
{
	private readonly IPackageCatalog _catalog;
	private readonly IPackageLoadOrderStore _loadOrderStore;
	
	private readonly PackageDiscover _discover;
	private readonly PackageValidator _validator;

	public PackageDiscoveryWorkflow(
		IPackageCatalog catalog,
		IPackageLoadOrderStore loadOrderStore,
		PackageDiscover discover,
		PackageValidator validator)
	{
		_catalog = catalog;
		_loadOrderStore = loadOrderStore;
		_discover = discover;
		_validator = validator;
	}

	public PackageDiscoveryResult Discover()
	{
		var packages = _discover.Discover(_catalog, _loadOrderStore.LoadEnabledOverrides());
		
		_validator.Validate(packages);
		
		return new PackageDiscoveryResult(packages);
	}
}
