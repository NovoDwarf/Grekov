using Grekov.Assemblies.Entities;
using Grekov.Assemblies.Interfaces;
using Grekov.Assemblies.Services;
using Grekov.Core.Interfaces;
using Grekov.Packaging.Entities;

namespace Grekov.Defaults;

public sealed class DefaultContextFactory : IPackageContextFactory
{
	private readonly IServiceProvider _services;
	private readonly AssemblyRegistry _assemblies;

	public DefaultContextFactory(IServiceProvider services, AssemblyRegistry assemblies)
	{
		_services = services;
		_assemblies = assemblies;
	}

	public IPackageContext Create(PackageInstance package)
	{
		var packageAssemblies = _assemblies
			.GetAssemblies(package.Id);

		return new PackageContext(package.Id, package.RootPath, _services, packageAssemblies);
	}
}