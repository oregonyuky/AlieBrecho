using Application.Features.OrderManager.Commands;
using Application.Features.OrderManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class OrderController : BaseApiController
{
    private readonly IHubContext<OrderNotificationsHub> _orderNotifications;

    public OrderController(
        ISender sender,
        IHubContext<OrderNotificationsHub> orderNotifications) : base(sender)
    {
        _orderNotifications = orderNotifications;
    }

    [Authorize]
    [HttpPost("CreateOrder")]
    public async Task<ActionResult<ApiSuccessResult<CreateOrderResult>>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyOrderChangedAsync("created", response.Data?.Id, response.Data?.Status.ToString(), cancellationToken);

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
    [HttpGet("GetMelhorEnvioBalance")]
    public async Task<ActionResult<ApiSuccessResult<GetMelhorEnvioBalanceResult>>> GetMelhorEnvioBalanceAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetMelhorEnvioBalanceRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<GetMelhorEnvioBalanceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetMelhorEnvioBalanceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("InsertMelhorEnvioBalance")]
    public async Task<ActionResult<ApiSuccessResult<InsertMelhorEnvioBalanceResult>>> InsertMelhorEnvioBalanceAsync(
        InsertMelhorEnvioBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<InsertMelhorEnvioBalanceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(InsertMelhorEnvioBalanceAsync)}",
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
        await NotifyOrderChangedAsync("updated", response.Data?.Id, response.Data?.Status.ToString(), cancellationToken);

        return Ok(new ApiSuccessResult<UpdateOrderResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateOrderAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("GenerateShippingLabel")]
    public async Task<ActionResult<ApiSuccessResult<GenerateShippingLabelResult>>> GenerateShippingLabelAsync(
        GenerateShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyOrderChangedAsync("shipping-label-created", request.OrderId, null, cancellationToken);

        return Ok(new ApiSuccessResult<GenerateShippingLabelResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GenerateShippingLabelAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("DownloadShippingLabel")]
    public async Task<IActionResult> DownloadShippingLabelAsync(
        CancellationToken cancellationToken,
        [FromQuery] string labelId)
    {
        var request = new DownloadShippingLabelRequest { LabelId = labelId };
        var response = await _sender.Send(request, cancellationToken);

        return File(response.Data, "application/pdf", $"etiqueta-{labelId}.pdf");
    }

    [Authorize]
    [HttpPost("MarkShippingCart")]
    public async Task<ActionResult<ApiSuccessResult<MarkShippingCartResult>>> MarkShippingCartAsync(
        MarkShippingCartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyOrderChangedAsync("shipping-cart-marked", request.OrderId, null, cancellationToken);

        return Ok(new ApiSuccessResult<MarkShippingCartResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(MarkShippingCartAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("BuyShippingCart")]
    public async Task<ActionResult<ApiSuccessResult<BuyShippingCartResult>>> BuyShippingCartAsync(
        BuyShippingCartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyOrderChangedAsync("shipping-cart-bought", request.OrderId, null, cancellationToken);

        return Ok(new ApiSuccessResult<BuyShippingCartResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(BuyShippingCartAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("GeneratePurchasedShippingLabel")]
    public async Task<ActionResult<ApiSuccessResult<GeneratePurchasedShippingLabelResult>>> GeneratePurchasedShippingLabelAsync(
        GeneratePurchasedShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyOrderChangedAsync("shipping-label-generated", request.OrderId, null, cancellationToken);

        return Ok(new ApiSuccessResult<GeneratePurchasedShippingLabelResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GeneratePurchasedShippingLabelAsync)}",
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
        await NotifyOrderChangedAsync("deleted", request.Id, null, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteOrderResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteOrderAsync)}",
            Content = response
        });
    }

    private Task NotifyOrderChangedAsync(
        string changeType,
        string? orderId,
        string? status,
        CancellationToken cancellationToken)
    {
        return _orderNotifications.Clients.All.SendAsync(
            "OrderChanged",
            new
            {
                changeType,
                orderId,
                status,
                changedAt = DateTime.UtcNow
            },
            cancellationToken);
    }
}
