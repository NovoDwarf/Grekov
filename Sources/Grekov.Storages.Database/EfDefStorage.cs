using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Microsoft.EntityFrameworkCore;

namespace Grekov.Storages.Database;

internal sealed class EfDefStorage : IDefStorage
{
    private readonly IDbContextFactory<DefDbContext> _contexts;

    public EfDefStorage(IDbContextFactory<DefDbContext> contexts)
    {
        _contexts = contexts;
    }

    public async Task LoadAsync(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default)
    {
        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

        await context.Database.EnsureDeletedAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task AddAsync(Def definition, CancellationToken cancellationToken = default)
    {
        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

        await context.AddAsync(definition, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(List<Def> definitions, CancellationToken cancellationToken = default)
    {
        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

        await context.AddRangeAsync(definitions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

        var definitions = await context.Set<Def>()
            .Where(definition => definition.PackageId == packageId)
            .ToListAsync(cancellationToken);

        context.RemoveRange(definitions);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

        var definitions = await context.Set<Def>().ToListAsync(cancellationToken);

        context.RemoveRange(definitions);

        await context.SaveChangesAsync(cancellationToken);
    }

    public T Get<T>(DefId id) where T : Def
    {
        return WithContext(context => context.Set<T>().Single(definition => definition.Id == id));
    }

    public T Get<T>(string id) where T : Def
    {
        return Get<T>(DefId.Parse(id));
    }

    public T? Find<T>(DefId id) where T : Def
    {
        return WithContext(context => context.Set<T>().SingleOrDefault(definition => definition.Id == id));
    }

    public T? Find<T>(string id) where T : Def
    {
        return Find<T>(DefId.Parse(id));
    }

    public Def Get(Type type, DefId id)
    {
        return (Def)WithContext(context => context.Find(type, id) ?? throw new KeyNotFoundException($"Definition [{id}] of type [{type.FullName}] was not found."));
    }

    public Def? Find(Type type, DefId id)
    {
        return (Def?)WithContext(context => context.Find(type, id));
    }

    public bool TryGet<T>(DefId id, out T? definition) where T : Def
    {
        throw new NotImplementedException();
    }

    public bool TryGet(Type type, DefId id, out Def? definition)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def
    {
        return WithContext(context => context.Set<T>().Where(definition => definition.PackageId == packageId).ToArray());
    }

    public IEnumerable<Def> GetByPackage(string packageId)
    {
        return WithContext(context => context.Set<Def>().Where(definition => definition.PackageId == packageId).ToArray());
    }

    public T? GetByResourcePath<T>(string resourcePath) where T : Def
    {
        return WithContext(context => context.Set<T>().SingleOrDefault(definition => definition.ResourcePath == resourcePath));
    }

    public IEnumerable<T> All<T>() where T : Def
    {
        return WithContext(context => context.Set<T>().ToArray());
    }

    public bool Contains<T>(DefId id) where T : Def
    {
        return WithContext(context => context.Set<T>().Any(definition => definition.Id == id));
    }

    public bool Contains(Type type, DefId id)
    {
        return Find(type, id) is not null;
    }

    public T? FirstOrDefault<T>() where T : Def
    {
        return WithContext(context => context.Set<T>().FirstOrDefault());
    }

    public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
    {
        packageId = definition.PackageId;
        resourcePath = definition.ResourcePath;

        return !string.IsNullOrWhiteSpace(packageId) || !string.IsNullOrWhiteSpace(resourcePath);
    }

    private TResult WithContext<TResult>(Func<DefDbContext, TResult> action)
    {
        using var context = _contexts.CreateDbContext();
        return action(context);
    }
}
