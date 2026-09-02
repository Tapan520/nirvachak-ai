using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.ModuleAccess;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Infrastructure.Filters;

public class ModuleAccessMiddleware
{
    private readonly RequestDelegate _next;

    public ModuleAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<AppUser> userManager,
        ModuleAccessService moduleAccessService)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (IsNonModulePath(path))
        {
            await _next(context);
            return;
        }

        var user = await userManager.GetUserAsync(context.User);
        if (user == null || user.Role == UserRole.SuperAdmin)
        {
            await _next(context);
            return;
        }

        var subModuleKey = ModuleAccessCatalog.FindSubModuleKeyByPath(path);
        if (string.IsNullOrWhiteSpace(subModuleKey))
        {
            await _next(context);
            return;
        }

        var allowed = await moduleAccessService.IsEnabledAsync(user.ConstituencyId, subModuleKey);
        if (allowed)
        {
            await _next(context);
            return;
        }

        if (path.StartsWith(Constants.Routes.ApiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message = "This module is disabled for your constituency."
            }));
            return;
        }

        context.Response.Redirect(Constants.Routes.AccessDeniedPath);
    }

    private static bool IsNonModulePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;

        return path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/Survey", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/icons", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/manifest", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/marketing", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/static-demo", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/brochure", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/", StringComparison.OrdinalIgnoreCase);
    }
}
