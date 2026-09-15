namespace Grekov.Localizations.Interfaces;

public interface ILocalizationRuntime
{
	public void Apply(string locale, string key, string value);
	public void RemovePackage(string packageId);
}
