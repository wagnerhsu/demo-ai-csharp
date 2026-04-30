using MarkdownViewer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarkdownViewer.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class UsersIndexModel : PageModel
{
    private readonly UserManager<AppUser> _users;

    public UsersIndexModel(UserManager<AppUser> users) => _users = users;

    public record UserRow(string Id, string UserName, string DisplayName, string Email, string Role, bool IsLockedOut);

    public List<UserRow> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var allUsers = await _users.Users.ToListAsync();
        foreach (var u in allUsers)
        {
            var roles = await _users.GetRolesAsync(u);
            Users.Add(new UserRow(
                u.Id,
                u.UserName ?? "",
                u.DisplayName,
                u.Email ?? "",
                roles.FirstOrDefault() ?? "—",
                await _users.IsLockedOutAsync(u)
            ));
        }
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await _users.IsLockedOutAsync(user))
            await _users.SetLockoutEndDateAsync(user, null);
        else
            await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

        TempData["Message"] = $"User '{user.UserName}' status updated.";
        return RedirectToPage();
    }
}
