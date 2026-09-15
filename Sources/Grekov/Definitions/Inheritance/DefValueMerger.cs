using Grekov.Core;
using Grekov.Core.Enums;

namespace Grekov.Definitions.Inheritance;

internal sealed class DefValueMerger
{
	public IReadOnlyDictionary<string, DefValue> Merge(IReadOnlyDictionary<string, DefValue> parent, IReadOnlyDictionary<string, DefValue> child)
	{
		var result = new Dictionary<string, DefValue>(parent, StringComparer.OrdinalIgnoreCase);

		foreach (var (key, childValue) in child)
		{
			if (result.TryGetValue(key, out var parentValue))
			{
				result[key] = MergeValue(parentValue, childValue);
			}
			else
			{
				result[key] = childValue;
			}
		}

		return result;
	}

	private DefValue MergeValue(DefValue parent, DefValue child)
	{
		if (child.Kind == DefValueKind.Null)
			return child;

		if (parent.Kind != DefValueKind.Object || child.Kind != DefValueKind.Object) 
			return child;
		
		var merged = Merge(parent.Object!, child.Object!);

		return DefValue.ObjectValue(merged);

	}
}