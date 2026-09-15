using System.Reflection;
using Grekov.Core;
using Grekov.Core.Attributes;

namespace Grekov.Definitions.Registry;

public sealed class DefTypeRegistry
{
    
    private readonly Dictionary<string, Type> _typesByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _typeNamesByElementName = new(StringComparer.OrdinalIgnoreCase);
    
    public void Refresh(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        _typesByName.Clear();
        _typeNamesByElementName.Clear();
        
        foreach (var type in assemblies.SelectMany(GetLoadableTypes).Where(IsDefinitionType))
        {
            Register(type);
        }
    }

    public void Clear()
    {
        _typesByName.Clear();
        _typeNamesByElementName.Clear();
    }

    public bool TryResolve(string typeName, out Type type)
    {
        return _typesByName.TryGetValue(typeName, out type!);
    }

    public bool TryResolveByElementName(string elementName, out string typeName)
    {
        if (_typeNamesByElementName.TryGetValue(elementName, out typeName!))
            return true;

        typeName = string.Empty;
        return false;
    }

    private void Register(Type type)
    {
        var typeName = type.FullName ?? type.Name;

        if (!_typesByName.TryAdd(typeName, type))
            throw new InvalidOperationException($"Definition type [{typeName}] is already registered.");

        var attribute = type.GetCustomAttribute<DefTypeAttribute>();
        var elementName = attribute?.ElementName ?? type.Name;

        if (!_typeNamesByElementName.TryAdd(elementName, typeName))
            throw new InvalidOperationException($"Definition element [{elementName}] is already registered.");
    }

    private static bool IsDefinitionType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false, IsInterface: false } && typeof(Def).IsAssignableFrom(type);
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