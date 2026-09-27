using Grekov.Core;

namespace Grekov.Definitions.Interfaces;

public interface IDefStorage : IDefCatalog
{
	public Task Load(IReadOnlyCollection<Type> definitionTypes, CancellationToken cancellationToken = default);

	public Task AddAsync(Def definition, CancellationToken cancellationToken = default);

	public Task RemovePackageAsync(string packageId, CancellationToken cancellationToken = default);

	public Task ClearAsync(CancellationToken cancellationToken = default);
}
