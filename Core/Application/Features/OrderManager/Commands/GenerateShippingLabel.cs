using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services;
using Application.Common.Services.MelhorEnvioManager;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Commands;

public class GenerateShippingLabelResult
{
    public string? LabelId { get; init; }
    public string? CartId { get; init; }
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
    private readonly ICommandRepository<Domain.Entities.Order> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMelhorEnvioService _melhorEnvioService;
    private readonly IShippingOriginProvider _shippingOriginProvider;

    public GenerateShippingLabelHandler(
        IQueryContext context,
        ICommandRepository<Domain.Entities.Order> repository,
        IUnitOfWork unitOfWork,
        IMelhorEnvioService melhorEnvioService,
        IShippingOriginProvider shippingOriginProvider)
    {
        _context = context;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _melhorEnvioService = melhorEnvioService;
        _shippingOriginProvider = shippingOriginProvider;
    }

    public async Task<GenerateShippingLabelResult> Handle(GenerateShippingLabelRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new Exception("Pedido nao informado.");
        }

        var order = await _repository.GetQuery()
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
            throw new Exception("O frete so pode ser adicionado ao carrinho para pedidos com status Pago.");
        }

        if (!string.IsNullOrWhiteSpace(order.MelhorEnvioCartId))
        {
            throw new Exception($"Este pedido ja foi adicionado ao carrinho do Melhor Envio. Codigo: {order.MelhorEnvioCartId}");
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

        var serviceId = request.ServiceId ?? await GetAvailableServiceIdAsync(
            order,
            company,
            originPostCode,
            cancellationToken);

        var payload = BuildCartPayload(
            order,
            company,
            originPostCode,
            serviceId,
            request.AgencyId);

        var rawResponse = await _melhorEnvioService.AdicionarEtiquetaAoCarrinhoAsync(payload, cancellationToken);
        var labelId = ExtractLabelId(rawResponse);

        if (string.IsNullOrWhiteSpace(labelId))
        {
            return new GenerateShippingLabelResult
            {
                LabelId = null,
                CartId = null,
                RawResponse = rawResponse
            };
        }

        order.MelhorEnvioCartId = labelId;
        order.MelhorEnvioCartAddedAt = DateTime.UtcNow;
        _repository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new GenerateShippingLabelResult
        {
            LabelId = labelId,
            CartId = labelId,
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
        var recipientName = !string.IsNullOrWhiteSpace(order.Customer?.Name)
            ? order.Customer.Name
            : JoinName(shipping.FirstName, shipping.LastName);
        var insuranceValue = box.InsuranceValue ?? order.TotalAmount ?? GetProductsTotal(order);
        var senderState = NormalizeState(company?.State);
        var recipientState = NormalizeState(shipping.State);
        var senderPhone = OnlyDigits(company?.PhoneNumber);
        var recipientPhone = OnlyDigits(shipping.PhoneNumber);
        var recipientDocument = OnlyDigits(order.Customer?.Cpf);

        ValidateRequired(senderPostCode, "CEP de origem nao configurado.");
        ValidateRequired(senderState, "Estado de origem invalido. Informe a UF da empresa, por exemplo SP.");
        ValidateRequired(company?.Name, "Nome da empresa nao informado.");
        ValidateRequired(company?.Street, "Endereco de origem nao informado.");
        ValidateRequired(company?.City, "Cidade de origem nao informada.");
        ValidateRequired(senderPhone, "Telefone de origem nao informado.");
        ValidateRequired(shipping.PostCode, "CEP do destinatario nao informado.");
        ValidateRequired(recipientState, "Estado do destinatario invalido. Informe a UF, por exemplo SP.");
        ValidateRequired(recipientName, "Nome do destinatario nao informado.");
        ValidateRequired(recipientDocument, "CPF do cliente nao informado.");
        ValidateRequired(shipping.Street, "Endereco do destinatario nao informado.");
        ValidateRequired(shipping.Number, "Numero do destinatario nao informado.");
        ValidateRequired(shipping.Neighborhood, "Bairro do destinatario nao informado.");
        ValidateRequired(shipping.City, "Cidade do destinatario nao informada.");
        ValidateRequired(recipientPhone, "Telefone do destinatario nao informado.");
        ValidateRequired(box.Width, "Largura da caixa nao informada.");
        ValidateRequired(box.Length, "Comprimento da caixa nao informado.");
        ValidateRequired(box.Height, "Altura da caixa nao informada.");
        ValidateRequired(box.Weight, "Peso da caixa nao informado.");

        var payload = new Dictionary<string, object?>
        {
            ["service"] = serviceId,
            ["from"] = new
            {
                name = company?.Name,
                email = company?.EmailAddress,
                phone = senderPhone,
                state_register = string.Empty,
                economic_activity_code = string.Empty,
                address = company?.Street,
                complement = string.Empty,
                number = "S/N",
                district = "Centro",
                city = company?.City,
                postal_code = senderPostCode,
                state_abbr = senderState,
                country_id = "BR"
            },
            ["to"] = new
            {
                name = recipientName,
                email = shipping.Email,
                phone = recipientPhone,
                document = recipientDocument,
                state_register = "ISENTO",
                address = shipping.Street,
                complement = shipping.Complement,
                number = shipping.Number,
                district = shipping.Neighborhood,
                city = shipping.City,
                postal_code = OnlyDigits(shipping.PostCode),
                country_id = "BR",
                state_abbr = recipientState
            },
            ["products"] = order.OrderDetails
                .Where(item => !item.IsDeleted)
                .Select(item => new
                {
                    name = item.Product?.Name ?? "Produto",
                    quantity = Math.Max(item.Quantity, 1),
                    unitary_value = item.UnitPrice ?? item.Product?.UnitPrice ?? 0m
                })
                .ToArray(),
            ["volumes"] = new[]
            {
                new
                {
                    height = box.Height ?? 0m,
                    width = box.Width ?? 0m,
                    length = box.Length ?? 0m,
                    weight = box.Weight ?? 0m
                }
            },
            ["options"] = new
            {
                platform = "AlieBrecho",
                reminder = $"Pedido {order.Id}",
                insurance_value = insuranceValue,
                receipt = false,
                own_hand = false,
                reverse = false,
                non_commercial = true
            }
        };

        if (agencyId.HasValue)
        {
            payload["agency"] = agencyId.Value;
        }

        return payload;
    }

    private async Task<int> GetAvailableServiceIdAsync(
        Domain.Entities.Order order,
        Domain.Entities.Company? company,
        string? originPostCode,
        CancellationToken cancellationToken)
    {
        var shipping = order.ShippingDetail!;
        var box = order.ShippingBox!;
        var senderPostCode = OnlyDigits(company?.ZipCode) ?? OnlyDigits(originPostCode);
        var destinationPostCode = OnlyDigits(shipping.PostCode);

        ValidateRequired(senderPostCode, "CEP de origem nao configurado.");
        ValidateRequired(destinationPostCode, "CEP do destinatario nao informado.");
        ValidateRequired(box.Width, "Largura da caixa nao informada.");
        ValidateRequired(box.Length, "Comprimento da caixa nao informado.");
        ValidateRequired(box.Height, "Altura da caixa nao informada.");
        ValidateRequired(box.Weight, "Peso da caixa nao informado.");

        var calculatePayload = new
        {
            from = new
            {
                postal_code = senderPostCode
            },
            to = new
            {
                postal_code = destinationPostCode
            },
            products = new[]
            {
                new
                {
                    id = box.Id,
                    width = box.Width ?? 0m,
                    height = box.Height ?? 0m,
                    length = box.Length ?? 0m,
                    weight = box.Weight ?? 0m,
                    insurance_value = box.InsuranceValue ?? order.TotalAmount ?? GetProductsTotal(order),
                    quantity = 1
                }
            },
            options = new
            {
                receipt = false,
                own_hand = false
            }
        };

        var rawResponse = await _melhorEnvioService.CalcularFreteAsync(calculatePayload);
        return ExtractAvailableServiceId(rawResponse);
    }

    private static int ExtractAvailableServiceId(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            throw new Exception("Melhor Envio nao retornou opcoes de frete para este trecho.");
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("Melhor Envio retornou uma resposta invalida ao calcular o frete.");
            }

            var services = document.RootElement
                .EnumerateArray()
                .Select(item => new
                {
                    Id = TryGetInt(item, "id"),
                    Price = GetDecimal(item, "custom_price") ?? GetDecimal(item, "price"),
                    HasError = item.TryGetProperty("error", out var error) &&
                        error.ValueKind != JsonValueKind.Null &&
                        !string.IsNullOrWhiteSpace(error.ToString())
                })
                .Where(item => item.Id != null && item.Price != null && item.Price > 0m && !item.HasError)
                .OrderBy(item => item.Price)
                .FirstOrDefault();

            if (services?.Id == null)
            {
                throw new Exception("Nenhuma transportadora atende este trecho para a caixa e CEP informados.");
            }

            return services.Id.Value;
        }
        catch (JsonException)
        {
            throw new Exception("Melhor Envio retornou uma resposta invalida ao calcular o frete.");
        }
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

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => number,
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

        var normalized = RemoveDiacritics(value)
            .Trim()
            .Replace(".", string.Empty)
            .ToUpper(CultureInfo.InvariantCulture);

        if (BrazilianStates.Contains(normalized))
        {
            return normalized;
        }

        return BrazilianStateNames.TryGetValue(normalized, out var stateAbbr)
            ? stateAbbr
            : null;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    private static readonly HashSet<string> BrazilianStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    private static readonly Dictionary<string, string> BrazilianStateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACRE"] = "AC",
        ["ALAGOAS"] = "AL",
        ["AMAPA"] = "AP",
        ["AMAZONAS"] = "AM",
        ["BAHIA"] = "BA",
        ["CEARA"] = "CE",
        ["DISTRITO FEDERAL"] = "DF",
        ["ESPIRITO SANTO"] = "ES",
        ["GOIAS"] = "GO",
        ["MARANHAO"] = "MA",
        ["MATO GROSSO"] = "MT",
        ["MATO GROSSO DO SUL"] = "MS",
        ["MINAS GERAIS"] = "MG",
        ["PARA"] = "PA",
        ["PARAIBA"] = "PB",
        ["PARANA"] = "PR",
        ["PERNAMBUCO"] = "PE",
        ["PIAUI"] = "PI",
        ["RIO DE JANEIRO"] = "RJ",
        ["RIO GRANDE DO NORTE"] = "RN",
        ["RIO GRANDE DO SUL"] = "RS",
        ["RONDONIA"] = "RO",
        ["RORAIMA"] = "RR",
        ["SANTA CATARINA"] = "SC",
        ["SAO PAULO"] = "SP",
        ["SERGIPE"] = "SE",
        ["TOCANTINS"] = "TO"
    };
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

