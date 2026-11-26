using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Voxa;
using Voxa.Abstractions;
using Voxa.Services;
using Voxa.WebApi;

#pragma warning disable CA1812
var builder = WebApplication.CreateBuilder(args);
#pragma warning restore CA1812

if (!builder.Environment.IsDevelopment())
{
    // Fail at start-up instead of on the first request. Add every secret the API cannot run without.
    string[] requiredSecrets = ["ConnectionString", "Admin:Password", "Codes:Pepper"];
    var missingSecrets = requiredSecrets.Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key])).ToArray();
    if (missingSecrets.Length > 0)
    {
        throw new InvalidOperationException($"Missing required secrets: {string.Join(", ", missingSecrets)}. Configure them through environment variables or a secret provider.");
    }

    var mail = builder.Configuration.GetSection(MailOptions.Section).Get<MailOptions>() ?? new MailOptions();
    if (mail.IsMailgun && (string.IsNullOrWhiteSpace(mail.Mailgun.ApiKey) || string.IsNullOrWhiteSpace(mail.Mailgun.Domain)))
    {
        throw new InvalidOperationException("Mail:Mode is Mailgun, so Mail:Mailgun:ApiKey and Mail:Mailgun:Domain are required.");
    }
}

builder.Services.AddDbContext<VoxaContext>(options => options.UseNpgsql(VoxaContextFactory.Configure(builder.Configuration["ConnectionString"])));
builder.Services.AddVoxaLogic(builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(static options =>
                {
                    options.Cookie.Name = "voxa_board";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.SlidingExpiration = true;
                    // An API answers 401 and 403 instead of redirecting to a login page.
                    options.Events.OnRedirectToLogin = static context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = static context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                });
builder.Services.AddAuthorization();
builder.Services.AddVoxaForwardedHeaders(builder.Configuration);
builder.Services.AddHsts(static options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// The API answers in the language the app asks for; the PWA sends its own language as Accept-Language.
builder.Services.AddRequestLocalization(static options =>
{
    string[] cultures = [.. Languages.Supported];
    options.SetDefaultCulture(Languages.Default).AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
    // Only the header counts: no query string or cookie can change the language behind the app's back.
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});
builder.Services.AddControllers().AddJsonOptions(static options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Voxa", Version = "v1" });
    // string? stays nullable and string does not, so the generated client (Voxa.PWA/src/logic/api.ts) matches C#.
    c.SupportNonNullableReferenceTypes();
});

var app = builder.Build();
var appVersion = AppVersion.Resolve(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voxa v1"));
}
else
{
    // Outside Development the API applies its own migrations on start-up (docs/deployment.md).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VoxaContext>();
    await db.Database.MigrateAsync();
}

// First, so everything after it sees the real client address and scheme behind the reverse proxy.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAppVersionHeader(appVersion);

var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(policy =>
{
    if (app.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(static _ => true);
    }
    else
    {
        policy.WithOrigins(allowedOrigins);
    }

    // Credentials carry the board's sign-in cookie; it is SameSite=Strict, so other sites cannot send it.
    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithExposedHeaders(AppVersion.HeaderName, "Retry-After");
});

// The deploy workflows wait until /api/health reports the version they just published.
app.MapGet("/health", () => Results.Ok(new { status = "ok", version = appVersion })).AllowAnonymous().ExcludeFromDescription();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = appVersion })).AllowAnonymous().ExcludeFromDescription();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();
