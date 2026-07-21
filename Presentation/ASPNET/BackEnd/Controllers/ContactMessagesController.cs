using Application.Features.ContactMessageManager.Commands;
using Application.Features.ContactMessageManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/contact-messages")]
public sealed class ContactMessagesController : BaseApiController
{
    private readonly IHubContext<MessageNotificationsHub> _messageNotifications;

    public ContactMessagesController(
        ISender sender,
        IHubContext<MessageNotificationsHub> messageNotifications) : base(sender)
    {
        _messageNotifications = messageNotifications;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiSuccessResult<CreateContactMessageResult>>> CreateAsync(
        CreateContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        await _messageNotifications.Clients.All.SendAsync(
            "MessageReceived",
            new
            {
                messageId = response.Id,
                name = request.Name?.Trim(),
                email = request.Email?.Trim(),
                phone = request.Phone?.Trim(),
                subject = request.Subject?.Trim(),
                message = request.Message?.Trim(),
                isRead = false,
                receivedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

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
