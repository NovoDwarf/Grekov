using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Binary;
using Grekov.Extensions;
using Microsoft.Extensions.Options;
using NovoDwarf.FS.Directories.Interfaces;
using NovoDwarf.FS.Files.Interfaces;

namespace Grekov.Providers.Binary;

internal sealed class BinaryPackageProvider : IPackageProvider
{
    private const string Extension = "*.gpak";

    private readonly string[] _roots;

    private readonly IFileSearcher _fileSearcher;
    private readonly IDirectoryReader _directoryReader;
    private readonly IFileStreamer _fileStreamer;
    private readonly IDefReaderRegistry _readerRegistry;

    private readonly GpakReader _reader;

    public BinaryPackageProvider(
        IOptions<BinaryOptions> options,
        IFileSearcher fileSearcher,
        IFileStreamer fileStreamer,
        IDefReaderRegistry readerRegistry,
        GpakReader reader, IDirectoryReader directoryReader)
    {
        _roots = [.. options.Value.Roots];

        _fileSearcher = fileSearcher;
        _reader = reader;
        _directoryReader = directoryReader;
        _readerRegistry = readerRegistry;
        _fileStreamer = fileStreamer;
    }

    public ValueTask<IReadOnlyList<PackageInstance>> DiscoverAsync(CancellationToken token = default)
    {
        List<PackageInstance> packages = [];

        try
        {
            foreach (var root in _roots)
            {
                token.ThrowIfCancellationRequested();
                DiscoverRoot(root, packages, token);
            }

            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IReadOnlyList<PackageInstance>>(packages);
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };

            foreach (var package in packages)
            {
                try
                {
                    package.Dispose();
                }
                catch (Exception cleanupFailure)
                {
                    errors.Add(cleanupFailure);
                }
            }

            if (errors.Count > 1)
            {
                throw new AggregateException("Package discovery and cleanup failed.", errors);
            }

            throw;
        }
    }

    private void DiscoverRoot(string root, ICollection<PackageInstance> packages, CancellationToken token)
    {
        if (!_directoryReader.Exists(root))
            return;

        foreach (var path in _fileSearcher.EnumerateFiles(root, Extension))
        {
            token.ThrowIfCancellationRequested();
            packages.Add(ReadPackage(path));
        }
    }

    private PackageInstance ReadPackage(string path)
    {
        var stream = _fileStreamer.OpenRead(path);

        try
        {
            var archive = _reader.Open(stream);
            var manifest = ReadManifest(archive);

            return new PackageInstance
            {
                Id = manifest.Id,
                Version = manifest.Version,
                Dependencies = manifest.Dependencies,
				Container = new BinaryPackageContainer(archive)
            };
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private PackageManifest ReadManifest(GpakArchive archive)
    {
        var index = archive.GetIndex(GpakSectionType.Manifest);

        if (index.Entries.Count != 1)
            throw new InvalidDataException("GPAK manifest section must contain exactly one entry.");

        var entry = index.Entries.Single();

        if (!_readerRegistry.TryResolve(entry.Path, out var reader))
            throw new InvalidDataException("No definition reader is registered " + $"for manifest [{entry.Path}].");

        using var stream = archive.OpenEntry(GpakSectionType.Manifest, entry);

        return reader!.ReadManifest(stream);
    }
}
