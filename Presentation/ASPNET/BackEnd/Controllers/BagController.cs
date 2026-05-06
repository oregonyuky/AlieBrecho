using Application.Features.BagManager.Commands;
using Application.Features.BagManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BagController : BaseApiController
{
    public BagController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("GetBagList")]
    public async Task<ActionResult<ApiSuccessResult<GetBagListResult>>> GetBagListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetBagListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBagListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBagListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBagSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetBagSingleResult>>> GetBagSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id)
    {
        var request = new GetBagSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBagSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBagSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBag")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBagResult>>> UpdateBagAsync(
        UpdateBagRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBagResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBagAsync)}",
            Content = response
        });
    }
}
