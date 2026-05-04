using System.ComponentModel;

namespace Domain.Enums;

public enum OrderStatus
{
    [Description("Pending")]
    Pending = 0,
    [Description("Paid")]
    Paid = 1,
    [Description("Dispatched")]
    Dispatched = 2,
    [Description("Shipped")]
    Shipped = 3,
    [Description("Delivered")]
    Delivered = 4,
    [Description("Cancelled")]
    Cancelled = 5
}