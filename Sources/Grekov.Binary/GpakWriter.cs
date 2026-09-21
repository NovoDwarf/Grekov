using System.Text;

namespace Grekov.Binary;

public sealed class GpakWriter
{
    public void Write(Stream output, GpakPackage package)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(package);

        if (!output.CanWrite)
            throw new ArgumentException("The output stream must be writable.", nameof(output));

        var sections = BuildSections(package);

        CalculateOffsets(sections);
        WriteHeader(output, sections.Count);
        WriteSectionTable(output, sections);

        foreach (var section in sections)
            output.Write(section.Data);
    }

    private static List<PreparedSection> BuildSections(GpakPackage package)
    {
        List<PreparedSection> sections = [];
        
        AddFileSection(sections, GpakSectionType.Manifest, [package.Manifest]);
        AddFileSection(sections, GpakSectionType.Assemblies, package.Assemblies);
        AddFileSection(sections, GpakSectionType.Definitions, package.Definitions);
        AddFileSection(sections, GpakSectionType.Localization, package.Localization);
        AddFileSection(sections, GpakSectionType.Resources, package.Resources);

        return sections;
    }
    
    private static void AddFileSection(ICollection<PreparedSection> sections, GpakSectionType type, IReadOnlyList<GpakFile> files)
    {
        if (files.Count == 0)
            return;

        sections.Add(CreateFileSection(type, files));
    }

    private static PreparedSection CreateFileSection(GpakSectionType type, IReadOnlyList<GpakFile> files)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        List<byte[]> data = [];
        List<GpakEntry> entries = [];

        long dataOffset = 0;

        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(file.Path);

            entries.Add(new GpakEntry(file.Name, dataOffset, bytes.Length));
            data.Add(bytes);

            dataOffset += bytes.Length;
        }

        writer.Write(entries.Count);

        foreach (var entry in entries)
            WriteEntry(writer, entry);

        foreach (var bytes in data) 
            writer.Write(bytes);

        return new PreparedSection(type, GpakSectionFlags.None, stream.ToArray());
    }

    private static void WriteEntry(BinaryWriter writer, GpakEntry entry)
    {
        var path = Encoding.UTF8.GetBytes(entry.Path);

        writer.Write(path.Length);
        writer.Write(path);
        writer.Write(entry.Offset);
        writer.Write(entry.Length);
    }

    private static void CalculateOffsets(IReadOnlyList<PreparedSection> sections)
    {
        long offset = GpakFormat.HeaderSize + checked(sections.Count * GpakFormat.SectionSize);

        foreach (var section in sections)
        {
            section.Offset = offset;
            section.Length = section.Data.Length;
            offset = checked(offset + section.Length);
        }
    }

    private static void WriteHeader(Stream stream, int sectionCount)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(GpakFormat.Magic);
        writer.Write(GpakFormat.CurrentVersion);
        writer.Write((ushort)GpakFlags.None);
        writer.Write((uint)sectionCount);
        writer.Write(GpakFormat.HeaderSize);
    }

    private static void WriteSectionTable(Stream stream, IReadOnlyList<PreparedSection> sections)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        foreach (var section in sections)
        {
            writer.Write((byte)section.Type);
            writer.Write((byte)section.Flags);
            writer.Write((ushort)0);
            writer.Write(section.Offset);
            writer.Write(section.Length);
        }
    }

    private sealed class PreparedSection
    {
        public PreparedSection(GpakSectionType type, GpakSectionFlags flags, byte[] data)
        {
            Type = type;
            Flags = flags;
            Data = data;
        }

        public GpakSectionType Type { get; }

        public GpakSectionFlags Flags { get; }

        public byte[] Data { get; }

        public long Offset { get; set; }

        public long Length { get; set; }
    }
}