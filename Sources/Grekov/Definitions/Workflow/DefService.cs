using Grekov.Definitions.Indexing;
using Grekov.Definitions.Models;
using Grekov.Definitions.Scanning;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Services.Conflicts;

namespace Grekov.Definitions.Workflow;

internal sealed class DefService
{
	private readonly DefRawIndex _rawIndex;
	private readonly DefScanner _scanner;
	private readonly DefWorkflow _workflow;

	public DefService(DefRawIndex rawIndex, DefScanner scanner, DefWorkflow workflow)
	{
		_rawIndex = rawIndex;
		_scanner = scanner;
		_workflow = workflow;
	}

	public void Load(IReadOnlyList<PackageInstance> packages, PackageConflictRegistry conflicts)
	{
		foreach (var package in packages)
		{
			LoadPackage(package, conflicts);
		}
	}

	public void Build()
	{
		_workflow.Build();
	}

	public void Unload(IReadOnlyList<PackageInstance> packages)
	{
		foreach (var package in packages.Reverse())
		{
			_rawIndex.RemovePackage(package.Id);
		}
	}

	public void Clear()
	{
		_rawIndex.Clear();
	}

	private void LoadPackage(PackageInstance package, PackageConflictRegistry conflicts)
	{
		foreach (var (path, reader) in _scanner.Scan(package))
		{
			var fallbackId = Path.GetFileNameWithoutExtension(path);
			var context = new DefReadContext(package.Id, path, fallbackId, []);
			var definitions = reader.ReadDefs(context);

			foreach (var definition in definitions)
			{
				conflicts.Register($"def:{definition.Id}", package.Id);

				_rawIndex.Add(definition);
			}
		}
	}
}
