namespace Grekov.Binary;

public sealed class GpakArchive : IDisposable
{
    private readonly Stream _stream;
    private readonly GpakReader _reader;

    private readonly Dictionary<GpakSectionType, GpakSection> _sectionsByType;
    private readonly Dictionary<GpakSectionType, GpakSectionIndex> _indexes = [];

    internal GpakArchive(
        Stream stream,
        GpakReader reader,
        GpakHeader header,
        IReadOnlyList<GpakSection> sections)
    {
        _stream = stream;
        _reader = reader;

        Header = header;
        Sections = [.. sections];

        _sectionsByType = new Dictionary<GpakSectionType, GpakSection>(sections.Count);

        foreach (var section in sections)
        {
            if (!_sectionsByType.TryAdd(section.Type, section))
            {
                throw new InvalidDataException($"GPAK contains duplicate section [{section.Type}].");
            }
        }
    }

    public GpakHeader Header { get; }

    public IReadOnlyList<GpakSection> Sections { get; }

    public bool HasSection(GpakSectionType type)
    {
        return _sectionsByType.ContainsKey(type);
    }

    public bool TryGetSection(GpakSectionType type, out GpakSection section)
    {
        return _sectionsByType.TryGetValue(type, out section);
    }

    public GpakSection GetSection(GpakSectionType type)
    {
        if (!_sectionsByType.TryGetValue(type, out var section))
            throw new InvalidDataException($"GPAK section [{type}] was not found.");

        return section;
    }

    public Stream OpenSection(GpakSectionType type)
    {
        var data = ReadSection(type);

        return new MemoryStream(data, writable: false);
    }

    public byte[] ReadSection(GpakSectionType type)
    {
        var section = GetSection(type);

        return _reader.ReadSection(_stream, section);
    }

    public GpakSectionIndex GetIndex(GpakSectionType type)
    {
        if (_indexes.TryGetValue(type, out var index))
            return index;

        var section = GetSection(type);

        index = _reader.ReadIndex(_stream, section);

        _indexes.Add(type, index);

        return index;
    }

    public bool TryGetEntry(GpakSectionType type, string path, out GpakEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return GetIndex(type).TryGet(path, out entry);
    }

    public byte[] ReadEntry(GpakSectionType type, GpakEntry entry)
    {
        var section = GetSection(type);
        var index = GetIndex(type);

        return _reader.ReadEntry(_stream, section, index, entry);
    }

    public byte[] ReadEntry(GpakSectionType type, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!TryGetEntry(type, path, out var entry))
            throw new KeyNotFoundException($"GPAK entry [{path}] was not found in section [{type}].");

        return ReadEntry(type, entry);
    }

    public Stream OpenEntry(GpakSectionType type, GpakEntry entry)
    {
        var data = ReadEntry(type, entry);

        return new MemoryStream(data, writable: false);
    }

    public Stream OpenEntry(GpakSectionType type, string path)
    {
        var data = ReadEntry(type, path);

        return new MemoryStream(data, writable: false);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}