# MarkdownViewer

A .NET 10.0 ASP.NET Core web application for reading and editing local Markdown files with GitHub-style rendering, document outline, WYSIWYG editing, and role-based access control.

## Requirements

- .NET 10.0 SDK

## Quick Start

```bash
cd MarkdownViewer
dotnet run
```

Open [http://localhost:5000](http://localhost:5000) and log in with the default admin account:

| Username | Password |
|---|---|
| `admin` | `Admin@123` |

The database and sample file are created automatically on first run.

## Configuration

Edit `appsettings.json` to change the markdown files directory or database path:

```json
{
  "MarkdownRoot": "./markdown-files",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

`MarkdownRoot` can be an absolute path or a path relative to the project directory.

## Features

### Markdown Rendering
- GitHub Flavored Markdown (GFM) via [Markdig](https://github.com/xoofx/markdig)
- Syntax highlighting for code blocks (highlight.js)
- GitHub-style CSS (`github-markdown-css`)

### Document Outline
- Auto-generated TOC from headings H1–H4
- Sticky sidebar, scrolls with the page
- Active heading highlighted as you scroll

### WYSIWYG Editor (EasyMDE)
- Split-pane live preview
- Toolbar: bold, italic, headings, lists, links, tables, images
- Create new files, edit existing files, delete files

### Authentication
- Cookie-based login/logout (ASP.NET Core Identity)
- Accounts stored in SQLite

### User & Permission Management

Three built-in roles:

| Role | View | Edit/Create/Delete | Manage Users |
|---|---|---|---|
| Admin | ✓ | ✓ | ✓ |
| Editor | ✓ | ✓ | — |
| Viewer | ✓ | — | — |

Admin users can create, edit, and lock/unlock accounts at `/Admin/Users`.

## Project Structure

```
MarkdownViewer/
  Data/                  # EF Core DbContext
  Models/                # AppUser (IdentityUser + DisplayName)
  Services/
    MarkdownService.cs   # Markdown → HTML, TOC extraction
    FileService.cs       # File I/O with path-traversal protection
  Pages/
    Index                # File browser
    View                 # GitHub-style render + TOC sidebar
    Edit                 # EasyMDE WYSIWYG editor
    Account/             # Login, Logout, AccessDenied
    Admin/Users/         # User management (Admin only)
  wwwroot/               # Static CSS and JS
  markdown-files/        # Default markdown file storage
  Migrations/            # EF Core database migrations
```

## Notes

- Only `.md` files in `MarkdownRoot` (and subdirectories) are listed and served.
- Path traversal is blocked — all file operations are constrained to `MarkdownRoot`.
- Passwords require at least 6 characters, one digit.
- The default admin user is seeded only if no account with `admin@example.com` exists.
