using System.Reflection;
using Grekov.Packaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Core.Interfaces;

public interface IPackageContext
{
	public string PackageId { get; }

	public IPackageStorage Storage { get; }

	public IReadOnlyList<Assembly> Assemblies { get; }

	public T GetService<T>() where T : notnull;
}
