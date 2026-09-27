using Grekov.Core;
using Grekov.Definitions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grekov.Storages.Database;

internal sealed class EfDefStorage : IDefStorage
{
	private readonly IDbContextFactory<DefDbContext> _contexts;

	public EfDefStorage(IDbContextFactory<DefDbContext> contexts)
	{
		_contexts = contexts;
	}

	public async Task Load(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(definitionTypes);
		await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

		await context.Database.EnsureDeletedAsync(cancellationToken);
		await context.Database.EnsureCreatedAsync(cancellationToken);
	}

	public async Task AddAsync(Def definition, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(definition);
		await using var context = await _contexts.CreateDbContextAsync(cancellationToken);
		context.Add(definition);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
		await using var context = await _contexts.CreateDbContextAsync(cancellationToken);

		var definitions = await context.Set<Def>()
			.Where(definition => definition.PackageId == packageId)
			.ToListAsync(cancellationToken);

		context.RemoveRange(definitions);
		await context.SaveChangesAsync(cancellationToken);
	}

	public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	public T? Get<T>(DefId id) where T : Def => WithContext(context => context.Set<T>().SingleOrDefault(definition => definition.Id == id));
	public T? Get<T>(string id) where T : Def => Get<T>(DefId.Parse(id));
	public IEnumerable<T> GetByPackage<T>(string packageId) where T : Def => WithContext(context => context.Set<T>().Where(definition => definition.PackageId == packageId).ToArray());
	public IEnumerable<Def> GetByPackage(string packageId) => WithContext(context => context.Set<Def>().Where(definition => definition.PackageId == packageId).ToArray());
	public T? GetByResourcePath<T>(string resourcePath) where T : Def => WithContext(context => context.Set<T>().SingleOrDefault(definition => definition.ResourcePath == resourcePath));
	public IEnumerable<T> All<T>() where T : Def => WithContext(context => context.Set<T>().ToArray());
	public bool Contains<T>(DefId id) where T : Def => WithContext(context => context.Set<T>().Any(definition => definition.Id == id));
	public bool Contains(Type type, DefId id) => WithContext(context => context.Find(type, id) is not null);
	public T? FirstOrDefault<T>() where T : Def => WithContext(context => context.Set<T>().FirstOrDefault());
	public bool TryGetOrigin(Def definition, out string packageId, out string resourcePath)
	{
		ArgumentNullException.ThrowIfNull(definition);
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
