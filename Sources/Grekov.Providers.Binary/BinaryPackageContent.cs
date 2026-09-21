using Grekov.Binary;
using Grekov.Packaging.Enums;
using Grekov.Packaging.Interfaces;

namespace Grekov.Providers.Binary;

internal sealed class BinaryPackageContent : IPackageContent
{
	private readonly GpakArchive _archive;

	public BinaryPackageContent(GpakArchive archive)
	{
		ArgumentNullException.ThrowIfNull(archive);

		_archive = archive;
	}

	public bool Exists(PackageContentType type, string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		foreach (var section in _archive.Sections)
		{
			if (_archive.TryGetEntry(section.Type, path, out _))
				return true;
		}

		return false;
	}

	public Stream OpenRead(PackageContentType type, string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		foreach (var section in _archive.Sections)
		{
			if (_archive.TryGetEntry(section.Type, path, out var entry))
				return _archive.OpenEntry(section.Type, entry);
		}

		throw new FileNotFoundException($"Package file [{path}] was not found.");
	}

	public IReadOnlyList<string> EnumerateFiles(PackageContentType type, string path = "")
	{
		List<string> files = [];

		foreach (var section in _archive.Sections)
		{
			var index = _archive.GetIndex(section.Type);

			foreach (var entry in index.Entries)
			{
				if (string.IsNullOrEmpty(path) || entry.Path.StartsWith(path.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
					files.Add(entry.Path);
			}
		}

		return files;
	}

	public void Dispose()
	{
		_archive.Dispose();
	}
}