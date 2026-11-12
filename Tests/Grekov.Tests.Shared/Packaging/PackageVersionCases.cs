using Grekov.Abstractions;
using Xunit;

namespace Grekov.Tests.Shared.Packaging;

public sealed class PackageVersionCases : TheoryData<string, string, PackageDependencyOperator, bool>
{
    public PackageVersionCases()
    {
        Add("1.0", "2.0", PackageDependencyOperator.Exact, false);
        Add("2.0", "2.0.0", PackageDependencyOperator.Exact, true);
        Add("2.1", "2.0", PackageDependencyOperator.AtLeast, true);
        Add("2.0-rc.1", "2.0", PackageDependencyOperator.AtLeast, false);
        Add("2.0-rc.10", "2.0-rc.2", PackageDependencyOperator.AtLeast, true);
        Add("invalid", "2.0", PackageDependencyOperator.AtLeast, false);
    }
}
