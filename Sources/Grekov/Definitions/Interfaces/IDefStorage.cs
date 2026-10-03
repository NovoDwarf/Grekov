using Grekov.Core;

namespace Grekov.Definitions.Interfaces;

public interface IDefStorage : IDefCatalog
{
	public Task LoadAsync(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default);

	public Task AddAsync(Def definition, CancellationToken cancellationToken = default);

	public Task AddRangeAsync(List<Def> definitions, CancellationToken cancellationToken = default);
	
	public Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default);

	public Task UnloadAsync(CancellationToken cancellationToken = default);
}
