using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Builders;

public sealed class GrekovBuilder
{
	internal IServiceCollection Services { get; }

	internal GrekovBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public GrekovBuilder Configure(Action<GrekovOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		Services.Configure(configure);

		return this;
	}

	public GrekovBuilder Readers(Action<GrekovReaderBuilder> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		var builder = new GrekovReaderBuilder(Services);
		
		configure(builder);

		return this;
	}
	
	public GrekovBuilder Providers(Action<GrekovProviderBuilder> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		var builder = new GrekovProviderBuilder(Services);
		
		configure(builder);

		return this;
	}

	public GrekovBuilder Storage(Action<GrekovStorageBuilder> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		configure(new GrekovStorageBuilder(Services));
		return this;
	}
}
