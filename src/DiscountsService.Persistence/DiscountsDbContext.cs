using System.ComponentModel.DataAnnotations;
using DiscountsService.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace DiscountsService.Persistence;

public class DiscountCode
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(maximumLength: 8, MinimumLength = 7)]
    public string Code { get; set; } = string.Empty;
    
    public bool Used { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}

public class DiscountsDbContext : DbContext
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _connectionString;
    
    public DbSet<DiscountCode> DiscountCodes { get; set; }
    
    public DiscountsDbContext(ILoggerFactory loggerFactory, IOptions<DatabaseConfiguration> opts)
    {
        _loggerFactory = loggerFactory;
        _connectionString = opts.Value.ConnectionString;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLoggerFactory(_loggerFactory)
            .EnableSensitiveDataLogging();
        
        optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString), mysql =>
        {
            // because MySQL does not support schemas: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1100
            mysql.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, table) => $"{schema}.{table}");
        });
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Configure(modelBuilder.Entity<DiscountCode>());
    }
    
    private static void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(8)
            .HasAnnotation("MinLength", 7);

        builder.HasIndex(b => b.Code).IsUnique();
        
        builder.ToTable(t => t.HasCheckConstraint("CK_Code_Length", "LENGTH(Code) >= 7 AND LENGTH(Code) <= 8"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Code_Alphanumeric", "Code REGEXP '^[A-Za-z0-9]+$'"));
    }
}
