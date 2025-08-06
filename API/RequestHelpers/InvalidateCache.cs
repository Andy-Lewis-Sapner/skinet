using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.RequestHelpers;

[AttributeUsage(AttributeTargets.All)]
public class InvalidateCache(string pattern) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ActionExecutedContext resultContext = await next();

        if (resultContext.Exception == null || resultContext.ExceptionHandled)
        {
            IResponseCacheService cacheService = context.HttpContext.RequestServices
                .GetRequiredService<IResponseCacheService>();

            await cacheService.RemoveCacheByPattern(pattern);
        }
    }
}
