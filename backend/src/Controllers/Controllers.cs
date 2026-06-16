using ChristianJewelry.Application.Common.Models;
using ChristianJewelry.Domain.Entities;
using ChristianJewelry.Domain.Enums;
using ChristianJewelry.Domain.Interfaces;
using ChristianJewelry.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChristianJewelry.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected bool IsAdmin => User.IsInRole("Admin");
}

// AUTH
/// <summary>Авторизація та реєстрація</summary>
[Tags("Auth")]
public class AuthController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _hasher;

    public AuthController(IUnitOfWork uow, IJwtService jwt, IPasswordHasher hasher)
    {
        _uow = uow; _jwt = jwt; _hasher = hasher;
    }

    /// <summary>Реєстрація нового користувача</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (await _uow.Users.ExistsAsync(req.Email, ct))
            return BadRequest(new { message = "Користувач з таким email вже існує" });

        var user = new User
        {
            Email = req.Email.ToLower(),
            FullName = req.FullName,
            Phone = req.Phone,
            PasswordHash = _hasher.Hash(req.Password),
            Role = UserRole.Customer
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = MapUserDto(user);
        return CreatedAtAction(nameof(GetMe), new AuthResponse(
            _jwt.GenerateAccessToken(user),
            _jwt.GenerateRefreshToken(),
            dto));
    }

    /// <summary>Вхід у систему</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(req.Email, ct);
        if (user == null || !_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Невірний email або пароль" });

        if (!user.IsActive)
            return Unauthorized(new { message = "Акаунт заблоковано" });

        return Ok(new AuthResponse(
            _jwt.GenerateAccessToken(user),
            _jwt.GenerateRefreshToken(),
            MapUserDto(user)));
    }

    /// <summary>Профіль поточного користувача</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(CurrentUserId!.Value, ct);
        return user is null ? NotFound() : Ok(MapUserDto(user));
    }

    private static UserDto MapUserDto(User u)
        => new(u.Id, u.Email, u.FullName, u.Phone, u.Role, u.IsVerified);
}


// CATEGORIES
/// <summary>Категорії виробів (каблучки, сережки, підвіски...)</summary>
[Tags("Categories")]
public class CategoriesController : BaseController
{
    private readonly IUnitOfWork _uow;
    public CategoriesController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Список активних категорій</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var cats = await _uow.Categories.GetActiveAsync(ct);
        return Ok(cats.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.IconUrl, c.SortOrder, c.IsActive)));
    }

    /// <summary>Деталі категорії за slug</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var c = await _uow.Categories.GetBySlugAsync(slug, ct);
        return c is null ? NotFound() : Ok(new CategoryDto(c.Id, c.Name, c.Slug, c.IconUrl, c.SortOrder, c.IsActive));
    }

    /// <summary>Створити категорію (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req, CancellationToken ct)
    {
        var cat = new Category
        {
            Name = req.Name, Slug = req.Slug,
            IconUrl = req.IconUrl, SortOrder = req.SortOrder
        };
        await _uow.Categories.AddAsync(cat, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = cat.Slug },
            new CategoryDto(cat.Id, cat.Name, cat.Slug, cat.IconUrl, cat.SortOrder, cat.IsActive));
    }

    /// <summary>Видалити категорію (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var cat = await _uow.Categories.GetByIdAsync(id, ct);
        if (cat is null) return NotFound();
        _uow.Categories.Delete(cat);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}


