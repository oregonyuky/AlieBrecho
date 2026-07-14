using Application.Features.ProductManager.Commands;
using Application.Features.ProductManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProductController : BaseApiController
{
    private readonly IHubContext<CatalogNotificationsHub> _catalogHubContext;

    public ProductController(
        ISender sender,
        IHubContext<CatalogNotificationsHub> catalogHubContext) : base(sender)
    {
        _catalogHubContext = catalogHubContext;
    }

    [Authorize]
    [HttpPost("CreateProduct")]
    public async Task<ActionResult<ApiSuccessResult<CreateProductResult>>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyProductChangedAsync("created", response.Data, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProduct")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProductResult>>> UpdateProductAsync(
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyProductChangedAsync("updated", response.Data, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProduct")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProductResult>>> DeleteProductAsync(
        DeleteProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyProductChangedAsync("deleted", response.Data, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProductSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetProductSingleResult>>> GetProductSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id
        )
    {
        var request = new GetProductSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductSingleAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpGet("GetProductList")]
    public async Task<ActionResult<ApiSuccessResult<GetProductListResult>>> GetProductListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool? availableOnly = null
        )
    {
        var request = new GetProductListRequest
        {
            IsDeleted = isDeleted,
            AvailableOnly = availableOnly ?? (User.Identity?.IsAuthenticated != true)
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductListAsync)}",
            Content = response
        });
    }

    private Task NotifyProductChangedAsync(
        string changeType,
        Domain.Entities.Product? product,
        CancellationToken cancellationToken)
    {
        return _catalogHubContext.Clients.All.SendAsync(
            "ProductChanged",
            new
            {
                changeType,
                productId = product?.Id,
                productAvailable = product?.ProductAvailable,
                unitPrice = product?.UnitPrice,
                oldPrice = product?.OldPrice,
                discountPercent = product?.DiscountPercent,
                changedAt = DateTime.UtcNow
            },
            cancellationToken);
    }
}
