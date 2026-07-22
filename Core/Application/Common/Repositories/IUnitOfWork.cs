using System.Data;

namespace Application.Common.Repositories;

public interface IUnitOfWork
{
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    void Save();
}
