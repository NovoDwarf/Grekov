namespace Grekov.Definitions.Registry;

public sealed class DefConflictRegistry
{
    private readonly Dictionary<string, string> _ownersByKey = new(StringComparer.OrdinalIgnoreCase);

    public bool Register(string key, string packageId, out string? ownerPackageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        if (!_ownersByKey.TryGetValue(key, out var owner))
        {
            _ownersByKey[key] = packageId;
            ownerPackageId = null;

            return true;
        }

        if (string.Equals(owner, packageId, StringComparison.OrdinalIgnoreCase))
        {
            ownerPackageId = owner;
            return true;
        }

        ownerPackageId = owner;
        return false;
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

    public void UnloadPackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var keys = _ownersByKey
                   .Where(pair => string.Equals(pair.Value, packageId, StringComparison.OrdinalIgnoreCase))
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