using Application.Common.CQS.Queries;
using Application.Common.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Queries;

public class CalculateOrderShippingCostResult
{
    public decimal Data { get; init; }
}

public class CalculateOrderShippingCostRequest : IRequest<CalculateOrderShippingCostResult>
{
    public string? ShippingBoxId { get; init; }
    public string? DestinationPostCode { get; init; }
}

public class CalculateOrderShippingCostHandler : IRequestHandler<CalculateOrderShippingCostRequest, CalculateOrderShippingCostResult>
{
    private readonly IQueryContext _context;
    private readonly IShippingCostService _shippingCostService;

    public CalculateOrderShippingCostHandler(
        IQueryContext context,
        IShippingCostService shippingCostService)
    {
        _context = context;
        _shippingCostService = shippingCostService;
    }

    public async Task<CalculateOrderShippingCostResult> Handle(
        CalculateOrderShippingCostRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingBoxId))
        {
            return new CalculateOrderShippingCostResult { Data = 0m };
        }

        var shippingBox = await _context.ShippingBox
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ShippingBoxId, cancellationToken);

        if (shippingBox == null)
        {
            throw new Exception($"ShippingBox not found: {request.ShippingBoxId}");
        }

        var shippingCost = await _shippingCostService.CalculateAsync(
            shippingBox,
            request.DestinationPostCode,
            cancellationToken);

        return new CalculateOrderShippingCostResult { Data = shippingCost };
    }
}
