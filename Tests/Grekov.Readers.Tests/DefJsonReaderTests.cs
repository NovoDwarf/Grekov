using Grekov.Tests.Shared.Definitions;
using System.Text;
using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Definitions.Registry;
using Grekov.Readers.Json;
using Microsoft.Extensions.DependencyInjection;
using NovoDwarf.FS.Extensions;

namespace Grekov.Readers.Tests;

public sealed class DefJsonReaderTests
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
        var (definitions, issues) = Read(json);

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
        var (definitions, issues) = Read(json);

        // Assert
        Assert.Equal(DefId.Parse("parent"), Assert.Single(definitions).ParentId);
        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("{\"$type\":12,\"Id\":\"sample\"}", "definition.property.type.invalid")]
    [InlineData("{\"$type\":{},\"Id\":\"sample\"}", "definition.property.type.invalid")]
    [InlineData("{\"$type\":\"Sample\",\"Id\":12}", "definition.id.invalid")]
    [InlineData("{\"$type\":\"Sample\",\"Id\":[]}", "definition.id.invalid")]
    public void InvalidStructuralTypesAreStillRejected(string json, string code)
    {
        // Arrange: json and code are supplied by the theory.

        // Act
        var (definitions, issues) = Read(json);

        // Assert
        Assert.Empty(definitions);
        Assert.Contains(issues, issue => issue.Code == code);
    }

    private static (IReadOnlyList<DefRaw> Definitions, IReadOnlyList<DefIssue> Issues) Read(string json)
    {
        var services = new ServiceCollection();
        
        services.AddFileSystemServices();
        
        using var provider = services.BuildServiceProvider();
       
        var types = new DefTypeRegistry();
       
        types.Refresh([typeof(JsonSampleDef).Assembly]);
       
        var issues = new DefIssueRegistry();
        var reader = ActivatorUtilities.CreateInstance<DefJsonReader>(provider, types, issues);
       
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var definitions = reader.ReadDefinitions(stream, new DefReadContext("test", "Sample.json", "sample", []));
        
        return (definitions, issues.GetIssues("test"));
    }
}
