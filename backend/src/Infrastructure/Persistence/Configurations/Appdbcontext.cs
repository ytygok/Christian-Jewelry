using ChristianJewelry.Domain.Entities;
using ChristianJewelry.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChristianJewelry.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Tables ────────────────────────────────────────────────
    public DbSet<Category>          Categories         => Set<Category>();
    public DbSet<Material>          Materials          => Set<Material>();
    public DbSet<Symbol>            Symbols            => Set<Symbol>();
    public DbSet<Product>           Products           => Set<Product>();
    public DbSet<ProductImage>      ProductImages      => Set<ProductImage>();
    public DbSet<ProductVariant>    ProductVariants    => Set<ProductVariant>();
    public DbSet<ProductMaterial>   ProductMaterials   => Set<ProductMaterial>();
    public DbSet<ProductSymbol>     ProductSymbols     => Set<ProductSymbol>();
    public DbSet<User>              Users              => Set<User>();
    public DbSet<UserAddress>       UserAddresses      => Set<UserAddress>();
    public DbSet<Order>             Orders             => Set<Order>();
    public DbSet<OrderItem>         OrderItems         => Set<OrderItem>();
    public DbSet<PromoCode>         PromoCodes         => Set<PromoCode>();
    public DbSet<CartItem>          CartItems          => Set<CartItem>();
    public DbSet<Review>            Reviews            => Set<Review>();
    public DbSet<CustomRequest>     CustomRequests     => Set<CustomRequest>();
    public DbSet<CustomRequestFile> CustomRequestFiles => Set<CustomRequestFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Конвертори enum → lowercase string ───────────────
        var userRoleConverter = new ValueConverter<UserRole, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<UserRole>(v, true));

        var orderStatusConverter = new ValueConverter<OrderStatus, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<OrderStatus>(v, true));

        var deliveryMethodConverter = new ValueConverter<DeliveryMethod, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<DeliveryMethod>(v, true));

        var paymentMethodConverter = new ValueConverter<PaymentMethod, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<PaymentMethod>(v, true));

        var paymentStatusConverter = new ValueConverter<PaymentStatus, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<PaymentStatus>(v, true));

        var discountTypeConverter = new ValueConverter<DiscountType, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<DiscountType>(v, true));

        var customRequestStatusConverter = new ValueConverter<CustomRequestStatus, string>(
            v => v.ToString().ToLower(),
            v => Enum.Parse<CustomRequestStatus>(v, true));

        // ── Category ─────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            e.Property(x => x.IconUrl).HasColumnName("icon_url");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── Material ─────────────────────────────────────────
        modelBuilder.Entity<Material>(e =>
        {
            e.ToTable("materials");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(20);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── Symbol ───────────────────────────────────────────
        modelBuilder.Entity<Symbol>(e =>
        {
            e.ToTable("symbols");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── Product ──────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(10,2)");
            e.Property(x => x.PriceOld).HasColumnName("price_old").HasColumnType("numeric(10,2)");
            e.Property(x => x.StockQty).HasColumnName("stock_qty");
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(50);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.IsCustom).HasColumnName("is_custom");
            e.Property(x => x.IsFeatured).HasColumnName("is_featured");
            e.Property(x => x.WeightGrams).HasColumnName("weight_grams").HasColumnType("numeric(8,2)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Sku).IsUnique();

            e.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId);
        });

        // ── ProductImage ─────────────────────────────────────
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_images");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Url).HasColumnName("url");
            e.Property(x => x.AltText).HasColumnName("alt_text");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsCover).HasColumnName("is_cover");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Product)
             .WithMany(p => p.Images)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductVariant ───────────────────────────────────
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.PriceModifier).HasColumnName("price_modifier").HasColumnType("numeric(8,2)");
            e.Property(x => x.StockQty).HasColumnName("stock_qty");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductMaterial (M:M) ────────────────────────────
        modelBuilder.Entity<ProductMaterial>(e =>
        {
            e.ToTable("product_materials");
            e.HasKey(x => new { x.ProductId, x.MaterialId });
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.MaterialId).HasColumnName("material_id");
            e.HasOne(x => x.Product)
             .WithMany(p => p.ProductMaterials)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Material)
             .WithMany(m => m.ProductMaterials)
             .HasForeignKey(x => x.MaterialId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductSymbol (M:M) ──────────────────────────────
        modelBuilder.Entity<ProductSymbol>(e =>
        {
            e.ToTable("product_symbols");
            e.HasKey(x => new { x.ProductId, x.SymbolId });
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.SymbolId).HasColumnName("symbol_id");
            e.HasOne(x => x.Product)
             .WithMany(p => p.ProductSymbols)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Symbol)
             .WithMany(s => s.ProductSymbols)
             .HasForeignKey(x => x.SymbolId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── User ─────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.IsVerified).HasColumnName("is_verified");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Role)
             .HasColumnName("role")
             .HasConversion(userRoleConverter)
             .HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ── UserAddress ──────────────────────────────────────
        modelBuilder.Entity<UserAddress>(e =>
        {
            e.ToTable("user_addresses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Label).HasColumnName("label").HasMaxLength(50);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            e.Property(x => x.Street).HasColumnName("street").HasMaxLength(200).IsRequired();
            e.Property(x => x.ZipCode).HasColumnName("zip_code");
            e.Property(x => x.NovaPoshtaRef).HasColumnName("nova_poshta_ref");
            e.Property(x => x.IsDefault).HasColumnName("is_default");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User)
             .WithMany(u => u.Addresses)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Order ────────────────────────────────────────────
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(255);
            e.Property(x => x.DeliveryCity).HasColumnName("delivery_city");
            e.Property(x => x.DeliveryAddress).HasColumnName("delivery_address");
            e.Property(x => x.NovaPoshtaRef).HasColumnName("nova_poshta_ref");
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(10,2)");
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("numeric(10,2)");
            e.Property(x => x.ShippingCost).HasColumnName("shipping_cost").HasColumnType("numeric(10,2)");
            e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(10,2)");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.AdminNotes).HasColumnName("admin_notes");
            e.Property(x => x.PromoCodeId).HasColumnName("promo_code_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.Property(x => x.Status)
             .HasColumnName("status")
             .HasConversion(orderStatusConverter)
             .HasMaxLength(30);
            e.Property(x => x.DeliveryMethod)
             .HasColumnName("delivery_method")
             .HasConversion(deliveryMethodConverter)
             .HasMaxLength(50);
            e.Property(x => x.PaymentMethod)
             .HasColumnName("payment_method")
             .HasConversion(paymentMethodConverter)
             .HasMaxLength(30);
            e.Property(x => x.PaymentStatus)
             .HasColumnName("payment_status")
             .HasConversion(paymentStatusConverter)
             .HasMaxLength(20);

            e.HasOne(x => x.User)
             .WithMany(u => u.Orders)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PromoCode)
             .WithMany(p => p.Orders)
             .HasForeignKey(x => x.PromoCodeId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── OrderItem ────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(50);
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(10,2)");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(10,2)");
            e.Property(x => x.CustomNote).HasColumnName("custom_note");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PromoCode ────────────────────────────────────────
        modelBuilder.Entity<PromoCode>(e =>
        {
            e.ToTable("promo_codes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            e.Property(x => x.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(8,2)");
            e.Property(x => x.MinOrderAmount).HasColumnName("min_order_amount").HasColumnType("numeric(10,2)");
            e.Property(x => x.MaxUses).HasColumnName("max_uses");
            e.Property(x => x.UsedCount).HasColumnName("used_count");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DiscountType)
             .HasColumnName("discount_type")
             .HasConversion(discountTypeConverter)
             .HasMaxLength(10);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── CartItem ─────────────────────────────────────────
        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(100);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.AddedAt).HasColumnName("added_at");
            e.HasOne(x => x.User)
             .WithMany(u => u.CartItems)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
             .WithMany(p => p.CartItems)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Review ───────────────────────────────────────────
        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.AuthorName).HasColumnName("author_name").HasMaxLength(100);
            e.Property(x => x.Rating).HasColumnName("rating");
            e.Property(x => x.Comment).HasColumnName("comment");
            e.Property(x => x.IsApproved).HasColumnName("is_approved");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Product)
             .WithMany(p => p.Reviews)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CustomRequest ────────────────────────────────────
        modelBuilder.Entity<CustomRequest>(e =>
        {
            e.ToTable("custom_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(255);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Budget).HasColumnName("budget").HasColumnType("numeric(10,2)");
            e.Property(x => x.ReferenceUrls).HasColumnName("reference_urls").HasColumnType("text[]");
            e.Property(x => x.AdminNotes).HasColumnName("admin_notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.Status)
             .HasColumnName("status")
             .HasConversion(customRequestStatusConverter)
             .HasMaxLength(20);
            e.HasOne(x => x.User)
             .WithMany(u => u.CustomRequests)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CustomRequestFile ────────────────────────────────
        modelBuilder.Entity<CustomRequestFile>(e =>
        {
            e.ToTable("custom_request_files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RequestId).HasColumnName("request_id");
            e.Property(x => x.Url).HasColumnName("url");
            e.Property(x => x.FileType).HasColumnName("file_type").HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Request)
             .WithMany(r => r.Files)
             .HasForeignKey(x => x.RequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

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