namespace Grekov.Binary;

public sealed class GpakSectionIndex
{
	private readonly Dictionary<string, GpakEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

	public GpakSectionIndex(IEnumerable<GpakEntry> entries, long dataOffset)
	{
		ArgumentNullException.ThrowIfNull(entries);

		foreach (var entry in entries)
			_entries.Add(entry.Path, entry);

		DataOffset = dataOffset;
	}

	public long DataOffset { get; }

	public IReadOnlyCollection<GpakEntry> Entries => _entries.Values;

	public bool TryGet(string path, out GpakEntry entry)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return _entries.TryGetValue(path, out entry);
	}
}