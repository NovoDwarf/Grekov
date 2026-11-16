using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;

namespace Grekov.Tests.Shared.Definitions;

[DefType("Sample")]
public sealed class JsonSampleDef : Def
{
    [DefField]
    public int Amount { get; set; }
}
