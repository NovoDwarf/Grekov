using Grekov.Builders;

namespace Grekov.Storages.InMemory;

public static class GrekovStorageBuilderExtensions
{
	public static GrekovStorageBuilder InMemory(this GrekovStorageBuilder builder)
	{
		return builder.Use<InMemoryDefStorage>();
	}
}
