using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentTypeManager.Queries;

public record GetPaymentTypeListDto
{
    public string? Id { get; init; }
    public string? TypeName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetPaymentTypeListProfile : Profile
{
    public GetPaymentTypeListProfile()
    {
        CreateMap<PaymentType, GetPaymentTypeListDto>();
    }
}

public class GetPaymentTypeListResult
{
    public List<GetPaymentTypeListDto>? Data { get; init; }
}

public class GetPaymentTypeListRequest : IRequest<GetPaymentTypeListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPaymentTypeListHandler : IRequestHandler<GetPaymentTypeListRequest, GetPaymentTypeListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentTypeListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentTypeListResult> Handle(GetPaymentTypeListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentType
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetPaymentTypeListDto>>(entities);

        return new GetPaymentTypeListResult
        {
            Data = dtos
        };
    }
}
