using System.Text;
using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

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
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidatorStub>();
        services.AddSingleton<IStripeCheckoutGateway, StripeCheckoutGatewayStub>();

        return services;
    }
}
