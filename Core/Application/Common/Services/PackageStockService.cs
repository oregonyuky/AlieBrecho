using Domain.Entities;

namespace Application.Common.Services;

public static class PackageStockService
{
    public static bool DeductOnce(Order order, ShippingBox package)
    {
        if (order.ShippingBoxStockDeducted)
        {
            return false;
        }
        if (package.StockQuantity <= 0)
        {
            throw new InvalidOperationException("A embalagem selecionada nao possui estoque para confirmar o pagamento.");
        }

        package.StockQuantity--;
        order.ShippingBoxStockDeducted = true;
        return true;
    }

    public static bool RestoreOnce(Order order, ShippingBox package)
    {
        if (!order.ShippingBoxStockDeducted)
        {
            return false;
        }

        package.StockQuantity++;
        order.ShippingBoxStockDeducted = false;
        return true;
    }
}
