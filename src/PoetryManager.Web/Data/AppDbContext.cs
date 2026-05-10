using Microsoft.EntityFrameworkCore;
using PoetryManager.Web.Models;

namespace PoetryManager.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Poem> Poems => Set<Poem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PoemTag> PoemTags => Set<PoemTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoemTag>()
            .HasKey(pt => new { pt.PoemId, pt.TagId });

        modelBuilder.Entity<PoemTag>()
            .HasOne(pt => pt.Poem)
            .WithMany(p => p.PoemTags)
            .HasForeignKey(pt => pt.PoemId);

        modelBuilder.Entity<PoemTag>()
            .HasOne(pt => pt.Tag)
            .WithMany(t => t.PoemTags)
            .HasForeignKey(pt => pt.TagId);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();
    }
}
