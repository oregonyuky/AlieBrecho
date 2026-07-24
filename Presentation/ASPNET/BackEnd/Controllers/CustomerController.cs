using Application.Features.CustomerManager.Commands;
using Application.Features.CustomerManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class CustomerController : BaseApiController
{
    private readonly IHubContext<CustomerNotificationsHub> _customerNotifications;

    public CustomerController(
        ISender sender,
        IHubContext<CustomerNotificationsHub> customerNotifications) : base(sender)
    {
        _customerNotifications = customerNotifications;
    }

    [AllowAnonymous]
    [HttpPost("CreateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<CreateCustomerResult>>> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyCustomerChangedAsync("created", response.Data?.Id, cancellationToken);

        return Ok(new ApiSuccessResult<CreateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCustomerAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult<ApiSuccessResult<LoginCustomerResult>>> LoginAsync(
        LoginCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<LoginCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(LoginAsync)}",
            Content = response
        });
    }

    [Authorize(Policy = "UserOrCustomer")]
    [HttpPost("UpdateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCustomerResult>>> UpdateCustomerAsync(
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (IsCustomerIdentity() && !IsCurrentCustomer(request.Id))
        {
            return Forbid();
        }

        var response = await _sender.Send(request, cancellationToken);
        await NotifyCustomerChangedAsync("updated", response.Data?.Id, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteCustomer")]
    public async Task<ActionResult<ApiSuccessResult<DeleteCustomerResult>>> DeleteCustomerAsync(
        DeleteCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await NotifyCustomerChangedAsync("deleted", request.Id, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteCustomerAsync)}",
            Content = response
        });
    }

    [Authorize(Policy = "UserOrCustomer")]
    [HttpGet("GetCustomerSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerSingleResult>>> GetCustomerSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id
        )
    {
        if (IsCustomerIdentity() && !IsCurrentCustomer(id))
        {
            return Forbid();
        }

        var request = new GetCustomerSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerList")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerListResult>>> GetCustomerListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetCustomerListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerListAsync)}",
            Content = response
        });
    }

    private Task NotifyCustomerChangedAsync(
        string changeType,
        string? customerId,
        CancellationToken cancellationToken)
    {
        return _customerNotifications.Clients.All.SendAsync(
            "CustomerChanged",
            new { changeType, customerId, changedAt = DateTime.UtcNow },
            cancellationToken);
    }

    private bool IsCustomerIdentity()
    {
        return User.HasClaim("identityType", "Customer") ||
            User.IsInRole("Customer");
    }

    private bool IsCurrentCustomer(string? customerId)
    {
        return !string.IsNullOrWhiteSpace(customerId) &&
            string.Equals(
                customerId,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                StringComparison.OrdinalIgnoreCase);
    }
}
