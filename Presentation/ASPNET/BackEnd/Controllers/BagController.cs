using Application.Features.BagManager.Commands;
using Application.Features.BagManager.Queries;
using Application.Common.Repositories;
using Application.Common.Services.MercadoPagoManager;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.MercadoPagoManager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BagController : BaseApiController
{
    private readonly ICommandRepository<Bag> _bagRepository;
    private readonly ICommandRepository<BagItem> _bagItemRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly MercadoPagoSettings _mercadoPagoSettings;

    public BagController(
        ISender sender,
        ICommandRepository<Bag> bagRepository,
        ICommandRepository<BagItem> bagItemRepository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMercadoPagoService mercadoPagoService,
        IOptions<MercadoPagoSettings> mercadoPagoSettings) : base(sender)
    {
        _bagRepository = bagRepository;
        _bagItemRepository = bagItemRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mercadoPagoService = mercadoPagoService;
        _mercadoPagoSettings = mercadoPagoSettings.Value;
    }

    [Authorize]
    [HttpGet("GetBagList")]
    public async Task<ActionResult<ApiSuccessResult<GetBagListResult>>> GetBagListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetBagListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBagListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBagListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBagSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetBagSingleResult>>> GetBagSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id)
    {
        var request = new GetBagSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBagSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBagSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBag")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBagResult>>> UpdateBagAsync(
        UpdateBagRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBagResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBagAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetActiveBag")]
    public async Task<ActionResult<ApiSuccessResult<GetActiveBagResult>>> GetActiveBagAsync(
        CancellationToken cancellationToken,
        [FromQuery] string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Cliente nao informado.");
        }

        var bag = await _bagRepository.GetQuery()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId && x.Status == BagStatus.Active && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new ApiSuccessResult<GetActiveBagResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetActiveBagAsync)}",
            Content = new GetActiveBagResult { Data = bag is null ? null : MapBag(bag) }
        });
    }

    [Authorize]
    [HttpPost("CheckoutBag")]
    public async Task<ActionResult<ApiSuccessResult<CheckoutBagResponse>>> CheckoutBagAsync(
        CheckoutBagRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return BadRequest("Cliente nao informado.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("Nenhum item informado para a sacolinha.");
        }

        var bag = await _bagRepository.GetQuery()
            .Where(x => x.CustomerId == request.CustomerId && x.Status == BagStatus.Active && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isNewBag = bag is null;
        if (bag is null)
        {
            bag = new Bag
            {
                CustomerId = request.CustomerId,
                Status = BagStatus.Active,
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(2),
                LastInteractionAt = DateTime.UtcNow
            };

            await _bagRepository.CreateAsync(bag, cancellationToken);
        }

        var checkoutItemsValue = 0m;
        var checkoutItemsWeight = 0m;
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                continue;
            }

            var product = await _productRepository.GetQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == item.ProductId && !x.IsDeleted, cancellationToken);

            if (product is null)
            {
                return NotFound($"Produto nao encontrado: {item.ProductId}");
            }

            var quantity = item.Quantity < 1 ? 1 : item.Quantity;
            var price = item.UnitPrice ?? product.UnitPrice ?? 0m;
            var weight = product.UnitWeight ?? 0m;

            checkoutItemsValue += price * quantity;
            checkoutItemsWeight += weight * quantity;

            await _bagItemRepository.CreateAsync(new BagItem
            {
                BagId = bag.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                Price = price,
                Weight = weight,
                IsPaid = false,
                IsReserved = true,
                ReservationExpiresAt = DateTime.UtcNow.AddMinutes(30),
                AddedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await ApplyBagTotalsAsync(bag, cancellationToken, checkoutItemsValue, checkoutItemsWeight);
        bag.LastInteractionAt = DateTime.UtcNow;
        bag.Notes = request.Notes;

        bag.UpdatedAtUtc = isNewBag ? bag.UpdatedAtUtc : DateTime.UtcNow;

        await _unitOfWork.SaveAsync(cancellationToken);

        var amount = Math.Round(checkoutItemsValue, 2);
        if (amount <= 0)
        {
            amount = Math.Round(bag.TotalItemsValue, 2);
        }

        var payer = BuildPayer(request);
        var validation = ValidatePayer(payer);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var expiration = DateTime.UtcNow.AddMinutes(Math.Max(_mercadoPagoSettings.ExpirationMinutes, 5));
        var payment = await _mercadoPagoService.CreatePixPaymentAsync(
            new MercadoPagoPixPaymentRequest
            {
                TransactionAmount = amount,
                Description = $"Sacolinha {bag.Id}",
                ExternalReference = bag.Id,
                DateOfExpiration = expiration,
                Payer = payer
            },
            cancellationToken);

        return Ok(new ApiSuccessResult<CheckoutBagResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CheckoutBagAsync)}",
            Content = new CheckoutBagResponse
            {
                Id = bag.Id,
                BagId = bag.Id,
                PaymentId = payment.PaymentId,
                PixQrCodeBase64 = payment.QrCodeBase64,
                PixCode = payment.QrCode
            }
        });
    }

    [Authorize]
    [HttpPost("FinalizeBag")]
    public async Task<ActionResult<ApiSuccessResult<FinalizeBagResponse>>> FinalizeBagAsync(
        FinalizeBagRequest request,
        CancellationToken cancellationToken)
    {
        var bag = await _bagRepository.GetQuery()
            .SingleOrDefaultAsync(x => x.Id == request.BagId && !x.IsDeleted, cancellationToken);

        if (bag is null)
        {
            return NotFound("Sacolinha nao encontrada.");
        }

        await ApplyBagTotalsAsync(bag, cancellationToken);
        bag.ShippingCost = CalculateBagShipping(bag);
        bag.Status = BagStatus.Shipped;
        bag.ClosedAt = DateTime.UtcNow;
        bag.LastInteractionAt = DateTime.UtcNow;
        bag.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(new ApiSuccessResult<FinalizeBagResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(FinalizeBagAsync)}",
            Content = new FinalizeBagResponse
            {
                Id = bag.Id,
                BagId = bag.Id,
                Status = bag.Status.ToString(),
                ShippingCost = bag.ShippingCost,
                TotalAmount = bag.TotalItemsValue + (bag.ShippingCost ?? 0m)
            }
        });
    }

    private async Task ApplyBagTotalsAsync(
        Bag bag,
        CancellationToken cancellationToken,
        decimal pendingItemsValue = 0m,
        decimal pendingItemsWeight = 0m)
    {
        var persistedItems = await _bagItemRepository.GetQuery()
            .AsNoTracking()
            .Where(x => x.BagId == bag.Id && !x.IsDeleted)
            .Select(x => new { x.Price, x.Quantity, x.Weight })
            .ToListAsync(cancellationToken);

        bag.TotalItemsValue = persistedItems.Sum(x => x.Price * x.Quantity) + pendingItemsValue;
        bag.TotalWeight = persistedItems.Sum(x => x.Weight * x.Quantity) + pendingItemsWeight;
    }

    private static decimal CalculateBagShipping(Bag bag)
    {
        var weightCost = Math.Max(bag.TotalWeight, 0.3m) * 10m;
        var insurance = bag.TotalItemsValue * 0.01m;
        return Math.Round(weightCost + insurance, 2);
    }

    private static BagSummaryResponse MapBag(Bag bag)
    {
        return new BagSummaryResponse
        {
            Id = bag.Id,
            Status = bag.Status.ToString(),
            ExpirationDate = bag.ExpirationDate,
            TotalItemsValue = bag.TotalItemsValue,
            ShippingCost = bag.ShippingCost,
            ItemCount = bag.Items?.Where(x => !x.IsDeleted).Sum(x => x.Quantity) ?? 0
        };
    }

    private static MercadoPagoPayerRequest BuildPayer(CheckoutBagRequest request)
    {
        var shipping = request.ShippingDetail;
        return new MercadoPagoPayerRequest
        {
            Email = shipping?.Email,
            FirstName = shipping?.FirstName,
            LastName = shipping?.LastName,
            IdentificationType = "CPF",
            IdentificationNumber = OnlyDigits(request.PayerCpf),
            Address = new MercadoPagoAddressRequest
            {
                ZipCode = OnlyDigits(shipping?.PostCode),
                StreetName = shipping?.Street,
                StreetNumber = shipping?.Number,
                Neighborhood = shipping?.Neighborhood,
                City = shipping?.City,
                FederalUnit = shipping?.State
            }
        };
    }

    private static string? ValidatePayer(MercadoPagoPayerRequest payer)
    {
        if (string.IsNullOrWhiteSpace(payer.Email))
        {
            return "Informe o e-mail do comprador.";
        }

        if (string.IsNullOrWhiteSpace(payer.FirstName))
        {
            return "Informe o nome do comprador.";
        }

        if (string.IsNullOrWhiteSpace(payer.IdentificationNumber) || payer.IdentificationNumber.Length != 11)
        {
            return "Informe um CPF valido para gerar o Pix da sacolinha.";
        }

        return null;
    }

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}

