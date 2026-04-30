using MarkdownViewer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarkdownViewer.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signIn;

    public LoginModel(SignInManager<AppUser> signIn) => _signIn = signIn;

    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string username, string password, string? returnUrl)
    {
        var result = await _signIn.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        Error = result.IsLockedOut ? "Account locked. Try again later." : "Invalid username or password.";
        return Page();
    }
}
