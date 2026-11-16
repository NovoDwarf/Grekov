using Grekov.Builders;
using Grekov.Extensions;
using Grekov.Providers.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grekov.Extensions;

public static class GrekovProviderBuilderExtensions
{
	public static GrekovProviderBuilder FileSystem(this GrekovProviderBuilder builder, Action<FileSystemOptions> configure)
	{
		builder.Services.Configure(configure);
		builder.Services.TryAddSingleton<FileSystemContentFactory>();
		
		return builder.Add<FileSystemPackageProvider>();
	}
}
