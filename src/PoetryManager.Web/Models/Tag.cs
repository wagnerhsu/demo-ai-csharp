using System.ComponentModel.DataAnnotations;

namespace PoetryManager.Web.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = "";

    public List<PoemTag> PoemTags { get; set; } = [];
}
