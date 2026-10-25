using Godot;
using FileAccess = Godot.FileAccess;

/// <summary>A numbered save file, eg; user://Profiles/Slot1.dat. <see cref="Index"/> starts at 0, <see cref="Number"/> at 1.</summary>
public sealed record SaveSlot(int Index, string Path, DateTime? LastModified)
{
    public int Number => Index + 1;
    public bool IsOccupied => LastModified is not null;
}

/// <summary>Where a fixed amount of save slots live, and how their files are named.</summary>
public sealed record SaveSlotLayout(string Directory, int Count, string FilePrefix = "Slot", string Extension = "dat")
{
    public string GetPath(int index)
    {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), $"Slot {index} is outside 0..{Count - 1}");
        return Directory.PathJoin($"{FilePrefix}{index + 1}.{Extension}");
    }

    public SaveSlot GetSlot(int index)
    {
        string path = GetPath(index);
        DateTime? lastModified = FileAccess.FileExists(path)
            ? DateTimeOffset.FromUnixTimeSeconds((long)FileAccess.GetModifiedTime(path)).LocalDateTime
            : null;
        return new SaveSlot(index, path, lastModified);
    }

    public IReadOnlyList<SaveSlot> GetSlots() => Enumerable.Range(0, Count).Select(GetSlot).ToList();
}
