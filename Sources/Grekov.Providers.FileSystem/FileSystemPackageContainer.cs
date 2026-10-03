using Grekov.Packaging.Enums;
using Grekov.Packaging.Interfaces;
using NovoDwarf.FS.Directories.Interfaces;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Providers.FileSystem;

internal sealed class FileSystemPackageContainer : IPackageContainer
{
    private readonly string _root;
    
    private readonly IPathResolver _pathResolver;
    private readonly IPathCombiner _pathCombiner;
    private readonly IFileSearcher _fileSearcher;
    private readonly IFileStreamer _fileStreamer;
    private readonly IDirectoryReader _directoryReader;

    public FileSystemPackageContainer(string root,
        IDirectoryReader directoryReader, 
        IPathResolver pathResolver, 
        IPathCombiner pathCombiner, 
        IFileSearcher fileSearcher,
        IFileStreamer fileStreamer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(directoryReader);

        _directoryReader = directoryReader;
        _pathResolver = pathResolver;
        _pathCombiner = pathCombiner;
        _fileSearcher = fileSearcher;
        _fileStreamer = fileStreamer;

        _root = _pathResolver.GetFullPath(root);
    }

    public bool Exists(PackageContentType type, string path)
    {
        return _fileSearcher.Exists(GetFullPath(type, path));
    }

    public Stream OpenRead(PackageContentType type, string path)
    {
        var fullPath = GetFullPath(type, path);

        if (!_fileSearcher.Exists(fullPath))
            throw new FileNotFoundException($"Package file [{path}] was not found.", fullPath);

        return _fileStreamer.OpenRead(fullPath);
    }

    public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "")
    {
        var root = GetDirectoryPath(type);
        var directory = string.IsNullOrWhiteSpace(path)
            ? root
            : _pathCombiner.Combine(root, path);

        if (!_directoryReader.Exists(directory))
            return [];

        return 
        [.. 
            _directoryReader
                .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file => _pathResolver.GetRelativePath(root, file))
        ];
    }

    public void Dispose()
    {
    }

    private string GetFullPath(PackageContentType type, string path)
    {
        return _pathCombiner.Combine(GetDirectoryPath(type), path);
    }

    private string GetDirectoryPath(PackageContentType type)
    {
        return type switch
        {
            PackageContentType.Manifest => _root,
            PackageContentType.Assembly => _pathCombiner.Combine(_root, "Assemblies"),
            PackageContentType.Definition => _pathCombiner.Combine(_root, "Definitions"),
            PackageContentType.Localization => _pathCombiner.Combine(_root, "Localization"),
            PackageContentType.Resource => _pathCombiner.Combine(_root, "Resources"),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
