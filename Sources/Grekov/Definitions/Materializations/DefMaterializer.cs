using Grekov.Core;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;

namespace Grekov.Definitions.Materializations;

internal sealed class DefMaterializer
{
	private readonly DefTypeRegistry _types;
	private readonly DefFactory _factory;
	private readonly DefFieldApplier _fields;

	public DefMaterializer(DefTypeRegistry types, DefFactory factory, DefFieldApplier fields)
	{
		ArgumentNullException.ThrowIfNull(types);
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(fields);

		_types = types;
		_factory = factory;
		_fields = fields;
	}

	public Def Materialize(DefRaw raw)
	{
		ArgumentNullException.ThrowIfNull(raw);

		if (!_types.TryResolve(raw.TypeName, out var type))
			throw new InvalidOperationException($"Unknown definition type '{raw.TypeName}'.");

		var definition = _factory.Create(type, raw);



		_fields.Apply(definition, raw.Fields);

		return definition;
	}
}