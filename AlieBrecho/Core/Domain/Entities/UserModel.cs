using System.ComponentModel.DataAnnotations;

namespace AlieBrecho.Core.Domain.Entities
{
    public class UserModel : BaseEntity
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? RoleName { get; set; }
    }
}
