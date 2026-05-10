using Application.Features.OrderManager.Commands;
using Application.Features.OrderManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class OrderController : BaseApiController
{
    public OrderController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateOrder")]
    public async Task<ActionResult<ApiSuccessResult<CreateOrderResult>>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateOrderResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateOrderAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetOrderList")]
    public async Task<ActionResult<ApiSuccessResult<GetOrderListResult>>> GetOrderListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetOrderListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetOrderListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetOrderListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetOrderSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetOrderSingleResult>>> GetOrderSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id)
    {
        var request = new GetOrderSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetOrderSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetOrderSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("CalculateShippingCost")]
    public async Task<ActionResult<ApiSuccessResult<CalculateOrderShippingCostResult>>> CalculateShippingCostAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? shippingBoxId,
        [FromQuery] string? destinationPostCode)
    {
        var request = new CalculateOrderShippingCostRequest
        {
            ShippingBoxId = shippingBoxId,
            DestinationPostCode = destinationPostCode
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CalculateOrderShippingCostResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CalculateShippingCostAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateOrder")]
    public async Task<ActionResult<ApiSuccessResult<UpdateOrderResult>>> UpdateOrderAsync(
        UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateOrderResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateOrderAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteOrder")]
    public async Task<ActionResult<ApiSuccessResult<DeleteOrderResult>>> DeleteOrderAsync(
        DeleteOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteOrderResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteOrderAsync)}",
            Content = response
        });
    }
}
