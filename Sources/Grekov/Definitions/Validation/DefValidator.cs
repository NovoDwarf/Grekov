using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Definitions.Validation;

internal sealed class DefValidator
{
    private readonly IServiceProvider _services;
    private readonly DefAttributeValidatorRegistry _registry;
    private readonly IDefCatalog _catalog;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<Type, object> _handlers = [];
    private readonly Dictionary<Type, TypeMetadata> _metadata = [];
    private readonly List<object> _ownedHandlers = [];
    private readonly List<DefIssue> _issues = [];

    private DefValidator(IServiceProvider services, DefAttributeValidatorRegistry registry,
        IDefCatalog catalog, CancellationToken cancellationToken)
    {
        _services = services;
        _registry = registry;
        _catalog = catalog;
        _cancellationToken = cancellationToken;
    }

    public static async Task<List<DefIssue>> ValidateAsync(
        IReadOnlyList<Def> definitions,
        IDefCatalog stored,
        DefAttributeValidatorRegistry registry,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var validator = new DefValidator(scope.ServiceProvider, registry,
            new DefValidationCatalog(definitions, stored), cancellationToken);

        try
        {
            foreach (var definition in definitions)
            {
                validator.ValidateValue(definition, string.Empty, definition,
                    new HashSet<object>(ReferenceEqualityComparer.Instance));
            }

            return validator._issues;
        }
        finally
        {
            for (var index = validator._ownedHandlers.Count - 1; index >= 0; index--)
            {
                var handler = validator._ownedHandlers[index];

                if (handler is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (handler is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    private void ValidateValue(object? value, string path, Def definition, HashSet<object> ancestors)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
        {
            return;
        }

        if (!ancestors.Add(value))
        {
            return;
        }

        try
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(value, new ValidationContext(value, _services, null),
                results, validateAllProperties: true);
            AddResults(results, path, definition);

            var metadata = GetMetadata(value.GetType());
            ValidateAttributes(metadata.Attributes, value,
                new DefValidationContext(definition, value, value.GetType(), path, _catalog, _cancellationToken));

            ValidateCollection(value, path, definition, ancestors);

            foreach (var field in metadata.Fields)
            {
                var propertyValue = field.Property.GetValue(value);
                var propertyPath = CombinePath(path, field.Property.Name);
                ValidateAttributes(field.Attributes, propertyValue,
                    new DefValidationContext(definition, value, field.Property.PropertyType,
                        propertyPath, _catalog, _cancellationToken));

                if (field.Traverse)
                {
                    ValidateValue(propertyValue, propertyPath, definition, ancestors);
                }
            }
        }
        finally
        {
            ancestors.Remove(value);
        }
    }

    private void ValidateCollection(object value, string path, Def definition, HashSet<object> ancestors)
    {
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                ValidateValue(entry.Value, $"{path}[{entry.Key}]", definition, ancestors);
            }
        }
        else if (value is IEnumerable collection)
        {
            var index = 0;

            foreach (var item in collection)
            {
                ValidateValue(item, $"{path}[{index}]", definition, ancestors);
                index++;
            }
        }
    }

    private void ValidateAttributes(
        IReadOnlyList<DefValidationAttribute> attributes, object? value, DefValidationContext context)
    {
        foreach (var attribute in attributes)
        {
            foreach (var registration in _registry.GetValidators(attribute.GetType()))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var handler = GetHandler(registration.ImplementationType);
                AddResults(registration.Validate(handler, attribute, value, context),
                    context.MemberPath, context.Definition);
            }
        }
    }

    private object GetHandler(Type type)
    {
        if (_handlers.TryGetValue(type, out var handler))
        {
            return handler;
        }

        handler = _services.GetService(type);

        if (handler is null)
        {
            handler = ActivatorUtilities.CreateInstance(_services, type);
            _ownedHandlers.Add(handler);
        }

        _handlers.Add(type, handler);
        return handler;
    }

    private void AddResults(IEnumerable<ValidationResult> results, string path, Def definition)
    {
        foreach (var result in results)
        {
            _cancellationToken.ThrowIfCancellationRequested();
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

        void AddIssue(string memberPath, ValidationResult result)
        {
            _issues.Add(DefIssues.ValidationFailed(definition.PackageId, definition.ResourcePath,
                definition.Id, memberPath, result.ErrorMessage ?? "Validation failed."));
        }
    }

    private TypeMetadata GetMetadata(Type type)
    {
        if (_metadata.TryGetValue(type, out var metadata))
        {
            return metadata;
        }

        var fields = new List<FieldMetadata>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod is not { IsPublic: true, IsStatic: false }
                || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var traverse = property.IsDefined(typeof(DefFieldAttribute), inherit: true);
            var attributes = property.GetCustomAttributes<DefValidationAttribute>(inherit: true).ToArray();

            if (traverse || attributes.Length > 0)
            {
                fields.Add(new FieldMetadata(property, traverse, attributes));
            }
        }

        metadata = new TypeMetadata(type.GetCustomAttributes<DefValidationAttribute>(inherit: true).ToArray(),
            fields.ToArray());
        _metadata.Add(type, metadata);
        return metadata;
    }

    private static string CombinePath(string path, string member)
    {
        return string.IsNullOrEmpty(path) ? member : $"{path}.{member}";
    }

    private sealed record TypeMetadata(DefValidationAttribute[] Attributes, FieldMetadata[] Fields);
    private sealed record FieldMetadata(PropertyInfo Property, bool Traverse, DefValidationAttribute[] Attributes);
}
