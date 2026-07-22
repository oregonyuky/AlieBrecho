using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities;

public class ShippingBox : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public decimal? InsuranceValue { get; set; }
    public string? PackageCategoryId { get; set; }
    public PackageCategory? PackageCategory { get; set; }
    public int StockQuantity { get; set; }
    public decimal? MaxWeight { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

