using Application.Features.ContactMessageManager.Commands;
using Application.Features.ContactMessageManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/contact-messages")]
public sealed class ContactMessagesController : BaseApiController
{
    public ContactMessagesController(ISender sender) : base(sender)
    {
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiSuccessResult<CreateContactMessageResult>>> CreateAsync(
        CreateContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateContactMessageResult>
        {
            Code = StatusCodes.Status200OK,
            Message = "Mensagem enviada com sucesso.",
            Content = response
        });
    }

    [Authorize(Roles = "Messages")]
    [HttpGet]
    public async Task<ActionResult<ApiSuccessResult<GetContactMessageListResult>>> GetListAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetContactMessageListRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetContactMessageListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = "Mensagens carregadas com sucesso.",
            Content = response
        });
    }

    [Authorize(Roles = "Messages")]
    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiSuccessResult<MarkContactMessageReadResult>>> MarkReadAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new MarkContactMessageReadRequest { Id = id },
            cancellationToken);

        return Ok(new ApiSuccessResult<MarkContactMessageReadResult>
        {
            Code = StatusCodes.Status200OK,
            Message = "Mensagem marcada como lida.",
            Content = response
        });
    }
}
