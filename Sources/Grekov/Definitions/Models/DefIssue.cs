namespace Grekov.Definitions.Models;

public sealed record DefIssue(string PackageId, string ResourcePath, string Code, string Message, int? Line = null, int? Column = null, Exception? Exception = null)
{
	public override string ToString()
	{
		var location = Line.HasValue
			? $"Line {Line}, position {Column ?? 0}. " 
			: string.Empty;

		return $"[{Code}] " + $"Package: [{PackageId}], " + $"Resource: [{ResourcePath}]. " + location + Message;
	}
}