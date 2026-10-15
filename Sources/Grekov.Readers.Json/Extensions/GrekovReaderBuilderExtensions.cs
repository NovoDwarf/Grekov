using Grekov.Builders;
using Grekov.Readers.Json;

namespace Grekov.Extensions;

public static class GrekovReaderBuilderExtensions
{
	public static GrekovReaderBuilder Json(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefJsonReader>();
	}
}