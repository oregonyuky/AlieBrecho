using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;
public class Payment : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? PaymentDateTime { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentTypeId { get; set; }
    public PaymentType? PaymentType { get; set; }
    public PaymentDetail? PaymentDetail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

