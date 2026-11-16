using Grekov.Abstractions;
using Grekov.Tests.Shared.Definitions;
using Grekov.Tests.Shared.Storages;

namespace Grekov.Tests;

[Trait("Category", "Integration")]
public sealed class DatabaseStorageTests : DatabaseTestBase
{
    [Fact]
    public async Task LoadingPreservesExistingDatabase()
    {
        // Arrange
        await ExecuteSqlAsync("CREATE TABLE marker (value INTEGER)");
        await ExecuteSqlAsync("INSERT INTO marker VALUES (42)");

        // Act
        await Storage.LoadAsync(DefinitionTypes);

        // Assert
        var value = await ReadScalarAsync("SELECT value AS Value FROM marker");
        Assert.Equal(42, value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryGetReportsWhetherDefinitionExists(bool exists)
    {
        // Arrange
        var id = DefId.Parse("sample");

        if (exists)
        {
            await Storage.AddAsync(DefinitionTestData.Create<DatabaseSampleDef>(id));
        }

        // Act
        var genericFound = Storage.TryGet<DatabaseSampleDef>(id, out var generic);
        var untypedFound = Storage.TryGet(typeof(DatabaseSampleDef), id, out var untyped);

        // Assert
        Assert.Equal(exists, genericFound);
        Assert.Equal(exists, untypedFound);
        Assert.Equal(exists, generic is not null);
        Assert.Equal(exists, untyped is not null);
    }
}
