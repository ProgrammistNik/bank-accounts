using bank_accounts.RabbitMQ.Models;
using Microsoft.EntityFrameworkCore;

namespace bank_accounts.Account.Data;

public class BankAccountsDbContext(DbContextOptions<BankAccountsDbContext> options) : DbContext(options)
{
    public DbSet<Models.Account> Accounts { get; set; }
    public DbSet<Models.Transaction> Transactions { get; set; }
    public DbSet<Outbox> Outboxes { get; set; }
    public DbSet<Inbox> Inboxes { get; set; }
    public DbSet<InboxDeadLetter> InboxDeadLetters { get; set; }

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
        
        modelBuilder.Entity<Outbox>(entity =>
        {
            
            entity.HasKey(o => o.Id);
        
            entity.OwnsOne(o => o.Meta, meta =>
            {
                meta.Property(m => m.Version).IsRequired();
                meta.Property(m => m.Source).IsRequired();
                meta.Property(m => m.CorrelationId).IsRequired();
                meta.Property(m => m.CausationId).IsRequired();
            });
        });
        
        modelBuilder.Entity<Inbox>(entity =>
        {
            
            entity.HasKey(o => o.Id);
            
            entity.OwnsOne(o => o.Payload, payload =>
            {
                payload.Property(p => p.OwnerId).IsRequired();
                payload.Property(p => p.Status).IsRequired().HasDefaultValue(string.Empty);
            });
        
            entity.OwnsOne(o => o.Meta, meta =>
            {
                meta.Property(m => m.Version).IsRequired();
                meta.Property(m => m.Source).IsRequired();
                meta.Property(m => m.CorrelationId).IsRequired();
                meta.Property(m => m.CausationId).IsRequired();
            });
        });
        
        modelBuilder.Entity<InboxDeadLetter>(entity =>
        {
            
            entity.HasKey(o => o.Id);
            
            entity.OwnsOne(o => o.Payload, payload =>
            {
                payload.Property(p => p.OwnerId).IsRequired();
                payload.Property(p => p.Status).IsRequired().HasDefaultValue(string.Empty);
            });
        
            entity.OwnsOne(o => o.Meta, meta =>
            {
                meta.Property(m => m.Version).IsRequired();
                meta.Property(m => m.Source).IsRequired();
                meta.Property(m => m.CorrelationId).IsRequired();
                meta.Property(m => m.CausationId).IsRequired();
            });
        });
    }
}