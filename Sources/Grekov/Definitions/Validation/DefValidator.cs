using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;

namespace Grekov.Definitions.Validation;

internal static class DefValidator
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static void Validate(Def definition, List<DefIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(issues);

        ValidateValue(definition, string.Empty, definition, issues, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static void ValidateValue(object? value, string path, Def definition, List<DefIssue> issues, HashSet<object> ancestors)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
            return;

        if (!ancestors.Add(value))
            return;

        try
        {
            ValidateObject(value, path, definition, issues);

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    ValidateValue(entry.Value, $"{path}[{entry.Key}]", definition, issues, ancestors);
                }
            }
            else if (value is IEnumerable collection)
            {
                var index = 0;

                foreach (var item in collection)
                {
                    ValidateValue(item, $"{path}[{index}]", definition, issues, ancestors);
                    index++;
                }
            }

            foreach (var property in PropertyCache.GetOrAdd(value.GetType(), ScanProperties))
            {
                ValidateValue(property.GetValue(value), CombinePath(path, property.Name),
                    definition, issues, ancestors);
            }
        }
        finally
        {
            ancestors.Remove(value);
        }
    }

    private static void ValidateObject(object value, string path, Def definition, List<DefIssue> issues)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);

        foreach (var result in results)
        {
            var hasMembers = false;

            foreach (var member in result.MemberNames)
            {
                hasMembers = true;
                AddIssue(CombinePath(path, member), result);
            }

            if (!hasMembers)
            {
                AddIssue(path, result);
            }
        }

        return;

        void AddIssue(string memberPath, ValidationResult result)
        {
            issues.Add(DefIssues.ValidationFailed(
                definition.PackageId,
                definition.ResourcePath,
                definition.Id,
                memberPath,
                result.ErrorMessage ?? "Validation failed."));
        }
    }

    private static PropertyInfo[] ScanProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetMethod is { IsPublic: true, IsStatic: false }
                && property.GetIndexParameters().Length == 0
                && property.IsDefined(typeof(DefFieldAttribute), inherit: true))
            .ToArray();
    }

    private static string CombinePath(string path, string member)
    {
        return string.IsNullOrEmpty(path) ? member : $"{path}.{member}";
    }
}
