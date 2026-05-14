using System.Globalization;
using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Services;
using Application.Common.Services.MelhorEnvioManager;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Commands;

public class GenerateShippingLabelResult
{
    public string? LabelId { get; init; }
    public string? RawResponse { get; init; }
}

public class GenerateShippingLabelRequest : IRequest<GenerateShippingLabelResult>
{
    public string? OrderId { get; init; }
    public int? ServiceId { get; init; }
    public int? AgencyId { get; init; }
}

public class GenerateShippingLabelHandler : IRequestHandler<GenerateShippingLabelRequest, GenerateShippingLabelResult>
{
    private readonly IQueryContext _context;
    private readonly IMelhorEnvioService _melhorEnvioService;
    private readonly IShippingOriginProvider _shippingOriginProvider;

    public GenerateShippingLabelHandler(
        IQueryContext context,
        IMelhorEnvioService melhorEnvioService,
        IShippingOriginProvider shippingOriginProvider)
    {
        _context = context;
        _melhorEnvioService = melhorEnvioService;
        _shippingOriginProvider = shippingOriginProvider;
    }

    public async Task<GenerateShippingLabelResult> Handle(GenerateShippingLabelRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new Exception("Pedido nao informado.");
        }

        var order = await _context.Order
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.ShippingDetail)
            .Include(x => x.ShippingBox)
            .Include(x => x.OrderDetails.Where(item => !item.IsDeleted))
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new Exception($"Pedido nao encontrado: {request.OrderId}");
        }

        if (order.Status != OrderStatus.Paid)
        {
            throw new Exception("A etiqueta so pode ser gerada para pedidos com status Pago.");
        }

        if (order.ShippingDetail == null)
        {
            throw new Exception("O pedido nao possui dados de envio.");
        }

        if (order.ShippingBox == null)
        {
            throw new Exception("O pedido nao possui caixa de envio.");
        }

        var originPostCode = _shippingOriginProvider.GetOriginPostCode();
        var company = await _context.Company
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted, cancellationToken);

        var payload = BuildCartPayload(
            order,
            company,
            originPostCode,
            request.ServiceId ?? 1,
            request.AgencyId);

        var rawResponse = await _melhorEnvioService.AdicionarEtiquetaAoCarrinhoAsync(payload, cancellationToken);

        return new GenerateShippingLabelResult
        {
            LabelId = ExtractLabelId(rawResponse),
            RawResponse = rawResponse
        };
    }

    private static object BuildCartPayload(
        Domain.Entities.Order order,
        Domain.Entities.Company? company,
        string? originPostCode,
        int serviceId,
        int? agencyId)
    {
        var shipping = order.ShippingDetail!;
        var box = order.ShippingBox!;
        var senderPostCode = OnlyDigits(company?.ZipCode) ?? OnlyDigits(originPostCode);
        var recipientName = JoinName(shipping.FirstName, shipping.LastName);
        var insuranceValue = box.InsuranceValue ?? order.TotalAmount ?? GetProductsTotal(order);

        ValidateRequired(senderPostCode, "CEP de origem nao configurado.");
        ValidateRequired(shipping.PostCode, "CEP do destinatario nao informado.");
        ValidateRequired(recipientName, "Nome do destinatario nao informado.");
        ValidateRequired(box.Width, "Largura da caixa nao informada.");
        ValidateRequired(box.Length, "Comprimento da caixa nao informado.");
        ValidateRequired(box.Height, "Altura da caixa nao informada.");
        ValidateRequired(box.Weight, "Peso da caixa nao informado.");

        return new
        {
            service = serviceId,
            agency = agencyId,
            from = new
            {
                name = company?.Name ?? "AlieBrecho",
                phone = OnlyDigits(company?.PhoneNumber),
                email = company?.EmailAddress,
                address = company?.Street,
                complement = string.Empty,
                number = "0",
                district = string.Empty,
                city = company?.City,
                state_abbr = NormalizeState(company?.State),
                postal_code = senderPostCode,
                country_id = "BR"
            },
            to = new
            {
                name = recipientName,
                phone = OnlyDigits(shipping.PhoneNumber),
                email = shipping.Email,
                address = shipping.Street,
                complement = shipping.Complement,
                number = shipping.Number,
                district = shipping.Neighborhood,
                city = shipping.City,
                state_abbr = NormalizeState(shipping.State),
                postal_code = OnlyDigits(shipping.PostCode),
                country_id = "BR"
            },
            products = order.OrderDetails
                .Where(item => !item.IsDeleted)
                .Select(item => new
                {
                    name = item.Product?.Name ?? "Produto",
                    quantity = Math.Max(item.Quantity, 1),
                    unitary_value = item.UnitPrice ?? item.Product?.UnitPrice ?? 0m
                })
                .ToArray(),
            volumes = new[]
            {
                new
                {
                    height = box.Height ?? 0m,
                    width = box.Width ?? 0m,
                    length = box.Length ?? 0m,
                    weight = box.Weight ?? 0m
                }
            },
            options = new
            {
                insurance_value = insuranceValue,
                receipt = false,
                own_hand = false,
                reverse = false,
                non_commercial = true
            }
        };
    }

    private static decimal GetProductsTotal(Domain.Entities.Order order)
    {
        return order.OrderDetails
            .Where(item => !item.IsDeleted)
            .Sum(item => item.TotalPrice ?? ((item.UnitPrice ?? 0m) * item.Quantity));
    }

    private static string? ExtractLabelId(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var first = root.EnumerateArray().FirstOrDefault();
                return TryGetString(first, "id") ?? TryGetString(first, "order_id") ?? TryGetString(first, "protocol");
            }

            return TryGetString(root, "id") ?? TryGetString(root, "order_id") ?? TryGetString(root, "protocol");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static void ValidateRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exception(message);
        }
    }

    private static void ValidateRequired(decimal? value, string message)
    {
        if (value == null || value <= 0m)
        {
            throw new Exception(message);
        }
    }

    private static string JoinName(string? firstName, string? lastName)
    {
        return $"{firstName} {lastName}".Trim();
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

    private static string? NormalizeState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Length > 2
            ? value.Trim()[..2].ToUpper(CultureInfo.InvariantCulture)
            : value.Trim().ToUpper(CultureInfo.InvariantCulture);
    }
}

public class DownloadShippingLabelResult
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
}

public class DownloadShippingLabelRequest : IRequest<DownloadShippingLabelResult>
{
    public string? LabelId { get; init; }
}

public class DownloadShippingLabelHandler : IRequestHandler<DownloadShippingLabelRequest, DownloadShippingLabelResult>
{
    private readonly IMelhorEnvioService _melhorEnvioService;

    public DownloadShippingLabelHandler(IMelhorEnvioService melhorEnvioService)
    {
        _melhorEnvioService = melhorEnvioService;
    }

    public async Task<DownloadShippingLabelResult> Handle(DownloadShippingLabelRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LabelId))
        {
            throw new Exception("Codigo da etiqueta nao informado.");
        }

        var data = await _melhorEnvioService.BaixarEtiquetaPdfAsync(request.LabelId, cancellationToken);

        return new DownloadShippingLabelResult
        {
            Data = data
        };
    }
}
