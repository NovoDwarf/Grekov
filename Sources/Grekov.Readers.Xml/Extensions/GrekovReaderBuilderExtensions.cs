using Grekov.Builders;
using Grekov.Readers.Xml;

namespace Grekov.Extensions;

public static class GrekovReaderBuilderExtensions
{
	public static GrekovReaderBuilder Xml(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefXmlReader>();
	}
}