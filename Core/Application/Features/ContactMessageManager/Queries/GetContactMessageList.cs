using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ContactMessageManager.Queries;

public sealed record ContactMessageAdminDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Subject { get; init; }
    public string? Message { get; init; }
    public bool IsRead { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
}

public sealed class GetContactMessageListResult
{
    public List<ContactMessageAdminDto> Data { get; init; } = [];
    public int UnreadCount { get; init; }
}

public sealed class GetContactMessageListRequest : IRequest<GetContactMessageListResult>
{
}

public sealed class GetContactMessageListHandler
    : IRequestHandler<GetContactMessageListRequest, GetContactMessageListResult>
{
    private readonly IQueryContext _context;

    public GetContactMessageListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetContactMessageListResult> Handle(
        GetContactMessageListRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _context.ContactMessage
            .AsNoTracking()
            .ApplyIsDeletedFilter()
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.ReceivedAtUtc)
            .Select(x => new ContactMessageAdminDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Subject = x.Subject,
                Message = x.Message,
                IsRead = x.IsRead,
                ReceivedAtUtc = x.ReceivedAtUtc,
                ReadAtUtc = x.ReadAtUtc
            })
            .ToListAsync(cancellationToken);

        return new GetContactMessageListResult
        {
            Data = data,
            UnreadCount = data.Count(x => !x.IsRead)
        };
    }
}
