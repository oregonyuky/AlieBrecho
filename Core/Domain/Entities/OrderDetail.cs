using Domain.Common;

namespace Domain.Entities;

public class OrderDetail : BaseEntity
{
    public string? OrderId { get; set; }
    public Order? Order { get; set; }
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
