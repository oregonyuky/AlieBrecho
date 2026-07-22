using Application.Common.Repositories;
using ASPNET.BackEnd.Hubs;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ASPNET.BackEnd;

public sealed class BagReservationExpirationService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<CatalogNotificationsHub> _catalogNotifications;
    private readonly ILogger<BagReservationExpirationService> _logger;

    public BagReservationExpirationService(
        IServiceScopeFactory scopeFactory,
        IHubContext<CatalogNotificationsHub> catalogNotifications,
        ILogger<BagReservationExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _catalogNotifications = catalogNotifications;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao liberar reservas vencidas de sacolinhas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task ReleaseExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var bagItemRepository = scope.ServiceProvider.GetRequiredService<ICommandRepository<BagItem>>();
        var bagRepository = scope.ServiceProvider.GetRequiredService<ICommandRepository<Bag>>();
        var productRepository = scope.ServiceProvider.GetRequiredService<ICommandRepository<Product>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var utcNow = DateTime.UtcNow;

        var expiredItems = await bagItemRepository.GetQuery()
            .Where(x => !x.IsDeleted && !x.IsPaid && x.IsReserved &&
                x.ReservationExpiresAt.HasValue && x.ReservationExpiresAt.Value <= utcNow)
            .ToListAsync(cancellationToken);
        if (expiredItems.Count == 0)
        {
            return;
        }

        foreach (var item in expiredItems)
        {
            item.IsReserved = false;
            item.UpdatedAtUtc = utcNow;
        }

        var bagIds = expiredItems
            .Where(x => !string.IsNullOrWhiteSpace(x.BagId))
            .Select(x => x.BagId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var bags = await bagRepository.GetQuery()
            .Where(x => bagIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var bag in bags)
        {
            bag.CurrentPaymentId = null;
            bag.CurrentPaymentProvider = null;
            bag.CurrentPaymentQrCodeBase64 = null;
            bag.CurrentPaymentQrCode = null;
            bag.CurrentPaymentExpiresAt = null;
            bag.UpdatedAtUtc = utcNow;
        }

        await unitOfWork.SaveAsync(cancellationToken);

        var productIds = expiredItems
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var products = await productRepository.GetQuery()
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var productId in productIds)
        {
            var productAvailable = products.TryGetValue(productId, out var product)
                && product.ProductAvailable != false;
            await _catalogNotifications.Clients.All.SendAsync(
                "ProductChanged",
                new
                {
                    changeType = "reservation-expired",
                    productId,
                    productAvailable,
                    reserved = false,
                    changedAt = utcNow
                },
                cancellationToken);
        }
    }
}
