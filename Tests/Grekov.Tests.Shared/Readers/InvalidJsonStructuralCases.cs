using Xunit;

namespace Grekov.Tests.Shared.Readers;

public sealed class InvalidJsonStructuralCases : TheoryData<string, string>
{
    public InvalidJsonStructuralCases()
    {
        Add("{\"$type\":12,\"Id\":\"sample\"}", "definition.property.type.invalid");
        Add("{\"$type\":{},\"Id\":\"sample\"}", "definition.property.type.invalid");
        Add("{\"$type\":\"Sample\",\"Id\":12}", "definition.id.invalid");
        Add("{\"$type\":\"Sample\",\"Id\":[]}", "definition.id.invalid");
    }
}
