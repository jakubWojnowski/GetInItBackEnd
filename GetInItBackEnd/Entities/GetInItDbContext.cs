﻿using Microsoft.EntityFrameworkCore;

namespace GetInItBackEnd.Entities;

public class GetInItDbContext : DbContext
{
    public GetInItDbContext(DbContextOptions<GetInItDbContext> options) : base(options)
    {
    }
   // private const string ConnectionString = "Server=localhost;Database=GetInItDB;User Id=SA;Password=GetInIt1234;TrustServerCertificate=True;Encrypt=True;";
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<EmploymentType> EmploymentTypes { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<Offer> Offers { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Technology> Technologies { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Accounts)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Company>()
            .HasOne(c => c.Address)
            .WithOne(a => a.Company)
            .HasForeignKey<Company>(a => a.AddressId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Company>()
            .HasMany(c => c.Offers)
            .WithOne(o => o.Company)
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Offer>()
            .HasMany(o => o.JobApplications)
            .WithOne(j => j.Offer)
            .HasForeignKey(j => j.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Offer>()
            .HasMany(o => o.Technologies)
            .WithMany(t => t.Offers);

        modelBuilder.Entity<Offer>()
            .HasMany(o => o.EmploymentTypes)
            .WithMany(e => e.Offers)
            .UsingEntity<Dictionary<string, object>>(
                "OfferEmploymentType",
                j => j.HasOne<EmploymentType>().WithMany().HasForeignKey("EmploymentTypeId"),
                j => j.HasOne<Offer>().WithMany().HasForeignKey("OfferId"),
                j =>
                {
                    j.Property<DateTime>("CreatedAt").HasDefaultValueSql("GETDATE()");
                    j.HasKey("OfferId", "EmploymentTypeId");
                });

     
        modelBuilder.Entity<Technology>()
            .HasMany(t => t.Offers)
            .WithMany(o => o.Technologies);
        //Account Table Configuration
        modelBuilder.Entity<Account>().Property(ac => ac.Email).IsRequired();
        modelBuilder.Entity<Account>().Property(ac => ac.Role)
            .HasConversion(r => r.ToString(), 
                r => (Role)Enum.Parse(typeof(Role), r))
            .IsRequired();
     
   

        //Table Address Configuration
        modelBuilder.Entity<Address>().Property(a => a.Country).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Address>().Property(a => a.City).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Address>().Property(a => a.Street).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Address>().Property(a => a.BuildingNumber).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Address>().Property(a => a.PostalCode).IsRequired().HasMaxLength(50);
        //Table Company Configuration
        modelBuilder.Entity<Company>().Property(c => c.Name).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Company>().Property(c => c.Nip).IsRequired();
        modelBuilder.Entity<Company>().Property(c => c.Regon).IsRequired();
        //Table EmploymentType Configuration
        modelBuilder.Entity<EmploymentType>().Property(e => e.Description).IsRequired().HasMaxLength(50);
        //Table JobApplication configuration
        modelBuilder.Entity<JobApplication>().Property(j => j.Name).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<JobApplication>().Property(j => j.LastName).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<JobApplication>().Property(j => j.Message).IsRequired().HasMaxLength(200);
        //Table Offer Configuration
        modelBuilder.Entity<Offer>().Property(o => o.Name).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Offer>().Property(o => o.Description).IsRequired().HasMaxLength(50);
        //Table Payment Configuration
        modelBuilder.Entity<Payment>().Property(p => p.PaymentDate).IsRequired();
        modelBuilder.Entity<Payment>().Property(p => p.Amount).IsRequired();
        //Technology Table Configuration
        modelBuilder.Entity<Technology>().Property(t => t.Skill).IsRequired().HasMaxLength(50);
    }
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.UseSqlServer(ConnectionString);
    // }
    
}