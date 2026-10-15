using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Registry;

internal sealed class DefAttributeValidatorRegistry
{
	private readonly Dictionary<Type, List<Registration>> _registrations = [];

	public DefAttributeValidatorRegistry()
	{
		Refresh([]);
	}

	public void Refresh(IEnumerable<Assembly> assemblies)
	{
		var registrations = new Dictionary<Type, List<Registration>>();

		foreach (var assembly in assemblies.Prepend(typeof(DefAttributeValidatorRegistry).Assembly).Distinct())
		{
			foreach (var type in assembly.GetTypes())
			{
				if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
				{
					continue;
				}

				foreach (var contract in type.GetInterfaces())
				{
					if (!contract.IsGenericType ||
					    contract.GetGenericTypeDefinition() != typeof(IDefAttributeValidator<>))
					{
						continue;
					}

					var attributeType = contract.GenericTypeArguments[0];
					
					var factory = typeof(DefAttributeValidatorRegistry)
					              .GetMethod(nameof(CreateRegistration), BindingFlags.NonPublic | BindingFlags.Static)!
					              .MakeGenericMethod(attributeType);
				
					var registration = (Registration)factory.Invoke(null, [type])!;

					if (!registrations.TryGetValue(attributeType, out var handlers))
					{
						handlers = [];
						registrations.Add(attributeType, handlers);
					}

					handlers.Add(registration);
				}
			}
		}

		_registrations.Clear();

		foreach (var pair in registrations)
		{
			_registrations.Add(pair.Key, pair.Value);
		}
	}

	public IReadOnlyList<Registration> GetValidators(Type attributeType)
	{
		if (!_registrations.TryGetValue(attributeType, out var handlers))
			throw new InvalidOperationException($"No definition validator is registered for attribute [{attributeType.FullName}].");

		return handlers;
	}

	private static Registration CreateRegistration<TAttribute>(Type implementationType) where TAttribute : DefValidationAttribute
	{
		return new Registration(implementationType, static (handler, attribute, value, context) =>
			((IDefAttributeValidator<TAttribute>)handler).Validate((TAttribute)attribute, value, context));
	}

	internal sealed record Registration(Type ImplementationType, Func<object, DefValidationAttribute, object?, DefValidationContext, IEnumerable<ValidationResult>> Validate);
}