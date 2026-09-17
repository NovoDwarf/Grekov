using Grekov.Definitions.Interfaces;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Definitions.Registry;

internal sealed class DefReaderRegistry
{
    private readonly IReadOnlyList<IDefFormatReader> _readers;
    private readonly Dictionary<string, IDefFormatReader> _readersByExtension;
    private readonly IPathService _pathService;

    public DefReaderRegistry(IEnumerable<IDefFormatReader> readers, IPathService pathService)
    {
        ArgumentNullException.ThrowIfNull(readers);
        ArgumentNullException.ThrowIfNull(pathService);

        _pathService = pathService;
        
        _readers = [.. readers];
        _readersByExtension = new Dictionary<string, IDefFormatReader>(StringComparer.OrdinalIgnoreCase);

        RegisterReaders(_readers);
    }

    public IReadOnlyCollection<string> Extensions => _readersByExtension.Keys;
    

    public bool TryResolve(string path, out IDefFormatReader? reader)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var extension = _pathService.GetExtension(path);

        if (!string.IsNullOrWhiteSpace(extension))
            return _readersByExtension.TryGetValue(NormalizeExtension(extension), out reader);
       
        reader = null;
        
        return false;
    }

    private void RegisterReaders(IEnumerable<IDefFormatReader> readers)
    {
        foreach (var reader in readers)
        {
            ArgumentNullException.ThrowIfNull(reader);

            foreach (var extension in reader.Extensions)
            {
                var normalizedExtension = NormalizeExtension(extension);

                if (!_readersByExtension.TryAdd(normalizedExtension, reader))
                    throw new InvalidOperationException($"Definition reader for extension [{normalizedExtension}] is already registered.");
            }
        }
    }

    private static string NormalizeExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var normalized = extension.Trim();

        return normalized.StartsWith(".", StringComparison.Ordinal) ? normalized : $".{normalized}";
    }
}