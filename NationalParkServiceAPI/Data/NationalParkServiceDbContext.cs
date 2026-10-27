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
    public DbSet<Campground> Campgrounds => Set<Campground>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Site> Sites => Set<Site>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PassType>().ToTable("PassType");
        modelBuilder.Entity<Pass>().ToTable("Pass");
        modelBuilder.Entity<Park>().ToTable("Park");
        modelBuilder.Entity<Park>().Property(p => p.Rating).HasPrecision(2, 1);
        modelBuilder.Entity<Campground>().ToTable("Campground");
        modelBuilder.Entity<Campground>().Property(c => c.SeasonOpens).HasColumnType("char(5)");
        modelBuilder.Entity<Campground>().Property(c => c.SeasonCloses).HasColumnType("char(5)");
        modelBuilder.Entity<Reservation>().ToTable("Reservation");
        modelBuilder.Entity<User>().ToTable("User");
        modelBuilder.Entity<Site>().ToTable("Site");
        modelBuilder.Entity<Site>().HasIndex(s => new { s.CampgroundId, s.SiteNumber }).IsUnique();

        modelBuilder.Entity<Site>()
            .HasOne<Campground>()
            .WithMany()
            .HasForeignKey(s => s.CampgroundId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne<Site>()
            .WithMany()
            .HasForeignKey(r => r.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Reservation>()
            .HasOne<Campground>()
            .WithMany()
            .HasForeignKey(r => r.CampgroundId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Park>()
            .HasMany(p => p.Campgrounds)
            .WithOne()
            .HasForeignKey(c => c.ParkId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pass>()
            .HasOne(p => p.PassType)
            .WithMany()
            .HasForeignKey(p => p.PassTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
