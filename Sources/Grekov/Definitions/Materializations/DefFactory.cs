using Grekov.Core;
using Grekov.Definitions.Models;

namespace Grekov.Definitions.Materializations;

internal sealed class DefFactory
{
	public Def Create(Type type, DefRaw raw)
	{
		ArgumentNullException.ThrowIfNull(type);

		if (!typeof(Def).IsAssignableFrom(type))
			throw new ArgumentException($"Type [{type.FullName}] must inherit from {nameof(Def)}.");

		if (Activator.CreateInstance(type) is not Def definition)
			throw new InvalidOperationException($"Could not create definition of type [{type.FullName}].");

		definition.Id = raw.Id;
		definition.PackageId = raw.PackageId;
		definition.ResourcePath = raw.ResourcePath;
		
		return definition;
	}
}