using System.Text;

namespace Grekov.Binary.Example;

internal static class Program
{
    public static void Main()
    {
        Console.WriteLine("======================================");
        Console.WriteLine("GPAK Serialization Demo");
        Console.WriteLine("======================================");
        Console.WriteLine();

        var package = CreatePackage();

        Console.WriteLine("=== PACKAGE ===");
        PrintPackage(package);

        Console.WriteLine();
        Console.WriteLine("=== SERIALIZATION ===");

        using var stream = new MemoryStream();

        var writer = new GpakWriter();

        writer.Write(stream, package);

        Console.WriteLine($"Serialized size : {stream.Length} bytes");
        Console.WriteLine($"Position        : {stream.Position}");

        Console.WriteLine();
        Console.WriteLine("Header bytes:");

        stream.Position = 0;

        var headerBytes =
            new byte[
                Math.Min(
                    (int)GpakFormat.HeaderSize,
                    (int)stream.Length)];

        stream.ReadExactly(headerBytes);

        Console.WriteLine(
            Convert.ToHexString(headerBytes));

        Console.WriteLine();
        Console.WriteLine("=== DESERIALIZATION ===");

        stream.Position = 0;

        var reader = new GpakReader();

        using var archive =
            reader.Open(stream);

        Console.WriteLine(
            $"Magic          : 0x{archive.Header.Magic:X8}");

        Console.WriteLine(
            $"Version        : {archive.Header.Version}");

        Console.WriteLine(
            $"Header size    : {archive.Header.HeaderSize}");

        Console.WriteLine(
            $"Section count  : {archive.Header.SectionCount}");

        Console.WriteLine();
        Console.WriteLine("Sections:");

        foreach (var section in archive.Sections)
        {
            Console.WriteLine(
                $"  {section.Type,-14} " +
                $"Offset={section.Offset,-6} " +
                $"Length={section.Length}");
        }

        Console.WriteLine();
        Console.WriteLine("=== MANIFEST ===");

        PrintManifest(archive);

        Console.WriteLine();
        Console.WriteLine("=== DEFINITIONS ===");

        PrintSectionEntries(
            archive,
            GpakSectionType.Definitions);

        Console.WriteLine();
        Console.WriteLine("=== ASSEMBLIES ===");

        PrintSectionEntries(
            archive,
            GpakSectionType.Assemblies);

        Console.WriteLine();
        Console.WriteLine("=== VALIDATION ===");

        Validate(
            package,
            archive);

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("SUCCESS");
        Console.WriteLine("GPAK round-trip completed successfully.");
        Console.WriteLine("======================================");
    }

    private static GpakPackage CreatePackage()
    {
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                $"grekov-gpak-{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            tempDirectory);

        var manifestPath =
            Path.Combine(
                tempDirectory,
                "manifest.xml");

        var definitionsDirectory =
            Path.Combine(
                tempDirectory,
                "definitions");

        var assembliesDirectory =
            Path.Combine(
                tempDirectory,
                "assemblies");

        Directory.CreateDirectory(
            definitionsDirectory);

        Directory.CreateDirectory(
            assembliesDirectory);

        var swordPath =
            Path.Combine(
                definitionsDirectory,
                "iron_sword.xml");

        var knightPath =
            Path.Combine(
                definitionsDirectory,
                "knight.json");

        var assemblyPath =
            Path.Combine(
                assembliesDirectory,
                "Grekov.Game.dll");

        File.WriteAllText(
            manifestPath,
            """
            <package>
                <id>demo</id>
                <version>1.0.0</version>
            </package>
            """);

        File.WriteAllText(
            swordPath,
            """
            <Sword>
                <Id>iron_sword</Id>
                <Damage>25</Damage>
            </Sword>
            """);

        File.WriteAllText(
            knightPath,
            """
            {
                "$type": "Knight",
                "Id": "knight",
                "Health": 100
            }
            """);

        // Для теста формата неважно, является ли это
        // настоящей DLL. Это просто бинарный payload.
        File.WriteAllBytes(
            assemblyPath,
            [
                0x4D, 0x5A, 0x90, 0x00,
                0x01, 0x02, 0x03, 0x04,
                0xAA, 0xBB, 0xCC, 0xDD
            ]);

        return new GpakPackage
        {
            Manifest =
                new GpakFile(
                    manifestPath,
                    "manifest.xml"),

            Definitions =
            [
                new GpakFile(
                    swordPath,
                    "items/iron_sword.xml"),

                new GpakFile(
                    knightPath,
                    "characters/knight.json")
            ],

            Assemblies =
            [
                new GpakFile(
                    assemblyPath,
                    "Grekov.Game.dll")
            ]
        };
    }

