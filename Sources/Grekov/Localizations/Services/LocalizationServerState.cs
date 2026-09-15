using Grekov.Localizations.Entities;

namespace Grekov.Localizations.Services;

internal sealed class LocalizationServerState
{
	public Dictionary<string, List<LocalizedStringDef>> LoadedByPackageId { get; } = new(StringComparer.OrdinalIgnoreCase);
}
