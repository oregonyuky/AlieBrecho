namespace Application.Common;

public sealed class ProductUnavailableException : Exception
{
    public ProductUnavailableException(string message = "Este produto acabou de ser reservado por outro cliente.")
        : base(message)
    {
    }

    public ProductUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static bool IsDatabaseConcurrencyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current.Message.Contains("1205", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("23505", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("40001", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("40P01", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
