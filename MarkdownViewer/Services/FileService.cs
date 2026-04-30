namespace MarkdownViewer.Services;

public record FileNode(string Name, string RelativePath, bool IsDirectory, IReadOnlyList<FileNode> Children);

public class FileService
{
    private readonly string _root;

    public FileService(IConfiguration config)
    {
        var raw = config["MarkdownRoot"] ?? "./markdown-files";
        _root = Path.GetFullPath(raw);
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    public IReadOnlyList<FileNode> GetTree() => BuildTree(_root, _root);

    private static IReadOnlyList<FileNode> BuildTree(string dir, string root)
    {
        var nodes = new List<FileNode>();

        foreach (var d in Directory.GetDirectories(dir).OrderBy(x => x))
        {
            var rel = Path.GetRelativePath(root, d).Replace('\\', '/');
            nodes.Add(new FileNode(Path.GetFileName(d), rel, true, BuildTree(d, root)));
        }

        foreach (var f in Directory.GetFiles(dir, "*.md").OrderBy(x => x))
        {
            var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            nodes.Add(new FileNode(Path.GetFileName(f), rel, false, Array.Empty<FileNode>()));
        }

        return nodes;
    }

    public string Read(string relativePath)
    {
        var full = Resolve(relativePath);
        return File.ReadAllText(full);
    }

    public void Write(string relativePath, string content)
    {
        var full = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    public void Delete(string relativePath)
    {
        var full = Resolve(relativePath);
        File.Delete(full);
    }

    public bool Exists(string relativePath)
    {
        try { return File.Exists(Resolve(relativePath)); }
        catch { return false; }
    }

    private string Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar) && full != _root)
            throw new InvalidOperationException("Path traversal detected.");
        return full;
    }
}
