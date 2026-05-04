using Domain.Common;

namespace Domain.Entities;

public class PaymentDetail : BaseEntity
{
    public string? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public DateTime? CapturedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
