namespace Grekov.Localizations.Interfaces;

public interface ILocalizationRuntime
{
	void Apply(string packageId, string locale, string key, string value);

	void RemovePackage(string packageId);

	void Clear();
}