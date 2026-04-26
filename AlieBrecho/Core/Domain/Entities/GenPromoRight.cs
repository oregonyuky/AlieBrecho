using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class GenPromoRight : BaseEntity
    {
        public string Title { get; set; } = string.Empty;

        public string? BadgeText { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? LinkUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
