using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Packaging.Services.Discovery;

internal sealed class PackageDiscover
{
    private const string ManifestFileName = "manifest.xml";

    private readonly IPathService _pathService;
    private static readonly XmlSerializer Serializer = new(typeof(PackageManifest));

    public PackageDiscover(IPathService pathService)
    {
        _pathService = pathService;
    }
    
    public IReadOnlyList<PackageInstance> Discover(IPackageCatalog catalog, IReadOnlyDictionary<string, bool> enabledOverrides)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(enabledOverrides);

        var packages = new List<PackageInstance>();

        foreach (var packageDirectory in catalog.GetPackageDirectories())
        {
            var manifest = ReadManifest(packageDirectory);
            var package = new PackageInstance(manifest.Id, manifest.Version, packageDirectory)
            {
                Enabled = enabledOverrides.GetValueOrDefault(manifest.Id, manifest.Enabled)
            };

            package.Dependencies.AddRange(manifest.Dependencies);

            packages.Add(package);
        }

        return packages;
    }

    private PackageManifest ReadManifest(string packageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);

        var manifestPath = Path.Combine(packageDirectory, ManifestFileName);

        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Package manifest not found: {manifestPath}", manifestPath);

        try
        {
            using var stream = File.OpenRead(manifestPath);

            var manifest = Serializer.Deserialize(stream) as PackageManifest 
                           ?? throw new InvalidDataException($"Manifest is empty: {manifestPath}");

            if (string.IsNullOrWhiteSpace(manifest.Id))
                manifest.Id = _pathService.GetDirectoryName(manifestPath);

            return manifest;
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException($"Invalid XML package manifest: {manifestPath}", exception);
        }
    }


}