using System.Reflection;
using Grekov.Core.Interfaces;
using Grekov.Packaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Assemblies.Entities;

public sealed class PackageContext : IPackageContext
{
	private readonly IServiceProvider _services;
	private readonly IReadOnlyList<Assembly> _assemblies;

	public PackageContext(
		string packageId,
		IPackageContent content,
		IServiceProvider services, 
		IReadOnlyList<Assembly> assemblies)
	{


		PackageId = packageId;
		Content = content;
		_services = services;
		_assemblies = assemblies;
	}

	public string PackageId { get; }

	public IPackageContent Content { get; }

	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	public T GetService<T>() where T : notnull
	{
		return _services.GetRequiredService<T>();
	}
}

