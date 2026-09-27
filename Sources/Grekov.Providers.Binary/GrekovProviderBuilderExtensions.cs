using Grekov.Builders;
using Grekov.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Providers.Binary;

public static class GrekovProviderBuilderExtensions
{
	public static GrekovProviderBuilder Binary(this GrekovProviderBuilder builder, Action<BinaryOptions> configure)
	{
		builder.Services.Configure(configure);
		
		return builder.Add<BinaryPackageProvider>();
	}
}
