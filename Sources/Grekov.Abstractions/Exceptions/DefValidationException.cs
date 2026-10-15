namespace Grekov.Abstractions.Exceptions;

/// <summary>Reports definition validation failures detected before definitions are stored.</summary>
public sealed class DefValidationException : InvalidOperationException
{
    public IReadOnlyList<DefIssue> Issues { get; }

    public DefValidationException(IEnumerable<DefIssue> issues) : this(CopyIssues(issues)) { }

    private DefValidationException(DefIssue[] issues) : base($"Definition validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, issues.Select(static issue => issue.ToString()))}")
    {
        Issues = Array.AsReadOnly(issues);
    }

    private static DefIssue[] CopyIssues(IEnumerable<DefIssue> issues)
    {
        return [.. issues];
    }
}
