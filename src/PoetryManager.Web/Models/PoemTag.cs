namespace PoetryManager.Web.Models;

public class PoemTag
{
    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
