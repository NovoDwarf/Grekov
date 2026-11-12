using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Assemblies.Services;
using Grekov.Definitions;
using Grekov.Definitions.Registry;
using Grekov.Localizations.Services;

namespace Grekov;

internal sealed class GrekovService : IGrekovService
{
    private readonly IPackageService _packages;
    private readonly AssemblyService _assemblies;
    private readonly AssemblyRegistry _assemblyRegistry;
    private readonly EntrypointService _entrypoints;
    private readonly DefService _defs;
    private readonly IDefStorage _storage;
    private readonly DefTypeRegistry _defTypes;
    private readonly DefAttributeValidatorRegistry _validators;
    private readonly DefValueConverterRegistry _converters;
    private readonly LocalizationService _localization;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private IReadOnlyList<PackageInstance>? _sessionPackages;
    private bool _storageLoadStarted;

    public GrekovService(
        IPackageService packages,
        AssemblyService assemblies,
        AssemblyRegistry assemblyRegistry,
        DefService defs,
        IDefStorage storage,
        DefTypeRegistry defTypes,
        LocalizationService localization,
        EntrypointService entrypoints,
        DefAttributeValidatorRegistry validators,
        DefValueConverterRegistry converters)
    {
        _packages = packages;
        _assemblies = assemblies;
        _assemblyRegistry = assemblyRegistry;
        _defs = defs;
        _storage = storage;
        _defTypes = defTypes;
        _localization = localization;
        _entrypoints = entrypoints;
        _validators = validators;
        _converters = converters;
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await _lifecycleGate.WaitAsync(token);

        try
        {
            if (_sessionPackages is not null)
            {
                throw new InvalidOperationException("Grekov is already started. Stop it before starting again.");
            }

            try
            {
                await _packages.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                var packages = GetActivePackages();
                _sessionPackages = packages;

                await _assemblies.LoadAsync(packages, token);
                var assemblies = _assemblyRegistry.GetAllAssemblies();
                _defTypes.Refresh(assemblies);
                _validators.Refresh(assemblies);
                _converters.Refresh(assemblies);

                _storageLoadStarted = true;
                await _storage.LoadAsync(_defTypes.Types, token);
                await _entrypoints.LoadAsync(packages);
                await _defs.LoadAsync(packages, token);
                await _localization.LoadAsync(packages);
                await _entrypoints.NotifyLoadedAsync(packages);
            }
            catch (Exception failure)
            {
                var cleanupErrors = await CleanupAsync();

                if (cleanupErrors.Count > 0)
                {
                    throw new AggregateException("Grekov start failed and rollback encountered errors.",
                        cleanupErrors.Prepend(failure));
                }

                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        await _lifecycleGate.WaitAsync(token);

        try
        {
            var cleanupErrors = await CleanupAsync();

            if (cleanupErrors.Count > 0)
            {
                throw new AggregateException("Grekov cleanup encountered errors.", cleanupErrors);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task<List<Exception>> CleanupAsync()
    {
        var errors = new List<Exception>();
        var batches = (_sessionPackages ?? [])
            .Reverse()
            .Select(static package => (IReadOnlyList<PackageInstance>)new[] { package })
            .ToArray();

        await UnloadEach(_localization.UnloadAsync);
        await UnloadEach(batch => _defs.UnloadAsync(batch));
        await UnloadEach(_entrypoints.UnloadAsync);
        _converters.Clear();
        await UnloadEach(batch => _assemblies.UnloadAsync(batch));

        if (_storageLoadStarted)
        {
            await Attempt(() => _storage.UnloadAsync());
        }

        await Attempt(() => _packages.UnloadAsync());
        await Attempt(() =>
        {
            _validators.Refresh([]);
            return Task.CompletedTask;
        });
        _defTypes.Clear();
        _sessionPackages = null;
        _storageLoadStarted = false;
        return errors;

        async Task UnloadEach(Func<IReadOnlyList<PackageInstance>, Task> unload)
        {
            foreach (var batch in batches)
            {
                await Attempt(() => unload(batch));
            }
        }

        async Task Attempt(Func<Task> cleanup)
        {
            try
            {
                // Once cleanup begins, cancellation must not leave partially loaded state behind.
                await cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
    }

    private IReadOnlyList<PackageInstance> GetActivePackages()
    {
        return _packages.LoadOrder
            .Where(static package => package is { Enabled: true, HasErrors: false })
            .ToArray();
    }
}
