using Application.Common.Repositories;
using Application.Features.OrderManager.Commands;
using Application.Features.OrderManager.Queries;
using Application.Common.Services.MelhorEnvioManager;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class OrderController : BaseApiController
{
    private readonly ICommandRepository<Order> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMelhorEnvioService _melhorEnvioService;

    public OrderController(
        ISender sender,
        ICommandRepository<Order> repository,
        IUnitOfWork unitOfWork,
        IMelhorEnvioService melhorEnvioService) : base(sender)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _melhorEnvioService = melhorEnvioService;
    }

    [Authorize]
    [HttpGet("GetOrderList")]
    public async Task<ActionResult<ApiSuccessResult<GetOrderListResult>>> GetOrderListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? status = null)
    {
        var request = new GetOrderListRequest { IsDeleted = isDeleted, Status = status };
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
    [HttpPost("GenerateShippingLabel")]
    public async Task<ActionResult<ApiSuccessResult<GenerateShippingLabelResult>>> GenerateShippingLabelAsync(
        GenerateShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetQuery().SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (order == null)
        {
            throw new Exception($"Order not found: {request.Id}");
        }

        order.Height = request.Height;
        order.Width = request.Width;
        order.Length = request.Length;
        order.Weight = request.Weight;
        order.Insurance_cost = request.InsuranceCost;

        var apiRequest = new
        {
            orderId = request.Id,
            height = request.Height,
            width = request.Width,
            length = request.Length,
            weight = request.Weight,
            insuranceCost = request.InsuranceCost,
            shippingPostCode = order.ShippingDetail?.PostCode
        };

        var apiResponse = await _melhorEnvioService.CriarEtiquetaAsync(apiRequest);
        var freight = ParseShippingCost(apiResponse);
        if (freight.HasValue)
        {
            order.ShippingCost = freight.Value;
        }

        _repository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(new ApiSuccessResult<GenerateShippingLabelResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GenerateShippingLabelAsync)}",
            Content = new GenerateShippingLabelResult
            {
                ShippingCost = order.ShippingCost,
                ApiResponse = apiResponse
            }
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

    private static decimal? ParseShippingCost(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(response);
            if (TryExtractShippingCost(document.RootElement, out var value))
            {
                return value;
            }
        }
        catch
        {
            // ignored
        }

        var cleaned = Regex.Match(response, @"[0-9]+(?:[.,][0-9]+)?");
        if (cleaned.Success && decimal.TryParse(cleaned.Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryExtractShippingCost(JsonElement element, out decimal value)
    {
        value = 0;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (IsShippingCostKey(property.Name) && TryParseDecimal(property.Value, out value))
                {
                    return true;
                }

                if (TryExtractShippingCost(property.Value, out value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryExtractShippingCost(item, out value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsShippingCostKey(string key)
    {
        return key.Equals("valor_frete", StringComparison.OrdinalIgnoreCase)
            || key.Equals("frete", StringComparison.OrdinalIgnoreCase)
            || key.Equals("shippingCost", StringComparison.OrdinalIgnoreCase)
            || key.Equals("shipping_cost", StringComparison.OrdinalIgnoreCase)
            || key.Equals("price", StringComparison.OrdinalIgnoreCase)
            || key.Equals("cost", StringComparison.OrdinalIgnoreCase)
            || key.Equals("total", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDecimal(JsonElement element, out decimal value)
    {
        value = 0;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return false;
    }
}

public record GenerateShippingLabelRequest
{
    public string? Id { get; init; }
    public decimal? Height { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceCost { get; init; }
}

public record GenerateShippingLabelResult
{
    public decimal? ShippingCost { get; init; }
    public string? ApiResponse { get; init; }
}
