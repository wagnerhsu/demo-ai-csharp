using MarkdownViewer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages;

[Authorize(Roles = "Admin,Editor")]
public class EditPageModel : PageModel
{
    private readonly FileService _files;

    public EditPageModel(FileService files) => _files = files;

    public string FilePath { get; private set; } = string.Empty;
    public new string Content { get; private set; } = string.Empty;
    public bool IsNew { get; private set; }
    public string? Error { get; private set; }

    public IActionResult OnGet(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            IsNew = true;
            return Page();
        }

        if (!_files.Exists(path))
            return NotFound();

        FilePath = path;
        Content = _files.Read(path);
        IsNew = false;
        return Page();
    }

    public IActionResult OnPost(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Error = "File path is required.";
            IsNew = true;
            Content = content;
            return Page();
        }

        if (!filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            filePath += ".md";

        try
        {
            _files.Write(filePath, content ?? string.Empty);
            return RedirectToPage("/View", new { path = filePath });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            FilePath = filePath;
            Content = content ?? string.Empty;
            IsNew = !_files.Exists(filePath);
            return Page();
        }
    }

    public IActionResult OnPostDelete(string filePath)
    {
        if (_files.Exists(filePath))
            _files.Delete(filePath);
        return RedirectToPage("/Index");
    }
}
