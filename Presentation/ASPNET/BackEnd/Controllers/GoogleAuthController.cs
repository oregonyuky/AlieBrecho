using Application.Common.Services.SecurityManager;
using Application.Features.CustomerManager.Queries;
using Application.Features.SecurityManager.Queries;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class GoogleAuthController(
    IGoogleAuthenticationService googleAuthentication) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("user/google")]
    public async Task<ActionResult<ApiSuccessResult<LoginResult>>> LoginUserAsync(
        GoogleCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var login = await googleAuthentication.LoginUserAsync(
            request.Credential ?? string.Empty,
            cancellationToken);

        return Ok(new ApiSuccessResult<LoginResult>
        {
            Code = StatusCodes.Status200OK,
            Message = "Google user login successful.",
            Content = new LoginResult { Data = login }
        });
    }

    [AllowAnonymous]
    [HttpPost("customer/google")]
    public async Task<ActionResult<ApiSuccessResult<LoginCustomerResult>>> LoginCustomerAsync(
        GoogleCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var login = await googleAuthentication.LoginCustomerAsync(
            request.Credential ?? string.Empty,
            cancellationToken);

        return Ok(new ApiSuccessResult<LoginCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = "Google customer login successful.",
            Content = new LoginCustomerResult { Data = login }
        });
    }
}

public sealed record GoogleCredentialRequest
{
    public string? Credential { get; init; }
}
