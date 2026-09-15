using Grekov.Localizations.Entities;
using Grekov.Localizations.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultLocalizationRuntime : ILocalizationRuntime
{
	private readonly Dictionary<string, Translation> _translations = new(StringComparer.OrdinalIgnoreCase);

	public IEnumerable<Translation> Translations => _translations.Values;

	public void Apply(string locale, string key, string value)
	{
		if (!_translations.TryGetValue(locale, out var translation))
		{
			translation = new Translation(locale);
			_translations[locale] = translation;
		}

		translation.Messages[key] = value;
	}

	public void RemovePackage(string packageId)
	{
	}
}