// PRODUCTS
/// <summary>Каталог прикрас</summary>
[Tags("Products")]
public class ProductsController : BaseController
{
    private readonly IUnitOfWork _uow;
    public ProductsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Каталог з фільтрацією та пагінацією</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams q, CancellationToken ct)
    {
        var (items, total) = await _uow.Products.GetPagedAsync(
            q.Page, q.PageSize, q.CategoryId, q.Search, q.MinPrice, q.MaxPrice, ct);

        var dtos = items.Select(p => new ProductListDto(
            p.Id, p.Name, p.Slug, p.Price, p.PriceOld,
            p.Images.FirstOrDefault()?.Url,
            p.Category.Name, p.IsActive, p.StockQty)).ToList();

        return Ok(new PagedResult<ProductListDto>(dtos, total, q.Page, q.PageSize));
    }

    /// <summary>Детальна сторінка продукту</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var p = await _uow.Products.GetBySlugAsync(slug, ct);
        if (p is null) return NotFound();

        var avgRating = await _uow.Reviews.GetAverageRatingAsync(p.Id, ct);
        var reviews = await _uow.Reviews.GetByProductAsync(p.Id, true, ct);

        return Ok(new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.PriceOld,
            p.StockQty, p.Sku, p.IsActive, p.IsCustom, p.IsFeatured, p.WeightGrams,
            new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Slug, p.Category.IconUrl, p.Category.SortOrder, p.Category.IsActive),
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.SortOrder, i.IsCover)).ToList(),
            p.Variants.Select(v => new ProductVariantDto(v.Id, v.Name, v.PriceModifier, v.StockQty)).ToList(),
            p.ProductMaterials.Select(pm => new MaterialDto(pm.Material.Id, pm.Material.Name, pm.Material.Code, pm.Material.Description)).ToList(),
            p.ProductSymbols.Select(ps => new SymbolDto(ps.Symbol.Id, ps.Symbol.Name, ps.Symbol.Slug, ps.Symbol.Description)).ToList(),
            avgRating, reviews.Count));
    }

    /// <summary>Рекомендовані вироби для головної</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductListDto>), 200)]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var products = await _uow.Products.GetFeaturedAsync(ct);
        return Ok(products.Select(p => new ProductListDto(
            p.Id, p.Name, p.Slug, p.Price, p.PriceOld,
            p.Images.FirstOrDefault()?.Url,
            p.Category?.Name ?? "", p.IsActive, p.StockQty)));
    }

    /// <summary>Створити продукт (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req, CancellationToken ct)
    {
        var product = new Product
        {
            CategoryId = req.CategoryId, Name = req.Name, Slug = req.Slug,
            Description = req.Description, Price = req.Price, PriceOld = req.PriceOld,
            StockQty = req.StockQty, Sku = req.Sku,
            IsCustom = req.IsCustom, IsFeatured = req.IsFeatured, WeightGrams = req.WeightGrams
        };

        if (req.MaterialIds != null)
            product.ProductMaterials = req.MaterialIds
                .Select(mid => new ProductMaterial { ProductId = product.Id, MaterialId = mid })
                .ToList();

        if (req.SymbolIds != null)
            product.ProductSymbols = req.SymbolIds
                .Select(sid => new ProductSymbol { ProductId = product.Id, SymbolId = sid })
                .ToList();

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, new { id = product.Id });
    }

    /// <summary>Видалити продукт (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var p = await _uow.Products.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        _uow.Products.Delete(p);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}


// CART
/// <summary>Кошик покупок</summary>
[Tags("Cart")]
public class CartController : BaseController
{
    private readonly IUnitOfWork _uow;
    public CartController(IUnitOfWork uow) => _uow = uow;

    private string? SessionId => HttpContext.Session.GetString("cart_session")
        ?? HttpContext.Session.Id;

    /// <summary>Вміст кошика</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartSummaryDto), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var items = CurrentUserId.HasValue
            ? await _uow.Cart.GetByUserAsync(CurrentUserId.Value, ct)
            : await _uow.Cart.GetBySessionAsync(SessionId!, ct);

