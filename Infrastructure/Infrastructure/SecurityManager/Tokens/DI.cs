using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Application.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.SecurityManager.Tokens;

public static class DI
{
    public static IServiceCollection RegisterToken(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSectionName = "Jwt";
        services.Configure<TokenSettings>(configuration.GetSection(jwtSectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var tokenSettings = configuration.GetSection(jwtSectionName).Get<TokenSettings>();

            if (tokenSettings?.Key.Length < 32)
            {
                throw new Exception("JWT key should be minimal 32 character");
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(tokenSettings?.ClockSkewInMinute ?? 5),
                ValidIssuer = tokenSettings?.Issuer,
                ValidAudience = tokenSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings?.Key ?? throw new ArgumentNullException("JWT Key is empty")))
            };

            options.Events = new JwtBearerEvents
            {
                // Prioritizing HttpOnly cookie before checking Authorization header
                OnMessageReceived = context =>
                {
                    var accessToken = context.HttpContext.Request.Cookies["accessToken"];
                    var requestPath = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    else if (requestPath.StartsWithSegments("/hubs/orders") &&
                        context.Request.Query.TryGetValue("access_token", out var hubAccessToken) &&
                        !string.IsNullOrWhiteSpace(hubAccessToken))
                    {
                        context.Token = hubAccessToken;
                    }
                    else
                    {
                        var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
                        {
                            context.Token = authorizationHeader.Substring("Bearer ".Length).Trim();
                        }
                    }

                    return Task.CompletedTask;
                },

                // Custom handling for expired tokens
                OnChallenge = context =>
                {
                    if (context.AuthenticateFailure is SecurityTokenExpiredException)
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 498; // Custom status code for expired token
                        context.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new
                        {
                            code = 498,
                            message = "Token has expired.",
                            error = new
                            {
                                @ref = "https://datatracker.ietf.org/doc/html/rfc9110",
                                exceptionType = "SecurityTokenExpiredException",
                                innerException = "SecurityTokenExpiredException",
                                source = "",
                                stackTrace = ""
                            }
                        });

                        return context.Response.WriteAsync(result);
                    }

                    return Task.CompletedTask;
                }



            };

        });

        services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<ICustomerTokenService, TokenService>();
        services.AddScoped<TokenSettings>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("UserOnly", policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("identityType", "User"));
            options.AddPolicy("CustomerOnly", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.HasClaim("identityType", "Customer") ||
                    context.User.IsInRole("Customer")));
            options.AddPolicy("UserOrCustomer", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.HasClaim("identityType", "User") ||
                    context.User.HasClaim("identityType", "Customer") ||
                    context.User.IsInRole("Customer")));
            options.DefaultPolicy = options.GetPolicy("UserOnly")
                ?? new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });

        return services;
    }
}

