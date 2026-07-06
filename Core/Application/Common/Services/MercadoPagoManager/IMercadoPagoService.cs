namespace Application.Common.Services.MercadoPagoManager;

public interface IMercadoPagoService
{
    Task<MercadoPagoPixPaymentResult> CreatePixPaymentAsync(
        MercadoPagoPixPaymentRequest request,
        CancellationToken cancellationToken);

    Task<MercadoPagoPaymentStatusResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken);
}
