namespace Grekov.Binary;

public static class GpakFormat
{
	/// <summary>
	/// Magic constant of string "GPAK".
	/// </summary>
	public const uint Magic = 0x4B415047;

	public const ushort CurrentVersion = 1;

	public const uint HeaderSize = 16;

	public const int SectionSize = 20;
}