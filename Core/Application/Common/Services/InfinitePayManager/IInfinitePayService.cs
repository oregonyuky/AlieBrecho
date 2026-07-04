namespace Application.Common.Services.InfinitePayManager;

public interface IInfinitePayService
{
    Task<InfinitePayCheckoutCreationResult> CreateCheckoutAsync(
        InfinitePayCreateCheckoutRequest request,
        CancellationToken cancellationToken = default);
}
