using Grekov.Abstractions;

namespace Grekov.Definitions.Materializations;

internal sealed class DefFactory
{
	public Def Create(Type type, DefRaw raw)
	{
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(raw);

		if (!typeof(Def).IsAssignableFrom(type))
			throw new ArgumentException($"Type [{type.FullName}] must inherit from {nameof(Def)}.", nameof(type));

		if (Activator.CreateInstance(type) is not Def definition)
			throw new InvalidOperationException($"Could not create definition of type [{type.FullName}].");

		definition.Id = raw.Id;
		definition.PackageId = raw.PackageId;
		definition.ResourcePath = raw.ResourcePath;

		return definition;
	}
}