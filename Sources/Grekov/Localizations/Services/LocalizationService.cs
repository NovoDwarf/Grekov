using Grekov.Definitions.Interfaces;
using Grekov.Localizations.Entities;
using Grekov.Packaging;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;

namespace Grekov.Localizations.Services;

internal sealed class LocalizationService
{
	private readonly IDefCatalog _defs;
	private readonly LocalizationServerState _state;
	private readonly TranslationRegistry _translations;

	public LocalizationService(IDefCatalog defs, LocalizationServerState state, TranslationRegistry translations)
	{
		_defs = defs;
		_state = state;
		_translations = translations;
	}

	public void Load(IReadOnlyList<PackageInstance> packages)
	{
		foreach (var package in packages)
		{
			var defs = _defs
			           .All<LocalizedStringDef>()
			           .Where(def => string.Equals(def.PackageId, package.Id, StringComparison.OrdinalIgnoreCase))
			           .ToArray();

			_state.LoadedByPackageId[package.Id] = [.. defs];

			_translations.Apply(defs);
		}
	}

	public void Unload(IReadOnlyList<PackageInstance> packages)
	{
		foreach (var package in packages.Reverse())
		{
			_state.LoadedByPackageId.Remove(package.Id);
			_translations.RemovePackage(package.Id);
		}
	}

	public void Clear()
	{
		_state.LoadedByPackageId.Clear();
	}
}