using Grekov.Core.Enums;

namespace Grekov.Core;

public sealed class DefValue
{
	public DefValueKind Kind { get; }

	public object? Scalar { get; }

	public IReadOnlyDictionary<string, DefValue>? Object { get; }

	public IReadOnlyList<DefValue>? List { get; }

	private DefValue(DefValueKind kind, object? scalar = null, IReadOnlyDictionary<string, DefValue>? @object = null, IReadOnlyList<DefValue>? list = null)
	{
		Kind = kind;
		Scalar = scalar;
		Object = @object;
		List = list;
	}

	public static DefValue Null() => new(DefValueKind.Null);

	public static DefValue ScalarValue(object? value) => new(DefValueKind.Scalar, scalar: value);

	public static DefValue ObjectValue(IReadOnlyDictionary<string, DefValue> value) => new(DefValueKind.Object, @object: value);

	public static DefValue ListValue(IReadOnlyList<DefValue> value) => new(DefValueKind.List, list: value);
}