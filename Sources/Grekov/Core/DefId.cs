namespace Grekov.Core;

public readonly record struct DefId : IParsable<DefId>
{
	public string Value { get; }

	private DefId(string value)
	{
		Value = value;
	}

	public bool IsEmpty => string.IsNullOrEmpty(Value);

	public override string ToString()
	{
		return Value ?? string.Empty;
	}

	public static DefId Parse(string raw)
	{
		return !TryParse(raw, out var id)
			? throw new FormatException($"Definition ID [{raw}] is invalid. Allowed characters: a-z, 0-9, '/', '_', '-'.")
			: id;
	}

	public static bool TryParse(string? raw, out DefId id)
	{
		var value = raw?.Trim() ?? string.Empty;

		if (value.Length == 0 || value.Any(c => !IsValidChar(c)))
		{
			id = default;
			return false;
		}

		id = new DefId(value);
		
		return true;
	}

	public static DefId Parse(string s, IFormatProvider? provider)
	{
		return Parse(s);
	}

	public static bool TryParse(string? s, IFormatProvider? provider, out DefId result)
	{
		return TryParse(s, out result);
	}

	private static bool IsValidChar(char c)
	{
		return c is
			>= 'a' and <= 'z' or
			>= '0' and <= '9' or
			'/' or '_' or '-';
	}
}