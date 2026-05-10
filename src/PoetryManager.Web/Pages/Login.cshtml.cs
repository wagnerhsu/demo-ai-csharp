using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PoetryManager.Web.Pages;

public class LoginModel(IConfiguration config) : PageModel
{
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string username, string password, string? returnUrl)
    {
        var expectedUser = config["Auth:Username"] ?? "admin";
        var expectedPass = config["Auth:Password"] ?? "admin123";

        if (username != expectedUser || password != expectedPass)
        {
            ErrorMessage = "用户名或密码错误";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}
