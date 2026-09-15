using Grekov.Definitions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Readers.Xml.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection Xml(this IServiceCollection services)
	{
		return services.AddSingleton<IDefFormatReader, DefXmlReader>();
	}
}
