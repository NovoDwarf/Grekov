namespace Grekov.Abstractions.Interfaces.Assemblies;

public interface IEntrypoint
{
	public void OnLoad(IPackageContext context);

	public void OnLoaded();

	public void OnUnload();
}