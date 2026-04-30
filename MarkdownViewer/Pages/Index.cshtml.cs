using MarkdownViewer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages;

public class IndexModel : PageModel
{
    private readonly FileService _files;

    public IndexModel(FileService files) => _files = files;

    public IReadOnlyList<FileNode> Files { get; private set; } = [];

    public void OnGet() => Files = _files.GetTree();
}
