using Grekov.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YamlDotNet.Serialization;

namespace Grekov.Readers.Yaml.Extensions;

public static class GrekovReaderBuilderExtensions
{
    public static GrekovReaderBuilder Yaml(this GrekovReaderBuilder builder)
    {
        builder.Services.TryAddSingleton<IDeserializer>(_ => new DeserializerBuilder().Build());
        return builder.Add<DefYamlReader>();
    }
}
