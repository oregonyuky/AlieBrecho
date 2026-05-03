using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record GetCustomerSingleDto
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

public class GetCustomerSingleProfile : Profile
{
    public GetCustomerSingleProfile()
    {
        CreateMap<Customer, GetCustomerSingleDto>();
    }
}

public class GetCustomerSingleResult
{
    public GetCustomerSingleDto? Data { get; init; }
}

public class GetCustomerSingleRequest : IRequest<GetCustomerSingleResult>
{
    public string? Id { get; init; }
}

public class GetCustomerSingleValidator : AbstractValidator<GetCustomerSingleRequest>
{
    public GetCustomerSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetCustomerSingleHandler : IRequestHandler<GetCustomerSingleRequest, GetCustomerSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetCustomerSingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetCustomerSingleResult> Handle(GetCustomerSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Customer
            .AsNoTracking()
            .AsQueryable();

        query = query.Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);
        var dto = _mapper.Map<GetCustomerSingleDto>(entity);

        return new GetCustomerSingleResult
        {
            Data = dto
        };
    }
}