public sealed record GetActiveBagResult
{
    public BagSummaryResponse? Data { get; init; }
}

public sealed record BagSummaryResponse
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public decimal TotalItemsValue { get; init; }
    public decimal? ShippingCost { get; init; }
    public int ItemCount { get; init; }
}

public sealed record CheckoutBagRequest
{
    public string? CustomerId { get; init; }
    public string? Notes { get; init; }
    public string? PayerCpf { get; init; }
    public CheckoutBagShippingDetailRequest? ShippingDetail { get; init; }
    public List<CheckoutBagItemRequest> Items { get; init; } = [];
}

public sealed record CheckoutBagShippingDetailRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostCode { get; init; }
}

public sealed record CheckoutBagItemRequest
{
    public string? ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
}

public sealed record CheckoutBagResponse
{
    public string? Id { get; init; }
    public string? BagId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? PixQrCodeBase64 { get; init; }
    public string? PixQrCode { get; init; }
    public string? PixCode { get; init; }
    public string? PaymentId { get; init; }
}

public sealed record FinalizeBagRequest
{
    public string? BagId { get; init; }
}

public sealed record FinalizeBagResponse
{
    public string? Id { get; init; }
    public string? BagId { get; init; }
    public string? Status { get; init; }
    public decimal? ShippingCost { get; init; }
    public decimal? TotalAmount { get; init; }
}
