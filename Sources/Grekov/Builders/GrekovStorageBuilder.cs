using Grekov.Definitions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Builders;

public sealed class GrekovStorageBuilder
{
	public IServiceCollection Services { get; }

	internal GrekovStorageBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public GrekovStorageBuilder Use<TStorage>() where TStorage : class, IDefStorage
	{
		if (Services.Any(static descriptor => descriptor.ServiceType == typeof(IDefStorage)))
			throw new InvalidOperationException("A definition storage has already been configured.");

		Services.AddSingleton<IDefStorage, TStorage>();
		Services.AddSingleton<IDefCatalog>(static provider => provider.GetRequiredService<IDefStorage>());
		return this;
	}
}
