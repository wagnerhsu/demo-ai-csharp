# Changelog

## [1.0.0] - 2026-04-30

### Added

- Initial release of MarkdownViewer (.NET 10.0 ASP.NET Core Razor Pages)
- GitHub Flavored Markdown rendering via Markdig with auto-anchored headings
- Syntax highlighting for code blocks using highlight.js
- Sticky document outline (TOC) sidebar with H1–H4 headings; active heading tracked via IntersectionObserver
- WYSIWYG markdown editor powered by EasyMDE with split-pane live preview
- Create, edit, and delete `.md` files through the web UI
- Configurable markdown file root directory via `appsettings.json` (`MarkdownRoot`)
- Cookie-based authentication with ASP.NET Core Identity and SQLite storage
- Three roles: `Admin`, `Editor`, `Viewer` with fine-grained page-level authorization
- Admin user management: create users, edit profile/password/role, lock/unlock accounts
- Automatic database migration and seed data on startup (roles, default admin, sample file)
- Path-traversal protection in `FileService` — all file operations constrained to `MarkdownRoot`
- Responsive two-column view layout (TOC collapses on narrow screens)
