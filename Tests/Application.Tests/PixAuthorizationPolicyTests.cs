using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Application.Tests;

public class PixAuthorizationPolicyTests
{
    private const string JwtKey = "alie-brecho-tests-jwt-key-with-at-least-32-characters";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UserOrCustomer_AllowsCustomerTokens_FromBothLoginFlows(
        bool isGoogleCustomer)
    {
        await using var provider = BuildServices();
        var tokenService = new TokenService(Options.Create(CreateTokenSettings()));
        var customer = new Customer
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Cliente Teste",
            EmailAddress = "cliente@teste.com",
            GoogleProviderUserId = isGoogleCustomer ? "google-user-id" : null
        };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            tokenService.GenerateToken(customer));
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(jwt.Claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role));
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            policyName: "UserOrCustomer");

        Assert.True(result.Succeeded);
        Assert.Equal(customer.Id, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.True(principal.IsInRole("Customer"));
        Assert.True(principal.HasClaim("identityType", "Customer"));
    }

    [Fact]
    public async Task UserOrCustomer_AllowsLegacyCustomerToken_WithRoleOnly()
    {
        await using var provider = BuildServices();
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, "customer-id"),
            new Claim(ClaimTypes.Role, "Customer"));
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            policyName: "UserOrCustomer");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UserOrCustomer_RejectsAnonymousRequest()
    {
        await using var provider = BuildServices();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null,
            policyName: "UserOrCustomer");

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider BuildServices()
    {
        var settings = CreateTokenSettings();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = settings.Key,
                ["Jwt:Issuer"] = settings.Issuer,
                ["Jwt:Audience"] = settings.Audience,
                ["Jwt:ExpireInMinute"] = settings.ExpireInMinute.ToString(),
                ["Jwt:ClockSkewInMinute"] = settings.ClockSkewInMinute.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterToken(configuration);
        return services.BuildServiceProvider();
    }

    private static TokenSettings CreateTokenSettings() => new()
    {
        Key = JwtKey,
        Issuer = "alie-brecho-tests",
        Audience = "alie-brecho-tests",
        ExpireInMinute = 30,
        ClockSkewInMinute = 0
    };

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role));
}
