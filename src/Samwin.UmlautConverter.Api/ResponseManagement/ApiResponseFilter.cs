using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    public class ApiResponseFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var response = context.HttpContext.Response;

            // Skip SSE
            if (context.HttpContext.IsSSEEndPoint())
            {
                response.OnStarting(() =>
                {
                    response.Headers.ContentType = "text/event-stream; charset=utf-8";
                    response.Headers.CacheControl = "no-cache";
                    return Task.CompletedTask;
                });
                await next();
                return;
            }

            if (context.Result is ObjectResult objectResult)
            {
                var apiResponse = objectResult.Value != null
                                ? ApiResponseFactory.Success<object>(objectResult.Value, context.HttpContext)
                                : ApiResponseFactory.Failure<object>("Value is null", context.HttpContext);

                context.Result = new ObjectResult(apiResponse)
                {
                    StatusCode = objectResult.StatusCode
                };
            }

            await next();
        }
    }
}