using System.Reflection;
using Grekov.Assemblies.Entities;
using Grekov.Core.Interfaces;
using Grekov.Packaging.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Assemblies.Services;

internal sealed class EntrypointService
{
    private readonly AssemblyRegistry _assemblies;
    private readonly IServiceProvider _services;

    private readonly Dictionary<string, LoadedEntrypoint> _entrypoints = new(StringComparer.OrdinalIgnoreCase);

    public EntrypointService(AssemblyRegistry assemblies, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(services);

        _assemblies = assemblies;
        _services = services;
    }

    public void Load(IReadOnlyList<PackageInstance> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        foreach (var package in packages) 
            LoadPackage(package);
    }

    public void NotifyLoaded(IReadOnlyList<PackageInstance> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        foreach (var package in packages)
        {
            if (!_entrypoints.TryGetValue(package.Id, out var loaded))
                continue;

            loaded.Entrypoint.OnLoaded();
        }
    }

    public void Unload(IReadOnlyList<PackageInstance> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        foreach (var package in packages.Reverse())
        {
            if (!_entrypoints.Remove(package.Id, out var loaded))
                continue;

            try
            {
                loaded.Entrypoint.OnUnload();
            }
            finally
            { 
                DisposeEntrypoint(loaded.Entrypoint);
            }
        }
    }

    public void Clear()
    {
        foreach (var loaded in _entrypoints.Values)
        {
            DisposeEntrypoint(loaded.Entrypoint);
        }

        _entrypoints.Clear();
    }

    private void LoadPackage(PackageInstance package)
    {
        if (_entrypoints.ContainsKey(package.Id))
            throw new InvalidOperationException($"Entrypoint for package '{package.Id}' is already loaded.");

        var assemblies = _assemblies.GetAssemblies(package.Id);
        var entrypointType = FindEntrypointType(package.Id, assemblies);
        
        if (entrypointType == null)
            return;
        
        var entrypoint = CreateEntrypoint(package.Id, entrypointType);
        
        var context = new PackageContext(package.Id, package.RootPath, _services, assemblies);
        var loaded = new LoadedEntrypoint(context, entrypoint);

        try
        {
            entrypoint.OnLoad(context);

            _entrypoints.Add(package.Id, loaded);
        }
        catch
        {
            DisposeEntrypoint(entrypoint);
            throw;
        }
    }

    private IEntrypoint CreateEntrypoint(string packageId, Type entrypointType)
    {
        try
        {
            return (IEntrypoint)ActivatorUtilities.CreateInstance(_services, entrypointType);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not create entrypoint '{entrypointType.FullName}' " + $"for package '{packageId}'.", exception);
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
            _ => throw new InvalidOperationException($"Package '{packageId}' contains multiple IEntrypoint implementations: " + string.Join(", ", candidates.Select(static type => type.FullName ?? type.Name)))
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
                                 .Where(static type => type is not null)
                                 .Cast<Type>()
                                 .ToArray();

            if (types.Length > 0)
                return types;

            throw new InvalidOperationException($"Could not load types from assembly '{assembly.FullName}'.", exception);
        }
    }

    private static void DisposeEntrypoint(IEntrypoint entrypoint)
    {
        if (entrypoint is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (entrypoint is IAsyncDisposable asyncDisposable)
        {

        }
    }

    private sealed record LoadedEntrypoint(PackageContext Context, IEntrypoint Entrypoint);
}