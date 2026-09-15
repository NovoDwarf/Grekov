using Grekov.Definitions.Indexing;
using Grekov.Definitions.Inheritance;
using Grekov.Definitions.Materializations;

namespace Grekov.Definitions.Workflow;

internal sealed class DefWorkflow
{
	private readonly DefRawIndex _rawIndex;
	private readonly DefIndex _index;
	private readonly DefMaterializer _materializer;
	private readonly DefInheritanceResolver _inheritance;

	public DefWorkflow(
		DefMaterializer materializer,
		DefIndex index,
		DefRawIndex rawIndex,
		DefInheritanceResolver inheritance)
	{
		_materializer = materializer;
		_index = index;
		_rawIndex = rawIndex;
		_inheritance = inheritance;
	}

	public void Build()
	{
		foreach (var raw in _rawIndex.All)
		{
			var resolved = _inheritance.Resolve(raw);
			var definition = _materializer.Materialize(resolved);

			_index.Add(definition);
		}
	}
}