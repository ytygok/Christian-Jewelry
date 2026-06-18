using ChristianJewelry.Domain.Enums;

namespace ChristianJewelry.Domain.Entities;

// Category 
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // "Каблучки / Перстні"
    public string Slug { get; set; } = string.Empty;       // "rings"
    public string? IconUrl { get; set; }
    public short SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

//  Material
public class Material : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // "Срібло 925"
    public string? Code { get; set; }                       // "AG925"
    public string? Description { get; set; }

    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
}

// Symbol 
public class Symbol : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // "Хрест", "Голуб"
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<ProductSymbol> ProductSymbols { get; set; } = new List<ProductSymbol>();
}

// Product
public class Product : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? PriceOld { get; set; }
    public int StockQty { get; set; } = 0;
    public string? Sku { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCustom { get; set; } = false;         // індивідуальне замовлення
    public bool IsFeatured { get; set; } = false;       // на головну
    public decimal? WeightGrams { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    public ICollection<ProductSymbol> ProductSymbols { get; set; } = new List<ProductSymbol>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

// ─── ProductImage 
public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public short SortOrder { get; set; } = 0;
    public bool IsCover { get; set; } = false;

    public Product Product { get; set; } = null!;
}

// ProductVariant 
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;   // "Розмір 17"
    public decimal PriceModifier { get; set; } = 0;    // +/- до базової ціни
    public int StockQty { get; set; } = 0;

    public Product Product { get; set; } = null!;
}

// Many-to-many joins 
public class ProductMaterial
{
    public Guid ProductId { get; set; }
    public Guid MaterialId { get; set; }

    public Product Product { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

public class ProductSymbol
{
    public Guid ProductId { get; set; }
    public Guid SymbolId { get; set; }

    public Product Product { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
}

// User 
public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<CustomRequest> CustomRequests { get; set; } = new List<CustomRequest>();
}

// UserAddress 
public class UserAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Label { get; set; }              // "Дім", "Робота"
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string? NovaPoshtaRef { get; set; }      // номер відділення НП
    public bool IsDefault { get; set; } = false;

    public User User { get; set; } = null!;
}

// Order
public class Order : AuditableEntity
{
    public Guid? UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    // Контактна інформація
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    // Доставка
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.NovaPoshta;
    public string? DeliveryCity { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? NovaPoshtaRef { get; set; }      // ТТН (трек-номер)

    // Фінанси
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal ShippingCost { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    // Оплата
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Примітки
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }

    // Промокод
    public Guid? PromoCodeId { get; set; }

    // Navigation
    public User? User { get; set; }
    public PromoCode? PromoCode { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

// OrderItem 
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }

    // Знімок продукту на момент замовлення
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Subtotal { get; set; }

    // Для індивідуального виробу
    public string? CustomNote { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }
}

// PromoCode
public class PromoCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; } = 0;
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

// CartItem
public class CartItem : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }          // для гостей
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

// Review
public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? UserId { get; set; }
    public string? AuthorName { get; set; }
    public short Rating { get; set; }               // 1-5
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = false;

    // Navigation
    public Product Product { get; set; } = null!;
    public User? User { get; set; }
}

// CustomRequest public
class CustomRequest : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string Description { get; set; } = string.Empty; // опис виробу
    public decimal? Budget { get; set; }
    public List<string> ReferenceUrls { get; set; } = new();
    public CustomRequestStatus Status { get; set; } = CustomRequestStatus.New;
    public string? AdminNotes { get; set; }

    // Navigation
    public User? User { get; set; }
    public ICollection<CustomRequestFile> Files { get; set; } = new List<CustomRequestFile>();
}

// CustomRequestFile public
class CustomRequestFile : BaseEntity
{
    public Guid RequestId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileType { get; set; }           // 'image', 'pdf', 'sketch'

    public CustomRequest Request { get; set; } = null!;
}