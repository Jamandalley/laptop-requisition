using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LaptopRequisition.Infrastructure.Services
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;

        public LoggingHandler(ILogger<LoggingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("➡️ Request: {Method} {Url}", request.Method, request.RequestUri);
            if (request.Content != null)
            {
                var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("📩 Request Body: {RequestBody}", requestBody);
            }

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogInformation(
                "Response Content-Type: {Type}",
                response.Content?.Headers?.ContentType?.MediaType);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Response Body: {Body}",
                responseBody);

            return response;
        }
    }
}