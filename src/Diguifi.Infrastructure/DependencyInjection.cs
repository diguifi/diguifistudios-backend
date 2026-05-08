using System.Text;
using System.Data.Common;
using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Diguifi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));

        var connectionString = NormalizePostgresConnectionString(configuration.GetConnectionString("Default"));
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure()));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
                };
            });

        services.AddAuthorization();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IBundleService, BundleService>();
        services.AddScoped<IGameNotionPlayerService, GameNotionPlayerService>();
        services.AddSingleton<Stripe.IStripeClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
            return new Stripe.StripeClient(options.SecretKey);
        });
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IStripeCheckoutGateway, StripeCheckoutGateway>();
        services.AddSingleton<IStripeBillingPortalGateway, StripeBillingPortalGateway>();

        return services;
    }

    private static string NormalizePostgresConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A connection string 'ConnectionStrings:Default' deve ser configurada.");
        }

        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        Remap(builder, "Server", "Host");
        Remap(builder, "Data Source", "Host");
        Remap(builder, "User ID", "Username");
        Remap(builder, "User", "Username");

        if (!builder.ContainsKey("Ssl Mode") && !builder.ContainsKey("SSL Mode") && !builder.ContainsKey("sslmode"))
        {
            builder["Ssl Mode"] = "Require";
        }

        return builder.ConnectionString;
    }

    private static void Remap(DbConnectionStringBuilder builder, string sourceKey, string targetKey)
    {
        if (!builder.TryGetValue(sourceKey, out var value) || builder.ContainsKey(targetKey))
        {
            return;
        }

        builder.Remove(sourceKey);
        builder[targetKey] = value;
    }
}
