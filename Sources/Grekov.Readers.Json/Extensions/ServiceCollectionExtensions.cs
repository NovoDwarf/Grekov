using Grekov.Builders;
using Grekov.Extensions;

namespace Grekov.Readers.Json.Extensions;

public static class GrekovExtensions
{
	public static GrekovReaderBuilder Json(this GrekovReaderBuilder builder)
	{
		return builder.Add<DefJsonReader>();
	}
}