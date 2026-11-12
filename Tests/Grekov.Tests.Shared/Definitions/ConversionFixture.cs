using Grekov.Definitions.Conversions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests.Shared.Definitions;

public sealed class ConversionFixture : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();

    public DefValueConverter CreateConverter(bool includePackageConverters = false)
    {
        var registry = new DefValueConverterRegistry(_services);

        if (includePackageConverters)
        {
            registry.Refresh([typeof(PayloadConverter).Assembly]);
        }

        return new DefValueConverter(registry);
    }

    public void Dispose()
    {
        _services.Dispose();
    }
}
