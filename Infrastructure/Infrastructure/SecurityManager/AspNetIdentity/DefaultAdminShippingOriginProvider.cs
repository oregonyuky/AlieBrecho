using Application.Common.Services;
using Infrastructure.Common;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.SecurityManager.AspNetIdentity;

public class DefaultAdminShippingOriginProvider : IShippingOriginProvider
{
    private readonly IdentitySettings _identitySettings;
    private readonly DataContext _context;

    public DefaultAdminShippingOriginProvider(IOptions<IdentitySettings> identitySettings, DataContext context)
    {
        _identitySettings = identitySettings.Value;
        _context = context;
    }

    public string? GetOriginPostCode()
    {
        var adminEmail = ConfigurationPlaceholderResolver.Resolve(_identitySettings.DefaultAdmin.Email);
        var profilePostCode = _context.Users
            .AsNoTracking()
            .Where(user => user.Email == adminEmail)
            .Select(user => user.PostCode)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(profilePostCode)
            ? ConfigurationPlaceholderResolver.Resolve(_identitySettings.DefaultAdmin.PostCode)
            : profilePostCode;
    }
}
