using Application.Common.Repositories;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.DataAccessManager.EFCore.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CommandContext _context;

    public UnitOfWork(CommandContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public void Save()
    {
        _context.SaveChanges();
    }
}
