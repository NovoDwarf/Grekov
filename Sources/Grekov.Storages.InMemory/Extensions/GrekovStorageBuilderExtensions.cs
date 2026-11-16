using Grekov.Builders;
using Grekov.Storages.InMemory;

namespace Grekov.Extensions;

public static class GrekovStorageBuilderExtensions
{
	public static GrekovStorageBuilder InMemory(this GrekovStorageBuilder builder)
	{
		return builder.Use<InMemoryDefStorage>();
	}
}
