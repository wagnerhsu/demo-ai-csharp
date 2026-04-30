using Microsoft.AspNetCore.Identity;

namespace MarkdownViewer.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
