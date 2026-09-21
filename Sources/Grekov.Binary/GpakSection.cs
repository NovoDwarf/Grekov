namespace Grekov.Binary;

public readonly record struct GpakSection(
	GpakSectionType Type,
	GpakSectionFlags Flags,
	long Offset,
	long Length);