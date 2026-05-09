using Application.Features.ShippingBoxManager.Commands;
using Application.Features.ShippingBoxManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ShippingBoxController : BaseApiController
{
    public ShippingBoxController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateShippingBox")]
    public async Task<ActionResult<ApiSuccessResult<CreateShippingBoxResult>>> CreateShippingBoxAsync(
        CreateShippingBoxRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateShippingBoxResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateShippingBoxAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateShippingBox")]
    public async Task<ActionResult<ApiSuccessResult<UpdateShippingBoxResult>>> UpdateShippingBoxAsync(
        UpdateShippingBoxRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateShippingBoxResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateShippingBoxAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteShippingBox")]
    public async Task<ActionResult<ApiSuccessResult<DeleteShippingBoxResult>>> DeleteShippingBoxAsync(
        DeleteShippingBoxRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteShippingBoxResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteShippingBoxAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetShippingBoxSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetShippingBoxSingleResult>>> GetShippingBoxSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id
    )
    {
        var request = new GetShippingBoxSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetShippingBoxSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetShippingBoxSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetShippingBoxList")]
    public async Task<ActionResult<ApiSuccessResult<GetShippingBoxListResult>>> GetShippingBoxListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
    )
    {
        var request = new GetShippingBoxListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetShippingBoxListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetShippingBoxListAsync)}",
            Content = response
        });
    }
}