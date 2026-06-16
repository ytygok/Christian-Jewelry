namespace ChristianJewelry.Domain.Enums;

public enum OrderStatus
{
    New,
    Confirmed,
    InProduction,
    Shipped,
    Delivered,
    Cancelled
}

public enum PaymentMethod
{
    Card,
    CashOnDelivery,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded
}

public enum DeliveryMethod
{
    NovaPoshta,
    UkrPoshta,
    Pickup
}

public enum UserRole
{
    Customer,
    Admin
}

public enum DiscountType
{
    Percent,
    Fixed
}

public enum CustomRequestStatus
{
    New,
    InReview,
    Quoted,
    Approved,
    Rejected
}