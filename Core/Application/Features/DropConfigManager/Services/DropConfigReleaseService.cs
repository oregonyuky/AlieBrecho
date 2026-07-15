using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.DropConfigManager.Services;

public class DropConfigReleaseService : IDropConfigReleaseService
{
    private readonly ICommandRepository<DropConfig> _dropRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<OrderDetail> _orderDetailRepository;
    private readonly ICommandRepository<BagItem> _bagItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DropConfigReleaseService> _logger;

    public DropConfigReleaseService(
        ICommandRepository<DropConfig> dropRepository,
        ICommandRepository<Product> productRepository,
        ICommandRepository<OrderDetail> orderDetailRepository,
        ICommandRepository<BagItem> bagItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<DropConfigReleaseService> logger)
    {
        _dropRepository = dropRepository;
        _productRepository = productRepository;
        _orderDetailRepository = orderDetailRepository;
        _bagItemRepository = bagItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ReleaseProductsForDropAsync(
        DropConfig drop,
        DateTime utcNow,
        bool requireReleaseTime,
        string source,
        CancellationToken cancellationToken)
    {
        var dropId = drop.Id;
        var isDue = drop.DataLiberacao <= utcNow;

        _logger.LogInformation(
            "Verificando liberacao de drop. Source={Source}; DropId={DropId}; Ativo={Ativo}; DataLiberacaoUtc={DataLiberacaoUtc}; UtcNow={UtcNow}; IsDue={IsDue}; RequireReleaseTime={RequireReleaseTime}.",
            source,
            dropId,
            drop.Ativo,
            drop.DataLiberacao,
            utcNow,
            isDue,
            requireReleaseTime);

        if (!drop.Ativo || (requireReleaseTime && !isDue))
        {
            return 0;
        }

        var paidOrderProductIds = await _orderDetailRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.ProductId != null &&
                x.Order != null &&
                x.Order.Status != Domain.Enums.OrderStatus.Pending &&
                x.Order.Status != Domain.Enums.OrderStatus.Cancelled)
            .Select(x => x.ProductId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var paidBagProductIds = await _bagItemRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.ProductId != null && x.IsPaid)
            .Select(x => x.ProductId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var soldProductIds = paidOrderProductIds
            .Concat(paidBagProductIds)
            .Distinct()
            .ToList();

        if (soldProductIds.Count > 0)
        {
            await _productRepository
                .GetQuery()
                .ApplyIsDeletedFilter()
                .Where(x => x.DropConfigId == dropId &&
                    soldProductIds.Contains(x.Id) &&
                    x.ProductAvailable == true)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ProductAvailable, false),
                    cancellationToken);
        }

        var releasedProductsCount = await _productRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.DropConfigId == dropId &&
                x.ProductAvailable != true &&
                !soldProductIds.Contains(x.Id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ProductAvailable, true),
                cancellationToken);

        _logger.LogInformation(
            "Liberacao de drop concluida. Source={Source}; DropId={DropId}; ProdutosLiberados={ReleasedProductsCount}.",
            source,
            dropId,
            releasedProductsCount);

        return releasedProductsCount;
    }

    public async Task<DropConfigReleaseSweepResult> ReleaseDueDropsAsync(
        string source,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var utcNow = DateTime.UtcNow;
            var activeDrops = await _dropRepository
                .GetQuery()
                .ApplyIsDeletedFilter()
                .Where(x => x.Ativo)
                .OrderBy(x => x.DataLiberacao)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Job de liberacao de drops executado. Source={Source}; UtcNow={UtcNow}; DropsVerificados={DropsChecked}; DropIds={DropIds}.",
                source,
                utcNow,
                activeDrops.Count,
                string.Join(",", activeDrops.Select(x => x.Id)));

            var productsReleased = 0;

            foreach (var drop in activeDrops)
            {
                productsReleased += await ReleaseProductsForDropAsync(
                    drop,
                    utcNow,
                    requireReleaseTime: true,
                    source,
                    cancellationToken);
            }

            return new DropConfigReleaseSweepResult
            {
                DropsChecked = activeDrops.Count,
                ProductsReleased = productsReleased
            };
        }, cancellationToken);
    }
}
