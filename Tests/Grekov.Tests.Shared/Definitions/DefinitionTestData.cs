using Grekov.Abstractions;
using Grekov.Definitions.Materializations;

namespace Grekov.Tests.Shared.Definitions;

public static class DefinitionTestData
{
    public static T Create<T>(DefId id) where T : Def
    {
        return (T)new DefFactory().Create(typeof(T), new DefRaw
        {
            Id = id,
            Type = typeof(T),
            Fields = new Dictionary<string, DefValue>(),
            PackageId = "test",
            ResourcePath = $"{id}.json"
        });
    }
}
