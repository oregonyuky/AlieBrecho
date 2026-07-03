using Application.Features.DropConfigManager.Commands;
using Application.Features.DropConfigManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/drop-config")]
public class DropConfigController : BaseApiController
{
    public DropConfigController(ISender sender) : base(sender)
    {
    }

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<ActionResult<ApiSuccessResult<GetActiveDropConfigResult>>> GetActiveAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetActiveDropConfigRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<GetActiveDropConfigResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetActiveAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiSuccessResult<GetDropConfigListResult>>> GetListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetDropConfigListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDropConfigListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiSuccessResult<CreateDropConfigResult>>> CreateAsync(
        CreateDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateDropConfigResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiSuccessResult<UpdateDropConfigResult>>> UpdateAsync(
        string id,
        UpdateDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDropConfigRequest
        {
            Id = id,
            Titulo = request.Titulo,
            Subtitulo = request.Subtitulo,
            DataLiberacaoBrasilia = request.DataLiberacaoBrasilia,
            Ativo = request.Ativo
        };
        var response = await _sender.Send(command, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateDropConfigResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiSuccessResult<DeleteDropConfigResult>>> DeleteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new DeleteDropConfigRequest { Id = id }, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteDropConfigResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteAsync)}",
            Content = response
        });
    }
}
