using System.Text;
using System.Text.Json.Serialization;
using ZoiNailAcademy.Application;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Host;
using ZoiNailAcademy.Host.Authentication;
using ZoiNailAcademy.Host.Authorization;
using ZoiNailAcademy.Host.Conventions;
using ZoiNailAcademy.Host.Middleware;
using ZoiNailAcademy.Host.Networking;
using ZoiNailAcademy.Host.RateLimiting;
using ZoiNailAcademy.Host.Services;
using ZoiNailAcademy.Infrastructure;
using ZoiNailAcademy.Infrastructure.Authentication;
using ZoiNailAcademy.Infrastructure.Data;
using ZoiNailAcademy.Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// "*" makes Kestrel bind dual-stack IPv6 (Railway's private network, Next.js → API, is IPv6)
// and fall back to IPv4 on hosts without IPv6. A literal [::] would crash on those hosts.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));
builder.Services.AddSingleton<RefreshTokenCookie>();

var forwardedHeadersSettings = builder.Configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
    ?? new ForwardedHeadersSettings();
builder.Services.Configure<ForwardedHeadersOptions>(forwardedHeadersSettings.Apply);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("La configuración 'Jwt' es obligatoria.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as issued ("sub", "role") instead of mapping them to the long
        // ClaimTypes URIs, so RequireRole reads the "role" claim written by JwtTokenGenerator.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = JwtTokenGenerator.RoleClaimType
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblemAsync(
                    context.Response, StatusCodes.Status401Unauthorized, "Unauthorized",
                    "Se requiere un token de autenticación válido.");
            },
            OnForbidden = context => WriteProblemAsync(
                context.Response, StatusCodes.Status403Forbidden, "Forbidden",
                "No tienes permisos para realizar esta acción.")
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    AuthorizationPolicies.AddPolicies(options);
});

var rateLimitSection = builder.Configuration.GetSection(RateLimitingOptions.SectionName);
var rateLimitSettings = rateLimitSection.Get<RateLimitingOptions>() ?? new RateLimitingOptions();
builder.Services.Configure<RateLimitingOptions>(rateLimitSection);
builder.Services.AddRateLimiter(options => RateLimitPolicies.AddPolicies(options, rateLimitSettings));

// In production the browser talks only to Next.js (same origin through the /api rewrite), so
// CORS is needed only when the front runs on its own dev server.
if (builder.Environment.IsDevelopment())
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}

builder.Services.AddControllers(options => options.Conventions.Add(new ApiRoutePrefixConvention()))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
    });
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();
    await seeder.SeedAsync(app.Environment.IsProduction());

    await scope.ServiceProvider.GetRequiredService<CursoSeeder>().SeedAsync();
}

if (forwardedHeadersSettings.TrustsAnyProxy)
{
    app.Logger.LogWarning(
        "ForwardedHeaders:KnownNetworks está vacío: se confía en cualquier proxy (ForwardLimit={ForwardLimit}). " +
        "Solo es aceptable mientras la API no tenga dominio público.",
        forwardedHeadersSettings.ForwardLimit);
}

// Order matters: ForwardedHeaders → ExceptionHandling → RateLimiter → Authentication → Authorization.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

var enableSwagger = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Swagger:EnableInProduction");
if (enableSwagger)
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ZoiNailAcademy API v1");
    });
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static Task WriteProblemAsync(HttpResponse response, int statusCode, string title, string detail)
{
    response.StatusCode = statusCode;
    var problemDetails = new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
    return response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
}

public partial class Program { }
