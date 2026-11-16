using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Abstractions;

/// <summary>
/// Describes the annotated member and the catalog available during loading.
/// </summary>
public sealed class DefValidationContext
{
	public DefValidationContext(Def definition, object instance, Type valueType, string memberPath, IDefCatalog catalog, CancellationToken cancellationToken = default)
	{
		Definition = definition;
		Instance = instance;
		ValueType = valueType;
		MemberPath = memberPath;
		Catalog = catalog;
		CancellationToken = cancellationToken;
	}
	
	public Def Definition { get; }
	public object Instance { get; }
	public Type ValueType { get; }
	public string MemberPath { get; }
	public IDefCatalog Catalog { get; }
	public CancellationToken CancellationToken { get; }
}