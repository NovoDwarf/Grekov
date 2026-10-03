using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services;

namespace Grekov.Packaging;

internal sealed class PackageService : IPackageService
{
    private readonly PackageStateStore _stateStore;
    private readonly PackageLoadOrderResolver _loadOrderResolver;
    private readonly PackageValidator _validator;
    private readonly IEnumerable<IPackageProvider> _providers;

    public PackageService(
        PackageStateStore stateStore,
        PackageLoadOrderResolver loadOrderResolver,
        PackageValidator validator,
        IEnumerable<IPackageProvider> providers)
    {
        _stateStore = stateStore;
        _loadOrderResolver = loadOrderResolver;
        _validator = validator;
        _providers = providers;
    }

    public IReadOnlyList<PackageInstance> Packages => _stateStore.Packages;
    public IReadOnlyList<PackageInstance> LoadOrder => _stateStore.LoadOrder;

    public async Task LoadAsync(CancellationToken token = default)
    {
        var discovery = await Discover(token);
        var state = await _stateStore.Load(token);
        var loadOrder = _loadOrderResolver.Resolve(discovery, state);

        ApplyDiscovery(discovery, loadOrder);

        await _stateStore.Save(token);
    }

    public Task UnloadAsync(CancellationToken token = default)
    {
        _stateStore.Packages.Clear();
        _stateStore.PackagesById.Clear();
        _stateStore.LoadOrder = [];

        return Task.CompletedTask;
    }

    private async Task<PackageDiscoveryResult> Discover(CancellationToken token)
    {
        List<PackageInstance> packages = [];

        foreach (var provider in _providers)
        {
            var discovered = await provider.DiscoverAsync(token);

            packages.AddRange(discovered);
        }

        _validator.Validate(packages);

        return new PackageDiscoveryResult(packages);
    }

    private void ApplyDiscovery(PackageDiscoveryResult discovery, IReadOnlyList<PackageInstance> loadOrder)
    {
        _stateStore.Packages.Clear();
        _stateStore.Packages.AddRange(discovery.Packages);

        _stateStore.PackagesById.Clear();

        foreach (var package in discovery.Packages)
            _stateStore.PackagesById.Add(package.Id, package);

        _stateStore.LoadOrder = loadOrder;
    }
}