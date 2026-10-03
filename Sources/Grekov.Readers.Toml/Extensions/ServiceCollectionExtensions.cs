using Grekov.Builders;

namespace Grekov.Readers.Toml.Extensions;

public static class GrekovExtensions
{
	public static GrekovReaderBuilder Toml(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefTomlReader>();
	}
}