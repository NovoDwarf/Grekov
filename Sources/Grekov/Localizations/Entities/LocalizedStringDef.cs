using Grekov.Core;
using Grekov.Core.Attributes;

namespace Grekov.Localizations.Entities;

[DefType("LocalizedString")]
public sealed class LocalizedStringDef : Def
{
	[DefField(Required = true)]
	public string Locale { get; set; } = string.Empty;

	[DefField(Required = true)]
	public string Key { get; set; } = string.Empty;

	[DefField(Required = true)]
	public string Value { get; set; } = string.Empty;
}
