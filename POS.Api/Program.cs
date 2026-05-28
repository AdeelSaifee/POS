using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Auth;
using POS.Api.Application.Categories;
using POS.Api.Application.Health;
using POS.Api.Application.Locations;
using POS.Api.Application.Sync;
using POS.Api.Application.Tenant;
using POS.Api.Application.UnitsOfMeasure;
using POS.Api.Configuration;
using POS.Api.Data;
using POS.Api.Services.Tenant;
using POS.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
const string userOrAdminPolicy = "UserOrAdmin";
const string posDevicePolicy = "PosDevice";
const string systemScopePolicy = "SystemScope";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<ICategoryReadService, CategoryReadService>();
builder.Services.AddScoped<IHealthStatusService, HealthStatusService>();
builder.Services.AddScoped<ILocationReadService, LocationReadService>();
builder.Services.AddScoped<ISyncIngestService, SyncIngestService>();
builder.Services.AddScoped<ITenantProfileReadService, TenantProfileReadService>();
builder.Services.AddScoped<IUnitOfMeasureReadService, UnitOfMeasureReadService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantContext, HttpContextCurrentTenantContext>();

var jwtConfiguration = JwtAuthenticationConfigurationGuard.GetRequiredConfiguration(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfiguration.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.SigningKey)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(userOrAdminPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.HasClaim(ApiClaimTypes.ClientType, "user") ||
            context.User.HasClaim(ApiClaimTypes.ClientType, "admin"));
    });

    options.AddPolicy(posDevicePolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ApiClaimTypes.ClientType, "device");
    });

    options.AddPolicy(systemScopePolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ApiClaimTypes.SystemScope, "true");
    });
});

builder.Services.AddDbContext<PosCentralDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = CentralDatabaseConfigurationGuard.GetRequiredConnectionString(configuration);

    options.UseSqlServer(connectionString);
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
