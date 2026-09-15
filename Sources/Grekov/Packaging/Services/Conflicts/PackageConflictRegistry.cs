using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services;

namespace Grekov.Packaging.Services.Conflicts;

public sealed class PackageConflictRegistry : IPackageConflictSink
{
    private readonly Dictionary<string, string> _ownersByKey = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string key, string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        if (!_ownersByKey.TryGetValue(key, out var ownerPackageId))
        {
            _ownersByKey[key] = packageId;

            return;
        }

        if (string.Equals(ownerPackageId, packageId, StringComparison.OrdinalIgnoreCase))
            return;

        throw new InvalidOperationException(PackageIssues.ConflictIssue(key, packageId, ownerPackageId).Message);
    }

    public bool IsRegistered(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _ownersByKey.ContainsKey(key);
    }

    public bool TryGetOwner(string key, out string? packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _ownersByKey.TryGetValue(key, out packageId);
    }

    public void UnregisterPackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var keys = _ownersByKey
                   .Where(pair =>
                       string.Equals(pair.Value, packageId, StringComparison.OrdinalIgnoreCase))
                   .Select(static pair => pair.Key)
                   .ToArray();

        foreach (var key in keys)
        {
            _ownersByKey.Remove(key);
        }
    }

    public void Clear()
    {
        _ownersByKey.Clear();
    }
}