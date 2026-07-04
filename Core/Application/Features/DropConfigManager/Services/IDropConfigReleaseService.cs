using Domain.Entities;

namespace Application.Features.DropConfigManager.Services;

public class DropConfigReleaseSweepResult
{
    public int DropsChecked { get; init; }
    public int ProductsReleased { get; init; }
}

public interface IDropConfigReleaseService
{
    Task<int> ReleaseProductsForDropAsync(
        DropConfig drop,
        DateTime utcNow,
        bool requireReleaseTime,
        string source,
        CancellationToken cancellationToken);

    Task<DropConfigReleaseSweepResult> ReleaseDueDropsAsync(
        string source,
        CancellationToken cancellationToken);
}
