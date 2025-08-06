using System.Text;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace API.RequestHelpers;

[AttributeUsage(AttributeTargets.All)]
public class CacheAttribute(int timeToLiveSeconds) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        IResponseCacheService cacheService = context.HttpContext.RequestServices
            .GetRequiredService<IResponseCacheService>();

        string cacheKey = GenerateCacheKeyFromRequest(context.HttpContext.Request);
        string? cachedResponse = await cacheService.GetCachedResponseAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResponse))
        {
            ContentResult contentResult = new()
            {
                Content = cachedResponse,
                ContentType = "application/json",
                StatusCode = 200
            };

            context.Result = contentResult;
            return;
        }

        ActionExecutedContext? executedContext = await next();

        if (executedContext.Result is OkObjectResult okObjectResult)
        {
            if (okObjectResult.Value != null)
            {
                await cacheService.CacheResponseAsync(cacheKey, okObjectResult.Value,
                    TimeSpan.FromSeconds(timeToLiveSeconds));
            }
        }
    }

    private static string GenerateCacheKeyFromRequest(HttpRequest request)
    {
        StringBuilder keyBuilder = new();

        keyBuilder.Append($"{request.Path}");
        foreach ((string key, StringValues value) in request.Query.OrderBy(x => x.Key))
            keyBuilder.Append($"|{key}-{value}");

        return keyBuilder.ToString();
    }
}
