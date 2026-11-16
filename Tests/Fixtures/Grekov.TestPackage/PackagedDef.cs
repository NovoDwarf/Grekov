 using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;


namespace Grekov.TestPackage;

[DefType("Packaged")]
public sealed class PackagedDef : Def
{
    [DefField]
    public int Amount { get; set; }

    [DefField]
    public string Dependency { get; set; } = Grekov.TestDependency.PackageDependency.GetValue();
}
