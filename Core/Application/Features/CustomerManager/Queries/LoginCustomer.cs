using Application.Common.CQS.Queries;
using Application.Common.Services;
using Application.Features.CustomerManager;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class LoginCustomerResult
{
    public LoginCustomerDto? Data { get; init; }
}

public class LoginCustomerDto
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public List<string> Roles { get; init; } = ["Customer"];
}

public class LoginCustomerRequest : IRequest<LoginCustomerResult>
{
    public string? Email { get; init; }
    public string? Password { get; init; }
}

public class LoginCustomerValidator : AbstractValidator<LoginCustomerRequest>
{
    public LoginCustomerValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCustomerHandler : IRequestHandler<LoginCustomerRequest, LoginCustomerResult>
{
    private readonly IQueryContext _context;
    private readonly ICustomerTokenService _customerTokenService;

    public LoginCustomerHandler(
        IQueryContext context,
        ICustomerTokenService customerTokenService)
    {
        _context = context;
        _customerTokenService = customerTokenService;
    }

    public async Task<LoginCustomerResult> Handle(
        LoginCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLower();
        var customer = await _context
            .Customer
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmailAddress != null && x.EmailAddress.ToLower() == email,
                cancellationToken);

        if (customer is null ||
            string.IsNullOrWhiteSpace(customer.PasswordHash) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            !CustomerPasswordHasher.Verify(request.Password, customer.PasswordHash))
        {
            return new LoginCustomerResult();
        }

        var nameParts = (customer.Name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new LoginCustomerResult
        {
            Data = new LoginCustomerDto
            {
                AccessToken = _customerTokenService.GenerateToken(customer),
                RefreshToken = _customerTokenService.GenerateRefreshToken(),
                UserId = customer.Id,
                Email = customer.EmailAddress,
                FirstName = nameParts.FirstOrDefault(),
                LastName = nameParts.Length <= 1 ? null : string.Join(' ', nameParts.Skip(1))
            }
        };
    }
}
