using Microsoft.Extensions.Logging;

namespace Grekov.Packaging.Extensions;

internal static partial class PackageLogMessages
{
	[LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Package load order built: {CandidateCount} candidates, {LoadedCount} loadable packages.")]
	public static partial void PackageLoadOrderBuilt(this ILogger logger, int candidateCount, int loadedCount);

	[LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Package loader {Loader} started for {PackageId} at stage {Stage}.")]
	public static partial void PackageLoaderStarted(this ILogger logger, string loader, string packageId, string stage);

	[LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Package loader {Loader} completed for {PackageId} at stage {Stage} in {ElapsedMs} ms.")]
	public static partial void PackageLoaderCompleted(this ILogger logger, string loader, string packageId, string stage, long elapsedMs);

	[LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Package loader {Loader} unloaded {PackageId} at stage {Stage}.")]
	public static partial void PackageLoaderUnloaded(this ILogger logger, string loader, string packageId, string stage);

	[LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Package reload applying: {PreviousCount} previous packages, {NextCount} next packages.")]
	public static partial void PackageReloadApplyStarted(this ILogger logger, int previousCount, int nextCount);

	[LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Package reload committed in {ElapsedMs} ms.")]
	public static partial void PackageReloadCommitted(this ILogger logger, long elapsedMs);

	[LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "Package reload transaction failed.")]
	public static partial void PackageReloadTransactionFailed(this ILogger logger, Exception exception);

	[LoggerMessage(EventId = 1007, Level = LogLevel.Error, Message = "Package reload rollback failed.")]
	public static partial void PackageReloadRollbackFailed(this ILogger logger, Exception exception);
}
