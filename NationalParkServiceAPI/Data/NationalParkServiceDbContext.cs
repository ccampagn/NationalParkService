using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Models;


namespace NationalParkServiceAPI.Data;

public class NationalParkServiceDbContext : DbContext
{
    public NationalParkServiceDbContext(DbContextOptions<NationalParkServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<PassType> PassTypes => Set<PassType>();
    public DbSet<Pass> Passes => Set<Pass>();
    public DbSet<Park> Parks => Set<Park>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PassType>().ToTable("PassType");
        modelBuilder.Entity<Pass>().ToTable("Pass");
        modelBuilder.Entity<Park>().ToTable("Park");
        modelBuilder.Entity<Park>().Property(p => p.Rating).HasPrecision(2, 1);

        modelBuilder.Entity<Pass>()
            .HasOne(p => p.PassType)
            .WithMany()
            .HasForeignKey(p => p.PassTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
