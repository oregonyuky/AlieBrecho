using Domain.Common;

namespace Domain.Entities;

public class DropConfig : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public DateTime DataLiberacao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
