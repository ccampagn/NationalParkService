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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PassType>().ToTable("PassType");
        modelBuilder.Entity<Pass>().ToTable("Pass");

        modelBuilder.Entity<Pass>()
            .HasOne(p => p.PassType)
            .WithMany()
            .HasForeignKey(p => p.PassTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