    private static void PrintPackage(
        GpakPackage package)
    {
        Console.WriteLine(
            $"Manifest        : {package.Manifest.Name}");

        Console.WriteLine(
            $"Assemblies      : {package.Assemblies.Count}");

        Console.WriteLine(
            $"Definitions     : {package.Definitions.Count}");

        Console.WriteLine(
            $"Localization    : {package.Localization.Count}");

        Console.WriteLine(
            $"Resources       : {package.Resources.Count}");
    }

    private static void PrintManifest(
        GpakArchive archive)
    {
        var index =
            archive.GetIndex(
                GpakSectionType.Manifest);

        Assert(
            index.Entries.Count == 1,
            "Manifest section must contain exactly one entry.");

        var entry =
            index.Entries.Single();

        Console.WriteLine(
            $"  Path   : {entry.Path}");

        Console.WriteLine(
            $"  Offset : {entry.Offset}");

        Console.WriteLine(
            $"  Length : {entry.Length}");

        var bytes =
            archive.ReadEntry(
                GpakSectionType.Manifest,
                entry);

        Console.WriteLine();
        Console.WriteLine(
            Encoding.UTF8.GetString(bytes));
    }

    private static void PrintSectionEntries(
        GpakArchive archive,
        GpakSectionType type)
    {
        if (!archive.HasSection(type))
        {
            Console.WriteLine("  <empty>");
            return;
        }

        var index =
            archive.GetIndex(type);

        foreach (var entry in index.Entries)
        {
            Console.WriteLine(
                $"  {entry.Path,-32} " +
                $"Offset={entry.Offset,-6} " +
                $"Length={entry.Length}");

            var bytes =
                archive.ReadEntry(
                    type,
                    entry);

            Console.WriteLine(
                $"      Data: " +
                $"{Convert.ToHexString(bytes)}");
        }
    }

    private static void Validate(
        GpakPackage package,
        GpakArchive archive)
    {
        Assert(
            archive.Header.Magic ==
            GpakFormat.Magic,
            "Invalid magic.");

        Assert(
            archive.Header.Version ==
            GpakFormat.CurrentVersion,
            "Invalid version.");

        Assert(
            archive.Header.HeaderSize ==
            GpakFormat.HeaderSize,
            "Invalid header size.");

        Assert(
            archive.HasSection(
                GpakSectionType.Manifest),
            "Manifest section is missing.");

        ValidateManifest(
            archive,
            package.Manifest);

        ValidateFiles(
            archive,
            GpakSectionType.Definitions,
            package.Definitions);

        ValidateFiles(
            archive,
            GpakSectionType.Assemblies,
            package.Assemblies);

        Console.WriteLine("  [OK] Header");
        Console.WriteLine("  [OK] Manifest");
        Console.WriteLine("  [OK] Definitions");
        Console.WriteLine("  [OK] Assemblies");
    }

    private static void ValidateManifest(
        GpakArchive archive,
        GpakFile expected)
    {
        var index =
            archive.GetIndex(
                GpakSectionType.Manifest);

        Assert(
            index.Entries.Count == 1,
            "Manifest entry count mismatch.");

        Assert(
            index.TryGet(
                expected.Name,
                out var entry),
            $"Manifest [{expected.Name}] is missing.");

        var expectedBytes =
            File.ReadAllBytes(
                expected.Path);

        var actualBytes =
            archive.ReadEntry(
                GpakSectionType.Manifest,
                entry);

        Assert(
            expectedBytes.AsSpan()
                .SequenceEqual(actualBytes),
            "Manifest content mismatch.");
    }

    private static void ValidateFiles(
        GpakArchive archive,
        GpakSectionType type,
        IReadOnlyList<GpakFile> expected)
    {
        if (expected.Count == 0)
        {
            Assert(
                !archive.HasSection(type),
                $"{type} section should be absent.");

            return;
        }

        var index =
            archive.GetIndex(type);

        Assert(
            index.Entries.Count == expected.Count,
            $"{type} entry count mismatch.");

        foreach (var file in expected)
        {
            Assert(
                index.TryGet(
                    file.Name,
                    out var entry),
                $"Entry [{file.Name}] is missing.");

            var expectedBytes =
                File.ReadAllBytes(
                    file.Path);

            var actualBytes =
                archive.ReadEntry(
                    type,
                    entry);

            Assert(
                expectedBytes.AsSpan()
                    .SequenceEqual(actualBytes),
                $"Content mismatch for [{file.Name}].");
        }
    }

    private static void Assert(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"[FAIL] {message}");
        }
    }
}