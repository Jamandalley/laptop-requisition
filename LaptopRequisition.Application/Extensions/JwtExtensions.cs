using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt; // For JwtSecurityTokenHandler
using Microsoft.IdentityModel.Logging; // For IdentityModelEventSource
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols; // For ConfigurationManager
using Microsoft.Extensions.DependencyInjection.Extensions; // For TryAddSingleton

namespace LaptopRequisition.Application.Extensions
{
    public static class JwtExtensions
    {
        public static IServiceCollection AddAuthPlatform(this IServiceCollection services, IConfiguration configuration)
        {
            // ✅ Enable PII logging for debugging (DISABLE IN PRODUCTION!)
           IdentityModelEventSource.ShowPII = 
                configuration.GetValue("Jwt:ShowPII", false);
            
            var issuer = configuration["Jwt:Issuer"];   // e.g. https://sso-app.digitvant.com/
            var audience = configuration["Jwt:Audience"]; // e.g. Profile

            // ✅ Validate required config
            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
            }

            // ✅ Normalize issuer URL (ensure it ends with /)
            if (!issuer.EndsWith("/"))
            {
                issuer += "/";
            }

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = issuer;
                options.Audience = audience;
                options.RequireHttpsMetadata = configuration.GetValue("Jwt:RequireHttpsMetadata", true);

                // ✅ FIX: Correct metadata address construction (ensure proper path)
                var metadataAddress = new Uri(new Uri(issuer), ".well-known/openid-configuration").ToString();
                
                var retriever = new OpenIdConnectConfigurationRetriever();
                var documentRetriever = new HttpDocumentRetriever
                {
                    RequireHttps = configuration.GetValue("Jwt:RequireHttpsMetadata", true)
                };
                
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    retriever,
                    documentRetriever);

                configManager.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
                configManager.RequestRefresh(); // Force initial fetch

                options.ConfigurationManager = configManager;
                options.RefreshOnIssuerKeyNotFound = true;
                
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = configuration.GetValue("Jwt:ValidateIssuer", false),
                    ValidIssuer = issuer,
                    
                    ValidateAudience = configuration.GetValue("Jwt:ValidateAudience", true),
                    ValidAudience = audience,
                    
                    ValidateLifetime = configuration.GetValue("Jwt:ValidateLifeTime", true),
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true, // This should be true for SSO
                    RoleClaimType = "role",

                    // ✅ Fixed: Synchronous resolver (async delegate not supported; use .GetAwaiter().GetResult() for fetch)
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        try
                        {
                            var config = configManager
                                .GetConfigurationAsync(CancellationToken.None)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                            if (config?.SigningKeys == null || !config.SigningKeys.Any())
                            {
                                Console.WriteLine("⚠️ No signing keys found in OIDC configuration");
                                return Enumerable.Empty<SecurityKey>();
                            }

                            // If kid specified, try to match it
                            if (!string.IsNullOrEmpty(kid))
                            {
                                var matchingKey = config.SigningKeys.FirstOrDefault(k => k.KeyId == kid);
                                if (matchingKey != null)
                                {
                                    Console.WriteLine($"✅ Found signing key with kid: {kid}");
                                    return new[] { matchingKey };
                                }
                                Console.WriteLine($"⚠️ No key found for kid: {kid}, returning all keys");
                            }

                            return config.SigningKeys;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ JWKS fetch failed: {ex.Message}");
                            Console.WriteLine($"   Metadata URL: {metadataAddress}");
                            return Enumerable.Empty<SecurityKey>();
                        }
                    }
                };

                // ✅ Enhanced diagnostic hooks
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                        var authHeader = ctx.Request.Headers["Authorization"].ToString();
                        
                        if (string.IsNullOrEmpty(authHeader))
                        {
                            logger?.LogWarning("No Authorization header found for {Path}", ctx.HttpContext.Request.Path);
                        }
                        else if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            logger?.LogWarning("Authorization header doesn't start with 'Bearer' for {Path}", ctx.HttpContext.Request.Path);
                        }
                        
                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        
                        var authHeader = ctx.Request.Headers["Authorization"].ToString();
                        var tokenPrefix = authHeader.Length > 20 ? authHeader.Substring(0, 20) + "..." : authHeader;
                        
                        logger.LogError(ctx.Exception, 
                            "❌ JWT authentication failed for {Path}\n" +
                            "   Token Prefix: {TokenPrefix}\n" +
                            "   Error: {Message}\n" +
                            "   Inner Error: {InnerMessage}\n" +
                            "   Expected Issuer: {Issuer}\n" +
                            "   Expected Audience: {Audience}",
                            ctx.HttpContext.Request.Path,
                            tokenPrefix,
                            ctx.Exception.Message,
                            ctx.Exception.InnerException?.Message ?? "None",
                            issuer,
                            audience);
                        
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                        logger?.LogInformation("✅ JWT validated successfully for subject: {Subject}, user: {User}", 
                            ctx.Principal?.FindFirst("sub")?.Value,
                            ctx.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },

                    OnChallenge = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                        logger?.LogWarning("🔒 JWT Challenge issued for {Path}: {Error} - {ErrorDescription}",
                            ctx.HttpContext.Request.Path,
                            ctx.Error,
                            ctx.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}