using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services.Discovery;

namespace Grekov.Packaging.Services.Runtime;

internal sealed class PackageService : IPackageService
{
	private readonly PackageServiceState _state;
	private readonly PackageDiscoveryWorkflow _packageDiscovery;
	private readonly PackageLoadOrderBuilder _loadOrderBuilder;
	private readonly PackageWorkflow _packageWorkflow;
	private readonly PackageSnapshotStore _packageSnapshots;

	public PackageService(
		PackageServiceState state,
		PackageDiscoveryWorkflow packageDiscovery,
		PackageLoadOrderBuilder loadOrderBuilder,
		PackageWorkflow packageWorkflow,
		PackageSnapshotStore packageSnapshots)
	{
		_state = state;
		_packageDiscovery = packageDiscovery;
		_loadOrderBuilder = loadOrderBuilder;
		_packageWorkflow = packageWorkflow;
		_packageSnapshots = packageSnapshots;
	}

	public IReadOnlyList<PackageInstance> Packages => _state.Packages;
	public IReadOnlyList<PackageInstance> LoadOrder => _state.LoadOrder;

	public void LoadAll()
	{
		var discovery = _packageDiscovery.Discover();
		var loadOrder = _loadOrderBuilder.BuildLoadOrder(discovery);

		_packageWorkflow.Apply(loadOrder);

		_state.Packages.Clear();
		_state.Packages.AddRange(discovery.Packages);
		_state.PackagesById.Clear();

		foreach (var package in discovery.Packages)
			_state.PackagesById[package.Id] = package;

		_state.LoadOrder = loadOrder;
		_packageSnapshots.Capture(_state.Packages, _state.LoadOrder);
	}
}
