using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PoetryManager.Web.Data;
using PoetryManager.Web.Models;

namespace PoetryManager.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Poem> Poems { get; private set; } = [];
    public string? SearchTerm { get; private set; }
    public string? FilterAuthor { get; private set; }
    public string? FilterDynasty { get; private set; }
    public int? FilterType { get; private set; }
    public string? FilterTag { get; private set; }
    public int PageIndex { get; private set; }
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

    private const int PageSize = 20;

    public async Task OnGetAsync(string? q, string? author, string? dynasty, int? type, string? tag, int pageIndex = 0)
    {
        SearchTerm = q;
        FilterAuthor = author;
        FilterDynasty = dynasty;
        FilterType = type;
        FilterTag = tag;
        PageIndex = pageIndex;

        var query = db.Poems.Include(p => p.PoemTags).ThenInclude(pt => pt.Tag).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p =>
                p.Title.Contains(q) || p.Author.Contains(q) ||
                p.Dynasty.Contains(q) || p.Content.Contains(q));

        if (!string.IsNullOrWhiteSpace(author))
            query = query.Where(p => p.Author.Contains(author));

        if (!string.IsNullOrWhiteSpace(dynasty))
            query = query.Where(p => p.Dynasty.Contains(dynasty));

        if (type.HasValue)
            query = query.Where(p => (int)p.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.PoemTags.Any(pt => pt.Tag.Name == tag));

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, TotalPages - 1));

        Poems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(PageIndex * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
