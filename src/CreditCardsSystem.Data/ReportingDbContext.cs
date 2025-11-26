using CreditCardsSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Data;

public partial class ReportingDbContext : DbContext
{
    public ReportingDbContext()
    {
    }

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ReportDetail> ReportDetails { get; set; }

    public virtual DbSet<ReportHeader> ReportHeaders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportDetail>(entity =>
        {
            entity.HasOne(d => d.ReportHeader).WithMany(p => p.ReportDetails).HasConstraintName("FK_ReportDetails_ReportHeader");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
