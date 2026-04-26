using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class AdminLogin : BaseEntity
    {
        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public int? RoleId { get; set; }

        public int? AdminEmployeeId { get; set; }

        // 🔗 Relacionamentos
        public Role? Role { get; set; }

        public AdminEmployee? AdminEmployee { get; set; }
    }
}
