namespace Grekov.Core.Interfaces;

public abstract class ModEntrypoint : IEntrypoint
{
	public virtual void OnLoad(IPackageContext context)
	{
	}

	public virtual void OnLoaded()
	{
	}

	public virtual void OnUnload()
	{
	}
}