using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LaptopRequisition.Application.Interfaces.SSO;
using LaptopRequisition.Application.DTOs.SSO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LaptopRequisition.Application.Configurations;

namespace LaptopRequisition.Infrastructure.Services
{
    public class SsoAuthHeaderHandler : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private static string? _cachedToken;
        private static DateTime _tokenExpiration;

        public SsoAuthHeaderHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedToken;
            }

            using var scope = _serviceProvider.CreateScope();
            var ssoClient = scope.ServiceProvider.GetRequiredService<ISsoClient>();
            var settings = scope.ServiceProvider.GetRequiredService<IOptions<OtpApiSettings>>().Value;

            var response = await ssoClient.GetClientCredentialsToken(new SsoClientCredentialsTokenRequestDto
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret
            });

            if (response != null && !string.IsNullOrEmpty(response.AccessToken))
            {
                _cachedToken = response.AccessToken;
                _tokenExpiration = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 60); // Refresh 1 min early
                return _cachedToken;
            }

            return string.Empty;
        }
    }
}
