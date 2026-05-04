using Domain.Common;

namespace Domain.Entities;

public class ShippingDetail : BaseEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? Complement { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}