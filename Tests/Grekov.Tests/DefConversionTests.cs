using Grekov.Abstractions;
using Grekov.Tests.Shared.Definitions;

namespace Grekov.Tests;

[Trait("Category", "Unit")]
public sealed class DefConversionTests(ConversionFixture fixture) : IClassFixture<ConversionFixture>
{
    [Fact]
    public void ConvertsHashSetAndRemovesDuplicates()
    {
        // Arrange
        var converter = fixture.CreateConverter();
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
        var converter = fixture.CreateConverter(includePackageConverters: true);
        var value = DefValue.ObjectValue(new Dictionary<string, DefValue>());

        // Act
        var result = converter.Convert(value, typeof(Payload));

        // Assert
        Assert.Equal(99, Assert.IsType<Payload>(result).Amount);
    }
}
