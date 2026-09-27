using System.Reflection;
using Grekov.Core;
using Grekov.Core.Attributes;

namespace Grekov.Definitions.Registry;

public sealed class DefTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _typesByElementName = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyCollection<Type> Types => [.. _typesByName.Values];

    public void Refresh(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var typesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var typesByElementName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in assemblies.SelectMany(GetLoadableTypes).Where(IsDefinitionType))
        {
            Register(type, typesByName, typesByElementName);
        }

        _typesByName.Clear();
        _typesByElementName.Clear();

        foreach (var pair in typesByName)
            _typesByName.Add(pair.Key, pair.Value);

        foreach (var pair in typesByElementName)
            _typesByElementName.Add(pair.Key, pair.Value);
    }

    public void Clear()
    {
        _typesByName.Clear();
        _typesByElementName.Clear();
    }

    public bool TryResolve(string typeName, out Type type)
    {
        return _typesByName.TryGetValue(typeName, out type!);
    }

    public bool TryResolveByElementName(string elementName, out Type type)
    {
        return _typesByElementName.TryGetValue(elementName, out type!);
    }

    private static void Register(Type type, Dictionary<string, Type> typesByName, Dictionary<string, Type> typesByElementName)
    {
        var typeName = type.FullName ?? type.Name;

        if (!typesByName.TryAdd(typeName, type))
            throw new InvalidOperationException($"Definition type [{typeName}] is already registered.");

        var attribute = type.GetCustomAttribute<DefTypeAttribute>();
        var elementName = attribute?.ElementName ?? type.Name;

        if (typesByElementName.TryAdd(elementName, type))
            return;
        
        var existingType = typesByElementName[elementName];

        throw new InvalidOperationException(
            $"Definition element [{elementName}] is already registered " +
            $"by [{existingType.FullName}] and cannot be registered by [{typeName}].");
    }

    private static bool IsDefinitionType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } && typeof(Def).IsAssignableFrom(type);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
