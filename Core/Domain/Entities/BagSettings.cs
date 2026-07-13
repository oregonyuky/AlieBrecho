using Domain.Common;

namespace Domain.Entities;

public class BagSettings : BaseEntity
{
    public int DefaultDurationValue { get; set; } = 60;
    public string DefaultDurationUnit { get; set; } = "months";
    public int ExtensionDurationValue { get; set; } = 30;
    public string ExtensionDurationUnit { get; set; } = "months";
    public int ExtensionResponseDeadlineValue { get; set; } = 7;
    public string ExtensionResponseDeadlineUnit { get; set; } = "days";
}