public class MarkShippingCartResult
{
    public string? CartId { get; init; }
}

public class MarkShippingCartRequest : IRequest<MarkShippingCartResult>
{
    public string? OrderId { get; init; }
    public string? CartId { get; init; }
}

public class MarkShippingCartHandler : IRequestHandler<MarkShippingCartRequest, MarkShippingCartResult>
{
    private readonly ICommandRepository<Domain.Entities.Order> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkShippingCartHandler(
        ICommandRepository<Domain.Entities.Order> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkShippingCartResult> Handle(MarkShippingCartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new Exception("Pedido nao informado.");
        }

        if (string.IsNullOrWhiteSpace(request.CartId))
        {
            throw new Exception("Codigo do carrinho nao informado.");
        }

        var order = await _repository.GetAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new Exception($"Pedido nao encontrado: {request.OrderId}");
        }

        if (!string.IsNullOrWhiteSpace(order.MelhorEnvioCartId))
        {
            throw new Exception($"Este pedido ja foi marcado como adicionado ao carrinho. Codigo: {order.MelhorEnvioCartId}");
        }

        order.MelhorEnvioCartId = request.CartId.Trim();
        order.MelhorEnvioCartAddedAt = DateTime.UtcNow;

        _repository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new MarkShippingCartResult
        {
            CartId = order.MelhorEnvioCartId
        };
    }
}

