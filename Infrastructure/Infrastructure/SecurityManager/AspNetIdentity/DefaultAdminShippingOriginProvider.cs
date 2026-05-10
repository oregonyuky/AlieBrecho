using Application.Common.Services;
using Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.SecurityManager.AspNetIdentity;

public class DefaultAdminShippingOriginProvider : IShippingOriginProvider
{
    private readonly IdentitySettings _identitySettings;

    public DefaultAdminShippingOriginProvider(IOptions<IdentitySettings> identitySettings)
    {
        _identitySettings = identitySettings.Value;
    }

    public string? GetOriginPostCode()
    {
        return ConfigurationPlaceholderResolver.Resolve(_identitySettings.DefaultAdmin.PostCode);
    }
}
