using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Scanning;

internal sealed class DefScanner
{
	private readonly IDefReaderRegistry _readers;

	public DefScanner(IDefReaderRegistry readers)
	{
		_readers = readers;
	}

	public IReadOnlyList<(string Path, IDefFormatReader Reader)> Scan(PackageInstance package)
	{
		ArgumentNullException.ThrowIfNull(package);

		var result = new List<(string Path, IDefFormatReader Reader)>();

		foreach (var path in package.Container.EnumerateFiles(PackageContentType.Definition))
		{
			if (_readers.TryResolve(path, out var reader))
			{
				result.Add((path, reader!));
			}
		}

		return result;
	}
}