public class BuyShippingCartResult
{
    public string? CartId { get; init; }
    public string? RawResponse { get; init; }
}

public class BuyShippingCartRequest : IRequest<BuyShippingCartResult>
{
    public string? OrderId { get; init; }
}

public class BuyShippingCartHandler : IRequestHandler<BuyShippingCartRequest, BuyShippingCartResult>
{
    private readonly ICommandRepository<Domain.Entities.Order> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMelhorEnvioService _melhorEnvioService;

    public BuyShippingCartHandler(
        ICommandRepository<Domain.Entities.Order> repository,
        IUnitOfWork unitOfWork,
        IMelhorEnvioService melhorEnvioService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _melhorEnvioService = melhorEnvioService;
    }

    public async Task<BuyShippingCartResult> Handle(BuyShippingCartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new Exception("Pedido nao informado.");
        }

        var order = await _repository.GetAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new Exception($"Pedido nao encontrado: {request.OrderId}");
        }

        if (string.IsNullOrWhiteSpace(order.MelhorEnvioCartId))
        {
            throw new Exception("Este pedido ainda nao foi adicionado ao carrinho do Melhor Envio.");
        }

        if (order.MelhorEnvioCheckoutAt != null)
        {
            throw new Exception("Este frete ja foi comprado no Melhor Envio.");
        }

        var payload = new
        {
            orders = new[] { order.MelhorEnvioCartId }
        };

        var rawResponse = await _melhorEnvioService.ComprarFretesAsync(payload, cancellationToken);

        order.MelhorEnvioCheckoutAt = DateTime.UtcNow;
        _repository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new BuyShippingCartResult
        {
            CartId = order.MelhorEnvioCartId,
            RawResponse = rawResponse
        };
    }
}

