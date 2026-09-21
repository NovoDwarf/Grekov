using Grekov.Binary;
using Grekov.Definitions.Registry;
using Grekov.Extensions;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;
using Microsoft.Extensions.Options;
using NovoDwarf.FS.Directories.Interfaces;
using NovoDwarf.FS.Files.Interfaces;

namespace Grekov.Providers.Binary;

internal sealed class BinaryPackageProvider : IPackageProvider
{
    private const string Extension = "*.gpak";

    private readonly string[] _roots;

    private readonly IDirectoryReader _directoryReader;
    private readonly IFileStreamer _fileStreamer;
    private readonly IDefReaderRegistry _readerRegistry;

    private readonly GpakReader _reader;
    
    public BinaryPackageProvider(
        IOptions<BinaryOptions> options,
        IDirectoryReader directoryReader,
        IFileStreamer fileStreamer,
        IDefReaderRegistry readerRegistry,
        GpakReader reader)
    {
        _roots = [.. options.Value.Roots];

        _directoryReader = directoryReader;
        _reader = reader;
        _readerRegistry = readerRegistry;
        _fileStreamer = fileStreamer;
    }

    public ValueTask<IReadOnlyList<PackageInstance>> DiscoverAsync(CancellationToken token = default)
    {
        List<PackageInstance> packages = [];

        foreach (var root in _roots)
        {
            token.ThrowIfCancellationRequested();

            DiscoverRoot(root, packages, token);
        }

        return ValueTask.FromResult<IReadOnlyList<PackageInstance>>(packages);
    }

    private void DiscoverRoot(string root, ICollection<PackageInstance> packages, CancellationToken token)
    {
        if (!_directoryReader.Exists(root))
            return;

        foreach (var path in _directoryReader.EnumerateFiles(root, Extension))
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
                Content = new BinaryPackageContent(archive)
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