using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Storages.Database;
using Microsoft.EntityFrameworkCore;

namespace Grekov.Tests;

public sealed class DatabaseStorageTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"grekov-{Guid.NewGuid():N}.db");
    private readonly DefTypeRegistry _types = new();
    private readonly TestContextFactory _factory;

    public DatabaseStorageTests()
    {
        _types.Refresh([typeof(DatabaseSampleDef).Assembly]);
        var options = new DbContextOptionsBuilder<DefDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .Options;
        _factory = new TestContextFactory(options, _types);
    }

    [Fact]
    public async Task LoadingPreservesExistingDatabase()
    {
        // Arrange
        await using var context = _factory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE marker (value INTEGER)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO marker VALUES (42)");
        var storage = new EfDefStorage(_factory);

        // Act
        await storage.LoadAsync(_types.Types);

        // Assert
        var value = await context.Database.SqlQueryRaw<int>("SELECT value AS Value FROM marker").SingleAsync();
        Assert.Equal(42, value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryGetReportsWhetherDefinitionExists(bool exists)
    {
        // Arrange
        var storage = new EfDefStorage(_factory);
        await storage.LoadAsync(_types.Types);
        var id = DefId.Parse("sample");

        if (exists)
        {
            var definition = new DatabaseSampleDef();
            typeof(Def).GetProperty(nameof(Def.Id))!.SetValue(definition, id);
            await storage.AddAsync(definition);
        }

        // Act
        var genericFound = storage.TryGet<DatabaseSampleDef>(id, out var generic);
        var untypedFound = storage.TryGet(typeof(DatabaseSampleDef), id, out var untyped);

        // Assert
        Assert.Equal(exists, genericFound);
        Assert.Equal(exists, untypedFound);
        Assert.Equal(exists, generic is not null);
        Assert.Equal(exists, untyped is not null);
    }

    public void Dispose()
    {
        File.Delete(_path);
    }

    private sealed class TestContextFactory(DbContextOptions<DefDbContext> options, DefTypeRegistry types)
        : IDbContextFactory<DefDbContext>
    {
        public DefDbContext CreateDbContext() => new(options, types);
    }
}
