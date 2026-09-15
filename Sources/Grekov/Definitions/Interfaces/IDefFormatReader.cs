using Grekov.Core;
using Grekov.Definitions.Models;

namespace Grekov.Definitions.Interfaces;

public interface IDefFormatReader
{
	public IReadOnlySet<string> Extensions { get; }
	
	public IReadOnlyList<DefRaw> ReadDefs(DefReadContext context);
}
