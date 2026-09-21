namespace Grekov.Binary;

public readonly record struct GpakEntry(
	string Path,
	long Offset,
	long Length);