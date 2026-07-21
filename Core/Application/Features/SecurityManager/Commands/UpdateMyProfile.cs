using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;



public class UpdateMyProfileResult
{
    public string? Data { get; init; }
}

public class UpdateMyProfileRequest : IRequest<UpdateMyProfileResult>
{
    public string? UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? PostCode { get; init; }
}

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.PostCode)
            .Matches(@"^\d{5}-?\d{3}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PostCode))
            .WithMessage("Informe um CEP valido.");
    }
}

public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileRequest, UpdateMyProfileResult>
{
    private readonly ISecurityService _securityService;

    public UpdateMyProfileHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<UpdateMyProfileResult> Handle(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        await _securityService.UpdateMyProfileAsync(
            request.UserId ?? "",
            request.FirstName ?? "",
            request.LastName ?? "",
            request.CompanyName ?? "",
            request.PostCode ?? "",
            cancellationToken
            );

        return new UpdateMyProfileResult
        {
            Data = "Update MyProfile Success"
        };
    }
}
