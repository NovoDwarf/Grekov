using Grekov.Binary;
using Grekov.Packaging.Enums;
using Grekov.Packaging.Interfaces;

namespace Grekov.Providers.Binary;

internal sealed class BinaryPackageContainer : IPackageContainer
{
	private readonly GpakArchive _archive;

	public BinaryPackageContainer(GpakArchive archive)
	{
		ArgumentNullException.ThrowIfNull(archive);

		_archive = archive;
	}

	public bool Exists(PackageContentType type, string path)
	{
		var section = GetSectionType(type);
		return _archive.HasSection(section) && _archive.TryGetEntry(section, path, out _);
	}

	public Stream OpenRead(PackageContentType type, string path)
	{
		var section = GetSectionType(type);

		if (_archive.HasSection(section) && _archive.TryGetEntry(section, path, out var entry))
			return _archive.OpenEntry(section, entry);

		throw new FileNotFoundException($"Package file [{path}] was not found in [{type}] container.");
	}

	public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "")
	{
		var section = GetSectionType(type);

		if (!_archive.HasSection(section))
			return [];

		return [.. _archive.GetIndex(section).Entries
			.Where(entry => string.IsNullOrEmpty(path) || entry.Path.StartsWith(path.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
			.Select(static entry => entry.Path)];
	}

	public void Dispose()
	{
		_archive.Dispose();
	}

	private static GpakSectionType GetSectionType(PackageContentType type)
	{
		return type switch
		{
			PackageContentType.Manifest => GpakSectionType.Manifest,
			PackageContentType.Assembly => GpakSectionType.Assemblies,
			PackageContentType.Definition => GpakSectionType.Definitions,
			PackageContentType.Localization => GpakSectionType.Localization,
			PackageContentType.Resource => GpakSectionType.Resources,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}
