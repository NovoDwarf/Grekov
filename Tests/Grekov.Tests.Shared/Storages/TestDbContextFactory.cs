using Grekov.Definitions.Registry;
using Grekov.Storages.Database;
using Microsoft.EntityFrameworkCore;

namespace Grekov.Tests.Shared.Storages;

internal sealed class TestDbContextFactory(DbContextOptions<DefDbContext> options, DefTypeRegistry types)
    : IDbContextFactory<DefDbContext>
{
    public DefDbContext CreateDbContext()
    {
        return new DefDbContext(options, types);
    }
}
