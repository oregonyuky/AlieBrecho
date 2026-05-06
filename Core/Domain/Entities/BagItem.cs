using Domain.Common;

namespace Domain.Entities;

public class BagItem : BaseEntity
{
    // Relacionamento
    public string? BagId { get; set; }

    // Produto
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }

    // Brechó → peça única
    public int Quantity { get; set; } = 1;

    // Valores
    public decimal Price { get; set; }

    // Peso individual (para cálculo de frete)
    public decimal Weight { get; set; }

    // Controle de pagamento
    public bool IsPaid { get; set; } = false;

    // Controle de reserva
    public bool IsReserved { get; set; } = true;

    // Expiração da reserva (ex: 15 minutos)
    public DateTime? ReservationExpiresAt { get; set; }

    // Datas
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}
