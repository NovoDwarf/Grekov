using Grekov.Packaging.Entities;
using Grekov.Packaging.Extensions;
using Grekov.Packaging.Services.Dependencies;
using Microsoft.Extensions.Logging;

namespace Grekov.Packaging.Services.Runtime;

internal sealed class PackageLoadOrderBuilder
{
	private readonly ILogger<PackageLoadOrderBuilder> _logger;

	public PackageLoadOrderBuilder(ILogger<PackageLoadOrderBuilder> logger)
	{
		_logger = logger;
	}

	public IReadOnlyList<PackageInstance> BuildLoadOrder(PackageDiscoveryResult discovery)
	{
		var candidates = discovery.Packages
			.Where(package => package.Enabled && !package.Issues.Any(static issue => issue.BlocksLoading))
			.ToList();

		var ordered = PackageGraph.TopologicalSort(
			candidates,
			id => discovery.PackagesById.GetValueOrDefault(id),
			(p, issue) => p.Issues.Add(issue));

		_logger.PackageLoadOrderBuilt(candidates.Count, ordered.Count);
		
		return ordered;
	}
}
