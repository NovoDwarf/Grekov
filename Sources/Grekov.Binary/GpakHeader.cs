namespace Grekov.Binary;

public readonly record struct GpakHeader(
	uint Magic,
	ushort Version,
	ushort Flags,
	uint SectionCount,
	uint HeaderSize);