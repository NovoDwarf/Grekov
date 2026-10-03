using Grekov.Abstractions.Interfaces.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grekov.Builders;

public sealed class GrekovReaderBuilder
{
	public IServiceCollection Services { get; }

	internal GrekovReaderBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public GrekovReaderBuilder Add<TReader>() where TReader : class, IDefFormatReader
	{
		Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDefFormatReader, TReader>());
		return this;
	}
}