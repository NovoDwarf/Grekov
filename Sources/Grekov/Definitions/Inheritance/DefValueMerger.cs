using Grekov.Abstractions;
using Grekov.Abstractions.Enums;

namespace Grekov.Definitions.Inheritance;

internal sealed class DefValueMerger
{
	public IReadOnlyDictionary<string, DefValue> Merge(IReadOnlyDictionary<string, DefValue> parent, IReadOnlyDictionary<string, DefValue> child)
	{
		ArgumentNullException.ThrowIfNull(parent);
		ArgumentNullException.ThrowIfNull(child);

		var result = new Dictionary<string, DefValue>(parent, StringComparer.OrdinalIgnoreCase);

		foreach (var (name, childValue) in child)
		{
			if (!result.TryGetValue(name, out var parentValue))
			{
				result.Add(name, childValue);
				continue;
			}

			result[name] = MergeValue(parentValue, childValue);
		}

		return result;
	}

	private DefValue MergeValue(DefValue parent, DefValue child)
	{
		if (child.Kind == DefValueKind.Null || parent.Kind != DefValueKind.Object || child.Kind != DefValueKind.Object)
			return child;

		var merged = Merge(parent.Object!, child.Object!);

		return DefValue.ObjectValue(merged);
	}
}