public class GeneratePurchasedShippingLabelResult
{
    public string? CartId { get; init; }
    public string? RawResponse { get; init; }
}

public class GeneratePurchasedShippingLabelRequest : IRequest<GeneratePurchasedShippingLabelResult>
{
    public string? OrderId { get; init; }
}

public class GeneratePurchasedShippingLabelHandler : IRequestHandler<GeneratePurchasedShippingLabelRequest, GeneratePurchasedShippingLabelResult>
{
    private readonly ICommandRepository<Domain.Entities.Order> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMelhorEnvioService _melhorEnvioService;

    public GeneratePurchasedShippingLabelHandler(
        ICommandRepository<Domain.Entities.Order> repository,
        IUnitOfWork unitOfWork,
        IMelhorEnvioService melhorEnvioService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _melhorEnvioService = melhorEnvioService;
    }

    public async Task<GeneratePurchasedShippingLabelResult> Handle(
        GeneratePurchasedShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            throw new Exception("Pedido nao informado.");
        }

        var order = await _repository.GetAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new Exception($"Pedido nao encontrado: {request.OrderId}");
        }

        if (string.IsNullOrWhiteSpace(order.MelhorEnvioCartId))
        {
            throw new Exception("Este pedido ainda nao foi adicionado ao carrinho do Melhor Envio.");
        }

        if (order.MelhorEnvioCheckoutAt == null)
        {
            throw new Exception("Este frete ainda nao foi comprado no Melhor Envio.");
        }

        if (order.MelhorEnvioGeneratedAt != null)
        {
            throw new Exception("Esta etiqueta ja foi gerada no Melhor Envio.");
        }

        var payload = new
        {
            orders = new[] { order.MelhorEnvioCartId }
        };

        var rawResponse = await _melhorEnvioService.GerarEtiquetasAsync(payload, cancellationToken);

        order.MelhorEnvioGeneratedAt = DateTime.UtcNow;
        _repository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new GeneratePurchasedShippingLabelResult
        {
            CartId = order.MelhorEnvioCartId,
            RawResponse = rawResponse
        };
    }
}
