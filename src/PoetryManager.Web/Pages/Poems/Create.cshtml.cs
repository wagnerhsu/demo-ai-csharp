using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PoetryManager.Web.Data;
using PoetryManager.Web.Models;

namespace PoetryManager.Web.Pages.Poems;

public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Poem Poem { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? tags)
    {
        if (!ModelState.IsValid)
            return Page();

        Poem.CreatedAt = DateTime.UtcNow;
        db.Poems.Add(Poem);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(tags))
            await AttachTagsAsync(Poem.Id, tags);

        return RedirectToPage("/Index");
    }

    private async Task AttachTagsAsync(int poemId, string tagsInput)
    {
        var tagNames = tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in tagNames.Distinct())
        {
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name)
                      ?? db.Tags.Add(new Tag { Name = name }).Entity;
            await db.SaveChangesAsync();
            db.PoemTags.Add(new PoemTag { PoemId = poemId, TagId = tag.Id });
        }
        await db.SaveChangesAsync();
    }
}
