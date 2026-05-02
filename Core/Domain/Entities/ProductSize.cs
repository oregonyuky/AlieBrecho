using Domain.Common;

namespace Domain.Entities;

public class ProductSize : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;
    public Product? Product { get; set; }

    public string Size { get; set; } = string.Empty;   // P, M, G...

    public decimal? Bust { get; set; } //Busto
    public decimal? Sleeve { get; set; } //Manga
    public decimal? Length { get; set; } //Comprimento
}
