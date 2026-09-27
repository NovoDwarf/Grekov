using Grekov.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Storages.Database;

public static class GrekovStorageBuilderExtensions
{
	public static GrekovStorageBuilder Database(this GrekovStorageBuilder builder, Action<DbContextOptionsBuilder> configure)
	{
		builder.Use<EfDefStorage>();
		
		builder.Services.AddDbContextFactory<DefDbContext>(options =>
		{
			configure(options);
			options.ReplaceService<IModelCacheKeyFactory, DefModelCacheKeyFactory>();
		});

		return builder;
	}
}
