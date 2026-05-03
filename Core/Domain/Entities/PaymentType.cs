using System.ComponentModel.DataAnnotations;
using Domain.Common;


namespace Domain.Entities;
public class PaymentType : BaseEntity
{
    public string? TypeName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

