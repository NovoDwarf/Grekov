using Grekov.Packaging.Interfaces;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultPackageCatalog : IPackageCatalog
{
	private const string ManifestFileName = "manifest.xml";

	private readonly string[] _roots;
	private readonly IFileService _fileService;

	public DefaultPackageCatalog(IFileService fileService, params string[] roots)
	{
		ArgumentNullException.ThrowIfNull(fileService);
		ArgumentNullException.ThrowIfNull(roots);

		_fileService = fileService;
		_roots = roots;
	}

	public IEnumerable<string> GetPackageDirectories()
	{
		return _roots
		       .Where(_fileService.DirectoryExists)
		       .SelectMany(root => _fileService.EnumerateFiles(root, ManifestFileName, SearchOption.AllDirectories))
		       .Select(Path.GetDirectoryName)
		       .Where(path => path is not null)
		       .Distinct(StringComparer.OrdinalIgnoreCase)
		       .Cast<string>();
	}
}