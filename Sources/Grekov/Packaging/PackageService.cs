using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Packaging.Entities;
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
        var published = false;

        try
        {
            token.ThrowIfCancellationRequested();
            var state = await _stateStore.Load(token);
            var loadOrder = _loadOrderResolver.Resolve(discovery, state);

            // From this point the state store owns the containers, including partial publication.
            published = true;
            ApplyDiscovery(discovery, loadOrder);
            await _stateStore.Save(token);
        }
        catch (Exception failure) when (!published)
        {
            var errors = DisposePackages(discovery.Packages);

            if (errors.Count > 0)
            {
                throw new AggregateException("Package preparation and container cleanup failed.", errors.Prepend(failure));
            }

            throw;
        }
    }

    public Task UnloadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var errors = DisposePackages(_stateStore.Packages);
        _stateStore.Packages.Clear();
        _stateStore.PackagesById.Clear();
        _stateStore.LoadOrder = [];

        if (errors.Count > 0)
        {
            throw new AggregateException("Failed to dispose package containers.", errors);
        }

        return Task.CompletedTask;
    }

    private async Task<PackageDiscoveryResult> Discover(CancellationToken token)
    {
        var packages = new List<PackageInstance>();

        try
        {
            foreach (var provider in _providers)
            {
                var discovered = await provider.DiscoverAsync(token);
                packages.AddRange(discovered);
            }

            _validator.Validate(packages);
            return new PackageDiscoveryResult(packages);
        }
        catch (Exception failure)
        {
            var errors = DisposePackages(packages);

            if (errors.Count > 0)
            {
                throw new AggregateException("Package discovery and container cleanup failed.", errors.Prepend(failure));
            }

            throw;
        }
    }

    private static List<Exception> DisposePackages(IEnumerable<PackageInstance> packages)
    {
        var errors = new List<Exception>();

        foreach (var package in packages)
        {
            try
            {
                package.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        return errors;
    }

    private void ApplyDiscovery(PackageDiscoveryResult discovery, IReadOnlyList<PackageInstance> loadOrder)
    {
        _stateStore.Packages.Clear();
        _stateStore.Packages.AddRange(discovery.Packages);
        _stateStore.PackagesById.Clear();

        foreach (var package in discovery.Packages)
        {
            _stateStore.PackagesById.Add(package.Id, package);
        }

        _stateStore.LoadOrder = loadOrder;
    }
}
