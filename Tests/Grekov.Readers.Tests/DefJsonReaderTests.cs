using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Tests.Shared.Definitions;
using Grekov.Tests.Shared.Readers;

namespace Grekov.Readers.Tests;

[Trait("Category", "Unit")]
public sealed class DefJsonReaderTests(JsonReaderFixture fixture) : IClassFixture<JsonReaderFixture>
{
    [Fact]
    public void ReadsStructuralStringValuesWithoutChangingFieldTokens()
    {
        // Arrange
        const string json = """
            {
                "$type": "Sample",
                "Id": "child",
                "Parent": "parent",
                "Amount": 12,
                "Settings": { "Enabled": true },
                "Items": [1, 2],
                "Optional": null
            }
            """;

        // Act
        var (definitions, issues) = fixture.Read(json);

        // Assert
        var definition = Assert.Single(definitions);
        Assert.Empty(issues);
        Assert.Equal(typeof(JsonSampleDef), definition.Type);
        Assert.Equal(DefId.Parse("child"), definition.Id);
        Assert.Equal(DefId.Parse("parent"), definition.ParentId);
        Assert.Equal("12", definition.Fields["Amount"].Scalar);
        Assert.Equal(DefValueKind.Object, definition.Fields["Settings"].Kind);
        Assert.Equal(DefValueKind.List, definition.Fields["Items"].Kind);
        Assert.Equal(DefValueKind.Null, definition.Fields["Optional"].Kind);
    }

    [Fact]
    public void StructuralPropertyNamesAreCaseInsensitive()
    {
        // Arrange
        const string json = """
            { "$TYPE": "Sample", "ID": "sample", "PARENT": "parent" }
            """;

        // Act
        var (definitions, issues) = fixture.Read(json);

        // Assert
        Assert.Equal(DefId.Parse("parent"), Assert.Single(definitions).ParentId);
        Assert.Empty(issues);
    }

    [Fact]
    public void ReadingValidInputDoesNotReusePreviousDiagnostics()
    {
        // Arrange
        fixture.Read("""{"$type":12,"Id":"invalid"}""");
        const string json = """{"$type":"Sample","Id":"valid"}""";

        // Act
        var (definitions, issues) = fixture.Read(json);

        // Assert
        Assert.Equal(DefId.Parse("valid"), Assert.Single(definitions).Id);
        Assert.Empty(issues);
    }

    [Theory]
    [ClassData(typeof(InvalidJsonStructuralCases))]
    public void InvalidStructuralTypesAreStillRejected(string json, string code)
    {
        // Arrange: json and code are supplied by the theory.

        // Act
        var (definitions, issues) = fixture.Read(json);

        // Assert
        Assert.Empty(definitions);
        Assert.Contains(issues, issue => issue.Code == code);
    }
}
