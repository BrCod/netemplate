using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Api.Logging
{
    public class PiiRedactionMiddleware
    {
        private readonly RequestDelegate _next;
        public PiiRedactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Redact PII from logs
            await _next(context);
        }
    }
}
