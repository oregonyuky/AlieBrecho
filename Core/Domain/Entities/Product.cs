using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CategoryID { get; set; }
    public Category? Category { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? UnitWeight { get; set; }
    public ICollection<ProductSize>? Sizes { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? ProductAvailable { get; set; }
    public string? MainImageURL { get; set; }
    public string? AltText { get; set; }
    public bool? AddBadge { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? Picture1 { get; set; }
    public string? Picture2 { get; set; }
    public string? Picture3 { get; set; }
    public string? Picture4 { get; set; }
    public string? Note { get; set; }
}
