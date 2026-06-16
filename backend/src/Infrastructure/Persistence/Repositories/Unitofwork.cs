using ChristianJewelry.Domain.Interfaces;
using ChristianJewelry.Infrastructure.Persistence.Repositories;

namespace ChristianJewelry.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public IProductRepository Products { get; }
    public IOrderRepository Orders { get; }
    public ICartRepository Cart { get; }
    public IUserRepository Users { get; }
    public IPromoCodeRepository PromoCodes { get; }
    public ICategoryRepository Categories { get; }
    public IReviewRepository Reviews { get; }
    public ICustomRequestRepository CustomRequests { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Products = new ProductRepository(db);
        Orders = new OrderRepository(db);
        Cart = new CartRepository(db);
        Users = new UserRepository(db);
        PromoCodes = new PromoCodeRepository(db);
        Categories = new CategoryRepository(db);
        Reviews = new ReviewRepository(db);
        CustomRequests = new CustomRequestRepository(db);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}