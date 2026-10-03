using Microsoft.Extensions.Logging;

namespace Grekov.Readers.Xml;

public static partial class DefLoggerExtensions
{
	[LoggerMessage(LogLevel.Debug, "{Indent}{Name}: null")]
	public static partial void LogIndentNameNull(this ILogger<DefXmlReader> logger, string indent, string name);

	[LoggerMessage(LogLevel.Debug, "{Indent}{Name}: Scalar [{Value}]")]
	public static partial void LogIndentNameScalarValue(this ILogger<DefXmlReader> logger, string indent, string name, object value);

	[LoggerMessage(LogLevel.Debug, "{Indent}{Name}: Object")]
	public static partial void LogIndentNameObject(this ILogger<DefXmlReader> logger, string indent, string name);

	[LoggerMessage(LogLevel.Debug, "{Indent}{Name}: List ({Count})")]
	public static partial void LogIndentNameListCount(this ILogger<DefXmlReader> logger, string indent, string name, int count);
}