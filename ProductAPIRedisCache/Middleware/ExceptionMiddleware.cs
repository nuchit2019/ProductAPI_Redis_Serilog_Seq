using Microsoft.Extensions.Logging;
using ProductAPIRedisCache.Common;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ProductAPIRedisCache.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                var message = context.Response.StatusCode == 404
                    ? "Resource not found."
                    : "Internal server error. Please contact support.";

                var response = ApiResponse<string>.Fail(message);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }


    }
}
