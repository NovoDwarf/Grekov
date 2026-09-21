using Grekov.Localizations.Entities;
using Grekov.Localizations.Interfaces;

namespace Grekov.Defaults;

internal sealed class DefaultLocalizationRuntime : ILocalizationRuntime
{
	private readonly Dictionary<string, Dictionary<string, Translation>> _packages =
		new(StringComparer.OrdinalIgnoreCase);

	public IEnumerable<Translation> Translations =>
		_packages.Values
		         .SelectMany(static translations => translations.Values);

	public void Apply(string packageId, string locale, string key, string value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
		ArgumentException.ThrowIfNullOrWhiteSpace(locale);
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		if (!_packages.TryGetValue(packageId, out var translations))
		{
			translations = new Dictionary<string, Translation>(StringComparer.OrdinalIgnoreCase);

			_packages.Add(packageId, translations);
		}

		if (!translations.TryGetValue(locale, out var translation))
		{
			translation = new Translation(locale);
			translations.Add(locale, translation);
		}

		translation.Messages[key] = value;
	}

	public void RemovePackage(string packageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

		_packages.Remove(packageId);
	}

	public void Clear()
	{
		_packages.Clear();
	}
}