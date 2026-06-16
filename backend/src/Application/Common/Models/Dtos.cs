using ChristianJewelry.Domain.Enums;

namespace ChristianJewelry.Application.Common.Models;

// Pagination 
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

// Auth
public record RegisterRequest(string Email, string Password, string? FullName, string? Phone);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);

// User 
public record UserDto(Guid Id, string Email, string? FullName, string? Phone, UserRole Role, bool IsVerified);

public record UserAddressDto(
    Guid Id, string? Label, string City, string Street,
    string? ZipCode, string? NovaPoshtaRef, bool IsDefault);

public record CreateAddressRequest(
    string? Label, string City, string Street,
    string? ZipCode, string? NovaPoshtaRef, bool IsDefault = false);

// Category
public record CategoryDto(Guid Id, string Name, string Slug, string? IconUrl, short SortOrder, bool IsActive);
public record CreateCategoryRequest(string Name, string Slug, string? IconUrl, short SortOrder = 0);
public record UpdateCategoryRequest(string? Name, string? Slug, string? IconUrl, short? SortOrder, bool? IsActive);

// Material
public record MaterialDto(Guid Id, string Name, string? Code, string? Description);
public record CreateMaterialRequest(string Name, string? Code, string? Description);

// Symbol 
public record SymbolDto(Guid Id, string Name, string Slug, string? Description);
public record CreateSymbolRequest(string Name, string Slug, string? Description);

// Product 
public record ProductListDto(
    Guid Id, string Name, string Slug, decimal Price, decimal? PriceOld,
    string? CoverImageUrl, string CategoryName, bool IsActive, int StockQty);

public record ProductDetailDto(
    Guid Id, string Name, string Slug, string? Description,
    decimal Price, decimal? PriceOld, int StockQty, string? Sku,
    bool IsActive, bool IsCustom, bool IsFeatured, decimal? WeightGrams,
    CategoryDto Category,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<MaterialDto> Materials,
    IReadOnlyList<SymbolDto> Symbols,
    double AverageRating, int ReviewCount);

public record ProductImageDto(Guid Id, string Url, string? AltText, short SortOrder, bool IsCover);
public record ProductVariantDto(Guid Id, string Name, decimal PriceModifier, int StockQty);

public record CreateProductRequest(
    Guid CategoryId, string Name, string Slug, string? Description,
    decimal Price, decimal? PriceOld, int StockQty, string? Sku,
    bool IsCustom = false, bool IsFeatured = false, decimal? WeightGrams = null,
    List<Guid>? MaterialIds = null, List<Guid>? SymbolIds = null);

public record UpdateProductRequest(
    string? Name, string? Description, decimal? Price, decimal? PriceOld,
    int? StockQty, bool? IsActive, bool? IsFeatured,
    List<Guid>? MaterialIds = null, List<Guid>? SymbolIds = null);

public record ProductQueryParams(
    int Page = 1, int PageSize = 20,
    Guid? CategoryId = null, string? Search = null,
    decimal? MinPrice = null, decimal? MaxPrice = null);

// Cart
public record CartItemDto(
    Guid Id, Guid ProductId, string ProductName, string? CoverImageUrl,
    decimal UnitPrice, Guid? VariantId, string? VariantName, int Quantity, decimal Subtotal);

public record CartSummaryDto(IReadOnlyList<CartItemDto> Items, decimal Total, int ItemCount);

public record AddToCartRequest(Guid ProductId, Guid? VariantId, int Quantity = 1);
public record UpdateCartItemRequest(int Quantity);

// Order 
public record OrderListDto(
    Guid Id, string CustomerName, string CustomerPhone,
    OrderStatus Status, PaymentStatus PaymentStatus,
    decimal TotalAmount, DateTime CreatedAt);

public record OrderDetailDto(
    Guid Id, Guid? UserId, string CustomerName, string CustomerPhone, string? CustomerEmail,
    OrderStatus Status, DeliveryMethod DeliveryMethod,
    string? DeliveryCity, string? DeliveryAddress, string? NovaPoshtaRef,
    decimal Subtotal, decimal DiscountAmount, decimal ShippingCost, decimal TotalAmount,
    PaymentMethod PaymentMethod, PaymentStatus PaymentStatus,
    string? Notes, string? AdminNotes,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt, DateTime UpdatedAt);

public record OrderItemDto(
    Guid Id, string ProductName, string? Sku,
    decimal UnitPrice, int Quantity, decimal Subtotal, string? CustomNote);

public record PlaceOrderRequest(
    string CustomerName, string CustomerPhone, string? CustomerEmail,
    DeliveryMethod DeliveryMethod, string? DeliveryCity, string? DeliveryAddress,
    PaymentMethod PaymentMethod, string? Notes, string? PromoCode,
    List<OrderItemRequest> Items);

public record OrderItemRequest(Guid ProductId, Guid? VariantId, int Quantity, string? CustomNote);

public record UpdateOrderStatusRequest(OrderStatus Status, string? AdminNotes, string? NovaPoshtaRef);

// Review 
public record ReviewDto(
    Guid Id, Guid ProductId, string? AuthorName,
    short Rating, string? Comment, bool IsApproved, DateTime CreatedAt);

public record CreateReviewRequest(Guid ProductId, string? AuthorName, short Rating, string? Comment);

// PromoCode
public record PromoCodeDto(
    Guid Id, string Code, DiscountType DiscountType, decimal DiscountValue,
    decimal MinOrderAmount, int? MaxUses, int UsedCount,
    DateTime? ExpiresAt, bool IsActive);

public record ValidatePromoRequest(string Code, decimal OrderAmount);
public record ValidatePromoResponse(bool IsValid, string? ErrorMessage, decimal? DiscountAmount);

public record CreatePromoCodeRequest(
    string Code, DiscountType DiscountType, decimal DiscountValue,
    decimal MinOrderAmount = 0, int? MaxUses = null, DateTime? ExpiresAt = null);

// ─── Custom Request 
public record CustomRequestDto(
    Guid Id, string CustomerName, string CustomerPhone, string? CustomerEmail,
    string Description, decimal? Budget, List<string> ReferenceUrls,
    CustomRequestStatus Status, string? AdminNotes,
    IReadOnlyList<CustomRequestFileDto> Files,
    DateTime CreatedAt);

public record CustomRequestFileDto(Guid Id, string Url, string? FileType);

public record SubmitCustomRequestRequest(
    string CustomerName, string CustomerPhone, string? CustomerEmail,
    string Description, decimal? Budget, List<string>? ReferenceUrls);

public record UpdateCustomRequestStatusRequest(CustomRequestStatus Status, string? AdminNotes);

// ─── Admin Dashboard 
public record DashboardStatsDto(
    int TotalOrders, int NewOrders, decimal TodayRevenue, decimal TotalRevenue,
    int TotalProducts, int LowStockProducts, int TotalUsers, int PendingReviews);