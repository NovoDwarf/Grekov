using Grekov.Builders;
using Grekov.Readers.Toml;

namespace Grekov.Extensions;

public static class GrekovReaderBuilderExtensions
{
	public static GrekovReaderBuilder Toml(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefTomlReader>();
	}
}