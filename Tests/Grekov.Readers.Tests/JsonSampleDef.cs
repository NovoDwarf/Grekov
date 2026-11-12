using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;

namespace Grekov.Readers.Tests;

[DefType("Sample")]
public sealed class JsonSampleDef : Def
{
    [DefField]
    public int Amount { get; set; }
}
