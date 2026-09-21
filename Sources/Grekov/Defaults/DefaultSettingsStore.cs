using Grekov.Packaging.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultSettingsStore : IPackageSettingsStore
{
	public IReadOnlyDictionary<string, bool> LoadEnabledOverrides()
	{
		return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
	}
}