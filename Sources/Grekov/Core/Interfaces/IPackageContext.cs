using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Core.Interfaces;

public interface IPackageContext
{
	string PackageId { get; }

	string Root { get; }

	IServiceProvider Services { get; }

	IReadOnlyList<Assembly> Assemblies { get; }
}