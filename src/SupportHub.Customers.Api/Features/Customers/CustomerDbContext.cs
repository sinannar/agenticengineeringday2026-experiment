using Microsoft.EntityFrameworkCore;

namespace SupportHub.Customers.Api.Features.Customers;

public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();

        customer.HasKey(x => x.Id);
        customer.Property(x => x.Name).HasMaxLength(CustomerValidation.NameMaximumLength).IsRequired();
        customer.Property(x => x.Email).HasMaxLength(CustomerValidation.EmailMaximumLength).IsRequired();
        customer.Property(x => x.PhoneNumber).HasMaxLength(CustomerValidation.PhoneMaximumLength);
        customer.Property(x => x.SupportTier).HasConversion<string>().HasMaxLength(32).IsRequired();
        customer.HasIndex(x => x.Email);
    }
}
