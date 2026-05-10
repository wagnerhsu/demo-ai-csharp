using System.ComponentModel.DataAnnotations;

namespace PoetryManager.Web.Models;

public class Poem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "标题不能为空")]
    [MaxLength(200)]
    [Display(Name = "标题")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "作者不能为空")]
    [MaxLength(100)]
    [Display(Name = "作者")]
    public string Author { get; set; } = "";

    [Required(ErrorMessage = "朝代不能为空")]
    [MaxLength(50)]
    [Display(Name = "朝代")]
    public string Dynasty { get; set; } = "";

    [Required(ErrorMessage = "正文不能为空")]
    [Display(Name = "正文")]
    public string Content { get; set; } = "";

    [Display(Name = "类型")]
    public PoemType Type { get; set; }

    [Display(Name = "译文")]
    public string? Translation { get; set; }

    [Display(Name = "注释")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PoemTag> PoemTags { get; set; } = [];
}

public enum PoemType
{
    [Display(Name = "诗")]
    Shi = 0,
    [Display(Name = "词")]
    Ci = 1,
    [Display(Name = "曲")]
    Qu = 2
}
