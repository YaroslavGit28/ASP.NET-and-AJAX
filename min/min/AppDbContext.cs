using Microsoft.EntityFrameworkCore;
using min;
using System.Text.Json;

namespace min;
{
    /// <summary>
    /// Контекст базы данных для работы с продуктами
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ProductEntity> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductEntity>(entity =>
            {
                // Первичный ключ
                entity.HasKey(e => e.Id);

                // Id — генерируется на стороне БД
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("NEWID()");

                // Name — обязательное, максимум 100 символов
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                // Description — необязательное, максимум 500 символов
                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // Category — обязательное, максимум 50 символов
                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                // PriceInCents — обязательное
                entity.Property(e => e.PriceInCents)
                    .IsRequired();

                // StockQuantity — обязательное
                entity.Property(e => e.StockQuantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                // IsActive — обязательное
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // CreatedAt — устанавливается автоматически
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // UpdatedAt — может быть null
                entity.Property(e => e.UpdatedAt)
                    .IsRequired(false);

                // DeletedAt — может быть null
                entity.Property(e => e.DeletedAt)
                    .IsRequired(false);

                // Attributes — JSON-колонка
                entity.Property(e => e.Attributes)
                    .HasColumnType("nvarchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions()) ?? new Dictionary<string, object>()
                    );

                // Индексы
                entity.HasIndex(e => e.Category)
                    .HasDatabaseName("IX_Products_Category");

                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_Products_Name");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_Products_IsActive");

                entity.HasIndex(e => new { e.Category, e.IsActive })
                    .HasDatabaseName("IX_Products_Category_IsActive");

                // Имя таблицы
                entity.ToTable("Products");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}