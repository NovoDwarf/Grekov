using Grekov.Assemblies.Services;
using Grekov.Definitions.Registry;
using Grekov.Definitions.Workflow;
using Grekov.Localizations.Services;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Services.Conflicts;

namespace Grekov.Packaging.Services.Runtime;

internal sealed class PackageWorkflow
{
    private readonly AssemblyService _assemblies;
    private readonly EntrypointService _entrypoints;
    private readonly DefService _defs;
    private readonly LocalizationService _localization;
    private readonly PackageConflictRegistry _conflicts;
    private readonly DefValueConverterRegistry _converterRegistry;
    private readonly DefTypeRegistry _defTypes;
    private readonly AssemblyRegistry _assemblyRegistry;

    public PackageWorkflow(
        AssemblyService assemblies,
        EntrypointService entrypoints,
        DefService defs,
        LocalizationService localization,
        PackageConflictRegistry conflicts,
        DefTypeRegistry defTypes,
        AssemblyRegistry assemblyRegistry, DefValueConverterRegistry converterRegistry)
    {
        _assemblies = assemblies;
        _entrypoints = entrypoints;
        _defs = defs;
        _localization = localization;
        _conflicts = conflicts;
        _defTypes = defTypes;
        _assemblyRegistry = assemblyRegistry;
        _converterRegistry = converterRegistry;
    }

    public void Apply(IReadOnlyList<PackageInstance> loadOrder)
    {
        var packages = loadOrder
            .Where(static package => package is { Enabled: true, HasErrors: false })
            .ToArray();

        Clear();

        _assemblies.Load(packages);
        _entrypoints.Load(packages);

        var assemblies = _assemblyRegistry.GetAllAssemblies();
        
        _defTypes.Refresh(assemblies);
        _converterRegistry.Refresh(assemblies);

        _defs.Load(packages, _conflicts);
        _defs.Build();

        _localization.Load(packages);

        _entrypoints.NotifyLoaded(packages);
    }

    public void Unload(IReadOnlyList<PackageInstance> loadOrder)
    {
        var packages = loadOrder
            .Where(static package => package is { Enabled: true, HasErrors: false })
            .ToArray();

        _localization.Unload(packages);
        _defs.Unload(packages);
        _entrypoints.Unload(packages);
        _assemblies.Unload(packages);

        _conflicts.Clear();
        _defTypes.Clear();
    }

    private void Clear()
    {
        _localization.Clear();
        _defs.Clear();
        _entrypoints.Clear();
        _assemblies.Clear();
        _conflicts.Clear();
        _defTypes.Clear();
    }
}
