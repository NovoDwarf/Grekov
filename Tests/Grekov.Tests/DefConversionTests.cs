using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests;

public sealed class DefConversionTests
{
    [Fact]
    public void ConvertsHashSetAndRemovesDuplicates()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var converter = new DefValueConverter(new DefValueConverterRegistry(services));
        var result = Assert.IsType<HashSet<int>>(converter.Convert(
            DefValue.ListValue([DefValue.ScalarValue("3"), DefValue.ScalarValue("3")]), typeof(HashSet<int>)));
        Assert.Equal(3, Assert.Single(result));
    }

    [Fact]
    public void UsesPackageConverterBeforeObjectFallback()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var registry = new DefValueConverterRegistry(services);
        registry.Refresh([typeof(PayloadConverter).Assembly]);
        var converter = new DefValueConverter(registry);
        Assert.Equal(99, Assert.IsType<Payload>(converter.Convert(DefValue.ObjectValue(
            new Dictionary<string, DefValue>()), typeof(Payload))).Amount);
    }
}
