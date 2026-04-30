using MarkdownViewer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class EditUserModel : PageModel
{
    private readonly UserManager<AppUser> _users;

    public EditUserModel(UserManager<AppUser> users) => _users = users;

    public string UserId { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        UserId = user.Id;
        Username = user.UserName ?? "";
        DisplayName = user.DisplayName;
        Email = user.Email ?? "";
        var roles = await _users.GetRolesAsync(user);
        Role = roles.FirstOrDefault() ?? "Viewer";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id, string username, string displayName, string email, string? newPassword, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.UserName = username;
        user.DisplayName = displayName;
        user.Email = email;
        user.NormalizedEmail = email.ToUpperInvariant();

        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            Error = string.Join(" ", updateResult.Errors.Select(e => e.Description));
            UserId = id; Username = username; DisplayName = displayName; Email = email; Role = role;
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var pwResult = await _users.ResetPasswordAsync(user, token, newPassword);
            if (!pwResult.Succeeded)
            {
                Error = string.Join(" ", pwResult.Errors.Select(e => e.Description));
                UserId = id; Username = username; DisplayName = displayName; Email = email; Role = role;
                return Page();
            }
        }

        var currentRoles = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, currentRoles);
        await _users.AddToRoleAsync(user, role);

        TempData["Message"] = $"User '{username}' updated.";
        return RedirectToPage("Index");
    }
}
