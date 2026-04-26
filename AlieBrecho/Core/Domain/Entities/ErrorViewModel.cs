using System.Diagnostics;

namespace AlieBrecho.Core.Domain.Entities
{
    public class ErrorViewModel : BaseEntity
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
