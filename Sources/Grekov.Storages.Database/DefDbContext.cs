using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Grekov.Core;
using Grekov.Core.Attributes;
using Grekov.Definitions.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Grekov.Storages.Database;

internal sealed class DefDbContext : DbContext
{
	private readonly DefTypeRegistry _types;

	public DefDbContext(DbContextOptions<DefDbContext> options, DefTypeRegistry types)
		: base(options)
	{
		_types = types;
	}

	public string SchemaKey => string.Join("|", _types.Types.Select(static type => type.AssemblyQualifiedName).Order(StringComparer.Ordinal));

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		var root = modelBuilder.Entity<Def>();
		root.UseTpcMappingStrategy();
		root.HasKey(static definition => definition.Id);
		root.Property(static definition => definition.Id)
			.HasConversion(new ValueConverter<DefId, string>(static id => id.Value, static value => DefId.Parse(value)));
		root.Property(static definition => definition.PackageId).IsRequired();
		root.Property(static definition => definition.ResourcePath).IsRequired();
		root.Property(static definition => definition.Name).IsRequired();
		root.Ignore(static definition => definition.Info);
		root.Ignore(static definition => definition.Description);
		root.Ignore(static definition => definition.Icon);

		foreach (var type in _types.Types)
		{
			var entity = modelBuilder.Entity(type);
			entity.ToTable(GetTableName(type));

			foreach (var property in GetPersistedProperties(type))
				entity.Property(property.PropertyType, property.Name);
		}
	}

	private static IEnumerable<PropertyInfo> GetPersistedProperties(Type type)
	{
		foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (property.DeclaringType == typeof(Def) || property.GetCustomAttribute<DefFieldAttribute>(inherit: true) is null)
				continue;

			if (!IsSupportedScalar(property.PropertyType))
			{
				throw new InvalidOperationException(
					$"Definition field [{type.FullName}.{property.Name}] has unsupported database type [{property.PropertyType.FullName}]. " +
					"The initial database storage supports scalar fields only.");
			}

			yield return property;
		}
	}

	private static bool IsSupportedScalar(Type type)
	{
		var valueType = Nullable.GetUnderlyingType(type) ?? type;

		return valueType.IsEnum || valueType.IsPrimitive ||
			valueType == typeof(string) || valueType == typeof(decimal) || valueType == typeof(DateTime) ||
			valueType == typeof(DateTimeOffset) || valueType == typeof(TimeSpan) || valueType == typeof(Guid) ||
			valueType == typeof(DefId);
	}

	private static string GetTableName(Type type)
	{
		var source = type.FullName ?? type.Name;
		var normalized = new string(source.Select(static character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_').ToArray()).Trim('_');
		var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..8].ToLowerInvariant();

		return $"def_{normalized[..Math.Min(normalized.Length, 48)]}_{hash}";
	}
}
