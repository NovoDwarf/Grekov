
namespace Grekov.Abstractions;

public static class DefIssues
{
    public static DefIssue UnknownType(string packageId, string resourcePath, string typeName, int? line = null, int? column = null)
    {
        return Error("definition.type.unknown", $"Unknown definition type [{typeName}].", packageId, resourcePath, line, column);
    }

    public static DefIssue MissingId(string packageId, string resourcePath, int? line = null, int? column = null)
    {
        return Error("definition.id.missing", "Definition does not contain required [Id].", packageId, resourcePath, line, column);
    }

    public static DefIssue InvalidId(string packageId, string resourcePath, string value, int? line = null, int? column = null, Exception? exception = null)
    {
        return Error("definition.id.invalid", $"Invalid definition Id [{value}].", packageId, resourcePath, line, column, exception);
    }

    public static DefIssue InvalidParentId(string packageId, string resourcePath, string value, int? line = null, int? column = null, Exception? exception = null)
    {
        return Error("definition.parent.invalid", $"Invalid parent Id [{value}].", packageId, resourcePath, line, column, exception);
    }

    public static DefIssue DuplicateField(string packageId, string resourcePath, string fieldName, int? line = null, int? column = null)
    {
        return Error("definition.field.duplicate", $"Field [{fieldName}] is declared more than once.", packageId, resourcePath, line, column);
    }

    public static DefIssue StructuralValueConflict(string packageId, string resourcePath, string name, int? line = null, int? column = null)
    {
        return Error("definition.structural.conflict", $"Definition contains both [{name}] attribute and [{name}] element.", packageId, resourcePath, line, column);
    }

    public static DefIssue MultipleStructuralValues(string packageId, string resourcePath, string name, int? line = null, int? column = null)
    {
        return Error("definition.structural.multiple", $"Definition contains multiple [{name}] elements.", packageId, resourcePath, line, column);
    }

    public static DefIssue FileEmpty(string packageId, string resourcePath)
    {
        return Error("definition.file.empty", "Definition file is empty.", packageId, resourcePath);
    }

    public static DefIssue FileInvalid(string packageId, string resourcePath, Exception? exception = null)
    {
        return Error("definition.file.invalid", "Definition file contains invalid data.", packageId, resourcePath, exception: exception);
    }

    public static DefIssue InvalidType(string packageId, string resourcePath, string value, string expectedType, int? line = null, int? column = null, Exception? exception = null)
    {
        return Error("definition.type.invalid", $"Value [{value}] cannot be converted to type [{expectedType}].", packageId, resourcePath, line, column, exception);
    }
    
    public static DefIssue MissingProperty(string packageId, string resourcePath, string propertyName, int? line = null, int? column = null)
    {
        return Error("definition.property.missing", $"Definition does not contain required [{propertyName}].", packageId, resourcePath, line, column);
    }

    public static DefIssue InvalidPropertyType(string packageId, string resourcePath, string propertyName, string value, string expectedType, int? line = null, int? column = null, Exception? exception = null)
    {
        return Error("definition.property.type.invalid", $"Property [{propertyName}] with value [{value}] must be of type [{expectedType}].", packageId, resourcePath, line, column, exception);
    }
    
    public static DefIssue ValidationFailed(string packageId, string resourcePath, DefId definitionId, string memberPath, string message)
    {
        var location = string.IsNullOrEmpty(memberPath) ? "$" : memberPath;
        return Error("definition.validation.failed", $"Definition [{definitionId}], field [{location}]: {message}", packageId, resourcePath);
    }

    private static DefIssue Error(string code, string message, string packageId, string resourcePath, int? line = null, int? column = null, Exception? exception = null)
    {
        return new DefIssue(packageId, resourcePath, code, message, line, column, exception);
    }
}
