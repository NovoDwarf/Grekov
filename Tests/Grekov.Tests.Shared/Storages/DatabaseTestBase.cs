using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Registry;
using Grekov.Storages.Database;
using Grekov.Tests.Shared.Definitions;
using Grekov.Tests.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Grekov.Tests.Shared.Storages;

public abstract class DatabaseTestBase : IAsyncLifetime
{
    private readonly TemporaryDirectory _directory = new();
    private readonly TestDbContextFactory _factory;
    
    protected DatabaseTestBase()
    {
        var types = new DefTypeRegistry();
       
        types.Refresh([typeof(DatabaseSampleDef).Assembly]);
        
        DefinitionTypes = types.Types;
      
        var path = Path.Combine(_directory.Path, "definitions.db");
        var options = new DbContextOptionsBuilder<DefDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;
      
        _factory = new TestDbContextFactory(options, types);
       
        Storage = new EfDefStorage(_factory);
    }

    protected IDefStorage Storage { get; }
    protected IReadOnlyCollection<Type> DefinitionTypes { get; }
    
    public async Task InitializeAsync()
    {
        await using var context = _factory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    protected async Task ExecuteSqlAsync(string sql)
    {
        await using var context = _factory.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    protected async Task<int> ReadScalarAsync(string sql)
    {
        await using var context = _factory.CreateDbContext();
        return await context.Database.SqlQueryRaw<int>(sql).SingleAsync();
    }

    public Task DisposeAsync()
    {
        _directory.Dispose();
        return Task.CompletedTask;
    }
}
