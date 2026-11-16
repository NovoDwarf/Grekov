using System.Reflection;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Abstractions.Interfaces.Assemblies;

public interface IPackageContext
{
	public string PackageId { get; }

	public IPackageContainer Container { get; }

	public IReadOnlyList<Assembly> Assemblies { get; }

	public T GetService<T>() where T : notnull;
}
