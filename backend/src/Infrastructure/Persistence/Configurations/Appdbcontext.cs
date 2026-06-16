using ChristianJewelry.Domain.Entities;
using ChristianJewelry.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChristianJewelry.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Tables 
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Symbol> Symbols => Set<Symbol>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductMaterial> ProductMaterials => Set<ProductMaterial>();
    public DbSet<ProductSymbol> ProductSymbols => Set<ProductSymbol>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CustomRequest> CustomRequests => Set<CustomRequest>();
    public DbSet<CustomRequestFile> CustomRequestFiles => Set<CustomRequestFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Category 
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── Material 
        modelBuilder.Entity<Material>(e =>
        {
            e.ToTable("materials");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── Symbol 
        modelBuilder.Entity<Symbol>(e =>
        {
            e.ToTable("symbols");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── Product 
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Price).HasColumnType("numeric(10,2)");
            e.Property(x => x.PriceOld).HasColumnType("numeric(10,2)");
            e.Property(x => x.WeightGrams).HasColumnType("numeric(8,2)");
            e.Property(x => x.Sku).HasMaxLength(50);
            e.HasIndex(x => x.Sku).IsUnique();

            e.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId);
        });

        // ── ProductImage 
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_images");
            e.HasOne(x => x.Product)
             .WithMany(p => p.Images)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductVariant 
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variants");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.PriceModifier).HasColumnType("numeric(8,2)");
            e.HasOne(x => x.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductMaterial (M:M) 
        modelBuilder.Entity<ProductMaterial>(e =>
        {
            e.ToTable("product_materials");
            e.HasKey(x => new { x.ProductId, x.MaterialId });
            e.HasOne(x => x.Product)
             .WithMany(p => p.ProductMaterials)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Material)
             .WithMany(m => m.ProductMaterials)
             .HasForeignKey(x => x.MaterialId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductSymbol (M:M) 
        modelBuilder.Entity<ProductSymbol>(e =>
        {
            e.ToTable("product_symbols");
            e.HasKey(x => new { x.ProductId, x.SymbolId });
            e.HasOne(x => x.Product)
             .WithMany(p => p.ProductSymbols)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Symbol)
             .WithMany(s => s.ProductSymbols)
             .HasForeignKey(x => x.SymbolId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── User 
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Role)
             .HasConversion<string>()
             .HasMaxLength(20);
        });

        // ── UserAddress 
        modelBuilder.Entity<UserAddress>(e =>
        {
            e.ToTable("user_addresses");
            e.Property(x => x.Label).HasMaxLength(50);
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.Street).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.User)
             .WithMany(u => u.Addresses)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Order 
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.DeliveryMethod).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhone).HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(255);
            e.Property(x => x.Subtotal).HasColumnType("numeric(10,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("numeric(10,2)");
            e.Property(x => x.ShippingCost).HasColumnType("numeric(10,2)");
            e.Property(x => x.TotalAmount).HasColumnType("numeric(10,2)");

            e.HasOne(x => x.User)
             .WithMany(u => u.Orders)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.PromoCode)
             .WithMany(p => p.Orders)
             .HasForeignKey(x => x.PromoCodeId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── OrderItem 
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(50);
            e.Property(x => x.UnitPrice).HasColumnType("numeric(10,2)");
            e.Property(x => x.Subtotal).HasColumnType("numeric(10,2)");
            e.HasOne(x => x.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PromoCode 
        modelBuilder.Entity<PromoCode>(e =>
        {
            e.ToTable("promo_codes");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DiscountValue).HasColumnType("numeric(8,2)");
            e.Property(x => x.MinOrderAmount).HasColumnType("numeric(10,2)");
        });

        // ── CartItem 
        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_items");
            e.Property(x => x.SessionId).HasMaxLength(100);
            e.HasOne(x => x.User)
             .WithMany(u => u.CartItems)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
             .WithMany(p => p.CartItems)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Review 
        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.Property(x => x.AuthorName).HasMaxLength(100);
            e.HasOne(x => x.Product)
             .WithMany(p => p.Reviews)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CustomRequest 
        modelBuilder.Entity<CustomRequest>(e =>
        {
            e.ToTable("custom_requests");
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhone).HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(255);
            e.Property(x => x.Budget).HasColumnType("numeric(10,2)");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReferenceUrls).HasColumnType("text[]");
            e.HasOne(x => x.User)
             .WithMany(u => u.CustomRequests)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CustomRequestFile 
        modelBuilder.Entity<CustomRequestFile>(e =>
        {
            e.ToTable("custom_request_files");
            e.Property(x => x.FileType).HasMaxLength(50);
            e.HasOne(x => x.Request)
             .WithMany(r => r.Files)
             .HasForeignKey(x => x.RequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Auto-update UpdatedAt
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}