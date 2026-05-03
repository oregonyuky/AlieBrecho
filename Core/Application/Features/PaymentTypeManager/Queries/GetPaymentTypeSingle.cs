using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentTypeManager.Queries;

public record GetPaymentTypeSingleDto
{
    public string? Id { get; init; }
    public string? TypeName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetPaymentTypeSingleProfile : Profile
{
    public GetPaymentTypeSingleProfile()
    {
        CreateMap<PaymentType, GetPaymentTypeSingleDto>();
    }
}

public class GetPaymentTypeSingleResult
{
    public GetPaymentTypeSingleDto? Data { get; init; }
}

public class GetPaymentTypeSingleRequest : IRequest<GetPaymentTypeSingleResult>
{
    public string? Id { get; init; }
}

public class GetPaymentTypeSingleValidator : AbstractValidator<GetPaymentTypeSingleRequest>
{
    public GetPaymentTypeSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetPaymentTypeSingleHandler : IRequestHandler<GetPaymentTypeSingleRequest, GetPaymentTypeSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetPaymentTypeSingleHandler(
        IQueryContext context,
        IMapper mapper
    )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetPaymentTypeSingleResult> Handle(GetPaymentTypeSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentType
            .AsNoTracking()
            .AsQueryable();

        query = query.Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);
        var dto = _mapper.Map<GetPaymentTypeSingleDto>(entity);

        return new GetPaymentTypeSingleResult
        {
            Data = dto
        };
    }
}
