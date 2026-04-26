using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class AdminEmployee : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? JobTitle { get; set; }

        public int? RoleId { get; set; }

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        // 🔗 Relacionamentos
        public Role? Role { get; set; }

        public ICollection<AdminLogin> AdminLogins { get; set; } = new List<AdminLogin>();
    }
}
