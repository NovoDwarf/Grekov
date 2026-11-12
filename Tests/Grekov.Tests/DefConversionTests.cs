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
        // Arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var converter = new DefValueConverter(new DefValueConverterRegistry(services));
        var value = DefValue.ListValue([DefValue.ScalarValue("3"), DefValue.ScalarValue("3")]);

        // Act
        var result = converter.Convert(value, typeof(HashSet<int>));

        // Assert
        var set = Assert.IsType<HashSet<int>>(result);
        Assert.Equal(3, Assert.Single(set));
    }

    [Fact]
    public void UsesPackageConverterBeforeObjectFallback()
    {
        // Arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var registry = new DefValueConverterRegistry(services);
        registry.Refresh([typeof(PayloadConverter).Assembly]);
        var converter = new DefValueConverter(registry);
        var value = DefValue.ObjectValue(new Dictionary<string, DefValue>());

        // Act
        var result = converter.Convert(value, typeof(Payload));

        // Assert
        Assert.Equal(99, Assert.IsType<Payload>(result).Amount);
    }
}
