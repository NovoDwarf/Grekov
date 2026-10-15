using Grekov.Builders;
using Grekov.Extensions;
using Grekov.Providers.Binary;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Extensions;

public static class GrekovProviderBuilderExtensions
{
	public static GrekovProviderBuilder Binary(this GrekovProviderBuilder builder, Action<BinaryOptions> configure)
	{
		builder.Services.Configure(configure);
		
		return builder.Add<BinaryPackageProvider>();
	}
}
