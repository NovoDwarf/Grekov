using System.Reflection;
using Grekov.Abstractions.Interfaces.Definitions;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Definitions.Registry;

public sealed class DefValueConverterRegistry
{
    private readonly IServiceProvider _services;

    private readonly List<IDefValueConverter> _builtIns = [];
    private readonly List<IDefValueConverter> _packageConverters = [];

    public DefValueConverterRegistry(IServiceProvider services)
    {
        _services = services;

        ScanAssembly(typeof(DefValueConverterRegistry).Assembly, _builtIns);
    }

    public IEnumerable<IDefValueConverter> Converters => _packageConverters.Concat(_builtIns);

    public void Refresh(IEnumerable<Assembly> assemblies)
    {
        Clear();

        foreach (var assembly in assemblies.Distinct())
        {
            if (assembly == typeof(DefValueConverterRegistry).Assembly)
            {
                continue;
            }

            ScanAssembly(assembly, _packageConverters);
        }
    }

    public void Clear()
    {
        _packageConverters.Clear();
    }

    private void ScanAssembly(Assembly assembly, List<IDefValueConverter> target)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(IDefValueConverter).IsAssignableFrom(type))
            {
                continue;
            }

            if (type is not { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            {
                continue;
            }

            var converter = (IDefValueConverter)ActivatorUtilities.CreateInstance(_services, type);

            target.Add(converter);
        }
    }
}
