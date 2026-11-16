using Grekov.Abstractions.Interfaces.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grekov.Builders;

public sealed class GrekovProviderBuilder
{
	public IServiceCollection Services { get; }

	internal GrekovProviderBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public GrekovProviderBuilder Add<TProvider>() where TProvider : class, IPackageProvider
	{
		Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPackageProvider, TProvider>());
		return this;
	}
}