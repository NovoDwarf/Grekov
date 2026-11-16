using Grekov.Binary;
using Grekov.Builders;
using Grekov.Extensions;
using Grekov.Providers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grekov.Extensions;

public static class GrekovProviderBuilderExtensions
{
    public static GrekovProviderBuilder Binary(this GrekovProviderBuilder builder, Action<BinaryOptions> configure)
    {
        builder.Services.Configure(configure);
            builder.Services.TryAddSingleton<GpakReader>();

        return builder.Add<BinaryPackageProvider>();
    }
}
