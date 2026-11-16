namespace Grekov.Abstractions.Interfaces.Packaging;

public interface IPackageSettingsStore
{
	IReadOnlyDictionary<string, bool> LoadEnabledOverrides();
}
