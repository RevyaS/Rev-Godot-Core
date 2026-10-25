using System.Diagnostics.CodeAnalysis;
using Godot;
using FileAccess = Godot.FileAccess;
using MessagePack;

/// <summary>
/// Saves and loads MessagePack objects through Godot's FileAccess, so any path Godot understands works
/// (res://, user:// or an absolute path). Each project decides what the stored objects look like.
/// </summary>
public static class MessagePackData
{
    public static void Save<T>(string path, T data, MessagePackSerializerOptions? options = null)
    {
        EnsureDirectory(path.GetBaseDir());
        byte[] bytes = MessagePackSerializer.Serialize(data, options);

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write)
            ?? throw new IOException($"Cannot write {path}: {FileAccess.GetOpenError()}");
        file.StoreBuffer(bytes);
    }

    public static T Load<T>(string path, MessagePackSerializerOptions? options = null)
    {
        if (!Exists(path)) throw new FileNotFoundException($"No data at {path}", path);

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read)
            ?? throw new IOException($"Cannot read {path}: {FileAccess.GetOpenError()}");
        byte[] bytes = file.GetBuffer((long)file.GetLength());
        return MessagePackSerializer.Deserialize<T>(bytes, options);
    }

    /// <summary>False when there is no file at the path. A file that is not a <typeparamref name="T"/> still throws.</summary>
    public static bool TryLoad<T>(string path, [MaybeNullWhen(false)] out T data, MessagePackSerializerOptions? options = null)
    {
        if (!Exists(path))
        {
            data = default;
            return false;
        }
        data = Load<T>(path, options);
        return true;
    }

    public static bool Exists(string path) => FileAccess.FileExists(path);

    public static void Delete(string path)
    {
        if (!Exists(path)) return;
        var error = DirAccess.RemoveAbsolute(path);
        if (error != Error.Ok) throw new IOException($"Cannot delete {path}: {error}");
    }

    public static void EnsureDirectory(string directory)
    {
        if (DirAccess.DirExistsAbsolute(directory)) return;
        var error = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (error != Error.Ok) throw new IOException($"Cannot create directory {directory}: {error}");
    }
}
