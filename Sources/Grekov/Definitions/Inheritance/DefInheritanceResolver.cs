using Grekov.Core;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Models;

namespace Grekov.Definitions.Inheritance;

internal sealed class DefInheritanceResolver
{
	private readonly DefRawIndex _rawIndex;
	private readonly DefValueMerger _merger;

	private readonly Dictionary<DefId, DefRaw> _cache = [];
	
	public DefInheritanceResolver(DefRawIndex rawIndex, DefValueMerger merger)
	{
		_rawIndex = rawIndex;
		_merger = merger;
	}

	public DefRaw Resolve(DefRaw raw)
	{
		ArgumentNullException.ThrowIfNull(raw);

		if (_cache.TryGetValue(raw.Id, out var cached))
			return cached;

		var resolving = new HashSet<DefId>();
		var resolved = ResolveInternal(raw, resolving);

		_cache[raw.Id] = resolved;

		return resolved;
	}

	public void ClearCache()
	{
		_cache.Clear();
	}

	private DefRaw ResolveInternal(DefRaw raw, HashSet<DefId> resolving)
	{
		if (_cache.TryGetValue(raw.Id, out var cached))
			return cached;

		if (!resolving.Add(raw.Id))
		{
			var chain = string.Join(" -> ", resolving.Append(raw.Id));

			throw new InvalidOperationException($"Circular definition inheritance detected: {chain}.");
		}

		try
		{
			if (raw.ParentId is not { } parentId)
			{
				_cache[raw.Id] = raw;
				return raw;
			}

			var parent = _rawIndex.Get(parentId);
			var resolvedParent = ResolveInternal(parent, resolving);

			var mergedFields = _merger.Merge(resolvedParent.Fields, raw.Fields);

			var result = new DefRaw
			{
				Id = raw.Id,
				TypeName = raw.TypeName,
				ParentId = raw.ParentId,
				Fields = mergedFields,
				PackageId = raw.PackageId,
				ResourcePath = raw.ResourcePath
			};

			_cache[raw.Id] = result;

			return result;
		}
		finally
		{
			resolving.Remove(raw.Id);
		}
	}
}