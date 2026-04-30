using MarkdownViewer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class CreateUserModel : PageModel
{
    private readonly UserManager<AppUser> _users;

    public CreateUserModel(UserManager<AppUser> users) => _users = users;

    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string username, string displayName, string email, string password, string role)
    {
        var user = new AppUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            Error = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        await _users.AddToRoleAsync(user, role);
        TempData["Message"] = $"User '{username}' created.";
        return RedirectToPage("Index");
    }
}
