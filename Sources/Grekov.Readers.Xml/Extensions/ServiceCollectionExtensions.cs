using Grekov.Builders;

namespace Grekov.Readers.Xml.Extensions;

public static class GrekovExtensions
{
	public static GrekovReaderBuilder Xml(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefXmlReader>();
	}
}