using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Grekov.Storages.Database;

internal sealed class DefModelCacheKeyFactory : IModelCacheKeyFactory
{
	public object Create(DbContext context, bool designTime)
	{
		var definitions = (DefDbContext)context;
		return (context.GetType(), definitions.SchemaKey, designTime);
	}
}
