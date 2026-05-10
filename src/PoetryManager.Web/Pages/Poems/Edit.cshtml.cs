using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PoetryManager.Web.Data;
using PoetryManager.Web.Models;

namespace PoetryManager.Web.Pages.Poems;

public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Poem Poem { get; set; } = null!;

    public string TagsString { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var poem = await db.Poems
            .Include(p => p.PoemTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poem is null)
            return NotFound();

        Poem = poem;
        TagsString = string.Join(",", poem.PoemTags.Select(pt => pt.Tag.Name));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? tags)
    {
        if (!ModelState.IsValid)
            return Page();

        var existing = await db.Poems
            .Include(p => p.PoemTags)
            .FirstOrDefaultAsync(p => p.Id == Poem.Id);

        if (existing is null)
            return NotFound();

        existing.Title = Poem.Title;
        existing.Author = Poem.Author;
        existing.Dynasty = Poem.Dynasty;
        existing.Content = Poem.Content;
        existing.Type = Poem.Type;
        existing.Translation = Poem.Translation;
        existing.Notes = Poem.Notes;

        db.PoemTags.RemoveRange(existing.PoemTags);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagNames = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var name in tagNames.Distinct())
            {
                var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name)
                          ?? db.Tags.Add(new Tag { Name = name }).Entity;
                await db.SaveChangesAsync();
                db.PoemTags.Add(new PoemTag { PoemId = existing.Id, TagId = tag.Id });
            }
        }

        await db.SaveChangesAsync();
        return RedirectToPage("/Poems/Details", new { id = existing.Id });
    }
}
