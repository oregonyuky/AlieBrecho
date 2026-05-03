using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record GetCustomerListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Cpf { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Website { get; init; }
    public string? Instagram { get; init; }
    public string? TwitterX { get; init; }
    public string? TikTok { get; init; }
    public string? CustomerStatus { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetCustomerListProfile : Profile
{
    public GetCustomerListProfile()
    {
        CreateMap<Customer, GetCustomerListDto>();
    }
}

public class GetCustomerListResult
{
    public List<GetCustomerListDto>? Data { get; init; }
}

public class GetCustomerListRequest : IRequest<GetCustomerListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetCustomerListHandler : IRequestHandler<GetCustomerListRequest, GetCustomerListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCustomerListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCustomerListResult> Handle(GetCustomerListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Customer
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetCustomerListDto>>(entities);

        return new GetCustomerListResult
        {
            Data = dtos
        };
    }
}
