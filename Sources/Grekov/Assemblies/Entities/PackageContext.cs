using System.Reflection;
using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Abstractions.Interfaces.Packaging;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Assemblies.Entities;

public sealed class PackageContext : IPackageContext
{
	private readonly IServiceProvider _services;
	private readonly IReadOnlyList<Assembly> _assemblies;

	public PackageContext(
		string packageId,
		IPackageContainer container,
		IServiceProvider services, 
		IReadOnlyList<Assembly> assemblies)
	{


		PackageId = packageId;
		Container = container;
		_services = services;
		_assemblies = assemblies;
	}

	public string PackageId { get; }

	public IPackageContainer Container { get; }

	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	public T GetService<T>() where T : notnull
	{
		return _services.GetRequiredService<T>();
	}
}

