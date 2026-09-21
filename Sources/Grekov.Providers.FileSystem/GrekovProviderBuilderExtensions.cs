using Grekov.Builders;
using Grekov.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grekov.Providers.FileSystem;

public static class GrekovProviderBuilderExtensions
{
	public static GrekovProviderBuilder FileSystem(this GrekovProviderBuilder builder, Action<FileSystemOptions> configure)
	{
		builder.Services.Configure(configure);
		builder.Services.TryAddSingleton<FileSystemContentFactory>();
		
		return builder.Add<FileSystemPackageProvider>();
	}
}
