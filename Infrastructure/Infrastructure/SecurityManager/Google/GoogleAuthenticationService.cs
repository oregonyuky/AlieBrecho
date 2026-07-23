using Application.Common.Services.SecurityManager;
using Application.Common.Services;
using Application.Features.CustomerManager.Queries;
using Domain.Entities;
using Google.Apis.Auth;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.NavigationMenu;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using static Domain.Common.Constants;

namespace Infrastructure.SecurityManager.Google;

public interface IGoogleAuthenticationService
{
    Task<LoginResultDto> LoginUserAsync(string credential, CancellationToken cancellationToken);
    Task<LoginCustomerDto> LoginCustomerAsync(string credential, CancellationToken cancellationToken);
}

public sealed class GoogleAuthenticationService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ICustomerTokenService customerTokenService,
    DataContext context,
    IOptions<GoogleAuthenticationOptions> options) : IGoogleAuthenticationService
{
    private const string Provider = "Google";
    private readonly GoogleAuthenticationOptions _options = options.Value;

    public async Task<LoginResultDto> LoginUserAsync(
        string credential,
        CancellationToken cancellationToken)
    {
        var payload = await ValidateAsync(credential, _options.Admin.ClientId);
        var user = await userManager.FindByLoginAsync(Provider, payload.Subject);

        if (user is null)
        {
            user = await userManager.FindByEmailAsync(payload.Email);
            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "Este e-mail Google não está cadastrado como usuário administrativo.");
            }

            var linkResult = await userManager.AddLoginAsync(
                user,
                new UserLoginInfo(Provider, payload.Subject, Provider));
            if (!linkResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(
                    ", ",
                    linkResult.Errors.Select(error => error.Description)));
            }
        }

        if (!string.Equals(user.Email, payload.Email, StringComparison.OrdinalIgnoreCase) ||
            user.IsBlocked == true ||
            user.IsDeleted == true)
        {
            throw new UnauthorizedAccessException("Acesso administrativo não autorizado.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        var accessToken = tokenService.GenerateToken(user, roleClaims);
        var refreshToken = tokenService.GenerateRefreshToken();

        await ReplaceRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        return new LoginResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            MenuNavigation = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode(),
            Roles = roles.ToList(),
            Avatar = user.ProfilePictureName
        };
    }

    public async Task<LoginCustomerDto> LoginCustomerAsync(
        string credential,
        CancellationToken cancellationToken)
    {
        var payload = await ValidateAsync(credential, _options.Customer.ClientId);
        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        var customer = await context.Customer.FirstOrDefaultAsync(
            item => item.GoogleProviderUserId == payload.Subject,
            cancellationToken);

        if (customer is null)
        {
            customer = await context.Customer.FirstOrDefaultAsync(
                item => item.EmailAddress != null &&
                        item.EmailAddress.ToLower() == normalizedEmail,
                cancellationToken);

            if (customer is null)
            {
                customer = new Customer
                {
                    Name = payload.Name ?? payload.GivenName ?? payload.Email,
                    EmailAddress = normalizedEmail,
                    GoogleProviderUserId = payload.Subject,
                    CustomerStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                await context.Customer.AddAsync(customer, cancellationToken);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(customer.GoogleProviderUserId) &&
                    !string.Equals(
                        customer.GoogleProviderUserId,
                        payload.Subject,
                        StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException(
                        "Esta conta de cliente já está vinculada a outra identidade Google.");
                }

                customer.GoogleProviderUserId = payload.Subject;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        if (!string.Equals(customer.EmailAddress, payload.Email, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(customer.CustomerStatus, "Blocked", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Conta de cliente não autorizada.");
        }

        var nameParts = (customer.Name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new LoginCustomerDto
        {
            AccessToken = customerTokenService.GenerateToken(customer),
            RefreshToken = customerTokenService.GenerateRefreshToken(),
            UserId = customer.Id,
            Email = customer.EmailAddress,
            FirstName = nameParts.FirstOrDefault(),
            LastName = nameParts.Length <= 1 ? null : string.Join(' ', nameParts.Skip(1)),
            PictureUrl = payload.Picture
        };
    }

    private async Task ReplaceRefreshTokenAsync(
        string userId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var existingTokens = await context.Token
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        context.Token.RemoveRange(existingTokens);
        await context.Token.AddAsync(new Token
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(TokenConsts.ExpiryInDays),
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = userId
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<GoogleJsonWebSignature.Payload> ValidateAsync(
        string credential,
        string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Google Client ID não configurado.");
        }

        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new UnauthorizedAccessException("Credencial Google ausente.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Credencial Google inválida.");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            payload.EmailVerified != true)
        {
            throw new UnauthorizedAccessException("O Google não confirmou este e-mail.");
        }

        return payload;
    }
}

public static class GoogleAuthenticationDependencyInjection
{
    public static IServiceCollection AddGoogleAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleAuthenticationOptions>(
            configuration.GetSection(GoogleAuthenticationOptions.SectionName));
        services.AddScoped<IGoogleAuthenticationService, GoogleAuthenticationService>();
        return services;
    }
}
