using MarkdownViewer.Data;
using MarkdownViewer.Models;
using MarkdownViewer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddSingleton<FileService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

await SeedAsync(app);

app.Run();

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Editor", "Viewer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    const string adminEmail = "admin@example.com";
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new AppUser
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    var files = scope.ServiceProvider.GetRequiredService<FileService>();
    if (!files.Exists("welcome.md"))
    {
        files.Write("welcome.md", """
# Welcome to Markdown Viewer

This is a sample markdown file rendered in **GitHub style**.

## Features

- GitHub-flavored markdown rendering
- Document outline (table of contents)
- WYSIWYG editor with live preview
- Role-based access control

## Getting Started

1. Login with `admin / Admin@123`
2. Browse files using the sidebar
3. Click **Edit** to open the WYSIWYG editor

## Code Example

```csharp
var greeting = "Hello, World!";
Console.WriteLine(greeting);
```

## Table Example

| Role    | View | Edit | Admin |
|---------|------|------|-------|
| Admin   | ✓    | ✓    | ✓     |
| Editor  | ✓    | ✓    | —     |
| Viewer  | ✓    | —    | —     |

> Tip: Use the outline panel on the right to navigate headings.
""");
    }
}
