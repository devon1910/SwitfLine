using Microsoft.AspNetCore.SignalR;

namespace SwiftLine.API.Extensions
{
    public class HubAuthorizationFilter : IHubFilter
    {
        //public async ValueTask<object> InvokeMethodAsync(
        //    HubInvocationContext context,
        //    Func<HubInvocationContext, ValueTask<object>> next)
        //{
        //    // Get the method being called
        //    var method = context.HubMethod;

        //    // Check if the method has the [HubAuthorize] attribute
        //    var hasAuthAttr = method.() != null;

        //    if (hasAuthAttr && !context.Context.User.Identity?.IsAuthenticated == true)
        //    {
        //        throw new HubException("Unauthorized access to a protected hub method.");
        //    }

        //    return await next(context);
        //}
    }

}
