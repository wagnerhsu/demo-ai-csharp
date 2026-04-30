using MarkdownViewer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages;

[Authorize]
public class ViewPageModel : PageModel
{
    private readonly FileService _files;
    private readonly MarkdownService _md;

    public ViewPageModel(FileService files, MarkdownService md)
    {
        _files = files;
        _md = md;
    }

    public string Path { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string HtmlContent { get; private set; } = string.Empty;
    public List<HeadingItem> Outline { get; private set; } = [];

    public IActionResult OnGet(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !_files.Exists(path))
            return NotFound();

        Path = path;
        FileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var raw = _files.Read(path);
        HtmlContent = _md.RenderToHtml(raw);
        Outline = _md.ExtractOutline(raw);
        return Page();
    }
}