        var dtos = items.Select(MapCartItemDto).ToList();
        return Ok(new CartSummaryDto(dtos, dtos.Sum(i => i.Subtotal), dtos.Sum(i => i.Quantity)));
    }

    /// <summary>Додати товар до кошика</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartSummaryDto), 200)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest req, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(req.ProductId, ct);
        if (product is null || !product.IsActive)
            return BadRequest(new { message = "Товар не знайдено" });

        var existing = await _uow.Cart.GetItemAsync(
            CurrentUserId, SessionId, req.ProductId, req.VariantId, ct);

        if (existing != null)
        {
            existing.Quantity += req.Quantity;
            _uow.Cart.Update(existing);
        }
        else
        {
            await _uow.Cart.AddAsync(new CartItem
            {
                UserId = CurrentUserId,
                SessionId = CurrentUserId.HasValue ? null : SessionId,
                ProductId = req.ProductId,
                VariantId = req.VariantId,
                Quantity = req.Quantity
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return await Get(ct);
    }

    /// <summary>Оновити кількість товару</summary>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(CartSummaryDto), 200)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest req, CancellationToken ct)
    {
        var item = await _uow.Cart.GetItemAsync(CurrentUserId, SessionId, id, null, ct);
        if (item is null) return NotFound();

        if (req.Quantity <= 0)
            _uow.Cart.Delete(item);
        else
        {
            item.Quantity = req.Quantity;
            _uow.Cart.Update(item);
        }

        await _uow.SaveChangesAsync(ct);
        return await Get(ct);
    }

    /// <summary>Видалити товар з кошика</summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        var item = await _uow.Cart.GetItemAsync(CurrentUserId, SessionId, id, null, ct);
        if (item is null) return NotFound();
        _uow.Cart.Delete(item);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Очистити кошик</summary>
    [HttpDelete]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _uow.Cart.ClearAsync(CurrentUserId, SessionId, ct);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CartItemDto MapCartItemDto(CartItem c)
    {
        var price = c.Product.Price + (c.Variant?.PriceModifier ?? 0);
        return new CartItemDto(
            c.Id, c.ProductId, c.Product.Name,
            c.Product.Images.FirstOrDefault()?.Url,
            price, c.VariantId, c.Variant?.Name,
            c.Quantity, price * c.Quantity);
    }
}


// ORDERS
/// <summary>Замовлення</summary>
[Tags("Orders")]
[Authorize]
public class OrdersController : BaseController
{
    private readonly IUnitOfWork _uow;
    public OrdersController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Мої замовлення</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderListDto>), 200)]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var orders = await _uow.Orders.GetByUserAsync(CurrentUserId!.Value, ct);
        return Ok(orders.Select(MapOrderList));
    }

    /// <summary>Деталі замовлення</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct);
        if (order is null) return NotFound();
        if (order.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapOrderDetail(order));
    }

    /// <summary>Оформити замовлення</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDetailDto), 201)]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
    {
        // Validate promo
        PromoCode? promo = null;
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(req.PromoCode))
        {
            promo = await _uow.PromoCodes.GetByCodeAsync(req.PromoCode, ct);
            if (promo != null)
            {
                discount = promo.DiscountType == DiscountType.Percent
                    ? req.Items.Sum(i => i.Quantity * 100) * promo.DiscountValue / 100  // simplified
                    : promo.DiscountValue;
            }
        }

        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var item in req.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
            if (product is null) return BadRequest(new { message = $"Товар {item.ProductId} не знайдено" });

            var variant = item.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == item.VariantId)
                : null;

            var price = product.Price + (variant?.PriceModifier ?? 0);
            var itemSubtotal = price * item.Quantity;
            subtotal += itemSubtotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                VariantId = variant?.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                UnitPrice = price,
                Quantity = item.Quantity,
                Subtotal = itemSubtotal,
                CustomNote = item.CustomNote
            });
        }

        var order = new Order
        {
            UserId = CurrentUserId,
            CustomerName = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerEmail = req.CustomerEmail,
            DeliveryMethod = req.DeliveryMethod,
            DeliveryCity = req.DeliveryCity,
            DeliveryAddress = req.DeliveryAddress,
            PaymentMethod = req.PaymentMethod,
            Notes = req.Notes,
            PromoCodeId = promo?.Id,
            Subtotal = subtotal,
            DiscountAmount = discount,
            ShippingCost = req.DeliveryMethod == DeliveryMethod.Pickup ? 0 : 0, // логіка доставки
            TotalAmount = subtotal - discount,
            Items = orderItems
        };

        await _uow.Orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.Orders.GetWithItemsAsync(order.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, MapOrderDetail(created!));
    }

    /// <summary>Скасувати замовлення</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var order = await _uow.Orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        if (order.UserId != CurrentUserId && !IsAdmin) return Forbid();
        if (order.Status != OrderStatus.New && order.Status != OrderStatus.Confirmed)
            return BadRequest(new { message = "Неможливо скасувати замовлення на цьому етапі" });

        order.Status = OrderStatus.Cancelled;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = "Замовлення скасовано" });
    }

    /// <summary>Всі замовлення (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<OrderListDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orders = await _uow.Orders.GetPagedAsync(page, pageSize, ct);
        return Ok(orders.Select(MapOrderList).ToList());
    }

    /// <summary>Змінити статус замовлення (Admin)</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest req, CancellationToken ct)
    {
        var order = await _uow.Orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        order.Status = req.Status;
        if (req.AdminNotes != null) order.AdminNotes = req.AdminNotes;
        if (req.NovaPoshtaRef != null) order.NovaPoshtaRef = req.NovaPoshtaRef;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = "Статус оновлено" });
    }

    private static OrderListDto MapOrderList(Order o)
        => new(o.Id, o.CustomerName, o.CustomerPhone, o.Status, o.PaymentStatus, o.TotalAmount, o.CreatedAt);

    private static OrderDetailDto MapOrderDetail(Order o)
        => new(o.Id, o.UserId, o.CustomerName, o.CustomerPhone, o.CustomerEmail,
            o.Status, o.DeliveryMethod, o.DeliveryCity, o.DeliveryAddress, o.NovaPoshtaRef,
            o.Subtotal, o.DiscountAmount, o.ShippingCost, o.TotalAmount,
            o.PaymentMethod, o.PaymentStatus, o.Notes, o.AdminNotes,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.Subtotal, i.CustomNote)).ToList(),
            o.CreatedAt, o.UpdatedAt);
}


