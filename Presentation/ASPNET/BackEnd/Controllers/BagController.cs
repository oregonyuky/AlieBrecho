using Application.Features.BagManager.Commands;
using Application.Features.BagManager.Queries;
using Application.Common;
using Application.Common.Repositories;
using Application.Common.Services.MercadoPagoManager;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Hubs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.MercadoPagoManager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BagController : BaseApiController
{
    private readonly ICommandRepository<Bag> _bagRepository;
    private readonly ICommandRepository<BagItem> _bagItemRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<BagSettings> _bagSettingsRepository;
    private readonly ICommandRepository<BagExpirationHistory> _bagExpirationHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly MercadoPagoSettings _mercadoPagoSettings;
    private readonly IHubContext<OrderNotificationsHub> _orderNotifications;
    private readonly IHubContext<CatalogNotificationsHub> _catalogNotifications;

    public BagController(
        ISender sender,
        ICommandRepository<Bag> bagRepository,
        ICommandRepository<BagItem> bagItemRepository,
        ICommandRepository<Product> productRepository,
        ICommandRepository<BagSettings> bagSettingsRepository,
        ICommandRepository<BagExpirationHistory> bagExpirationHistoryRepository,
        IUnitOfWork unitOfWork,
        IMercadoPagoService mercadoPagoService,
        IOptions<MercadoPagoSettings> mercadoPagoSettings,
        IHubContext<OrderNotificationsHub> orderNotifications,
        IHubContext<CatalogNotificationsHub> catalogNotifications) : base(sender)
    {
        _bagRepository = bagRepository;
        _bagItemRepository = bagItemRepository;
        _productRepository = productRepository;
        _bagSettingsRepository = bagSettingsRepository;
        _bagExpirationHistoryRepository = bagExpirationHistoryRepository;
        _unitOfWork = unitOfWork;
        _mercadoPagoService = mercadoPagoService;
        _mercadoPagoSettings = mercadoPagoSettings.Value;
        _orderNotifications = orderNotifications;
        _catalogNotifications = catalogNotifications;
    }

    [Authorize]
    [HttpGet("GetBagSettings")]
    public async Task<ActionResult<ApiSuccessResult<BagSettingsResponse>>> GetBagSettingsAsync(
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateBagSettingsAsync(cancellationToken);

        return Ok(new ApiSuccessResult<BagSettingsResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBagSettingsAsync)}",
            Content = MapBagSettings(settings)
        });
    }

    [Authorize]
    [HttpPost("UpdateBagSettings")]
    public async Task<ActionResult<ApiSuccessResult<BagSettingsResponse>>> UpdateBagSettingsAsync(
        UpdateBagSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidDurationUnit(request.DefaultDurationUnit)
            || !IsValidDurationUnit(request.ExtensionDurationUnit)
            || !IsValidDurationUnit(request.ExtensionResponseDeadlineUnit))
        {
            return BadRequest("Unidade de duracao invalida.");
        }

        var settings = await GetOrCreateBagSettingsAsync(cancellationToken);
        settings.DefaultDurationValue = Math.Max(request.DefaultDurationValue, 1);
        settings.DefaultDurationUnit = NormalizeDurationUnit(request.DefaultDurationUnit);
        settings.ExtensionDurationValue = Math.Max(request.ExtensionDurationValue, 1);
        settings.ExtensionDurationUnit = NormalizeDurationUnit(request.ExtensionDurationUnit);
        settings.ExtensionResponseDeadlineValue = Math.Max(request.ExtensionResponseDeadlineValue, 1);
        settings.ExtensionResponseDeadlineUnit = NormalizeDurationUnit(request.ExtensionResponseDeadlineUnit);
        settings.UpdatedAtUtc = DateTime.UtcNow;

        _bagSettingsRepository.Update(settings);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(new ApiSuccessResult<BagSettingsResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBagSettingsAsync)}",
            Content = MapBagSettings(settings)
        });
    }

    [Authorize]
    [HttpPost("UpdateBagExpiration")]
    public async Task<ActionResult<ApiSuccessResult<BagExpirationUpdateResponse>>> UpdateBagExpirationAsync(
        UpdateBagExpirationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BagId))
        {
            return BadRequest("Sacola nao informada.");
        }

        var bag = await _bagRepository.GetQuery()
            .Include(x => x.ExpirationHistory)
            .SingleOrDefaultAsync(x => x.Id == request.BagId && !x.IsDeleted, cancellationToken);

        if (bag is null)
        {
            return NotFound("Sacolinha nao encontrada.");
        }

        var oldExpirationDate = bag.ExpirationDate;
        var extensionBaseDate = oldExpirationDate > DateTime.UtcNow
            ? oldExpirationDate
            : DateTime.UtcNow;
        var newExpirationDate = request.NewExpirationDate
            ?? AddDuration(extensionBaseDate, request.AddValue ?? 0, request.AddUnit);

        if (newExpirationDate <= DateTime.UtcNow)
        {
            return BadRequest("O novo prazo deve ser maior que a data atual.");
        }

        bag.ExpirationDate = newExpirationDate;
        bag.LastInteractionAt = DateTime.UtcNow;
        bag.UpdatedAtUtc = DateTime.UtcNow;

        var history = new BagExpirationHistory
        {
            BagId = bag.Id,
            OldExpirationDate = oldExpirationDate,
            NewExpirationDate = newExpirationDate,
            ChangedBy = GetCurrentUserLabel(),
            ChangedAtUtc = DateTime.UtcNow,
            Note = request.Note
        };

        await _bagExpirationHistoryRepository.CreateAsync(history, cancellationToken);
        _bagRepository.Update(bag);
        await _unitOfWork.SaveAsync(cancellationToken);
        await NotifyBagChangedAsync("expiration-updated", bag.Id, bag.Status.ToString(), cancellationToken);

        return Ok(new ApiSuccessResult<BagExpirationUpdateResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBagExpirationAsync)}",
            Content = new BagExpirationUpdateResponse
            {
                BagId = bag.Id,
                ExpirationDate = bag.ExpirationDate,
                History = MapExpirationHistory((bag.ExpirationHistory ?? []).Append(history))
            }
        });
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
        var changeType = request.IsDeleted == true ? "deleted" : "updated";
        await NotifyBagChangedAsync(changeType, response.Data?.Id, response.Data?.Status.ToString(), cancellationToken);
        if (request.IsDeleted == true)
        {
            await NotifyBagProductsAvailableAsync(response.Data, cancellationToken);
        }

        return Ok(new ApiSuccessResult<UpdateBagResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBagAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
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

        var summary = bag is null
            ? null
            : await MapBagAsync(bag, cancellationToken);

        return Ok(new ApiSuccessResult<GetActiveBagResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetActiveBagAsync)}",
            Content = new GetActiveBagResult { Data = summary }
        });
    }

    [Authorize]
    [HttpGet("GetPurchaseHistory")]
    public async Task<ActionResult<ApiSuccessResult<List<BagPurchaseHistoryResponse>>>> GetPurchaseHistoryAsync(
        [FromQuery] string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Cliente nao informado.");
        }

        var bags = await _bagRepository.GetQuery()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId && !x.IsDeleted &&
                x.Items != null && x.Items.Any(item => !item.IsDeleted && item.IsPaid))
            .OrderByDescending(x => x.Items!.Max(item => item.PaidAt))
            .ToListAsync(cancellationToken);

        var productIds = bags
            .SelectMany(x => x.Items ?? [])
            .Where(x => x.IsPaid && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct()
            .ToList();
        var productImages = await _productRepository.GetQuery()
            .AsNoTracking()
            .Where(x => x.Id != null && productIds.Contains(x.Id))
            .Select(x => new { x.Id, ImageUrl = x.MainImageURL ?? x.Picture1 })
            .ToDictionaryAsync(x => x.Id!, x => x.ImageUrl, cancellationToken);

        var history = bags.Select(bag =>
        {
            var items = (bag.Items ?? [])
                .Where(x => x.IsPaid && !x.IsDeleted)
                .OrderByDescending(x => x.PaidAt)
                .Select(x => new BagPurchaseItemResponse
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    ProductImageUrl = !string.IsNullOrWhiteSpace(x.ProductId) && productImages.TryGetValue(x.ProductId, out var image)
                        ? image
                        : null,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    IsPaid = true,
                    PaidAt = x.PaidAt
                })
                .ToList();

            return new BagPurchaseHistoryResponse
            {
                Id = bag.Id,
                Status = "Paid",
                PaidAt = items.Max(x => x.PaidAt),
                TotalItemsValue = items.Sum(x => x.Price * x.Quantity),
                ShippingCost = bag.Status == BagStatus.Shipped ? bag.ShippingCost : null,
                Items = items
            };
        }).ToList();

        return Ok(new ApiSuccessResult<List<BagPurchaseHistoryResponse>>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseHistoryAsync)}",
            Content = history
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

        CheckoutReservationResult reservation;
        try
        {
            reservation = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var utcNow = DateTime.UtcNow;
                var bag = await _bagRepository.GetQuery()
                    .Where(x => x.CustomerId == request.CustomerId && x.Status == BagStatus.Active && !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var isNewBag = bag is null;
                if (bag is null)
                {
                    var settings = await GetOrCreateBagSettingsAsync(cancellationToken);
                    bag = new Bag
                    {
                        CustomerId = request.CustomerId,
                        Status = BagStatus.Active,
                        CreatedAt = utcNow,
                        ExpirationDate = AddDuration(utcNow, settings.DefaultDurationValue, settings.DefaultDurationUnit),
                        LastInteractionAt = utcNow
                    };

                    await _bagRepository.CreateAsync(bag, cancellationToken);
                }

                var productIds = request.Items
                    .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
                    .Select(x => x.ProductId!)
                    .ToList();
                if (productIds.Count != productIds.Distinct(StringComparer.Ordinal).Count())
                {
                    throw new ProductUnavailableException("Este produto ja esta reservado nesta sacolinha.");
                }

                var expiredReservations = await _bagItemRepository.GetQuery()
                    .Where(x => productIds.Contains(x.ProductId!) && !x.IsDeleted && !x.IsPaid && x.IsReserved &&
                        x.ReservationExpiresAt.HasValue && x.ReservationExpiresAt.Value <= utcNow)
                    .ToListAsync(cancellationToken);
                if (expiredReservations.Count > 0)
                {
                    foreach (var expired in expiredReservations)
                    {
                        expired.IsReserved = false;
                        expired.UpdatedAtUtc = utcNow;
                    }
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                var checkoutItemsValue = 0m;
                var checkoutItemsWeight = 0m;
                var reservedProductIds = new List<string>();
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
                        throw new KeyNotFoundException($"Produto nao encontrado: {item.ProductId}");
                    }

                    if (product.ProductAvailable == false)
                    {
                        throw new ProductUnavailableException();
                    }

                    var reservedByAnotherBag = await _bagItemRepository.GetQuery()
                        .AsNoTracking()
                        .AnyAsync(x => x.ProductId == product.Id && x.BagId != bag.Id && !x.IsDeleted && !x.IsPaid &&
                            x.IsReserved && (!x.ReservationExpiresAt.HasValue || x.ReservationExpiresAt.Value > utcNow),
                            cancellationToken);
                    var reservedByThisBag = await _bagItemRepository.GetQuery()
                        .AsNoTracking()
                        .AnyAsync(x => x.ProductId == product.Id && x.BagId == bag.Id && !x.IsDeleted && !x.IsPaid &&
                            x.IsReserved && (!x.ReservationExpiresAt.HasValue || x.ReservationExpiresAt.Value > utcNow),
                            cancellationToken);
                    if (reservedByAnotherBag || reservedByThisBag)
                    {
                        throw new ProductUnavailableException(reservedByThisBag
                            ? "Este produto ja esta reservado nesta sacolinha."
                            : "Este produto acabou de ser reservado por outro cliente.");
                    }

                    var quantity = item.Quantity < 1 ? 1 : item.Quantity;
                    var price = item.UnitPrice ?? product.UnitPrice ?? 0m;
                    var weight = product.UnitWeight ?? 0m;

                    checkoutItemsValue += price * quantity;
                    checkoutItemsWeight += weight * quantity;
                    reservedProductIds.Add(product.Id);

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
                        ReservationExpiresAt = utcNow.AddMinutes(30),
                        AddedAt = utcNow
                    }, cancellationToken);
                }

                await ApplyBagTotalsAsync(bag, cancellationToken, checkoutItemsValue, checkoutItemsWeight);
                bag.LastInteractionAt = utcNow;
                bag.Notes = request.Notes;
                bag.UpdatedAtUtc = isNewBag ? bag.UpdatedAtUtc : utcNow;

                await _unitOfWork.SaveAsync(cancellationToken);
                return new CheckoutReservationResult(bag, isNewBag, checkoutItemsValue, reservedProductIds);
            }, IsolationLevel.Serializable, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ProductUnavailableException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex) when (IsReservationConcurrencyFailure(ex))
        {
            return Conflict("Este produto acabou de ser reservado por outro cliente.");
        }

        var bag = reservation.Bag;
        var isNewBag = reservation.IsNewBag;
        var checkoutItemsValue = reservation.CheckoutItemsValue;
        await NotifyBagChangedAsync(isNewBag ? "created" : "checkout-updated", bag.Id, bag.Status.ToString(), cancellationToken);
        await NotifyProductsReservedAsync(reservation.ProductIds, cancellationToken);

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

        bag.CurrentPaymentId = payment.PaymentId;
        bag.CurrentPaymentProvider = "MercadoPago";
        bag.CurrentPaymentQrCodeBase64 = payment.QrCodeBase64;
        bag.CurrentPaymentQrCode = payment.QrCode;
        bag.CurrentPaymentExpiresAt = payment.DateOfExpiration ?? expiration;
        _bagRepository.Update(bag);
        await _unitOfWork.SaveAsync(cancellationToken);

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

    private sealed record CheckoutReservationResult(
        Bag Bag,
        bool IsNewBag,
        decimal CheckoutItemsValue,
        IReadOnlyCollection<string> ProductIds);

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
        await NotifyBagChangedAsync("finalized", bag.Id, bag.Status.ToString(), cancellationToken);

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

    private async Task<BagSettings> GetOrCreateBagSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _bagSettingsRepository.GetQuery()
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => !x.IsDeleted, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new BagSettings
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        await _bagSettingsRepository.CreateAsync(settings, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return settings;
    }

    private static BagSettingsResponse MapBagSettings(BagSettings settings)
    {
        return new BagSettingsResponse
        {
            Id = settings.Id,
            DefaultDurationValue = settings.DefaultDurationValue,
            DefaultDurationUnit = settings.DefaultDurationUnit,
            ExtensionDurationValue = settings.ExtensionDurationValue,
            ExtensionDurationUnit = settings.ExtensionDurationUnit,
            ExtensionResponseDeadlineValue = settings.ExtensionResponseDeadlineValue,
            ExtensionResponseDeadlineUnit = settings.ExtensionResponseDeadlineUnit
        };
    }

    private static bool IsValidDurationUnit(string? unit)
    {
        return string.Equals(unit, "minutes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(unit, "days", StringComparison.OrdinalIgnoreCase)
            || string.Equals(unit, "months", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDurationUnit(string? unit)
    {
        if (string.Equals(unit, "months", StringComparison.OrdinalIgnoreCase))
        {
            return "months";
        }

        return string.Equals(unit, "minutes", StringComparison.OrdinalIgnoreCase)
            ? "minutes"
            : "days";
    }

    private static DateTime AddDuration(DateTime date, int value, string? unit)
    {
        var safeValue = Math.Max(value, 1);
        if (string.Equals(unit, "months", StringComparison.OrdinalIgnoreCase))
        {
            return date.AddMonths(safeValue);
        }

        return string.Equals(unit, "minutes", StringComparison.OrdinalIgnoreCase)
            ? date.AddMinutes(safeValue)
            : date.AddDays(safeValue);
    }

    private string GetCurrentUserLabel()
    {
        return User.FindFirst("email")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.Identity?.Name
            ?? "admin";
    }

    private static List<BagExpirationHistoryResponse> MapExpirationHistory(IEnumerable<BagExpirationHistory> history)
    {
        return history
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.ChangedAtUtc)
            .Select(x => new BagExpirationHistoryResponse
            {
                Id = x.Id,
                OldExpirationDate = x.OldExpirationDate,
                NewExpirationDate = x.NewExpirationDate,
                ChangedBy = x.ChangedBy,
                ChangedAtUtc = x.ChangedAtUtc,
                Note = x.Note
            })
            .ToList();
    }

    private Task NotifyBagChangedAsync(
        string changeType,
        string? bagId,
        string? status,
        CancellationToken cancellationToken)
    {
        return _orderNotifications.Clients.All.SendAsync(
            "BagChanged",
            new
            {
                changeType,
                bagId,
                status,
                isDeleted = string.Equals(changeType, "deleted", StringComparison.OrdinalIgnoreCase),
                changedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private async Task NotifyBagProductsAvailableAsync(Bag? bag, CancellationToken cancellationToken)
    {
        var productIds = (bag?.Items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _productRepository.GetQuery()
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            await _catalogNotifications.Clients.All.SendAsync(
                "ProductChanged",
                new
                {
                    changeType = "bag-deleted",
                    productId = product.Id,
                    productAvailable = product.ProductAvailable,
                    unitPrice = product.UnitPrice,
                    oldPrice = product.OldPrice,
                    discountPercent = product.DiscountPercent,
                    changedAt = DateTime.UtcNow
                },
                cancellationToken);
        }
    }

    private async Task NotifyProductsReservedAsync(
        IEnumerable<string> productIds,
        CancellationToken cancellationToken)
    {
        foreach (var productId in productIds.Distinct(StringComparer.Ordinal))
        {
            await _catalogNotifications.Clients.All.SendAsync(
                "ProductChanged",
                new
                {
                    changeType = "reserved",
                    productId,
                    productAvailable = false,
                    reserved = true,
                    changedAt = DateTime.UtcNow
                },
                cancellationToken);
        }
    }

    private static bool IsReservationConcurrencyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var message = current.Message;
            if (message.Contains("UX_BagItem_ActiveReservation_ProductId", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2601", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2627", StringComparison.OrdinalIgnoreCase)
                || message.Contains("1205", StringComparison.OrdinalIgnoreCase)
                || message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
                || message.Contains("database is locked", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<BagSummaryResponse> MapBagAsync(Bag bag, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var items = (bag.Items ?? [])
            .Where(x => !x.IsDeleted)
            .ToList();
        var productIds = items
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var productImages = await _productRepository.GetQuery()
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .Select(x => new { x.Id, ImageUrl = x.MainImageURL ?? x.Picture1 })
            .ToDictionaryAsync(x => x.Id, x => x.ImageUrl, cancellationToken);
        var paidItemCount = items.Where(x => x.IsPaid).Sum(x => x.Quantity);
        var pendingItemCount = items
            .Where(x => !x.IsPaid && x.IsReserved &&
                (!x.ReservationExpiresAt.HasValue || x.ReservationExpiresAt.Value > utcNow))
            .Sum(x => x.Quantity);
        var hasExpiredReservation = pendingItemCount == 0 && items.Any(x =>
            !x.IsPaid && x.ReservationExpiresAt.HasValue && x.ReservationExpiresAt.Value <= utcNow);
        var paymentIsOpen = !bag.AllItemsPaid
            && !string.IsNullOrWhiteSpace(bag.CurrentPaymentId)
            && (!bag.CurrentPaymentExpiresAt.HasValue || bag.CurrentPaymentExpiresAt.Value > utcNow);

        return new BagSummaryResponse
        {
            Id = bag.Id,
            Status = hasExpiredReservation ? "ReservationExpired" : bag.Status.ToString(),
            ExpirationDate = bag.ExpirationDate,
            TotalItemsValue = bag.TotalItemsValue,
            ShippingCost = bag.ShippingCost,
            ItemCount = paidItemCount + pendingItemCount,
            PaidItemCount = paidItemCount,
            PendingItemCount = pendingItemCount,
            Items = items.Select(item => new BagItemSummaryResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductImageUrl = !string.IsNullOrWhiteSpace(item.ProductId)
                    && productImages.TryGetValue(item.ProductId, out var imageUrl)
                        ? imageUrl
                        : null,
                Quantity = item.Quantity,
                Price = item.Price,
                IsPaid = item.IsPaid,
                IsReserved = item.IsReserved,
                ReservationExpiresAt = item.ReservationExpiresAt,
                PaidAt = item.PaidAt
            }).ToList(),
            CurrentPaymentId = paymentIsOpen ? bag.CurrentPaymentId : null,
            CurrentPaymentProvider = paymentIsOpen ? bag.CurrentPaymentProvider : null,
            CurrentPaymentQrCodeBase64 = paymentIsOpen ? bag.CurrentPaymentQrCodeBase64 : null,
            CurrentPaymentQrCode = paymentIsOpen ? bag.CurrentPaymentQrCode : null,
            CurrentPaymentExpiresAt = paymentIsOpen ? bag.CurrentPaymentExpiresAt : null
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
    public int PaidItemCount { get; init; }
    public int PendingItemCount { get; init; }
    public List<BagItemSummaryResponse> Items { get; init; } = [];
    public string? CurrentPaymentId { get; init; }
    public string? CurrentPaymentProvider { get; init; }
    public string? CurrentPaymentQrCodeBase64 { get; init; }
    public string? CurrentPaymentQrCode { get; init; }
    public DateTime? CurrentPaymentExpiresAt { get; init; }
}

public sealed record BagItemSummaryResponse
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public bool IsPaid { get; init; }
    public bool IsReserved { get; init; }
    public DateTime? ReservationExpiresAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed record BagPurchaseHistoryResponse
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public decimal TotalItemsValue { get; init; }
    public decimal? ShippingCost { get; init; }
    public List<BagPurchaseItemResponse> Items { get; init; } = [];
}

public sealed record BagPurchaseItemResponse
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed record BagSettingsResponse
{
    public string? Id { get; init; }
    public int DefaultDurationValue { get; init; }
    public string? DefaultDurationUnit { get; init; }
    public int ExtensionDurationValue { get; init; }
    public string? ExtensionDurationUnit { get; init; }
    public int ExtensionResponseDeadlineValue { get; init; }
    public string? ExtensionResponseDeadlineUnit { get; init; }
}

public sealed record UpdateBagSettingsRequest
{
    public int DefaultDurationValue { get; init; }
    public string? DefaultDurationUnit { get; init; }
    public int ExtensionDurationValue { get; init; }
    public string? ExtensionDurationUnit { get; init; }
    public int ExtensionResponseDeadlineValue { get; init; }
    public string? ExtensionResponseDeadlineUnit { get; init; }
}

public sealed record UpdateBagExpirationRequest
{
    public string? BagId { get; init; }
    public DateTime? NewExpirationDate { get; init; }
    public int? AddValue { get; init; }
    public string? AddUnit { get; init; }
    public string? Note { get; init; }
}

public sealed record BagExpirationUpdateResponse
{
    public string? BagId { get; init; }
    public DateTime ExpirationDate { get; init; }
    public List<BagExpirationHistoryResponse> History { get; init; } = [];
}

public sealed record BagExpirationHistoryResponse
{
    public string? Id { get; init; }
    public DateTime OldExpirationDate { get; init; }
    public DateTime NewExpirationDate { get; init; }
    public string? ChangedBy { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string? Note { get; init; }
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
