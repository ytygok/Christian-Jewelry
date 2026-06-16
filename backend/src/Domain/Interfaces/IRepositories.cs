using ChristianJewelry.Domain.Entities;

namespace ChristianJewelry.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface ICartRepository
{
    Task<IReadOnlyList<CartItem>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CartItem>> GetBySessionAsync(string sessionId, CancellationToken ct = default);
    Task<CartItem?> GetItemAsync(Guid? userId, string? sessionId, Guid productId, Guid? variantId, CancellationToken ct = default);
    Task AddAsync(CartItem item, CancellationToken ct = default);
    void Update(CartItem item);
    void Delete(CartItem item);
    Task ClearAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
}

public interface IPromoCodeRepository : IRepository<PromoCode>
{
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetByProductAsync(Guid productId, bool approvedOnly = true, CancellationToken ct = default);
    Task<double> GetAverageRatingAsync(Guid productId, CancellationToken ct = default);
}

public interface ICustomRequestRepository : IRepository<CustomRequest>
{
    Task<IReadOnlyList<CustomRequest>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomRequest>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    ICartRepository Cart { get; }
    IUserRepository Users { get; }
    IPromoCodeRepository PromoCodes { get; }
    ICategoryRepository Categories { get; }
    IReviewRepository Reviews { get; }
    ICustomRequestRepository CustomRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}