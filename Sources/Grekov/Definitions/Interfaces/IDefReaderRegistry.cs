using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Registry;

public interface IDefReaderRegistry
{
	public IReadOnlyCollection<string> Extensions { get; }

	public bool TryResolve(string path, out IDefFormatReader? reader);
}