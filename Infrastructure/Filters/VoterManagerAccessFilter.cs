using Microsoft.AspNetCore.Identity;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Infrastructure.Filters;

/// <summary>
/// Middleware that restricts users with the VoterManager role to the /Voters section only.
/// Any attempt to access other pages is redirected to /Voters/Index.
/// </summary>
public class VoterManagerAccessMiddleware
{
    private readonly RequestDelegate _next;

    public VoterManagerAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? "";

            // Only restrict page navigation (not API, static files, etc.)
            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/Survey", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user?.Role == UserRole.VoterManager)
                {
                    // Allow /Voters, /VoterSlips, and permitted Operations sub-sections
                    var allowed =
                        path.StartsWith("/Voters", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/VoterSlips", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/Announcements", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/ElectionDay", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/Analytics", StringComparison.OrdinalIgnoreCase);

                    if (!allowed)
                    {
                        context.Response.Redirect("/Voters/Index");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}

