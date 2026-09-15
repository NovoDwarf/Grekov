using System.Reflection;
using Grekov.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Assemblies.Entities;

public sealed class PackageContext : IPackageContext
{
	public PackageContext(string packageId, string root, IServiceProvider services, IReadOnlyList<Assembly> assemblies)
	{
		PackageId = packageId;
		Root = root;
		Services = services;
		Assemblies = assemblies;
	}

	public string PackageId { get; }

	public string Root { get; }

	public IServiceProvider Services { get; }

	public IReadOnlyList<Assembly> Assemblies { get; }
}