// REVIEWS
/// <summary>Відгуки на вироби</summary>
[Tags("Reviews")]
public class ReviewsController : BaseController
{
    private readonly IUnitOfWork _uow;
    public ReviewsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Відгуки до продукту</summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewDto>), 200)]
    public async Task<IActionResult> GetByProduct(Guid productId, CancellationToken ct)
    {
        var reviews = await _uow.Reviews.GetByProductAsync(productId, true, ct);
        return Ok(reviews.Select(MapReviewDto));
    }

    /// <summary>Залишити відгук</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest req, CancellationToken ct)
    {
        var review = new Review
        {
            ProductId = req.ProductId,
            UserId = CurrentUserId,
            AuthorName = req.AuthorName,
            Rating = req.Rating,
            Comment = req.Comment,
            IsApproved = false   // очікує модерації
        };
        await _uow.Reviews.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetByProduct), new { productId = req.ProductId }, MapReviewDto(review));
    }

    /// <summary>Схвалити відгук (Admin)</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var review = await _uow.Reviews.GetByIdAsync(id, ct);
        if (review is null) return NotFound();
        review.IsApproved = true;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = "Відгук схвалено" });
    }

    private static ReviewDto MapReviewDto(Review r)
        => new(r.Id, r.ProductId, r.AuthorName, r.Rating, r.Comment, r.IsApproved, r.CreatedAt);
}

