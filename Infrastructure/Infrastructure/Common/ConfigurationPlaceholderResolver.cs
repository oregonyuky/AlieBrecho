namespace Infrastructure.Common;

public static class ConfigurationPlaceholderResolver
{
    public static string? Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (value.Length > 2 && value.StartsWith('%') && value.EndsWith('%'))
        {
            var variableName = value[1..^1];
            return Environment.GetEnvironmentVariable(variableName) ?? value;
        }

        return value;
    }
}
