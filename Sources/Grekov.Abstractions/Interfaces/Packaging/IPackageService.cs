namespace Grekov.Abstractions.Interfaces.Packaging;

public interface IPackageService
{
	public IReadOnlyList<PackageInstance> Packages { get; }
	public IReadOnlyList<PackageInstance> LoadOrder { get; }

	public Task LoadAsync(CancellationToken token = default);
	public Task UnloadAsync(CancellationToken token = default);
}
