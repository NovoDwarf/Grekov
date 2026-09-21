namespace Grekov.Packaging.Interfaces;

public interface IPackageSettingsStore
{
	IReadOnlyDictionary<string, bool> LoadEnabledOverrides();
}
