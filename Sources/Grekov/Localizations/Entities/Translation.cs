namespace Grekov.Localizations.Entities;

public sealed class Translation
{
	public Translation(string locale)
	{
		Locale = locale;
	}

	public string Locale { get; }
	public Dictionary<string, string> Messages { get; } = new(StringComparer.OrdinalIgnoreCase);

	public IEnumerable<string> GetKeys()
	{
		return Messages.Keys;
	}
}
