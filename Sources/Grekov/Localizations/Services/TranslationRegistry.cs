using Grekov.Localizations.Entities;
using Grekov.Localizations.Interfaces;

namespace Grekov.Localizations.Services;

internal sealed class TranslationRegistry
{
	private readonly ILocalizationRuntime _runtime;

	public TranslationRegistry(ILocalizationRuntime runtime)
	{
		_runtime = runtime;
	}

	public void Apply(IEnumerable<LocalizedStringDef> defs)
	{
		foreach (var def in defs)
			_runtime.Apply(def.Locale, def.Key, def.Value);
	}

	public void RemovePackage(string packageId)
	{
		_runtime.RemovePackage(packageId);
	}
}
