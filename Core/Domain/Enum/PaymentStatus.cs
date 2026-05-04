using System.ComponentModel;

namespace Domain.Enums;

public enum PaymentStatus
{
    [Description("Pending")]
    Pending = 0,
    [Description("Paid")]
    Paid = 1,
    [Description("Cancelled")]
    Cancelled = 2,
}