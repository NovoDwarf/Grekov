using System.Reflection;
using Grekov.Assemblies.Entities;
using Grekov.Assemblies.Interfaces;
using Grekov.Core.Interfaces;
using Grekov.Packaging.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Assemblies.Services;

internal sealed class EntrypointService
{
    private readonly AssemblyRegistry _assemblies;
    private readonly IPackageContextFactory _contextFactory;
    private readonly IServiceProvider _services;

    private readonly Dictionary<string, LoadedEntrypoint> _entrypoints = new(StringComparer.OrdinalIgnoreCase);

    public EntrypointService(
        AssemblyRegistry assemblies,
        IServiceProvider services, 
        IPackageContextFactory contextFactory)
    {
        _assemblies = assemblies;
        _services = services;
        _contextFactory = contextFactory;
    }

    public Task LoadAsync(IReadOnlyList<PackageInstance> packages)
    {
        foreach (var package in packages)
            LoadPackage(package);

        return Task.CompletedTask;
    }
    
    public Task UnloadAsync(IReadOnlyList<PackageInstance> packages)
    {
        foreach (var package in packages.Reverse())
            UnloadPackage(package);

        return Task.CompletedTask;
    }
    
    public Task NotifyLoadedAsync(IReadOnlyList<PackageInstance> packages)
    {
        foreach (var package in packages)
        {
            if (_entrypoints.TryGetValue(package.Id, out var loaded))
            {
                loaded.Entrypoint.OnLoaded();
            }
        }

        return Task.CompletedTask;
    }
    
    private void LoadPackage(PackageInstance package)
    {
        if (_entrypoints.ContainsKey(package.Id))
            throw new InvalidOperationException($"Entrypoint for package [{package.Id}] is already loaded.");

        var assemblies = _assemblies.GetAssemblies(package.Id);
        var type = FindEntrypointType(package.Id, assemblies);

        if (type is null)
            return;

        var entrypoint = CreateEntrypoint(package.Id, type);

        try
        {
            var context = _contextFactory.Create(package);
            entrypoint.OnLoad(context);
            _entrypoints.Add(package.Id, new LoadedEntrypoint(context, entrypoint));
        }
        catch
        {
            DisposeEntrypoint(entrypoint);
            throw;
        }
    }

    private void UnloadPackage(PackageInstance package)
    {
        if (!_entrypoints.Remove(package.Id, out var loaded))
            return;

        try
        {
            loaded.Entrypoint.OnUnload();
        }
        finally
        {
            DisposeEntrypoint(loaded.Entrypoint);
        }
    }

    private IEntrypoint CreateEntrypoint(
        string packageId,
        Type type)
    {
        try
        {
            return (IEntrypoint)ActivatorUtilities.CreateInstance(_services, type);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Could not create entrypoint [{type.FullName}] for package [{packageId}].", exception);
        }
    }

    private static Type? FindEntrypointType(string packageId, IReadOnlyList<Assembly> assemblies)
    {
        var candidates = assemblies
            .SelectMany(GetLoadableTypes)
            .Where(static type => type is { IsClass: true, IsAbstract: false } && typeof(IEntrypoint).IsAssignableFrom(type))
            .ToArray();

        return candidates.Length switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new InvalidOperationException(
                $"Package [{packageId}] contains multiple IEntrypoint implementations: " +
                $"{string.Join(", ", candidates.Select(static type => type.FullName ?? type.Name))}")
        };
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var types = exception.Types
                .OfType<Type>()
                .ToArray();

            if (types.Length > 0)
                return types;

            throw new InvalidOperationException($"Could not load types from assembly [{assembly.FullName}].", exception);
        }
    }

    private static void DisposeEntrypoint(IEntrypoint entrypoint)
    {
        if (entrypoint is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return;
        }

        if (entrypoint is IDisposable disposable)
            disposable.Dispose();
    }

    private sealed record LoadedEntrypoint(IPackageContext Context, IEntrypoint Entrypoint);
}