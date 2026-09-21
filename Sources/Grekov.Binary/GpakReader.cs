using System.Text;

namespace Grekov.Binary;

public sealed class GpakReader
{
    public GpakArchive Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(
                "The input stream must be readable.",
                nameof(stream));
        }

        if (!stream.CanSeek)
        {
            throw new ArgumentException(
                "The input stream must support seeking.",
                nameof(stream));
        }

        var header = ReadHeader(stream);
        var sections = ReadSections(stream, header);

        ValidateSections(
            stream,
            header,
            sections);

        return new GpakArchive(
            stream,
            this,
            header,
            sections);
    }

    private static GpakHeader ReadHeader(
        Stream stream)
    {
        using var reader = new BinaryReader(
            stream,
            Encoding.UTF8,
            leaveOpen: true);

        var header = new GpakHeader(
            reader.ReadUInt32(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt32(),
            reader.ReadUInt32());

        ValidateHeader(header);

        return header;
    }

    private static IReadOnlyList<GpakSection> ReadSections(
        Stream stream,
        GpakHeader header)
    {
        stream.Position = header.HeaderSize;

        using var reader = new BinaryReader(
            stream,
            Encoding.UTF8,
            leaveOpen: true);

        var sections =
            new GpakSection[header.SectionCount];

        for (var i = 0; i < sections.Length; i++)
        {
            var type =
                (GpakSectionType)reader.ReadByte();

            var flags =
                (GpakSectionFlags)reader.ReadByte();

            reader.ReadUInt16();

            var offset =
                reader.ReadInt64();

            var length =
                reader.ReadInt64();

            if (offset < 0)
            {
                throw new InvalidDataException(
                    $"Invalid GPAK section offset [{offset}].");
            }

            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Invalid GPAK section length [{length}].");
            }

            sections[i] = new GpakSection(
                type,
                flags,
                offset,
                length);
        }

        return sections;
    }

    internal byte[] ReadSection(
        Stream stream,
        GpakSection section)
    {
        ValidateRange(
            stream,
            section.Offset,
            section.Length);

        stream.Position = section.Offset;

        var buffer = new byte[section.Length];

        stream.ReadExactly(buffer);

        return buffer;
    }

    internal GpakSectionIndex ReadIndex(
        Stream stream,
        GpakSection section)
    {
        var data = ReadSection(
            stream,
            section);

        using var memory =
            new MemoryStream(data);

        using var reader =
            new BinaryReader(
                memory,
                Encoding.UTF8,
                leaveOpen: true);

        var count = reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException(
                "Invalid GPAK entry count.");
        }

        List<GpakEntry> entries = [];

        for (var i = 0; i < count; i++)
        {
            var pathLength =
                reader.ReadInt32();

            if (pathLength <= 0)
            {
                throw new InvalidDataException(
                    "Invalid GPAK entry path length.");
            }

            var pathBytes =
                reader.ReadBytes(pathLength);

            if (pathBytes.Length != pathLength)
            {
                throw new EndOfStreamException();
            }

            var path =
                Encoding.UTF8.GetString(pathBytes);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidDataException(
                    "GPAK entry path cannot be empty.");
            }

            var offset =
                reader.ReadInt64();

            var length =
                reader.ReadInt64();

            if (offset < 0)
            {
                throw new InvalidDataException(
                    $"Invalid GPAK entry offset [{offset}].");
            }

            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Invalid GPAK entry length [{length}].");
            }

            entries.Add(
                new GpakEntry(
                    path,
                    offset,
                    length));
        }

        var dataOffset = memory.Position;

        ValidateEntries(
            section,
            dataOffset,
            data.Length,
            entries);

        return new GpakSectionIndex(
            entries,
            dataOffset);
    }

    internal byte[] ReadEntry(
        Stream stream,
        GpakSection section,
        GpakSectionIndex index,
        GpakEntry entry)
    {
        var absoluteOffset =
            checked(
                section.Offset +
                index.DataOffset +
                entry.Offset);

        ValidateRange(
            stream,
            absoluteOffset,
            entry.Length);

        stream.Position = absoluteOffset;

        var buffer =
            new byte[entry.Length];

        stream.ReadExactly(buffer);

        return buffer;
    }

    private static void ValidateHeader(
        GpakHeader header)
    {
        if (header.Magic != GpakFormat.Magic)
        {
            throw new InvalidDataException(
                "The file is not a valid GPAK package.");
        }

        if (header.Version !=
            GpakFormat.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Unsupported GPAK version [{header.Version}].");
        }

        if (header.HeaderSize !=
            GpakFormat.HeaderSize)
        {
            throw new InvalidDataException(
                $"Unsupported GPAK header size " +
                $"[{header.HeaderSize}].");
        }

        if (header.SectionCount == 0)
        {
            throw new InvalidDataException(
                "GPAK contains no sections.");
        }
    }

    private static void ValidateSections(
        Stream stream,
        GpakHeader header,
        IReadOnlyList<GpakSection> sections)
    {
        var tableEnd =
            checked(
                header.HeaderSize +
                sections.Count *
                GpakFormat.SectionSize);

        if (tableEnd > stream.Length)
        {
            throw new InvalidDataException(
                "GPAK section table exceeds stream length.");
        }

        foreach (var section in sections)
        {
            ValidateRange(
                stream,
                section.Offset,
                section.Length);
        }
    }

    private static void ValidateEntries(
        GpakSection section,
        long dataOffset,
        long sectionLength,
        IReadOnlyList<GpakEntry> entries)
    {
        var seen =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (!seen.Add(entry.Path))
            {
                throw new InvalidDataException(
                    $"Duplicate GPAK entry [{entry.Path}].");
            }

            var end =
                checked(
                    dataOffset +
                    entry.Offset +
                    entry.Length);

            if (entry.Offset < 0 ||
                entry.Length < 0 ||
                end > sectionLength)
            {
                throw new InvalidDataException(
                    $"GPAK entry [{entry.Path}] " +
                    $"exceeds section bounds.");
            }
        }
    }

    private static void ValidateRange(
        Stream stream,
        long offset,
        long length)
    {
        if (offset < 0 || length < 0)
        {
            throw new InvalidDataException(
                "Invalid GPAK data range.");
        }

        var end = checked(offset + length);

        if (end > stream.Length)
        {
            throw new InvalidDataException(
                "GPAK data range exceeds stream length.");
        }
    }
}