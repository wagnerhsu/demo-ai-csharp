using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PoetryManager.Web.Data;
using PoetryManager.Web.Models;

namespace PoetryManager.Web.Pages.Poems;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Poem Poem { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var poem = await db.Poems
            .Include(p => p.PoemTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poem is null)
            return NotFound();

        Poem = poem;
        return Page();
    }
}
