using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Extensions;
using Microsoft.Extensions.Options;
using NovoDwarf.FS.Directories.Interfaces;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Providers.FileSystem;

internal sealed class FileSystemPackageProvider : IPackageProvider
{
    private readonly string[] _roots;
    private readonly string _manifestName;

    private readonly IDirectoryReader _directoryReader;
	private readonly IFileStreamer _fileStreamer;
    private readonly IPathParser _pathParser;
    private readonly IDefReaderRegistry _readerRegistry;

    private readonly FileSystemContentFactory _contentFactory;
    
    public FileSystemPackageProvider(
        IOptions<GrekovOptions> grekovOptions,
        IOptions<FileSystemOptions> fileOptions,
        IDefReaderRegistry readerRegistry,
        IDirectoryReader directoryReader,
		IFileStreamer fileStreamer,
        IPathParser pathParser,
        FileSystemContentFactory contentFactory)
    {
        _manifestName = grekovOptions.Value.ManifestName;
        _roots = [.. fileOptions.Value.Roots];

        _contentFactory = contentFactory;
        _readerRegistry = readerRegistry;
        
        _directoryReader = directoryReader;
		_fileStreamer = fileStreamer;
        _pathParser = pathParser;
    }

    public ValueTask<IReadOnlyList<PackageInstance>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(Discover());
    }

    public IReadOnlyList<PackageInstance> Discover()
    {
        List<PackageInstance> packages = [];

        foreach (var discovered in DiscoverManifests()) 
            packages.Add(CreatePackage(discovered));

        return packages;
    }

    private PackageInstance CreatePackage(DiscoveredManifest discovered)
    {
        var manifest = discovered.Manifest;

        return new PackageInstance
        {
            Id = manifest.Id,
            Version = manifest.Version,
            Dependencies = manifest.Dependencies,
			Container = _contentFactory.Create(discovered.Directory)
        };
    }

    private IEnumerable<DiscoveredManifest> DiscoverManifests()
    {
        foreach (var manifestFile in DiscoverManifestFiles())
		{
			using var stream = _fileStreamer.OpenRead(manifestFile.ManifestPath);
			var manifest = manifestFile.Reader.ReadManifest(stream);

			if (string.IsNullOrWhiteSpace(manifest.Id))
				manifest.Id = manifestFile.Directory;

            yield return new DiscoveredManifest(manifestFile.Directory, manifestFile.ManifestPath, manifest);
        }
    }

    private IEnumerable<ManifestFile> DiscoverManifestFiles()
    {
        foreach (var root in _roots)
        {
            if (!_directoryReader.Exists(root))
                continue;

            var files = FindManifestFiles(root).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            foreach (var group in files.GroupBy(_pathParser.GetDirectoryName, StringComparer.OrdinalIgnoreCase))
            {
                var manifestFiles = group.ToArray();

                if (manifestFiles.Length != 1)
                    throw new InvalidOperationException(manifestFiles.Length == 0
                        ? $"No manifest found in directory [{group.Key}]."
                        : $"Multiple manifests found in directory [{group.Key}]: " +
                          $"{string.Join(", ", manifestFiles)}");

                var manifestPath = manifestFiles[0];

                if (!_readerRegistry.TryResolve(manifestPath, out var reader))
                    throw new InvalidOperationException($"No definition reader registered for file [{manifestPath}].");

                var directory = group.Key
                                ?? throw new InvalidOperationException($"Cannot determine directory for manifest [{manifestPath}].");

                yield return new ManifestFile(directory, manifestPath, reader!);
            }
        }
    }

    private IEnumerable<string> FindManifestFiles(string root)
    {
        foreach (var extension in _readerRegistry.Extensions)
        {
            var manifestFileName = $"{_manifestName}{extension}";

            foreach (var file in _directoryReader.EnumerateFiles(root, manifestFileName, SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    private sealed record ManifestFile(string Directory, string ManifestPath, IDefFormatReader Reader);
    private sealed record DiscoveredManifest(string Directory, string ManifestPath, PackageManifest Manifest);
}
