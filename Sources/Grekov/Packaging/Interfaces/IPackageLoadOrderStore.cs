namespace Grekov.Packaging.Interfaces;

public interface IPackageLoadOrderStore
{
	IReadOnlyDictionary<string, bool> LoadEnabledOverrides();
}
