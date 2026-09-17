using BioPipeline.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BioPipeline.Api.Data;

public class BioPipelineDbContext(DbContextOptions<BioPipelineDbContext> options) : DbContext(options)
{
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<Variant> Variants => Set<Variant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Run>()
            .Property(r => r.Status)
            .HasConversion<string>(); // store "Queued"/"Running"/etc, not an int, for readability

        modelBuilder.Entity<Run>()
            .Property(r => r.Kind)
            .HasConversion<string>(); // store "Toy"/"RealSarek", same reasoning

        modelBuilder.Entity<Run>()
            .HasMany(r => r.Variants)
            .WithOne(v => v.Run!)
            .HasForeignKey(v => v.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
