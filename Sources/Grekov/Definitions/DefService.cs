using Grekov.Abstractions;
using Grekov.Abstractions.Exceptions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Inheritance;
using Grekov.Definitions.Indexing;
using Grekov.Definitions.Materializations;
using Grekov.Definitions.Registry;
using Grekov.Definitions.Scanning;
using Grekov.Definitions.Validation;
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
    private readonly DefAttributeValidatorRegistry _validators;
    private readonly IServiceProvider _services;
    private readonly IPathParser _pathParser;

    public DefService(
        DefRawIndex rawIndex,
        DefScanner scanner,
        IPathParser pathParser,
        IDefStorage storage,
        DefMaterializer materializer,
        DefInheritanceResolver inheritance,
        DefConflictRegistry conflictRegistry,
        DefIssueRegistry issueRegistry,
        DefAttributeValidatorRegistry validators,
        IServiceProvider services)
    {
        _rawIndex = rawIndex;
        _scanner = scanner;
        _pathParser = pathParser;
        _storage = storage;
        _materializer = materializer;
        _inheritance = inheritance;
        _conflictRegistry = conflictRegistry;
        _issueRegistry = issueRegistry;
        _validators = validators;
        _services = services;
    }

    public async Task LoadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken cancellationToken = default)
    {
        _inheritance.Clear();

        foreach (var package in packages)
        {
            await LoadPackageAsync(package, cancellationToken);
            ApplyIssues(package);
        }

        ThrowIfReadFailed(packages);
        var definitions = new List<Def>();

        foreach (var raw in _rawIndex.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolved = _inheritance.Resolve(raw);
            definitions.Add(_materializer.Materialize(resolved));
        }

        var validationIssues = await DefValidator.ValidateAsync(
            definitions, _storage, _validators, _services, cancellationToken);

        if (validationIssues.Count > 0)
        {
            foreach (var issue in validationIssues)
            {
                _issueRegistry.Add(issue);
            }

            foreach (var package in packages)
            {
                ApplyIssues(package);
            }

            throw new DefValidationException(validationIssues);
        }

        await _storage.AddRangeAsync(definitions, cancellationToken);
    }

    public async Task UnloadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var package in packages.Reverse())
            {
                try
                {
                    await _storage.RemovePackageAsync(package.Id, cancellationToken);
                }
                finally
                {
                    _rawIndex.UnloadPackage(package.Id);
                    _issueRegistry.UnloadPackage(package.Id);
                    _conflictRegistry.UnloadPackage(package.Id);
                }
            }
        }
        finally
        {
            _inheritance.Clear();
        }
    }

    private void ThrowIfReadFailed(IReadOnlyList<PackageInstance> packages)
    {
        var issues = packages.SelectMany(package => _issueRegistry.GetIssues(package.Id)).ToArray();

        if (issues.Length > 0)
        {
            throw new DefValidationException(issues);
        }

        var blockingIssues = packages.SelectMany(static package => package.Issues)
            .Where(static issue => issue.BlocksLoading)
            .ToArray();

        if (blockingIssues.Length > 0)
        {
            var details = string.Join(Environment.NewLine, blockingIssues.Select(static issue => issue.Message));
            throw new InvalidOperationException($"Definition loading failed:{Environment.NewLine}{details}");
        }
    }

    private async Task LoadPackageAsync(PackageInstance package, CancellationToken cancellationToken)
    {
        foreach (var (path, reader) in _scanner.Scan(package))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fallbackId = _pathParser.GetFileNameWithoutExtension(path);
            var context = new DefReadContext(package.Id, path, fallbackId, []);
            await using var stream = package.Container.OpenRead(PackageContentType.Definition, path);
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
