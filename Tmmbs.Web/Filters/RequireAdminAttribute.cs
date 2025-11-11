using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tmmbs.Web.Filters
{
    /// <summary>
    /// Redirects to /Auth/SignIn if not authenticated or not in Admin role.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireAdminAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true && user.IsInRole("Admin"))
            {
                await next();
                return;
            }

            var path = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            var url = "/Auth/SignIn?returnUrl=" + Uri.EscapeDataString(path);
            context.Result = new RedirectResult(url);
        }
    }
}