// PROMO CODES
/// <summary>Промокоди та знижки</summary>
[Tags("PromoCodes")]
public class PromoCodesController : BaseController
{
    private readonly IUnitOfWork _uow;
    public PromoCodesController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Перевірити промокод</summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidatePromoResponse), 200)]
    public async Task<IActionResult> Validate([FromBody] ValidatePromoRequest req, CancellationToken ct)
    {
        var promo = await _uow.PromoCodes.GetByCodeAsync(req.Code, ct);

        if (promo is null) return Ok(new ValidatePromoResponse(false, "Промокод не існує", null));
        if (promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow)
            return Ok(new ValidatePromoResponse(false, "Промокод закінчився", null));
        if (promo.MaxUses.HasValue && promo.UsedCount >= promo.MaxUses)
            return Ok(new ValidatePromoResponse(false, "Промокод вичерпано", null));
        if (req.OrderAmount < promo.MinOrderAmount)
            return Ok(new ValidatePromoResponse(false, $"Мінімальна сума замовлення: {promo.MinOrderAmount} грн", null));

        var discount = promo.DiscountType == DiscountType.Percent
            ? req.OrderAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        return Ok(new ValidatePromoResponse(true, null, discount));
    }

    /// <summary>Всі промокоди (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<PromoCodeDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var codes = await _uow.PromoCodes.GetAllAsync(ct);
        return Ok(codes.Select(p => new PromoCodeDto(
            p.Id, p.Code, p.DiscountType, p.DiscountValue,
            p.MinOrderAmount, p.MaxUses, p.UsedCount, p.ExpiresAt, p.IsActive)));
    }

    /// <summary>Створити промокод (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PromoCodeDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeRequest req, CancellationToken ct)
    {
        var promo = new PromoCode
        {
            Code = req.Code.ToUpper(),
            DiscountType = req.DiscountType,
            DiscountValue = req.DiscountValue,
            MinOrderAmount = req.MinOrderAmount,
            MaxUses = req.MaxUses,
            ExpiresAt = req.ExpiresAt
        };
        await _uow.PromoCodes.AddAsync(promo, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new PromoCodeDto(
            promo.Id, promo.Code, promo.DiscountType, promo.DiscountValue,
            promo.MinOrderAmount, promo.MaxUses, promo.UsedCount, promo.ExpiresAt, promo.IsActive));
    }
}

// CUSTOM REQUESTS (Ваша ідея)
/// <summary>Індивідуальні замовлення ("Ваша ідея")</summary>
[Tags("CustomRequests")]
public class CustomRequestsController : BaseController
{
    private readonly IUnitOfWork _uow;
    public CustomRequestsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Подати запит на індивідуальний виріб</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomRequestDto), 201)]
    public async Task<IActionResult> Submit([FromBody] SubmitCustomRequestRequest req, CancellationToken ct)
    {
        var request = new CustomRequest
        {
            UserId = CurrentUserId,
            CustomerName = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerEmail = req.CustomerEmail,
            Description = req.Description,
            Budget = req.Budget,
            ReferenceUrls = req.ReferenceUrls ?? new List<string>()
        };
        await _uow.CustomRequests.AddAsync(request, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, MapDto(request));
    }

    /// <summary>Мої запити</summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<CustomRequestDto>), 200)]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var requests = await _uow.CustomRequests.GetByUserAsync(CurrentUserId!.Value, ct);
        return Ok(requests.Select(MapDto));
    }

    /// <summary>Деталі запиту</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomRequestDto), 200)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _uow.CustomRequests.GetByIdAsync(id, ct);
        if (r is null) return NotFound();
        if (r.UserId != CurrentUserId && !IsAdmin) return Forbid();
        return Ok(MapDto(r));
    }

    /// <summary>Всі запити (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomRequestDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var requests = await _uow.CustomRequests.GetAllPagedAsync(page, pageSize, ct);
        return Ok(requests.Select(MapDto));
    }

    /// <summary>Змінити статус запиту (Admin)</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCustomRequestStatusRequest req, CancellationToken ct)
    {
        var r = await _uow.CustomRequests.GetByIdAsync(id, ct);
        if (r is null) return NotFound();
        r.Status = req.Status;
        if (req.AdminNotes != null) r.AdminNotes = req.AdminNotes;
        _uow.CustomRequests.Update(r);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = "Статус оновлено" });
    }

    private static CustomRequestDto MapDto(CustomRequest r)
        => new(r.Id, r.CustomerName, r.CustomerPhone, r.CustomerEmail,
            r.Description, r.Budget, r.ReferenceUrls, r.Status, r.AdminNotes,
            r.Files.Select(f => new CustomRequestFileDto(f.Id, f.Url, f.FileType)).ToList(),
            r.CreatedAt);
}