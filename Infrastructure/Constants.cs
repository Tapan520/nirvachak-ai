namespace Nirvachak_AI.Infrastructure;

public static class Constants
{
    public static class Routes
    {
        public const string ApiPrefix        = "/api";
        public const string LoginPath        = "/Account/Login";
        public const string LogoutPath       = "/Account/Logout";
        public const string AccessDeniedPath = "/Account/AccessDenied";
    }

    public static class Policy
    {
        public const string CorsAllowAll = "AllowAll";
    }

    public static class Jwt
    {
        public const string ConfigKey      = "Jwt:Key";
        public const string IssuerKey      = "Jwt:Issuer";
        public const string AudienceKey    = "Jwt:Audience";
    }

    public static class Cors
    {
        public const string AllowedOriginsKey = "Cors:AllowedOrigins";
    }
}
