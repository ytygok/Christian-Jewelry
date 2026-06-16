using ChristianJewelry.Domain.Entities;
using ChristianJewelry.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChristianJewelry.Infrastructure.Persistence.Repositories;

// Generic
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);
    public void Delete(T entity) => _set.Remove(entity);
}

// ─── Product 
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _set
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
            .Include(p => p.ProductSymbols).ThenInclude(ps => ps.Symbol)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await _set
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Include(p => p.Images.Where(i => i.IsCover))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(CancellationToken ct = default)
        => await _set
            .Where(p => p.IsFeatured && p.IsActive)
            .Include(p => p.Images.Where(i => i.IsCover))
            .OrderBy(p => p.Name)
            .Take(12)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        var query = _set.Where(p => p.IsActive).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(p => p.Images.Where(i => i.IsCover))
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}

// ─── Order 
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext db) : base(db) { }

    public async Task<Order?> GetWithItemsAsync(Guid id, CancellationToken ct = default)
        => await _set
            .Include(o => o.Items)
            .Include(o => o.PromoCode)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _set
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => await _set
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}

// ─── Cart 
public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CartItem>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product).ThenInclude(p => p.Images.Where(i => i.IsCover))
            .Include(c => c.Variant)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CartItem>> GetBySessionAsync(string sessionId, CancellationToken ct = default)
        => await _db.CartItems
            .Where(c => c.SessionId == sessionId)
            .Include(c => c.Product).ThenInclude(p => p.Images.Where(i => i.IsCover))
            .Include(c => c.Variant)
            .ToListAsync(ct);

    public async Task<CartItem?> GetItemAsync(Guid? userId, string? sessionId, Guid productId, Guid? variantId, CancellationToken ct = default)
        => await _db.CartItems.FirstOrDefaultAsync(c =>
            (userId != null ? c.UserId == userId : c.SessionId == sessionId) &&
            c.ProductId == productId &&
            c.VariantId == variantId, ct);

    public async Task AddAsync(CartItem item, CancellationToken ct = default)
        => await _db.CartItems.AddAsync(item, ct);

    public void Update(CartItem item) => _db.CartItems.Update(item);
    public void Delete(CartItem item) => _db.CartItems.Remove(item);

    public async Task ClearAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        var items = userId.HasValue
            ? _db.CartItems.Where(c => c.UserId == userId)
            : _db.CartItems.Where(c => c.SessionId == sessionId);
        _db.CartItems.RemoveRange(items);
        await Task.CompletedTask;
    }
}

// ─── User 
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
        => await _set.AnyAsync(u => u.Email == email.ToLower(), ct);
}

// ─── Category 
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default)
        => await _set
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(c => c.Slug == slug, ct);
}

// ─── PromoCode
public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
{
    public PromoCodeRepository(AppDbContext db) : base(db) { }

    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(p => p.Code == code.ToUpper() && p.IsActive, ct);
}

// ─── Review 
public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Review>> GetByProductAsync(Guid productId, bool approvedOnly = true, CancellationToken ct = default)
        => await _set
            .Where(r => r.ProductId == productId && (!approvedOnly || r.IsApproved))
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken ct = default)
    {
        var ratings = await _set
            .Where(r => r.ProductId == productId && r.IsApproved)
            .Select(r => (int)r.Rating)
            .ToListAsync(ct);
        return ratings.Count == 0 ? 0 : ratings.Average();
    }
}

// ─── CustomRequest 
public class CustomRequestRepository : Repository<CustomRequest>, ICustomRequestRepository
{
    public CustomRequestRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<CustomRequest>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _set
            .Where(r => r.UserId == userId)
            .Include(r => r.Files)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CustomRequest>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => await _set
            .Include(r => r.Files)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}