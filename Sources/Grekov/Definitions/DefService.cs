using Grekov.Core;
using Grekov.Definitions.Inheritance;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Materializations;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;
using Grekov.Definitions.Scanning;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Enums;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Definitions;

internal sealed class DefService
{
	private readonly DefRawIndex _rawIndex;
	private readonly DefScanner _scanner;
	private readonly IDefStorage _storage;
	private readonly DefMaterializer _materializer;
	private readonly DefInheritanceResolver _inheritance;
	private readonly DefIssueRegistry _issueRegistry;
	private readonly DefConflictRegistry _conflictRegistry;

	private readonly IPathParser _pathParser;

	public DefService(DefRawIndex rawIndex, 
		DefScanner scanner, 
		IPathParser pathParser, 
		IDefStorage storage,
		DefMaterializer materializer, 
		DefInheritanceResolver inheritance, 
		DefConflictRegistry conflictRegistry, DefIssueRegistry issueRegistry)
	{
		_rawIndex = rawIndex;
		_scanner = scanner;
		
		_pathParser = pathParser;
		_storage = storage;
		_materializer = materializer;
		_inheritance = inheritance;
		_conflictRegistry = conflictRegistry;
		_issueRegistry = issueRegistry;
	}

	public async Task LoadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken cancellationToken = default)
    {
        foreach (var package in packages)
        {
			await LoadPackageAsync(package, cancellationToken);
            ApplyIssues(package);
        }

        var definitions = new List<Def>();

        foreach (var raw in _rawIndex.All)
        {
            var resolved = _inheritance.Resolve(raw);
            var definition = _materializer.Materialize(resolved);

            definitions.Add(definition);
        }
        
        await _storage.AddRangeAsync(definitions, cancellationToken);

	}

	public async Task UnloadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken cancellationToken = default)
    {
        foreach (var package in packages.Reverse())
        {
			await _storage.RemovePackageAsync(package.Id, cancellationToken);
			_rawIndex.UnloadPackage(package.Id);
            _issueRegistry.UnloadPackage(package.Id);
            _conflictRegistry.UnloadPackage(package.Id);
        }
	}

	private async Task LoadPackageAsync(PackageInstance package, CancellationToken cancellationToken)
    {
		foreach (var (path, reader) in _scanner.Scan(package))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var fallbackId = _pathParser.GetFileNameWithoutExtension(path);
			var context = new DefReadContext(package.Id, path, fallbackId, []);

			using var stream = package.Container.OpenRead(PackageContentType.Definition, path);
			var definitions = reader.ReadDefinitions(stream, context);

            foreach (var definition in definitions)
            {
                var key = $"def:{definition.Id}";

                if (!_conflictRegistry.Register(key, package.Id, out var ownerPackageId))
                {
                    package.AddIssue(PackageIssue.ConflictIssue(key, package.Id, ownerPackageId!));
                    continue;
                }

                _rawIndex.Add(definition);
            }
        }
    }

    private void ApplyIssues(PackageInstance package)
    {
        foreach (var issue in _issueRegistry.GetIssues(package.Id))
        {
            package.AddIssue(PackageIssue.DefinitionIssue(issue));
        }
    }
}
