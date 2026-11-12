using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Storages.Database;
using Microsoft.EntityFrameworkCore;

namespace Grekov.Tests;

public sealed class DatabaseStorageTests
{
    [Fact]
    public async Task LoadingPreservesExistingDatabaseAndSupportsTryGet()
    {
        var path = Path.Combine(Path.GetTempPath(), $"grekov-{Guid.NewGuid():N}.db");
        var types = new DefTypeRegistry();
        types.Refresh([typeof(DatabaseSampleDef).Assembly]);
        var options = new DbContextOptionsBuilder<DefDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        var factory = new TestContextFactory(options, types);

        try
        {
            await using (var context = factory.CreateDbContext())
            {
                await context.Database.EnsureCreatedAsync();
                await context.Database.ExecuteSqlRawAsync("CREATE TABLE marker (value INTEGER)");
                await context.Database.ExecuteSqlRawAsync("INSERT INTO marker VALUES (42)");
            }

            var storage = new EfDefStorage(factory);
            await storage.LoadAsync(types.Types);
            await using (var context = factory.CreateDbContext())
            {
                Assert.Equal(42, await context.Database.SqlQueryRaw<int>("SELECT value AS Value FROM marker").SingleAsync());
            }

            var definition = new DatabaseSampleDef();
            typeof(Def).GetProperty(nameof(Def.Id))!.SetValue(definition, DefId.Parse("sample"));
            await storage.AddAsync(definition);
            Assert.True(storage.TryGet<DatabaseSampleDef>(DefId.Parse("sample"), out var generic));
            Assert.NotNull(generic);
            Assert.True(storage.TryGet(typeof(DatabaseSampleDef), DefId.Parse("sample"), out var untyped));
            Assert.NotNull(untyped);
            Assert.False(storage.TryGet<DatabaseSampleDef>(DefId.Parse("missing"), out _));
            Assert.False(storage.TryGet(typeof(DatabaseSampleDef), DefId.Parse("missing"), out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class TestContextFactory(DbContextOptions<DefDbContext> options, DefTypeRegistry types)
        : IDbContextFactory<DefDbContext>
    {
        public DefDbContext CreateDbContext() => new(options, types);
    }
}
