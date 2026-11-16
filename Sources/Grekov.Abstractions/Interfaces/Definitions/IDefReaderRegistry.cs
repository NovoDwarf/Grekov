namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefReaderRegistry
{
	public IReadOnlyCollection<string> Extensions { get; }

	public bool TryResolve(string path, out IDefFormatReader? reader);
}