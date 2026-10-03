using Grekov.Builders;

namespace Grekov.Readers.Yaml.Extensions;

public static class GrekovExtensions
{
	public static GrekovReaderBuilder Yaml(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefYamlReader>();
	}
}