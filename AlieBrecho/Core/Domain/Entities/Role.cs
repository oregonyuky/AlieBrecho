using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<AdminLogin> AdminLogins { get; set; } = new List<AdminLogin>();
    }
}
