using System.Text;
using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Readers.Json;
using Grekov.Tests.Shared.Definitions;
using Microsoft.Extensions.DependencyInjection;
using NovoDwarf.FS.Extensions;

namespace Grekov.Tests.Shared.Readers;

public sealed class JsonReaderFixture : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly DefTypeRegistry _types = new();

    public JsonReaderFixture()
    {
        var services = new ServiceCollection();
        services.AddFileSystemServices();
        _services = services.BuildServiceProvider();
        _types.Refresh([typeof(JsonSampleDef).Assembly]);
    }

    public (IReadOnlyList<DefRaw> Definitions, IReadOnlyList<DefIssue> Issues) Read(string json)
    {
        // Reader diagnostics belong to this read, not to the shared fixture.
        var issues = new DefIssueRegistry();
        var reader = ActivatorUtilities.CreateInstance<DefJsonReader>(_services, _types, issues);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var definitions = reader.ReadDefinitions(stream, new DefReadContext("test", "Sample.json", "sample", []));
        return (definitions, issues.GetIssues("test"));
    }

    public void Dispose()
    {
        _services.Dispose();
    }
}
