using Application.Features.PaymentTypeManager.Commands;
using Application.Features.PaymentTypeManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PaymentTypeController : BaseApiController
{
    public PaymentTypeController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePaymentType")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentTypeResult>>> CreatePaymentTypeAsync(
        CreatePaymentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePaymentTypeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePaymentTypeAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePaymentType")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePaymentTypeResult>>> UpdatePaymentTypeAsync(
        UpdatePaymentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePaymentTypeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePaymentTypeAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePaymentType")]
    public async Task<ActionResult<ApiSuccessResult<DeletePaymentTypeResult>>> DeletePaymentTypeAsync(
        DeletePaymentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePaymentTypeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePaymentTypeAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentTypeSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTypeSingleResult>>> GetPaymentTypeSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id)
    {
        var request = new GetPaymentTypeSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentTypeSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentTypeSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentTypeList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTypeListResult>>> GetPaymentTypeListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetPaymentTypeListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentTypeListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentTypeListAsync)}",
            Content = response
        });
    }
}
