using Grekov.Abstractions;

namespace Grekov.Definitions.Materializations;

internal sealed class DefMaterializer
{
	private readonly DefFactory _factory;
	private readonly DefFieldApplier _fields;

	public DefMaterializer(DefFactory factory, DefFieldApplier fields)
	{
		_factory = factory;
		_fields = fields;
	}

	public Def Materialize(DefRaw raw)
	{
		ArgumentNullException.ThrowIfNull(raw);

		var definition = _factory.Create(raw.Type, raw);

		_fields.Apply(definition, raw.Fields);

		return definition;
	}
}