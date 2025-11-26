using CreditCardsSystem.Domain.Entities;
using CreditCardsSystem.Domain.Entities.Admin;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Data;

public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cache>(option =>
        {

            option.Property(p => p.Id).HasMaxLength(449);
        });
    }

    public DbSet<Cache> Cache => Set<Cache>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<CardType> CardTypes { get; set; } = null!;
}
