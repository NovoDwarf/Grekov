using Grekov.Packaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Providers.FileSystem;

internal sealed class FileSystemContentFactory
{
	private readonly IServiceProvider _services;

	public FileSystemContentFactory(IServiceProvider services)
	{
		_services = services;
	}

	public IPackageContainer Create(string root)
	{
		return ActivatorUtilities.CreateInstance<FileSystemPackageContainer>(_services, root);
	}
}
