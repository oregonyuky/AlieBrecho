using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities;

public class ShippingBox : BaseEntity
{
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public decimal? InsuranceValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

