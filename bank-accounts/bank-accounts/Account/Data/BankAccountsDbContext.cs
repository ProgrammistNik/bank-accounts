using Microsoft.EntityFrameworkCore;

namespace bank_accounts.Account.Data;

public class BankAccountsDbContext(DbContextOptions<BankAccountsDbContext> options) : DbContext(options)
{
    public DbSet<Models.Account> Accounts { get; set; }
    public DbSet<Models.Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Account>()
            .HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Models.Account>()
            .HasIndex(a => a.OwnerId)
            .HasMethod("hash");
        
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Models.Account>()
                .Property(u => u.Xmin)
                .HasColumnName("xmin")
                .IsRowVersion()
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }

        modelBuilder.Entity<Models.Transaction>()
            .HasIndex(t => new { t.AccountId, t.CommitedAt });

        modelBuilder.Entity<Models.Transaction>()
            .HasIndex(t => t.CommitedAt)
            .HasMethod("btree");
    }
}