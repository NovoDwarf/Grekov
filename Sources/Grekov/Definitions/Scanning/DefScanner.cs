using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Registry;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Definitions.Scanning;

internal sealed class DefScanner
{
	private readonly DefReaderRegistry _readers;
	private readonly IFileService _file;

	public DefScanner(DefReaderRegistry readers, IFileService file)
	{
		ArgumentNullException.ThrowIfNull(readers);
		ArgumentNullException.ThrowIfNull(file);

		_readers = readers;
		_file = file;
	}

	public IReadOnlyList<(string Path, IDefFormatReader Reader)> Scan(PackageInstance package)
	{
		ArgumentNullException.ThrowIfNull(package);

		if (!_file.DirectoryExists(package.DefinitionsPath))
			return [];

		var result = new List<(string Path, IDefFormatReader Reader)>();

		foreach (var path in _file.EnumerateFiles(package.DefinitionsPath, "*", SearchOption.AllDirectories))
		{
			if (_readers.TryResolve(path, out var reader))
			{
				result.Add((path, reader));
			}
		}

		return result;
	}
}