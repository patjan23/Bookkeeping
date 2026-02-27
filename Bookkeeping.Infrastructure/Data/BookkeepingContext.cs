using Bookkeeping.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Data;

public class BookkeepingContext : DbContext
{
    public BookkeepingContext(DbContextOptions<BookkeepingContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
            e.Property(a => a.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Color).HasMaxLength(20);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Description).HasMaxLength(200);
            e.HasOne(t => t.Category)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Account)
             .WithMany(a => a.Transactions)
             .HasForeignKey(t => t.AccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
